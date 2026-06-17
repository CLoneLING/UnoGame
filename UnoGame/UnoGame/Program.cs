using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Windows.ApplicationModel.Resources.Core;
using UnoGame;

namespace UnoGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 全局异常捕获
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show($"UI线程异常：{e.Exception.Message}\n{e.Exception.StackTrace}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                MessageBox.Show($"未捕获异常：{(e.ExceptionObject as Exception)?.Message}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // 连接对话框
            string playerName = null;
            bool isHost = false;
            string joinIP = null;
            int joinPort = 8888;
            string GetLocalIPv4()
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
            string IPAddress = GetLocalIPv4();

            using (Form dialog = new Form { Width = 480, Height = 330, Text = " UNO 联机", StartPosition = FormStartPosition.CenterScreen, })
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    try
                    {
                        dialog.Icon = new Icon(iconPath);
                    }
                    catch { /* 图标文件损坏时忽略 */ }
                }
                Label tbNameText = new Label
                {
                    Text = "输入昵称：",
                    Location = new Point(90, 20),
                    Font = new Font("微软雅黑", 9), 
                    AutoSize = true,
                };
                TextBox tbName = new TextBox
                {
                    PlaceholderText = "玩家名",
                    Location = new Point(90, 45),
                    Width = 280,
                    Font = new Font("微软雅黑", 9) 
                };
                Button btnHost = new Button
                {
                    Text = "创建房间（房主）",
                    Location = new Point(90, 105),
                    Width = 280,
                    Height = 35,
                    Font = new Font("微软雅黑", 9)
                };
                Button btnJoin = new Button
                {
                    Text = "加入房间",
                    Location = new Point(90, 165),
                    Width = 280,
                    Height = 35,
                    Font = new Font("微软雅黑", 9)
                };
                Label authorInfo = new Label
                {
                    Text = "版本：1.1.0\n作者：YuMoo；画师：画鷁_ y；测试：四叶草",
                    Location = new Point(10, 220),
                    Font = new Font("微软雅黑", 8),
                    AutoSize = true
                };
                dialog.Controls.Add(tbName);
                dialog.Controls.Add(btnHost);
                dialog.Controls.Add(btnJoin);
                dialog.Controls.Add(tbNameText);
                dialog.Controls.Add(authorInfo);

                bool choiceMade = false;
                btnHost.Click += (s, e) =>
                {
                    MessageBox.Show($"请认真阅读以下内容，发生异常后果自负!\n\n创建房间后你可以将IP作为邀请码分享给信任的朋友，打开该程序后点击主页的加入房间，输入你的IP即可加入你的房间，局域网IP连接需要双方处于同一网络下且未被限制.\n\n！！请勿将您的公网IP地址分享给陌生人，防止黑客攻击！！\n\n如果要使用公网进行联机，请装载外部 内网穿透.\n安装教程网上有，也可以问该程序开发者(有联系方式的话，没有还是上网搜吧)\n\n本机IP：{IPAddress}\n默认开启端口：8888", "联机须知");
                    if (string.IsNullOrWhiteSpace(tbName.Text)) return;
                    playerName = tbName.Text;
                    isHost = true;
                    choiceMade = true;
                    dialog.Close();
                };
                btnJoin.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(tbName.Text)) return;
                    playerName = tbName.Text;
                    isHost = false;
                    choiceMade = true;
                    dialog.Close();
                    MessageBox.Show("适度游戏，请勿沉迷\n他人IP为其个人网络虚拟财产，请勿随意分享造成财产损失，触犯法律后果自负.\r\n\n可使用域名.","联机须知");
                    using (Form joinDialog = new Form { Width = 480, Height = 270, Text = "加入游戏", StartPosition = FormStartPosition.CenterParent })
                    {
                        if (System.IO.File.Exists(iconPath))
                        {
                            try { joinDialog.Icon = new Icon(iconPath); } catch { }
                        }
                        Label tbIPText = new Label { Text = "房间IP：", Location = new Point(90, 20), Font = new Font("微软雅黑", 9), AutoSize = true };
                        TextBox tbIP = new TextBox { Location = new Point(90, 45), Width = 300, PlaceholderText = "输入房间IP，如127.0.0.1", Font = new Font("微软雅黑", 9) };
                        TextBox tbPort = new TextBox { Location = new Point(90, 90), Width = 300, PlaceholderText = "端口，默认8888", Font = new Font("微软雅黑", 9) };
                        Button btnConnect = new Button { Text = "连接", Location = new Point(90, 150), Width = 300, Height = 35, Font = new Font("微软雅黑", 9) };
                        joinDialog.Controls.Add(tbIP);
                        joinDialog.Controls.Add(tbPort);
                        joinDialog.Controls.Add(btnConnect);
                        joinDialog.Controls.Add(tbIPText);
                        bool joined = false;
                        btnConnect.Click += (s2, e2) =>
                        {
                            joinIP = tbIP.Text;
                            if (int.TryParse(tbPort.Text, out int port))
                                joinPort = port;
                            joined = true;
                            joinDialog.Close();
                        };
                        joinDialog.ShowDialog();
                        if (!joined) choiceMade = false;
                    }
                };
                dialog.ShowDialog();
                if (!choiceMade) return;
            }


            Application.Run(new MainForm(playerName, isHost, joinIP, joinPort));
        }

    }
}