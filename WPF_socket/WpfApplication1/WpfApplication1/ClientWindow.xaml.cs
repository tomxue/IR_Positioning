using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        Socket c;
        public ClientWindow()
        {
            InitializeComponent();
            InitClient();
        }

        private void InitClient()
        {
            int port = 2000;
            string host = "127.0.0.1";
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);//把ip和端口转化为IPEndPoint实例
            c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建一个Socket

            ShowText("连接到Socket服务端...");

            try
            {
                c.Connect(ipe);//连接到服务器
            }
            catch (SocketException e)
            {
                MessageBox.Show("SocketException:" + e);
            }
        }

        private void sendBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowText("发送消息到服务端...");
                string sendStr = textBox2.Text;
                byte[] bs = Encoding.ASCII.GetBytes(sendStr);
                c.Send(bs, bs.Length, 0);

                //从服务器端接受返回信息
                string recvStr = "";
                byte[] recvBytes = new byte[1024];
                int bytes;
                bytes = c.Receive(recvBytes, recvBytes.Length, 0);
                recvStr += Encoding.UTF8.GetString(recvBytes, 0, bytes);
                ShowText("服务器返回信息：" + recvStr);
            }
            catch (ArgumentNullException ex1)
            {
                MessageBox.Show("ArgumentNullException:" + ex1);
            }
            catch (SocketException ex2)
            {
                MessageBox.Show("SocketException:" + ex2);
            }
        }

        private void ShowText(string text)
        {
            txtSockInfo.AppendText(text + "\n");
            txtSockInfo.ScrollToEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            closeBtn_Click(sender, (RoutedEventArgs) e);
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            //c.Disconnect(true);
            c.Shutdown(SocketShutdown.Both);
            c.Dispose();
            c.Close();
            Close();
        }
    }
}
