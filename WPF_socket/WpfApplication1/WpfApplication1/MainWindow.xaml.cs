using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // for generating bar code
        const int windowSize = 25;
        const int consecutiveBits = 3;
        //const int resolutionX = 1280 / 2;
        //const int resolutionY = 800/2;
        const int resolutionX = 800;
        const int resolutionY = 600;
        byte[] randomData = new byte[resolutionX];
        byte[] patternData = new byte[resolutionX];
        byte[] pattern2 = new byte[resolutionX];
        byte[] patternReadout = new byte[resolutionX];
        byte[] windowBits = new byte[windowSize];

        const int RECV_DATA_COUNT = 512;
        const bool X = true;
        const bool Y = false;
        int[] rx16 = new int[RECV_DATA_COUNT];
        int count, bytesRec;
        float sum, avg, avgX, avgY;
        byte[] bytes;
        int counterOfGood = 0, counterOfBad = 0;

        public MainWindow()
        {
            InitializeComponent();

            InitServer();

            GuiShowAssociate();
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
        { }

        private void GuiShowAssociate()
        {
            ReceiveTextEvent += this.ShowText;
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            GenerateBarCode();
        }

        private void GenerateBarCode()
        {
            int flagMatch111or000 = 0;
            string PATH = System.IO.Directory.GetCurrentDirectory() + @"\pattern.txt";

            // method 1: Get the randomValues from real random method
            //Random random = new Random();
            //ShowRandomNumbers(random);  // generate all the resolutionX random numbers at this point

            // method 2: Get the randomValues from the saved file
            byte[] patternReadout = File.ReadAllBytes(PATH);
            Console.WriteLine("\r\nShow the readout pattern below:");
            foreach (var value in patternReadout)
                Console.Write("{0, 5}", value);

            for (int n = 0; n < resolutionX; n++)
                randomData[n] = patternReadout[n];

            // method 3:
            // generate the test stream: 0 1 0 1 0 1 0 ...
            //for (int n = 0; n < resolutionX; n++)
            //{
            //    if (n % 2 == 1)
            //        randomData[n] = 1;
            //    else
            //        randomData[n] = 0;
            //}

            Bitmap bitmap = new Bitmap(resolutionX, resolutionY);  // Coolux DLP projector's resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.Black);

            for (int i = 0; i < resolutionX; i++)
            {
                if (randomData[i] % 2 == 1)
                {
                    // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three
                    if (i >= 3 && ((randomData[i - 1] % 2) == 1) && ((randomData[i - 2] % 2) == 1) && ((randomData[i - 3] % 2) == 1))
                    {
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black), i, 0, i, resolutionX);
                        patternData[i] = 0;
                        randomData[i] = 0;  // will change the input data: randomData accordingly, important!
                    }
                    else
                    {
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.White), i, 0, i, resolutionX);
                        patternData[i] = 1;
                        randomData[i] = 1;
                    }
                }
                else
                {
                    // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three.
                    if (i >= 3 && ((randomData[i - 1] % 2) == 0) && ((randomData[i - 2] % 2) == 0) && ((randomData[i - 3] % 2) == 0))
                    {
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.White), i, 0, i, resolutionX);
                        patternData[i] = 1;
                        randomData[i] = 1;
                    }
                    else
                    {
                        g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black), i, 0, i, resolutionX);
                        patternData[i] = 0;
                        randomData[i] = 0;
                    }
                }
            }

            // Requirement 2: every window contain at least one run of length exactly one.
            // It does not influence the result of requirement 1.
            for (int i = 0; i < resolutionX; i++)
            {
                // Step 1: sweep within one window: loop to check
                if (i <= (resolutionX - windowSize))
                {
                    for (int j = 0; j < (windowSize - consecutiveBits + 1); j++)
                    {
                        if (patternData[i + j] + patternData[i + j + 1] + patternData[i + j + 2] == 3 || patternData[i + j] + patternData[i + j + 1] + patternData[i + j + 2] == 0)
                        {
                            flagMatch111or000 = 1;
                            break;
                        }
                        else
                            flagMatch111or000 = 0;
                    }
                }

                // Step 2: after sweeping, if no 111 or 000 pattern found, then make it
                if (flagMatch111or000 == 0)
                {
                    if (patternData[i + windowSize - consecutiveBits - 1] == 1)
                    {
                        int j;
                        for (j = 0; j <= (consecutiveBits - 1); j++)
                            patternData[i + windowSize - consecutiveBits + j] = 0;
                        patternData[i + windowSize] = 1;
                    }
                    else
                    {
                        int j;
                        for (j = 0; j <= (consecutiveBits - 1); j++)
                            patternData[i + windowSize - consecutiveBits + j] = 1;
                        patternData[i + windowSize] = 0;
                    }

                    flagMatch111or000 = 1;
                }

                // Step 3: jump to next window for sweeping, notice that next window has some overlap with current window
                i = i + windowSize - consecutiveBits;
            }

            // Requirement 3: the bit-patterns of different windows differ in at least two places, to ensure that 
            // single bit-flips caused by noise could not result in an incorrect identification.
            // For real random seed, this requirement can be easily fulfilled
            int diffCount = 0;

            for (int i = 0; i <= resolutionX - windowSize; i++)
            {
                // prepare the window "k" (base is "i") to be compared with all the other windows
                for (int k = 0; k < windowSize; k++)
                    windowBits[k] = patternData[i + k];

                // prepare another window "n" (base is "m") and compare it with window "k"
                for (int m = 0; m <= (resolutionX - windowSize); m++)
                {
                    if (m != i)
                    {
                        for (int n = 0; n < windowSize; n++)
                        {
                            if (windowBits[n] != patternData[m + n])
                                diffCount++;
                        }
                        if (diffCount < 2)
                        {
                            MessageBox.Show("Requirement 3 is not fulfilled!");
                            return;
                        }
                        else
                            diffCount = 0;
                    }
                }
            }

            // show it
            Console.WriteLine("\r\nShow patternData below:");
            foreach (var value in patternData)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\nShow index below:");
            for (int i = 0; i < resolutionX; i++)
                Console.Write("{0, 5}", i);

            Console.WriteLine("\r\nShow randomData below:");
            foreach (var value in randomData)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\n");

            g.Save();
            g.Dispose();
            //bitmap.MakeTransparent(Color.Red);
            bitmap.Save("BarCode.png", ImageFormat.Png);

            File.WriteAllBytes(PATH, patternData);

            MessageBox.Show("The bar code is generated successfully!");
        }

        private void ShowRandomNumbers(Random rand)
        {
            Console.WriteLine();
            rand.NextBytes(randomData);
            Console.WriteLine("\r\nShow raw random below:");
            foreach (var value in randomData)
                Console.Write("{0, 5}", value);

            Console.WriteLine();
        }

        private void startServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread th = new Thread(new ThreadStart(SocketListen));
            th.Start();
            //startServiceBtn.IsEnabled = false;
        }

        private void SocketListen()
        {
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

            StartListen(port, ip);
        }

        // Tom Xue: to show how many client windows/connections are alive
        // 主要功能：接收消息，发还消息
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

                do
                {
                    //为新建连接创建新的Socket，阻塞在此
                    Socket connectionSocket = s.Accept();

                    ReceiveText("客户端[" + connectionSocket.RemoteEndPoint.ToString() + "]连接已建立...");
                    ReceiveText(Environment.NewLine);

                    HandleSensorData(connectionSocket);
                } while (false);
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

        public void HandleSensorData(Socket socket)
        {
            while (true)
            {
                bytes = new byte[RECV_DATA_COUNT];

                Thread.Sleep(10);
                //等待接收消息
                bytesRec = socket.Receive(bytes);

                if (bytesRec == 0)
                {
                    ReceiveText("客户端[" + socket.RemoteEndPoint.ToString() + "]连接关闭...\r\n");
                    break;
                }
                else if (bytesRec == 512)
                {
                    counterOfGood++;
                    //ReceiveText("The received data count is: " + bytesRec + " Good data = " + counterOfGood + " Bad data = " + counterOfBad + "\r\n");
                }
                else
                {
                    counterOfBad++;
                    //ReceiveText("The received data count is: " + bytesRec + " Good data = " + counterOfGood + " Bad data = " + counterOfBad + "---------not 512!!!--------\r\n");
                }

                ShowRawData(X);   // X_axis
                ShowRawData(Y);   // Y_axis

                GetThreashold(X);
                GetThreashold(Y);

                BadPatternFiltered(X);
                BadPatternFiltered(Y);
            }
        }

        private void ShowRawData(bool X_axis)
        {
            //if (X_axis == true)   // X_axis
            //    ReceiveText("---X axis raw data---\r\n");
            //else
            //    ReceiveText("---Y axis raw data---\r\n");

            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i = i + 2)
            {
                rx16[i] = bytes[i];
                rx16[i] = rx16[i] << 8 | bytes[i + 1];
                rx16[i] = rx16[i] & 0x1fff;
                rx16[i] = rx16[i] >> 2;
                sum += rx16[i];
                count++;
                if (i == 2)
                    ReceiveText(Convert.ToString(rx16[i]));

                //if (i % 64 == 0)
                //    ReceiveText(Environment.NewLine);
            }
            avg = sum / count;
            if (X_axis == true)
                avgX = avg;
            else
                avgY = avg;

            //ReceiveText("---The average value of the axis is " + avg + "\r\n\r\n");
        }

        private void GetThreashold(bool X_axis)
        {
            sum = 0; count = 0; avg = 0;
            //if (X_axis == true)
            //    ReceiveText("\r\n---X axis data checked by threshold---");
            //else
            //    ReceiveText("\r\n---Y axis data checked by threashold---");

            // replace the bigger value with the average value, important!
            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i = i + 2)
            {
                if (rx16[i] >= ((X_axis == true) ? avgX : avgY) && rx16[i] != 2047)  // 2047: the maximal value
                    rx16[i] = (int)((X_axis == true) ? avgX : avgY) + 1;

                sum += rx16[i];
                count++;
            }

            // recalcaulate the new average value
            avg = sum / count;
            //ReceiveText("The new average value of the axis is " + avg + "\r\n");

            // convert the threasholded data to digital ones and show them
            ConvertRawToDigital(X_axis);
        }

        private void ConvertRawToDigital(bool X_axis)
        {
            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i = i + 2)
            {
                if (rx16[i] >= avg)
                    rx16[i] = 1;
                else
                    rx16[i] = 0;

                //ReceiveText(Convert.ToString(rx16[i]));

                //if (i % 64 == 0)
                //ReceiveText("\r\n");
            }

            //ReceiveText("\r\n");
        }

        private void BadPatternFiltered(bool X_axis)
        {
            int pattern;

            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? (bytesRec - 8) : (bytesRec / 2 - 8)); i = i + 2)
            {
                // pattern 1
                pattern = (rx16[i] << 4) | (rx16[i + 2] << 3) | (rx16[i + 4] << 2) | (rx16[i + 6] << 1) | rx16[i + 8];

                if (pattern == 0x4)           // 0x4 = 0b00100
                    rx16[i + 4] = 0;
                else if (pattern == 0x1b)     // 0x1b = 0b11011
                    rx16[i + 4] = 1;

                // pattern 2: at the beginning of X or Y data array...
                if (i == ((X_axis == true) ? (bytesRec / 2) : 0))
                {
                    if ((pattern & Convert.ToInt32("11100", 2)) == Convert.ToInt32("10000", 2))
                        rx16[i] = 0;
                    else if ((pattern & Convert.ToInt32("11110", 2)) == Convert.ToInt32("01000", 2))
                        rx16[i + 2] = 0;
                    else if ((pattern & Convert.ToInt32("11100", 2)) == Convert.ToInt32("01100", 2))
                        rx16[i] = 1;
                    else if ((pattern & Convert.ToInt32("11110", 2)) == Convert.ToInt32("10110", 2))
                        rx16[i + 2] = 1;
                }

                // pattern 3: at the end of X or Y data array...
                if (i == ((X_axis == true) ? (bytesRec - 10) : (bytesRec / 2 - 10)))
                {
                    if ((pattern & Convert.ToInt32("00111", 2)) == Convert.ToInt32("00001", 2))
                        rx16[i + 8] = 0;
                    else if ((pattern & Convert.ToInt32("01111", 2)) == Convert.ToInt32("00010", 2))
                        rx16[i + 6] = 0;
                    else if ((pattern & Convert.ToInt32("00111", 2)) == Convert.ToInt32("00110", 2))
                        rx16[i + 8] = 1;
                    else if ((pattern & Convert.ToInt32("01111", 2)) == Convert.ToInt32("01101", 2))
                        rx16[i + 6] = 1;
                }
            }

            //if (X_axis == true)
            //    ReceiveText("-------FilteredData of X-------\r\n");
            //else
            //    ReceiveText("-------FilteredData of Y-------\r\n");

            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i = i + 2)
            {
                //ReceiveText(Convert.ToString(rx16[i]));

                //if (i % 64 == 0)
                //    ReceiveText("\r\n");
            }

            //ReceiveText("\r\n");
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
