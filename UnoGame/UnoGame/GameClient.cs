using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UnoGame
{
    public class GameClient
    {
        private TcpClient client;
        private NetworkStream stream;
        public event Action<Message> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnServerClosed;
        public string MyId { get; private set; }

        public async Task Connect(string ip, int port)
        {
            client = new TcpClient();
            await client.ConnectAsync(ip, port);
            stream = client.GetStream();
            OnConnected?.Invoke();
            _ = ReceiveLoop();
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[65536];
            while (client.Connected)
            {
                try
                {
                    int len = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (len == 0) break;
                    string json = Encoding.UTF8.GetString(buffer, 0, len);
                    var msg = JsonConvert.DeserializeObject<Message>(json);
                    if (msg.Type == "SetMyId")
                        MyId = msg.Content;
                    OnMessageReceived?.Invoke(msg);
                }
                catch
                {
                    break;
                }
            }
            OnServerClosed?.Invoke();
        }

        public async Task Send(Message msg)
        {
            if (client == null || !client.Connected) return;
            msg.SenderId = MyId;
            string json = JsonConvert.SerializeObject(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public void Close()
        {
            stream?.Close();
            client?.Close();
        }
    }
}