using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UnoGame
{
    public class GameServer
    {
        private TcpListener listener;
        private Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private Dictionary<string, string> clientNames = new Dictionary<string, string>();
        private List<Player> players = new List<Player>();
        private UnoGameLogic gameLogic;
        private bool gameStarted = false;
        private string hostConnectionId = null;

        public event Action<string> LogEvent;
        public event Action GameStateChanged;
        public event Action<List<string>> PlayerListChanged;
        public event Action<string> SystemMessage;

        public async Task Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            _ = AcceptClients();
            Log($"服务器启动，端口 {port}");
            SystemMessage?.Invoke($"服务器已启动在端口: {port}");
        }

        private async Task AcceptClients()
        {
            while (true)
            {
                TcpClient client = null;
                try
                {
                    client = await listener.AcceptTcpClientAsync();
                }
                catch { break; }
                _ = HandleClient(client);
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            string connectionId = Guid.NewGuid().ToString();
            string playerName = null;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                int len = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (len == 0) return;
                string data = Encoding.UTF8.GetString(buffer, 0, len);
                var msg = JsonConvert.DeserializeObject<Message>(data);

                if (msg.Type != "Join" || gameStarted)
                {
                    client.Close();
                    return;
                }

                playerName = msg.Content;
                lock (clients)
                {
                    clients[connectionId] = client;
                    clientNames[connectionId] = playerName;
                    players.Add(new Player { Name = playerName, ConnectionId = connectionId });
                    if (hostConnectionId == null) hostConnectionId = connectionId;
                }

                Log($"{playerName} 加入，当前 {players.Count}/14 人");

                var idMsg = new Message { Type = "SetMyId", Content = connectionId };
                await SendToClient(client, idMsg);
                BroadcastPlayerList();
                SystemMessage?.Invoke($"{playerName} 加入了游戏");

                while (true)
                {
                    len = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (len == 0) break;
                    data = Encoding.UTF8.GetString(buffer, 0, len);
                    var gameMsg = JsonConvert.DeserializeObject<Message>(data);

                    if (gameMsg.Type == "StartGame" && !gameStarted && connectionId == hostConnectionId)
                    {
                        if (players.Count >= 2)
                            StartGame();
                        else
                            await SendToClient(client, new Message { Type = "Error", Content = "人数不足2人，无法开始" });
                        continue;
                    }
                    if (gameMsg.Type == "RestartGame" && gameStarted && connectionId == hostConnectionId)
                    {
                        RestartGame();
                        continue;
                    }

                    if (gameStarted)
                        _ = HandleGameMessage(client, connectionId, gameMsg);
                }
            }
            catch (Exception ex)
            {
                Log($"客户端错误: {ex.Message}");
            }
            finally
            {
                bool needShutdown = false;
                lock (clients)
                {
                    if (clients.ContainsKey(connectionId))
                    {
                        string leftName = clientNames.ContainsKey(connectionId) ? clientNames[connectionId] : "未知";
                        clients.Remove(connectionId);
                        clientNames.Remove(connectionId);
                        players.RemoveAll(p => p.ConnectionId == connectionId);
                        if (!string.IsNullOrEmpty(leftName) && leftName != "未知")
                        {
                            SystemMessage?.Invoke($"{leftName} 离开了游戏");
                            Log($"{leftName} 断开连接");
                        }
                        BroadcastPlayerList();
                        if (clients.Count == 0 && players.Count == 0)
                            needShutdown = true;
                    }
                }
                if (needShutdown)
                    await ShutdownServer();
                client.Close();
            }
        }

        private async Task ShutdownServer()
        {
            Broadcast(new Message { Type = "ServerShutdown", Content = "" });
            await Task.Delay(100);
            Stop();
        }

        private void StartGame()
        {
            try
            {
                if (players.Count < 2) return;
                gameStarted = true;
                gameLogic = new UnoGameLogic(players);
                BroadcastGameState();
                SystemMessage?.Invoke("游戏已开始");
                Log("游戏开始！");
            }
            catch (Exception ex)
            {
                Log($"启动游戏失败: {ex.Message}");
                gameStarted = false;
            }
        }

        private void RestartGame()
        {
            try
            {
                foreach (var p in players)
                {
                    p.HandCards.Clear();
                    p.HasCalledUno = false;
                }
                gameLogic = new UnoGameLogic(players);
                BroadcastGameState();
                SystemMessage?.Invoke("游戏已重新开始");
                Log("游戏已重新开始");
            }
            catch (Exception ex)
            {
                Log($"重置游戏失败: {ex.Message}");
            }
        }

        private async Task HandleGameMessage(TcpClient client, string clientId, Message msg)
        {
            var player = players.FirstOrDefault(p => p.ConnectionId == clientId);
            if (player == null) return;

            // 罚牌检查（允许出+2/+4转移）
            if (gameLogic.DrawStack > 0 && (msg.Type == "PlayCard" || msg.Type == "JumpIn"))
            {
                int cardIndex = int.Parse(msg.Content);
                if (cardIndex >= 0 && cardIndex < player.HandCards.Count)
                {
                    var card = player.HandCards[cardIndex];
                    if (card.Type != CardType.DrawTwo && card.Type != CardType.WildDrawFour)
                    {
                        await SendToClient(client, new Message { Type = "Error", Content = $"你头上还有 {gameLogic.DrawStack} 张罚牌，必须先出 +2 或 +4 转移，或者点击「抽牌」接受惩罚！" });
                        return;
                    }
                }
                else
                {
                    await SendToClient(client, new Message { Type = "Error", Content = "无效的牌索引" });
                    return;
                }
            }

            if (msg.Type == "PlayCard" || msg.Type == "JumpIn")
            {
                int cardIndex = int.Parse(msg.Content);
                bool isJump = (msg.Type == "JumpIn");
                try
                {
                    if (isJump)
                    {
                        if (gameLogic.CanJumpIn(player.HandCards[cardIndex]))
                            gameLogic.JumpInPlay(player, cardIndex, gameLogic.SelectedWildColor);
                        else
                            throw new InvalidOperationException("不能抢出此牌");
                    }
                    else
                    {
                        if (gameLogic.GetPlayerIndex(player) != gameLogic.CurrentPlayerIndex)
                            throw new InvalidOperationException("不是你的回合");
                        gameLogic.PlayCard(player, cardIndex, gameLogic.SelectedWildColor);
                    }

                    if (player.HandCards.Count == 0)
                    {
                        GameOver(player);
                        return;
                    }
                    BroadcastGameState();
                }
                catch (Exception ex)
                {
                    await SendToClient(client, new Message { Type = "Error", Content = ex.Message });
                }
            }
            else if (msg.Type == "DrawCard")
            {
                if (gameLogic.GetPlayerIndex(player) != gameLogic.CurrentPlayerIndex)
                {
                    await SendToClient(client, new Message { Type = "Error", Content = "不是你的回合" });
                    return;
                }
                gameLogic.DrawCard(player);
                BroadcastGameState();
            }
            else if (msg.Type == "CallUno")
            {
                player.HasCalledUno = true;
                BroadcastGameState();
            }
            else if (msg.Type == "SelectWildColor")
            {
                // 保留选色消息但不再使用（为了兼容旧客户端，可忽略）
                // gameLogic.SelectedWildColor = Enum.Parse<CardColor>(msg.Content);
                // BroadcastGameState();
            }
            else if (msg.Type == "Report")
            {
                if (!int.TryParse(msg.Content, out int reportedPlayerIndex))
                {
                    await SendToClient(client, new Message { Type = "Error", Content = "举报参数无效" });
                    return;
                }
                if (reportedPlayerIndex < 0 || reportedPlayerIndex >= players.Count)
                {
                    await SendToClient(client, new Message { Type = "Error", Content = "举报目标无效" });
                    return;
                }
                var reportedPlayer = players[reportedPlayerIndex];
                if (gameStarted && reportedPlayer.HandCards.Count == 1 && !reportedPlayer.HasCalledUno)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var newCard = gameLogic.DrawCardFromPile();
                        reportedPlayer.HandCards.Add(newCard);
                    }
                    BroadcastGameState();
                    SystemMessage?.Invoke($"{reportedPlayer.Name} 因未喊UNO被举报，罚抽2张牌");
                    MessageBox.Show("因未喊UNO被举报，罚抽2张牌");
                    await SendToClient(client, new Message { Type = "SystemMessage", Content = $"举报成功！{reportedPlayer.Name} 被罚抽2张牌" });
                }
                else
                {
                    await SendToClient(client, new Message { Type = "Error", Content = "举报无效：该玩家已喊UNO或手牌数不是1" });
                }
            }
        }

        private void GameOver(Player winner)
        {
            foreach (var p in players)
                winner.TotalScore += p.HandCards.Sum(c => c.Points);
            var result = new { winner = winner.Name, scores = players.ToDictionary(p => p.Name, p => p.TotalScore) };
            Broadcast(new Message { Type = "GameOver", Content = JsonConvert.SerializeObject(result) });
            gameStarted = false;
            MessageBox.Show("游戏结束，将回到主菜单","提示");
            Application.Restart();
        }

        private void BroadcastPlayerList()
        {
            var names = players.Select(p => p.Name).ToList();
            Broadcast(new Message { Type = "PlayerList", Content = JsonConvert.SerializeObject(names) });
            PlayerListChanged?.Invoke(names);
        }

        private void BroadcastGameState()
        {
            var state = new
            {
                currentPlayerIndex = gameLogic.CurrentPlayerIndex,
                currentTopCard = new { Color = gameLogic.CurrentTopCard.Color.ToString(), Type = gameLogic.CurrentTopCard.Type.ToString(), Number = gameLogic.CurrentTopCard.Number, Display = gameLogic.CurrentTopCard.GetDisplayText() },
                players = players.Select(p => new
                {
                    p.Name,
                    HandCardCount = p.HandCards.Count,
                    HasCalledUno = p.HasCalledUno,
                    TotalScore = p.TotalScore,
                    HandCards = p.HandCards.Select(c => new { c.Color, c.Type, c.Number, Display = c.GetDisplayText(), ImageFile = c.GetImageFileName() }).ToList()
                }),
                drawStack = gameLogic.DrawStack,
                isClockwise = gameLogic.IsClockwise,
                selectedWildColor = gameLogic.SelectedWildColor?.ToString()
            };
            Broadcast(new Message { Type = "GameState", Content = JsonConvert.SerializeObject(state) });
            GameStateChanged?.Invoke();
        }

        private void Broadcast(Message msg)
        {
            string json = JsonConvert.SerializeObject(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);
            List<TcpClient> clientList;
            lock (clients)
            {
                clientList = clients.Values.ToList();
            }
            foreach (var client in clientList)
            {
                try { client.GetStream().Write(data, 0, data.Length); } catch { }
            }
        }

        private async Task SendToClient(TcpClient client, Message msg)
        {
            string json = JsonConvert.SerializeObject(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await client.GetStream().WriteAsync(data, 0, data.Length);
        }

        private void Log(string msg) => LogEvent?.Invoke(msg);
        public void Stop() => listener?.Stop();
    }
}