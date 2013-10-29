using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SocketListener listener;

        public MainWindow()
        {
            InitializeComponent();

            InitServer();
        }

        private void InitServer()
        {
            System.Timers.Timer t = new System.Timers.Timer(2000);
            //实例化Timer类，设置间隔时间为2000毫秒；
            t.Elapsed += new System.Timers.ElapsedEventHandler(CheckListen);
            //到达时间的时候执行事件； 
            t.AutoReset = false;
            t.Start();
        }

        private void CheckListen(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (listener != null && SocketListener.ConnectionPair != null)
            {
                ShowText("连接数：" + SocketListener.ConnectionPair.Count.ToString());
            }
        }

        private void startServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread th = new Thread(new ThreadStart(SocketListen));
            th.Start();
            //startServiceBtn.IsEnabled = false;
        }

        private void SocketListen()
        {
            listener = new SocketListener();
            // Tom Xue: associate the callback delegate with SocketListener
            listener.ReceiveTextEvent += new SocketListener.ReceiveTextHandler(ShowText);
            int port = 0;
            string ip = "";
            this.textBox1.Dispatcher.Invoke(delegate
            {
                ip = textBox1.Text;
            });
            this.textBox2.Dispatcher.Invoke(delegate
            {
                port = Convert.ToInt32(textBox2.Text);
            });
            listener.StartListen(port, ip);
        }

        // ShowTextHandler is a delegate class/type
        public delegate void ShowTextHandler(string text);
        ShowTextHandler setText;

        private void ShowText(string text)
        {
            if (System.Threading.Thread.CurrentThread != txtSocketInfo.Dispatcher.Thread)
            {
                if (setText == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    // ShowTextHandler.ShowTextHandler(void (string) target)
                    setText = new ShowTextHandler(ShowText);
                }
                txtSocketInfo.Dispatcher.BeginInvoke(setText, DispatcherPriority.Normal, new string[] { text });
            }
            else
            {
                txtSocketInfo.AppendText(text + " ");
                txtSocketInfo.ScrollToEnd();
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            txtSocketInfo.Clear();
        }

    }

    // Tom Xue: to show how many client windows/connections are alive
    // 主要功能：接收消息，发还消息
    public class SocketWork
    {
        Socket _connection;
        const int RECV_DATA_COUNT = 512;
        int[] rx_X16 = new int[RECV_DATA_COUNT];
        int[] rx_Y16 = new int[RECV_DATA_COUNT];
        int countX = 0, countY = 0, bytesRec;
        float sumX = 0, sumY = 0, avgX = 0, avgY = 0;
        byte[] bytes;

        public SocketWork(Socket socket)
        {
            _connection = socket;
        }

        public void HandleSensorData()
        {
            int counterOfGood = 0, counterOfBad = 0;

            while (true)
            {
                bytes = new byte[RECV_DATA_COUNT];

                //等待接收消息
                bytesRec = this._connection.Receive(bytes);

                if (bytesRec == 0)
                {
                    ReceiveText("客户端[" + _connection.RemoteEndPoint.ToString() + "]连接关闭...");
                    SocketListener.ConnectionPair.Remove(_connection.RemoteEndPoint.ToString());
                    break;
                }
                else if (bytesRec == 512)
                {
                    counterOfGood++;
                    ReceiveText("The received data count is: " + bytesRec + " Good data = " + counterOfGood + " Bad data = " + counterOfBad);
                }
                else
                {
                    counterOfBad++;
                    ReceiveText("The received data count is: " + bytesRec + " Good data = " + counterOfGood + " Bad data = " + counterOfBad + "---------not 512!!!--------");
                }

                ReceiveText(Environment.NewLine);

                // ---------------------------- X axis begin ----------------------------
                ReceiveText("---X axis---");
                ReceiveText(Environment.NewLine);
                for (int i = bytesRec / 2; i < bytesRec; i = i + 2)
                {
                    rx_X16[i] = bytes[i];
                    rx_X16[i] = rx_X16[i] << 8 | bytes[i + 1];
                    rx_X16[i] = rx_X16[i] & 0x1fff;
                    rx_X16[i] = rx_X16[i] >> 2;
                    sumX += rx_X16[i];
                    countX++;
                    ReceiveText(Convert.ToString(rx_X16[i]));

                    if (i % 64 == 0)
                        ReceiveText(Environment.NewLine);
                }
                avgX = sumX / countX;
                ReceiveText("---The end of 256 data!---");
                ReceiveText(Environment.NewLine);
                ReceiveText("The average value of X axis is " + avgX);
                ReceiveText(Environment.NewLine);
                ReceiveText(Environment.NewLine);
                
                // ---------------------------- X axis end ----------------------------

                // ---------------------------- Y axis begin ----------------------------
                ReceiveText("---Y axis---");
                ReceiveText(Environment.NewLine);
                for (int i = 0; i < bytesRec / 2; i = i + 2)
                {
                    rx_Y16[i] = bytes[i];
                    rx_Y16[i] = rx_Y16[i] << 8 | bytes[i + 1];
                    rx_Y16[i] = rx_Y16[i] & 0x1fff;
                    rx_Y16[i] = rx_Y16[i] >> 2;
                    sumY += rx_Y16[i];
                    countY++;
                    ReceiveText(Convert.ToString(rx_Y16[i]));

                    if (i % 64 == 0)
                        ReceiveText(Environment.NewLine);
                }
                avgY = sumY / countY;
                ReceiveText("---The end of 512 data!---");
                ReceiveText(Environment.NewLine);
                ReceiveText("The average value of Y axis is " + avgY);
                // ---------------------------- Y axis end ----------------------------

                ParseSensorData();
            }
        }

        public void ParseSensorData()
        {
            ReceiveText(Environment.NewLine);
            ReceiveText(Environment.NewLine);
            ReceiveText("---X axis---");
            for (int i = bytesRec / 2; i < bytesRec; i = i + 2)
            {
                rx_X16[i] = bytes[i];
                rx_X16[i] = rx_X16[i] << 8 | bytes[i + 1];
                rx_X16[i] = rx_X16[i] & 0x1fff;
                rx_X16[i] = rx_X16[i] >> 2;
                if (rx_X16[i] > avgX)
                    rx_X16[i] = 1;
                else
                    rx_X16[i] = 0;
                ReceiveText(Convert.ToString(rx_X16[i]));

                if (i % 64 == 0)
                    ReceiveText(Environment.NewLine);
            }
            ReceiveText(Environment.NewLine);

            ReceiveText(Environment.NewLine);
            ReceiveText("---Y axis---");
            for (int i = 0; i < bytesRec / 2; i = i + 2)
            {
                rx_Y16[i] = bytes[i];
                rx_Y16[i] = rx_Y16[i] << 8 | bytes[i + 1];
                rx_Y16[i] = rx_Y16[i] & 0x1fff;
                rx_Y16[i] = rx_Y16[i] >> 2;
                if (rx_Y16[i] > avgY)
                    rx_Y16[i] = 1;
                else
                    rx_Y16[i] = 0;
                ReceiveText(Convert.ToString(rx_Y16[i]));

                if (i % 64 == 0)
                    ReceiveText(Environment.NewLine);
            }
            ReceiveText(Environment.NewLine);
            ReceiveText(Environment.NewLine);
        }

        public delegate void ReceiveTextHandler(string text);
        public event ReceiveTextHandler ReceiveTextEvent;
        private void ReceiveText(string text)
        {
            if (ReceiveTextEvent != null)
            {
                ReceiveTextEvent(text);
            }
        }
    }

    public class SocketListener
    {
        public static Hashtable ConnectionPair = new Hashtable();

        public void StartListen(int PORT, string HOST)
        {
            try
            {
                //端口号、IP地址
                int port = PORT;
                string host = HOST;
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);

                //创建一个Socket类
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Bind(ipe);//绑定2000端口
                s.Listen(0);//开始监听

                ReceiveText("启动Socket监听...");

                while (true)
                {
                    //为新建连接创建新的Socket，阻塞在此
                    Socket connectionSocket = s.Accept();

                    ReceiveText("客户端[" + connectionSocket.RemoteEndPoint.ToString() + "]连接已建立...");
                    ReceiveText(Environment.NewLine);

                    SocketWork mySocketWork = new SocketWork(connectionSocket);
                    // Tom Xue: associate the callback delegate (SocketListener.ReceiveText) with Connection
                    mySocketWork.ReceiveTextEvent += new SocketWork.ReceiveTextHandler(ReceiveText);

                    ConnectionPair.Add(connectionSocket.RemoteEndPoint.ToString(), mySocketWork);

                    //在新线程中完成socket的功能：接收消息，发还消息
                    Thread thread = new Thread(new ThreadStart(mySocketWork.HandleSensorData));
                    thread.Name = connectionSocket.RemoteEndPoint.ToString();
                    thread.Start();
                }
            }
            catch (ArgumentNullException ex1)
            {
                ReceiveText("ArgumentNullException:" + ex1);
            }
            catch (SocketException ex2)
            {
                ReceiveText("SocketException:" + ex2);
            }
        }

        public delegate void ReceiveTextHandler(string text);
        public event ReceiveTextHandler ReceiveTextEvent;   // 去掉event效果一样
        private void ReceiveText(string text)   // Tom Xue: it is a callback
        {
            if (ReceiveTextEvent != null)
            {
                ReceiveTextEvent(text);
            }
        }
    }
}