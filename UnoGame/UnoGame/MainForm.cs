using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Diagnostics;

namespace UnoGame
{
    public partial class MainForm : Form
    {
        private readonly bool isHost;
        private readonly string playerName;
        private readonly string joinIP;
        private readonly int joinPort;

        private GameServer server;
        private GameClient client;

        // UI控件
        private Panel gameBoardPanel;
        private FlowLayoutPanel handPanel;
        private Panel topCardPanel;
        private Label statusLabel;
        private Label infoLabel;
        private Label playerCountLabel;
        private ListBox playerListBox;
        private MenuStrip menuStrip;
        private ToolStripMenuItem gameMenu;
        private ToolStripMenuItem startGameMenuItem;
        private ToolStripMenuItem restartGameMenuItem;
        private ToolStripButton btnDrawTS;
        private ToolStripButton btnCallUnoTS;
        private Button btnReport;
        private PictureBox smallTopCardPicture;

        private dynamic currentGameState;
        private int myPlayerIndex = -1;
        private Dictionary<string, Image> cardImages = new Dictionary<string, Image>();
        private string imageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");
        private List<string> missingImages = new List<string>();

        public MainForm(string playerName, bool isHost, string joinIP = null, int joinPort = 8888)
        {
            this.playerName = playerName;
            this.isHost = isHost;
            this.joinIP = joinIP;
            this.joinPort = joinPort;
            InitializeComponent();
            LoadCardImages();
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            gameBoardPanel = new Panel();
            lblNarrator = new Label();
            handPanel = new FlowLayoutPanel();
            topCardPanel = new Panel();
            statusLabel = new Label();
            infoLabel = new Label();
            playerCountLabel = new Label();
            playerListBox = new ListBox();
            menuStrip = new MenuStrip();
            gameMenu = new ToolStripMenuItem();
            startGameMenuItem = new ToolStripMenuItem();
            restartGameMenuItem = new ToolStripMenuItem();
            btnDrawTS = new ToolStripButton();
            btnCallUnoTS = new ToolStripButton();
            btnToRule = new ToolStripMenuItem();
            btnReport = new Button();
            smallTopCardPicture = new PictureBox();
            IPInfo = new Label();
            gameBoardPanel.SuspendLayout();
            menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)smallTopCardPicture).BeginInit();
            SuspendLayout();
            // 
            // gameBoardPanel
            // 
            gameBoardPanel.BackColor = Color.DarkGreen;
            gameBoardPanel.Controls.Add(lblNarrator);
            gameBoardPanel.Dock = DockStyle.Fill;
            gameBoardPanel.Location = new Point(0, 137);
            gameBoardPanel.Name = "gameBoardPanel";
            gameBoardPanel.Size = new Size(1778, 827);
            gameBoardPanel.TabIndex = 2;
            gameBoardPanel.Paint += GameBoard_Paint;
            // 
            // lblNarrator
            // 
            lblNarrator.AutoSize = true;
            lblNarrator.BackColor = Color.ForestGreen;
            lblNarrator.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 134);
            lblNarrator.ForeColor = Color.PeachPuff;
            lblNarrator.Location = new Point(816, 760);
            lblNarrator.Name = "lblNarrator";
            lblNarrator.Size = new Size(145, 28);
            lblNarrator.TabIndex = 0;
            lblNarrator.Text = "轮到你的回合!";
            lblNarrator.Visible = false;
            lblNarrator.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            // 
            // handPanel
            // 
            handPanel.AutoScroll = true;
            handPanel.BackColor = Color.Beige;
            handPanel.Dock = DockStyle.Bottom;
            handPanel.Location = new Point(0, 964);
            handPanel.Name = "handPanel";
            handPanel.Padding = new Padding(5);
            handPanel.Size = new Size(1778, 180);
            handPanel.TabIndex = 3;
            // 
            // topCardPanel
            // 
            topCardPanel.BackColor = Color.WhiteSmoke;
            topCardPanel.Dock = DockStyle.Top;
            topCardPanel.Location = new Point(0, 137);
            topCardPanel.Name = "topCardPanel";
            topCardPanel.Size = new Size(1778, 0);
            topCardPanel.TabIndex = 4;
            topCardPanel.Visible = false;
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            statusLabel.Location = new Point(0, 97);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(1778, 40);
            statusLabel.TabIndex = 5;
            statusLabel.Text = "等待游戏开始...";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // infoLabel
            // 
            infoLabel.Dock = DockStyle.Top;
            infoLabel.Font = new Font("微软雅黑", 9F);
            infoLabel.Location = new Point(0, 67);
            infoLabel.Name = "infoLabel";
            infoLabel.Size = new Size(1778, 30);
            infoLabel.TabIndex = 6;
            // 
            // playerCountLabel
            // 
            playerCountLabel.Dock = DockStyle.Top;
            playerCountLabel.Font = new Font("微软雅黑", 9F);
            playerCountLabel.Location = new Point(0, 37);
            playerCountLabel.Name = "playerCountLabel";
            playerCountLabel.Size = new Size(1778, 30);
            playerCountLabel.TabIndex = 7;
            playerCountLabel.Text = "当前人数: 0 / 14";
            playerCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // playerListBox
            // 
            playerListBox.BackColor = Color.LightYellow;
            playerListBox.Dock = DockStyle.Right;
            playerListBox.Font = new Font("微软雅黑", 9F);
            playerListBox.ItemHeight = 24;
            playerListBox.Location = new Point(1538, 137);
            playerListBox.Name = "playerListBox";
            playerListBox.Size = new Size(240, 827);
            playerListBox.TabIndex = 1;
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(24, 24);
            menuStrip.Items.AddRange(new ToolStripItem[] { gameMenu, btnDrawTS, btnCallUnoTS, btnToRule });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1778, 37);
            menuStrip.TabIndex = 0;
            // 
            // gameMenu
            // 
            gameMenu.DropDownItems.AddRange(new ToolStripItem[] { startGameMenuItem, restartGameMenuItem });
            gameMenu.Name = "gameMenu";
            gameMenu.Size = new Size(62, 33);
            gameMenu.Text = "游戏";
            // 
            // startGameMenuItem
            // 
            startGameMenuItem.Enabled = false;
            startGameMenuItem.Name = "startGameMenuItem";
            startGameMenuItem.Size = new Size(218, 34);
            startGameMenuItem.Text = "开始游戏";
            startGameMenuItem.Click += StartGameMenuItem_Click;
            // 
            // restartGameMenuItem
            // 
            restartGameMenuItem.Enabled = false;
            restartGameMenuItem.Name = "restartGameMenuItem";
            restartGameMenuItem.Size = new Size(218, 34);
            restartGameMenuItem.Text = "重新开始游戏";
            restartGameMenuItem.Visible = false;
            restartGameMenuItem.Click += RestartGameMenuItem_Click;
            // 
            // btnDrawTS
            // 
            btnDrawTS.Enabled = false;
            btnDrawTS.Font = new Font("微软雅黑", 9F);
            btnDrawTS.Name = "btnDrawTS";
            btnDrawTS.Size = new Size(50, 28);
            btnDrawTS.Text = "抽牌";
            btnDrawTS.Click += btnDraw_Click;
            // 
            // btnCallUnoTS
            // 
            btnCallUnoTS.Enabled = false;
            btnCallUnoTS.Font = new Font("微软雅黑", 9F);
            btnCallUnoTS.Name = "btnCallUnoTS";
            btnCallUnoTS.Size = new Size(80, 28);
            btnCallUnoTS.Text = "喊 UNO";
            btnCallUnoTS.Click += btnCallUno_Click;
            // 
            // btnToRule
            // 
            btnToRule.Name = "btnToRule";
            btnToRule.Size = new Size(98, 33);
            btnToRule.Text = "游戏规则";
            btnToRule.Click += btnToRule_Click;
            // 
            // btnReport
            // 
            btnReport.BackColor = Color.Moccasin;
            btnReport.Enabled = false;
            btnReport.Font = new Font("微软雅黑", 10F);
            btnReport.Location = new Point(0, 0);
            btnReport.Name = "btnReport";
            btnReport.Size = new Size(80, 50);
            btnReport.TabIndex = 0;
            btnReport.Text = "举报!";
            btnReport.UseVisualStyleBackColor = false;
            btnReport.Visible = false;
            btnReport.Click += BtnReport_Click;
            // 
            // smallTopCardPicture
            // 
            smallTopCardPicture.BackColor = Color.LightGray;
            smallTopCardPicture.BorderStyle = BorderStyle.FixedSingle;
            smallTopCardPicture.Location = new Point(0, 0);
            smallTopCardPicture.Name = "smallTopCardPicture";
            smallTopCardPicture.Size = new Size(80, 120);
            smallTopCardPicture.SizeMode = PictureBoxSizeMode.StretchImage;
            smallTopCardPicture.TabIndex = 0;
            smallTopCardPicture.TabStop = false;
            smallTopCardPicture.Visible = false;
            // 
            // IPInfo
            // 
            IPInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            IPInfo.AutoSize = true;
            IPInfo.Cursor = Cursors.IBeam;
            IPInfo.Location = new Point(1495, 110);
            IPInfo.Name = "IPInfo";
            IPInfo.Size = new Size(228, 24);
            IPInfo.TabIndex = 0;
            IPInfo.Text = $"本机IP: {IPAddress}:8888";
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(1778, 1144);
            Controls.Add(IPInfo);
            Controls.Add(btnReport);
            Controls.Add(playerListBox);
            Controls.Add(gameBoardPanel);
            Controls.Add(handPanel);
            Controls.Add(topCardPanel);
            Controls.Add(statusLabel);
            Controls.Add(infoLabel);
            Controls.Add(playerCountLabel);
            Controls.Add(menuStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(1536, 900);
            Name = "MainForm";
            Text = $"UNO联机游戏 - {playerName}";
            gameBoardPanel.ResumeLayout(false);
            gameBoardPanel.PerformLayout();
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)smallTopCardPicture).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private void CenterSmallCard()
        {
            if (smallTopCardPicture == null || gameBoardPanel == null) return;
            if (smallTopCardPicture.Parent != gameBoardPanel) return;
            int x = (gameBoardPanel.Width - smallTopCardPicture.Width) / 2;
            int y = (gameBoardPanel.Height - smallTopCardPicture.Height) / 2;
            smallTopCardPicture.Location = new Point(x, y);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            CenterSmallCard();
            if (btnReport != null && playerListBox != null)
                btnReport.Location = new Point(playerListBox.Left - 110, playerListBox.Top + 10);
            gameBoardPanel.Invalidate();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            if (!this.gameBoardPanel.Controls.Contains(this.smallTopCardPicture))
            {
                this.gameBoardPanel.Controls.Add(this.smallTopCardPicture);
                this.smallTopCardPicture.BringToFront();
            }
            if (!this.gameBoardPanel.Controls.Contains(this.btnReport))
            {
                this.gameBoardPanel.Controls.Add(this.btnReport);
                this.btnReport.BringToFront();
            }
            CenterSmallCard();

            this.Size = new Size(1800, 1200);
            if (isHost)
            {
                await StartAsHost();
                IPInfo.Visible = true;// 仅房主显示本机IP信息
            }
            else
            {
                await StartAsClient(joinIP, joinPort);
                IPInfo.Visible = false;// 加入者不显示IP信息
            }
            MessageBox.Show("因为程序架构及TCP协议限制，该程序的Uno桌游规则与经典Uno略有不同\n详见“游戏规则”", "游戏须知");
        }

        private void LoadCardImages()
        {
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
                statusLabel.Text = "提示: image 文件夹不存在，已自动创建，请放入牌图片";
                return;
            }

            string[] possibleFiles = {
                "Red_0.png","Red_1.png","Red_2.png","Red_3.png","Red_4.png","Red_5.png","Red_6.png","Red_7.png","Red_8.png","Red_9.png",
                "Yellow_0.png","Yellow_1.png","Yellow_2.png","Yellow_3.png","Yellow_4.png","Yellow_5.png","Yellow_6.png","Yellow_7.png","Yellow_8.png","Yellow_9.png",
                "Green_0.png","Green_1.png","Green_2.png","Green_3.png","Green_4.png","Green_5.png","Green_6.png","Green_7.png","Green_8.png","Green_9.png",
                "Blue_0.png","Blue_1.png","Blue_2.png","Blue_3.png","Blue_4.png","Blue_5.png","Blue_6.png","Blue_7.png","Blue_8.png","Blue_9.png",
                "Red_Skip.png","Red_Reverse.png","Red_DrawTwo.png",
                "Yellow_Skip.png","Yellow_Reverse.png","Yellow_DrawTwo.png",
                "Green_Skip.png","Green_Reverse.png","Green_DrawTwo.png",
                "Blue_Skip.png","Blue_Reverse.png","Blue_DrawTwo.png",
                "Wild.png","WildDrawFour.png"
            };
            missingImages.Clear();
            foreach (var file in possibleFiles)
            {
                string path = Path.Combine(imageFolder, file);
                if (File.Exists(path))
                    cardImages[file] = Image.FromFile(path);
                else
                    missingImages.Add(file);
            }
            if (missingImages.Count > 0 && isHost)
                statusLabel.Text = $"缺少 {missingImages.Count} 张牌图片，将显示灰色背景。";
        }

        private async Task StartAsHost()
        {
            server = new GameServer();
            server.LogEvent += (msg) =>
            {
                if (this.IsHandleCreated)
                    this.Invoke((MethodInvoker)(() => infoLabel.Text = msg));
            };
            server.GameStateChanged += () =>
            {
                if (this.IsHandleCreated)
                    this.Invoke((MethodInvoker)RefreshUI);
            };
            server.PlayerListChanged += (playerNames) =>
            {
                if (this.IsHandleCreated)
                    this.Invoke((MethodInvoker)(() => UpdatePlayerList(playerNames)));
            };
            server.SystemMessage += (msg) =>
            {
                if (this.IsHandleCreated)
                    this.Invoke((MethodInvoker)(() => statusLabel.Text = msg));
            };
            await server.Start(8888);

            client = new GameClient();
            client.OnMessageReceived += OnClientMessage;
            client.OnServerClosed += () =>
            {
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        MessageBox.Show("服务器已关闭", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Application.Exit();
                    }));
                }
            };
            await client.Connect("127.0.0.1", 8888);
            await client.Send(new Message { Type = "Join", Content = playerName });
        }

        private async Task StartAsClient(string ip, int port)
        {
            client = new GameClient();
            client.OnMessageReceived += OnClientMessage;
            client.OnServerClosed += () =>
            {
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        MessageBox.Show("房主已关闭连接，游戏结束", "连接断开", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Application.Restart();
                    }));
                }
            };
            await client.Connect(ip, port);
            await client.Send(new Message { Type = "Join", Content = playerName });
        }
        private static string GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
            return "127.0.0.1";
        }
        private string IPAddress = GetLocalIPv4();
        private void UpdatePlayerList(List<string> playerNames)
        {
            playerListBox.Items.Clear();
            playerListBox.Items.Add($"玩家列表");
            foreach (var name in playerNames)
                playerListBox.Items.Add($"{name} (在线)");
            if (playerNames.Count > 1)
            {
                playerCountLabel.Text = $"当前人数: {playerNames.Count} / 14，房主可点击“游戏”按钮开始游戏";// 当人数超过1人时，提示房主可以开始游戏
            }
            else
            {
                playerCountLabel.Text = $"当前人数: {playerNames.Count} / 14，等待更多玩家加入...";
            }
            if (isHost)
            {
                startGameMenuItem.Enabled = playerNames.Count >= 2;
                if (currentGameState != null)
                {
                    restartGameMenuItem.Visible = true;
                    restartGameMenuItem.Enabled = true;
                }
            }
        }

        private async void StartGameMenuItem_Click(object sender, EventArgs e)
        {
            if (isHost && startGameMenuItem.Enabled)
            {
                await client.Send(new Message { Type = "StartGame" });
                startGameMenuItem.Enabled = false;
                restartGameMenuItem.Visible = true;
                restartGameMenuItem.Enabled = true;
            }
        }

        private async void RestartGameMenuItem_Click(object sender, EventArgs e)
        {
            if (isHost)
            {
                var result = MessageBox.Show("确定要重新开始游戏吗？当前对局将重置。", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    await client.Send(new Message { Type = "RestartGame" });
                }
            }
        }

        private async void BtnReport_Click(object sender, EventArgs e)
        {
            if (currentGameState == null) return;
            int currentPlayerIdx = (int)currentGameState.currentPlayerIndex;
            await client.Send(new Message { Type = "Report", Content = currentPlayerIdx.ToString() });
        }

        private Rectangle GetPlayerRect(int playerIndex)
        {
            if (currentGameState == null || currentGameState.players.Count == 0) return Rectangle.Empty;
            int n = (int)currentGameState.players.Count;
            if (n < 2 || playerIndex < 0 || playerIndex >= n) return Rectangle.Empty;

            int centerX = gameBoardPanel.Width / 2;
            int centerY = gameBoardPanel.Height / 2;
            int radius = Math.Min(centerX, centerY) - (n <= 4 ? 150 : 100);
            int rectWidth = 160;
            int rectHeight = 70;
            double angle = 2 * Math.PI * playerIndex / n - Math.PI / 2;
            int x = centerX + (int)(radius * Math.Cos(angle)) - rectWidth / 2;
            int y = centerY + (int)(radius * Math.Sin(angle)) - rectHeight / 2;
            return new Rectangle(x, y, rectWidth, rectHeight);
        }

        private void OnClientMessage(Message msg)
        {
            if (!this.IsHandleCreated) return;
            this.Invoke((MethodInvoker)(() =>
            {
                switch (msg.Type)
                {
                    case "PlayerList":
                        UpdatePlayerList(JsonConvert.DeserializeObject<List<string>>(msg.Content));
                        break;
                    case "GameState":
                        currentGameState = JsonConvert.DeserializeObject<dynamic>(msg.Content);
                        int idx = 0;
                        foreach (var p in currentGameState.players)
                        {
                            if ((string)p.Name == playerName)
                            {
                                myPlayerIndex = idx;
                                break;
                            }
                            idx++;
                        }
                        RefreshUI();
                        if (isHost)
                        {
                            restartGameMenuItem.Visible = true;
                            restartGameMenuItem.Enabled = true;
                        }
                        break;
                    case "Error":
                        MessageBox.Show(msg.Content, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    case "GameOver":
                        var result = JsonConvert.DeserializeObject<dynamic>(msg.Content);
                        MessageBox.Show($"游戏结束！胜者：{result.winner}\n得分：{result.scores}");
                        break;
                    case "SystemMessage":
                        statusLabel.Text = msg.Content;
                        break;
                    case "ServerShutdown":
                        MessageBox.Show("房主已关闭连接，游戏结束", "连接断开", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Application.Restart();
                        break;
                }
            }));
        }

        private void RefreshUI()
        {
            try
            {
                if (currentGameState == null)
                {
                    smallTopCardPicture.Visible = false;
                    return;
                }
                smallTopCardPicture.Visible = true;
                if (smallTopCardPicture.Parent != gameBoardPanel)
                {
                    gameBoardPanel.Controls.Add(smallTopCardPicture);
                    smallTopCardPicture.BringToFront();
                    CenterSmallCard();
                }
                else
                {
                    smallTopCardPicture.BringToFront();
                    CenterSmallCard();
                }

                int drawStack = (int)currentGameState.drawStack;
                statusLabel.Text = $"当前牌: {currentGameState.currentTopCard.Color} {currentGameState.currentTopCard.Display}  |  方向: {((bool)currentGameState.isClockwise ? "顺时针" : "逆时针")}  |  叠加罚牌: {drawStack}";

                var top = currentGameState.currentTopCard;
                string topImgFile = "";
                string topType = top.Type.ToString();
                if (topType == "Wild")
                    topImgFile = "Wild.png";
                else if (topType == "WildDrawFour")
                    topImgFile = "WildDrawFour.png";
                else
                {
                    string color = top.Color.ToString();
                    if (topType == "Number")
                        topImgFile = $"{color}_{top.Number}.png";
                    else if (topType == "Skip")
                        topImgFile = $"{color}_Skip.png";
                    else if (topType == "Reverse")
                        topImgFile = $"{color}_Reverse.png";
                    else if (topType == "DrawTwo")
                        topImgFile = $"{color}_DrawTwo.png";
                    else
                        topImgFile = $"{color}_{topType}.png";
                }
                if (cardImages.ContainsKey(topImgFile))
                    smallTopCardPicture.Image = cardImages[topImgFile];
                else
                    smallTopCardPicture.Image = null;

                handPanel.Controls.Clear();
                if (myPlayerIndex >= 0)
                {
                    var playersArray = currentGameState.players as Newtonsoft.Json.Linq.JArray;
                    if (playersArray != null && myPlayerIndex < playersArray.Count)
                    {
                        var handCards = playersArray[myPlayerIndex]["HandCards"] as Newtonsoft.Json.Linq.JArray;
                        if (handCards != null)
                        {
                            int idx = 0;
                            foreach (var card in handCards)
                            {
                                string fileName = card["ImageFile"]?.ToString();
                                PictureBox cardBox = new PictureBox { Size = new Size(80, 120), Tag = idx, Margin = new Padding(3), SizeMode = PictureBoxSizeMode.StretchImage };
                                if (!string.IsNullOrEmpty(fileName) && cardImages.ContainsKey(fileName))
                                    cardBox.Image = cardImages[fileName];
                                else
                                    cardBox.BackColor = Color.LightGray;
                                cardBox.Click += (s, e) => OnCardClick((int)((PictureBox)s).Tag);
                                handPanel.Controls.Add(cardBox);
                                idx++;
                            }
                        }
                    }
                }

                gameBoardPanel.Invalidate();

                int currentPlayerIdx = (int)currentGameState.currentPlayerIndex;
                bool isMyTurn = (myPlayerIndex == currentPlayerIdx);
                btnDrawTS.Enabled = isMyTurn;
                bool hasOneCard = false;
                if (myPlayerIndex >= 0)
                {
                    var playersArray = currentGameState.players as Newtonsoft.Json.Linq.JArray;
                    if (playersArray != null && myPlayerIndex < playersArray.Count)
                    {
                        var handCards = playersArray[myPlayerIndex]["HandCards"] as Newtonsoft.Json.Linq.JArray;
                        hasOneCard = (handCards?.Count == 1);
                    }
                }
                btnCallUnoTS.Enabled = isMyTurn && hasOneCard;
                if (isMyTurn && hasOneCard == true)
                {
                    btnCallUnoTS.Enabled = true;
                    btnCallUnoTS.BackColor = Color.Yellow;
                }
                else
                {
                    btnCallUnoTS.Enabled = false;
                    btnCallUnoTS.BackColor = Color.Transparent;
                }
                if (isMyTurn)
                {
                    lblNarrator.Visible = true;
                }
                else
                {
                    lblNarrator.Visible = false;
                }

                bool canReport = (myPlayerIndex != -1 && myPlayerIndex != currentPlayerIdx &&
                  (int)currentGameState.players[currentPlayerIdx].HandCardCount == 1 &&
                  !(bool)currentGameState.players[currentPlayerIdx].HasCalledUno);
                if (canReport)
                {
                    Rectangle playerRect = GetPlayerRect(currentPlayerIdx);
                    if (playerRect != Rectangle.Empty)
                    {
                        int btnX = playerRect.X + (playerRect.Width - btnReport.Width) / 2;
                        int btnY = playerRect.Y + playerRect.Height + 2;
                        btnReport.Location = new Point(btnX, btnY);
                        btnReport.Visible = true;
                        btnReport.Enabled = true;
                        btnReport.BringToFront();
                    }
                    else
                    {
                        btnReport.Visible = false;
                        btnReport.Enabled = false;
                    }
                }
                else
                {
                    btnReport.Visible = false;
                    btnReport.Enabled = false;
                }

                infoLabel.Text = $"当前玩家: {currentGameState.players[currentPlayerIdx].Name}";

            }
            catch (Exception ex)
            {
                statusLabel.Text = $"刷新错误: {ex.Message}";
            }
        }

        private async void OnCardClick(int cardIndex)
        {
            try
            {
                if (currentGameState == null) return;
                int currentPlayerIdx = (int)currentGameState.currentPlayerIndex;
                bool isMyTurn = (myPlayerIndex == currentPlayerIdx);
                var playersArray = currentGameState.players as Newtonsoft.Json.Linq.JArray;
                if (playersArray == null || myPlayerIndex < 0 || myPlayerIndex >= playersArray.Count) return;
                var handCards = playersArray[myPlayerIndex]["HandCards"] as Newtonsoft.Json.Linq.JArray;
                if (handCards == null || cardIndex >= handCards.Count) return;
                var selectedCard = handCards[cardIndex];
                var top = currentGameState.currentTopCard;

                bool canJump = (!isMyTurn &&
                                selectedCard["Color"]?.ToString() == top.Color?.ToString() &&
                                selectedCard["Type"]?.ToString() == top.Type?.ToString());

                if (canJump)
                {
                    await client.Send(new Message { Type = "JumpIn", Content = cardIndex.ToString() });
                }
                else if (isMyTurn)
                {
                    await client.Send(new Message { Type = "PlayCard", Content = cardIndex.ToString() });
                }
                else
                {
                    MessageBox.Show("不是你的回合，你不能出牌");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"出牌出错: {ex.Message}");
            }
        }

        private async void btnDraw_Click(object sender, EventArgs e) => await client.Send(new Message { Type = "DrawCard" });
        private async void btnCallUno_Click(object sender, EventArgs e) => await client.Send(new Message { Type = "CallUno" });

        private void GameBoard_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (currentGameState == null || currentGameState.players.Count == 0) return;
                int n = (int)currentGameState.players.Count;
                if (n < 2) return;

                int centerX = gameBoardPanel.Width / 2;
                int centerY = gameBoardPanel.Height / 2;
                int radius = Math.Min(centerX, centerY) - (n <= 4 ? 150 : 100);
                Font playerFont = new Font("微软雅黑", 12, FontStyle.Bold);
                int currentPlayerIdx = (int)currentGameState.currentPlayerIndex;

                int rectWidth = 160;
                int rectHeight = 70;

                for (int i = 0; i < n; i++)
                {
                    double angle = 2 * Math.PI * i / n - Math.PI / 2;
                    int x = centerX + (int)(radius * Math.Cos(angle)) - rectWidth / 2;
                    int y = centerY + (int)(radius * Math.Sin(angle)) - rectHeight / 2;
                    Rectangle rect = new Rectangle(x, y, rectWidth, rectHeight);
                    var p = currentGameState.players[i];
                    bool isCurrent = (i == currentPlayerIdx);
                    using (Brush bg = new SolidBrush(isCurrent ? Color.LightGreen : Color.LightGray))
                    {
                        e.Graphics.FillRectangle(bg, rect);
                        e.Graphics.DrawRectangle(Pens.Black, rect);
                    }
                    string playerName = p.Name.ToString();
                    int handCount = (int)p.HandCardCount;
                    bool hasCalledUno = (bool)p.HasCalledUno;
                    string text = $"{playerName}\n手牌:{handCount}";
                    if (hasCalledUno) text += " UNO!";
                    e.Graphics.DrawString(text, playerFont, Brushes.Black, rect.X + 5, rect.Y + 5);
                }
            }
            catch (Exception ex)
            {
                // 静默处理
            }
        }
        private void btnToRule_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "https://www.yuque.com/yumoo-vivy6/aqwhp3/hffbi0u8xy2spg9o?",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}