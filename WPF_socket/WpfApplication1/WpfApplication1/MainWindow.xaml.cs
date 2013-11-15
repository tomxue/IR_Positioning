using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        const int windowSize = 17;  // 128/16 = 8, means the steps can be 8
        const int consecutiveBits = 3;
        const int resolutionX = 1280 / 2;
        const int resolutionY = 800;
        byte[] randomData = new byte[resolutionX];
        byte[] patternData = new byte[resolutionX];
        byte[] patternReadout = new byte[resolutionX];
        byte[] toBeCompared = new byte[windowSize];

        const int RECV_DATA_COUNT = 512;
        const bool X = true;
        const bool Y = false;
        int[] rx16 = new int[RECV_DATA_COUNT];
        int count, bytesRec;
        float sum, avg, avgX, avgY;
        byte[] bytes = null;
        private static Mutex mutexDataReady = new Mutex();
        int counterOfGood = 0, counterOfBad = 0;
        bool flagShow = false;
        const int steps = 10;
        const int stepBegin = 2;
        const int stepEnd = 8;
        byte[] stepwisedDigitalValue = new byte[RECV_DATA_COUNT];
        Dictionary<String, int> patternAxis = new Dictionary<string, int>();
        static int runOnce = 0;
        private int coordinateValue = -2;
        showWindow showWin = new showWindow();
        int lastStepSize = 0;
        int seqCount = 0;
        int seq_a = 0, seq_b = 0, seq_c = 0;

        public MainWindow()
        {
            InitializeComponent();

            Guithread();

            GenerateBarHash();

            Socketthread();

            showWin.Show();
        }

        public int Coordinate
        {
            get { return coordinateValue; }
        }

        private void barBtn_Click(object sender, RoutedEventArgs e)
        {
            GenerateBarHash();
        }

        private void showBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((string)(showBtn.Content) == "Show more")
            {
                showBtn.Content = "Show less";
                flagShow = true;
            }
            else
            {
                showBtn.Content = "Show more";
                flagShow = false;
            }
        }

        public int getter()
        {
            return coordinateValue;
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void GenerateBarHash()
        {
            string PATH = System.IO.Directory.GetCurrentDirectory() + @"\pattern.txt";

        GenerateBarLoop:

            // method 1: Get the randomValues from real random method
            //Random random = new Random();
            //randomDataFilled(random);  // generate all the resolutionX random numbers at this point

            // method 2: Get the randomValues from the saved file
            byte[] patternReadout = File.ReadAllBytes(PATH);

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

            Bitmap bitmap = new Bitmap(2 * resolutionX, resolutionY);  // Coolux DLP projector's resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.Black);

            // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three
            //for (int i = 0; i < resolutionX; i++)
            //{
            //    if (randomData[i] % 2 == 1)
            //    {
            //        if (i >= 3 && ((randomData[i - 1] % 2) == 1) && ((randomData[i - 2] % 2) == 1) && ((randomData[i - 3] % 2) == 1))
            //        {
            //            g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black), i, 0, i, resolutionX);
            //            patternData[i] = 0;
            //            randomData[i] = 0;  // will change the input data: randomData accordingly, important!
            //        }
            //        else
            //        {
            //            g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.White), i, 0, i, resolutionX);
            //            patternData[i] = 1;
            //            randomData[i] = 1;
            //        }
            //    }
            //    else
            //    {
            //        if (i >= 3 && ((randomData[i - 1] % 2) == 0) && ((randomData[i - 2] % 2) == 0) && ((randomData[i - 3] % 2) == 0))
            //        {
            //            g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.White), i, 0, i, resolutionX);
            //            patternData[i] = 1;
            //            randomData[i] = 1;
            //        }
            //        else
            //        {
            //            g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black), i, 0, i, resolutionX);
            //            patternData[i] = 0;
            //            randomData[i] = 0;
            //        }
            //    }
            //}

            // from randomData to patternData and draw the picture
            int j = 0;
            for (int i = 0; i < resolutionX; i++)
            {
                j = 2 * i;

                if (randomData[i] % 2 == 1)
                {
                    g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.White), j, 0, j, resolutionY);
                    g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.White), j + 1, 0, j + 1, resolutionY);
                    patternData[i] = 1;
                }
                else
                {
                    g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black), j, 0, j, resolutionY);
                    g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black), j + 1, 0, j + 1, resolutionY);
                    patternData[i] = 0;
                }
            }

            if (runOnce == 0)
                GenerateHashTable(patternData);
            runOnce = 1;

            // Requirement 2: every window contain at least one run of length exactly one.
            // It does not influence the result of requirement 1.
            //for (int i = 0; i < resolutionX; i++)
            //{
            //    // Step 1: sweep within one window: loop to check
            //    if (i <= (resolutionX - windowSize))
            //    {
            //        for (int j = 0; j < (windowSize - consecutiveBits + 1); j++)
            //        {
            //            if (patternData[i + j] + patternData[i + j + 1] + patternData[i + j + 2] == 3 || patternData[i + j] + patternData[i + j + 1] + patternData[i + j + 2] == 0)
            //            {
            //                flagMatch111or000 = 1;
            //                break;
            //            }
            //            else
            //                flagMatch111or000 = 0;
            //        }
            //    }

            //    // Step 2: after sweeping, if no 111 or 000 pattern found, then make it
            //    if (flagMatch111or000 == 0)
            //    {
            //        if (patternData[i + windowSize - consecutiveBits - 1] == 1)
            //        {
            //            int j;
            //            for (j = 0; j <= (consecutiveBits - 1); j++)
            //                patternData[i + windowSize - consecutiveBits + j] = 0;
            //            patternData[i + windowSize] = 1;
            //        }
            //        else
            //        {
            //            int j;
            //            for (j = 0; j <= (consecutiveBits - 1); j++)
            //                patternData[i + windowSize - consecutiveBits + j] = 1;
            //            patternData[i + windowSize] = 0;
            //        }

            //        flagMatch111or000 = 1;
            //    }

            //    // Step 3: jump to next window for sweeping, notice that next window has some overlap with current window
            //    i = i + windowSize - consecutiveBits;
            //}

            // Requirement 3: the bit-patterns of different windows differ in at least two places, to ensure that 
            // single bit-flips caused by noise could not result in an incorrect identification.
            // For real random seed, this requirement can be easily fulfilled
            int diffCount = 0;

            for (int i = 0; i <= resolutionX - windowSize; i++)
            {
                // prepare the window "k" (base is "i") to be compared with all the other windows
                for (int k = 0; k < windowSize; k++)
                    toBeCompared[k] = patternData[i + k];

                // prepare another window "n" (base is "m") and compare it with window "k"
                for (int m = 0; m <= (resolutionX - windowSize); m++)
                {
                    if (m != i)
                    {
                        for (int n = 0; n < windowSize; n++)
                        {
                            if (toBeCompared[n] != patternData[m + n])
                                diffCount++;
                        }
                        if (diffCount < 2) // if no diffrence, continue to regenerate
                        {
                            ReceiveText("Requirement 3 is not fulfilled! i = " + i + " m= " + m + " diffCount = " + diffCount, true);
                            goto GenerateBarLoop;
                            //return;
                        }
                        else
                            diffCount = 0;
                    }
                }
            }

            g.Save();
            g.Dispose();
            bitmap.Save("BarCode.png", ImageFormat.Png);

            File.WriteAllBytes(PATH, patternData);

            ReceiveText("The bar code is generated successfully!", true);
        }

        private void GenerateHashTable(byte[] inputData)
        {
            // 1st /2 is for X-Y; 2nd /2 half of inputData[] is empty
            int arraySizeMax = RECV_DATA_COUNT / 2 / 2 / stepBegin + 1;
            //int arraySizeMin = RECV_DATA_COUNT / 2 / 2 / stepEnd - 1;
            int arraySizeMin = RECV_DATA_COUNT / 2 / 2 / stepEnd - 1;
            string hash;

            for (int arraySize = arraySizeMin; arraySize <= arraySizeMax; arraySize++)
            {
                for (int axisValue = 0; axisValue < resolutionX - arraySize + 1; axisValue++)
                {
                    byte[] partialPattern = new byte[arraySize];
                    for (int j = 0; j < arraySize; j++)
                    {
                        partialPattern[j] = inputData[axisValue + j];
                    }

                    using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
                    {
                        hash = Convert.ToBase64String(sha1.ComputeHash(partialPattern));
                        patternAxis.Add(hash, axisValue);
                    }
                }
            }
        }

        private void randomDataFilled(Random rand)
        {
            Console.WriteLine();
            rand.NextBytes(randomData);
        }

        private void Guithread()
        {
            ReceiveTextEvent += this.ShowText;
        }

        public delegate void ReceiveTextHandler(string text, bool showIt);
        public event ReceiveTextHandler ReceiveTextEvent;   // Tom: 去掉event效果一样
        private void ReceiveText(string text, bool showIt)
        {
            if (ReceiveTextEvent != null)
            {
                ReceiveTextEvent(text, showIt);
            }
        }

        // ShowTextHandler is a delegate class/type
        public delegate void ShowTextHandler(string text, bool showIt);
        ShowTextHandler setText;

        private void ShowText(string text, bool showIt)
        {
            if (System.Threading.Thread.CurrentThread != textBox.Dispatcher.Thread)
            {
                if (setText == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    setText = new ShowTextHandler(ShowText);
                }

                object[] myArray = new object[2];
                myArray[0] = text;
                myArray[1] = showIt;
                textBox.Dispatcher.Invoke(setText, DispatcherPriority.Normal, myArray);
            }
            else
            {
                if (showIt)
                {
                    textBox.AppendText(text + " ");
                    // Set some limitation, otherwise the program needs to refresh all the old data (accumulated) and cause performance down
                    if (textBox.LineCount > 45)
                        textBox.Clear();
                }
            }
        }

        private void Socketthread()
        {
            Thread th = new Thread(new ThreadStart(SocketListen));
            th.Start();
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

            StartSocketListen(port, ip);
        }

        // Tom Xue: to show how many client windows/connections are alive
        // 主要功能：接收消息，发还消息
        public void StartSocketListen(int PORT, string HOST)
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

                ReceiveText("启动Socket监听...", flagShow);

                do
                {
                    //为新建连接创建新的Socket，阻塞在此
                    Socket connectionSocket = s.Accept();

                    ReceiveText("客户端[" + connectionSocket.RemoteEndPoint.ToString() + "]连接已建立...", flagShow);
                    ReceiveText(Environment.NewLine, flagShow);

                    HandleSensorData(connectionSocket);
                } while (false);
            }
            catch (ArgumentNullException ex1)
            {
                ReceiveText("ArgumentNullException:" + ex1, flagShow);
            }
            catch (SocketException ex2)
            {
                ReceiveText("SocketException:" + ex2, flagShow);
            }
        }

        public void HandleSensorData(Socket socket)
        {
            Stopwatch swLoop = new Stopwatch();

            while (true)
            {
                bytes = new byte[RECV_DATA_COUNT];

                // Key parameter, to adjust it properly will improve the performance

                //等待接收消息
                Stopwatch sw = new Stopwatch();
                sw.Start();
                bytesRec = socket.Receive(bytes);
                sw.Stop();
                ReceiveText("Socket spends time: " + sw.Elapsed.TotalMilliseconds + "ms, bytesRec = " + bytesRec + "\r\n", flagShow);

                if (bytesRec == 0)
                {
                    ReceiveText("客户端[" + socket.RemoteEndPoint.ToString() + "]连接关闭...\r\n", flagShow);
                    break;
                }
                else if (bytesRec == RECV_DATA_COUNT)
                {
                    counterOfGood++;
                    ReceiveText("\r\n The received data count is: " + bytesRec + " Good data = " + counterOfGood + " Bad data = " + counterOfBad + "\r\n", flagShow);
                }
                else
                {
                    counterOfBad++;
                    ReceiveText("The received data count is: " + bytesRec + " Good data = " + counterOfGood + " Bad data = " + counterOfBad + "\r\n", flagShow);
                }

                // calculate the data rate every 10 times of receiving data
                if (counterOfGood % 10 == 1)
                {
                    swLoop.Reset();
                    swLoop.Start();
                }
                else if (counterOfGood % 10 == 9)
                {
                    swLoop.Stop();
                    if (swLoop.Elapsed.TotalMilliseconds > 1)   // the time of one sample is usually more than 1ms
                        ReceiveText("\r\n The good data rate is: " + ((9 - 1) * 1000 / swLoop.Elapsed.TotalMilliseconds) + " number/sec \r\n", flagShow);
                }

                if (bytes != null)
                {
                    sw.Reset();
                    sw.Start();

                    ShowRawData(X);   // X_axis
                    ShowRawData(Y);   // Y_axis

                    bytes = null;

                    sw.Stop();
                    ReceiveText("GUI spends time: " + sw.Elapsed.TotalMilliseconds + "ms \r\n", flagShow);

                    sw.Reset();
                    sw.Start();

                    Stepwized(X);
                    Stepwized(Y);

                    sw.Stop();
                    ReceiveText("-------------Stepwized spends time: " + sw.Elapsed.TotalMilliseconds + "ms \r\n", flagShow);

                    Thread.Sleep(5);   // Give other app running on OS some time to be executed, otherwise will cause busy.
                }
            }
        }

        private void ShowRawData(bool X_axis)
        {
            if (X_axis == true)   // X_axis
                ReceiveText("---X axis raw data---\r\n", flagShow);
            else
                ReceiveText("---Y axis raw data---\r\n", flagShow);

            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i = i + 2)
            {
                rx16[i] = bytes[i];
                rx16[i] = rx16[i] << 8 | bytes[i + 1];
                rx16[i] = rx16[i] & 0x1fff;
                rx16[i] = rx16[i] >> 2;
                sum += rx16[i];
                count++;
                ReceiveText(Convert.ToString(rx16[i]), flagShow);

                if (i % 64 == 0)
                    ReceiveText(Environment.NewLine, flagShow);
            }

            avg = sum / count;
            if (X_axis == true)
                avgX = avg;
            else
                avgY = avg;

            ReceiveText("---The average value of the axis is " + avg + "\r\n\r\n", flagShow);

            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i = i + 2)
            {
                if (rx16[i] >= avg)
                    rx16[i] = 1;
                else
                    rx16[i] = 0;

                ReceiveText(Convert.ToString(rx16[i]), flagShow);

                if (i % 64 == 0)
                    ReceiveText("\r\n", flagShow);
            }

            ReceiveText("\r\n", flagShow);
        }

        private void Stepwized(bool X_axis)
        {
            int offset = 0;
            int searchRet = 0;
            int sum = 0;
            double sum2 = 0;
            int currentWindowIndex;  // means the pixel number of light source's window
            int argNum, argNum2;
            const int jitter = 3;

            // search starts from (lastStepSize - 3), then the workload can be reduced a lot comparing with start from (stepBegin * steps)
            for (int stepSize = lastStepSize - jitter; stepSize <= stepEnd * steps + 1; stepSize++)
            {
                argNum = stepSizeToItemNum(stepSize);
                argNum2 = argNum - 1000;

                switch (argNum)
                {
                    // integral steps
                    case 2: // e.g. currentStep == 2
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        currentWindowIndex = 0;

                        for (offset = 0; offset < 2 * argNum; offset += 2)
                        {
                            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i + 2 * (argNum - 1) + offset < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i += 2 * argNum)
                            {
                                for (int n = 0; n < argNum; n++)
                                    sum += rx16[i + 2 * n + offset];

                                if (sum >= thresholdCal(argNum))
                                    stepwisedDigitalValue[currentWindowIndex] = 1;
                                else
                                    stepwisedDigitalValue[currentWindowIndex] = 0;

                                sum = 0;
                                currentWindowIndex++;
                            }
                            searchRet = searchPattern(stepwisedDigitalValue, currentWindowIndex);
                            currentWindowIndex = 0;
                            if (searchRet == 0)
                            {
                                if (diff(lastStepSize, stepSize) < 10)
                                    lastStepSize = stepSize;
                                ReceiveText("\r\n argNum = " + argNum + "\t  stepSize = " + stepSize + "\t max stepSize = " + stepEnd * steps, true);
                                return;
                            }
                        }
                        break;
                    // fractional step
                    case 1003:
                    case 1004:
                    case 1005:
                    case 1006:
                    case 1007:
                    case 1008:
                        currentWindowIndex = 0;

                        for (offset = 0; offset < 2 * argNum2; offset += 2)
                        {
                            for (int i = ((X_axis == true) ? (bytesRec / 2) : 0); i + 2 * (argNum2 + 1) + offset < ((X_axis == true) ? bytesRec : (bytesRec / 2)); i += 2 * (argNum2 - 1))
                            {
                                switch (stepSize % steps)
                                {
                                    case 1:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                                sum2 += 1.0 * rx16[i + offset] + 0.1 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                                sum2 += 0.9 * rx16[i + offset] + 0.2 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 2:
                                                sum2 += 0.8 * rx16[i + offset] + 0.3 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 3:
                                                sum2 += 0.7 * rx16[i + offset] + 0.4 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 4:
                                                sum2 += 0.6 * rx16[i + offset] + 0.5 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 5:
                                                sum2 += 0.5 * rx16[i + offset] + 0.6 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 6:
                                                sum2 += 0.4 * rx16[i + offset] + 0.7 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 7:
                                                sum2 += 0.3 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 8:
                                                sum2 += 0.2 * rx16[i + offset] + 0.9 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 9:
                                                sum2 += 0.1 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 2:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                            case 5:
                                                sum2 += 1.0 * rx16[i + offset] + 0.2 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                            case 6:
                                                sum2 += 0.8 * rx16[i + offset] + 0.4 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 2:
                                            case 7:
                                                sum2 += 0.6 * rx16[i + offset] + 0.6 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 3:
                                            case 8:
                                                sum2 += 0.4 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 4:
                                            case 9:
                                                sum2 += 0.2 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 3:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                                sum2 += 1.0 * rx16[i + offset] + 0.3 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                                sum2 += 0.7 * rx16[i + offset] + 0.6 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 2:
                                                sum2 += 0.4 * rx16[i + offset] + 0.9 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 3:
                                                sum2 += 0.1 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.2 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 4:
                                                sum2 += 0.8 * rx16[i + offset] + 0.5 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 5:
                                                sum2 += 0.5 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 6:
                                                sum2 += 0.2 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.1 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 7:
                                                sum2 += 0.9 * rx16[i + offset] + 0.4 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 8:
                                                sum2 += 0.6 * rx16[i + offset] + 0.7 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 9:
                                                sum2 += 0.3 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 4:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                            case 5:
                                                sum2 += 1.0 * rx16[i + offset] + 0.4 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                            case 6:
                                                sum2 += 0.6 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 2:
                                            case 7:
                                                sum2 += 0.2 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.2 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 3:
                                            case 8:
                                                sum2 += 0.8 * rx16[i + offset] + 0.6 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 4:
                                            case 9:
                                                sum2 += 0.4 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 5:
                                        switch (currentWindowIndex % 2)
                                        {
                                            case 0:
                                                sum2 += 1.0 * rx16[i + offset] + 0.5 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                                sum2 += 0.5 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 6:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                            case 5:
                                                sum2 += 1.0 * rx16[i + offset] + 0.6 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                            case 6:
                                                sum2 += 0.4 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.2 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 2:
                                            case 7:
                                                sum2 += 0.8 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 3:
                                            case 8:
                                                sum2 += 0.2 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.4 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 4:
                                            case 9:
                                                sum2 += 0.6 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 7:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                                sum2 += 1.0 * rx16[i + offset] + 0.7 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                                sum2 += 0.3 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.4 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 2:
                                                sum2 += 0.6 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.1 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 3:
                                                sum2 += 0.9 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 4:
                                                sum2 += 0.2 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.5 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 5:
                                                sum2 += 0.5 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.2 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 6:
                                                sum2 += 0.8 * rx16[i + offset] + 0.9 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 7:
                                                sum2 += 0.1 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.6 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 8:
                                                sum2 += 0.4 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.3 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 9:
                                                sum2 += 0.7 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 8:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                            case 5:
                                                sum2 += 1.0 * rx16[i + offset] + 0.8 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                            case 6:
                                                sum2 += 0.2 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.6 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 2:
                                            case 7:
                                                sum2 += 0.4 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.4 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 3:
                                            case 8:
                                                sum2 += 0.6 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.2 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 4:
                                            case 9:
                                                sum2 += 0.8 * rx16[i + offset] + 1.0 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                    case 9:
                                        switch (currentWindowIndex % 10)
                                        {
                                            case 0:
                                                sum2 += 1.0 * rx16[i + offset] + 0.9 * rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                break;
                                            case 1:
                                                sum2 += 0.1 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.8 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 2:
                                                sum2 += 0.2 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.7 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 3:
                                                sum2 += 0.3 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.6 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 4:
                                                sum2 += 0.4 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.5 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 5:
                                                sum2 += 0.5 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.4 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 6:
                                                sum2 += 0.6 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.3 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 7:
                                                sum2 += 0.7 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.2 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 8:
                                                sum2 += 0.8 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset] + 0.1 * rx16[i + 2 * argNum2 + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                            case 9:
                                                sum2 += 0.9 * rx16[i + offset] + rx16[i + 2 * (argNum2 - 1) + offset];
                                                for (int j = 1; j < argNum2 - 1; j++)
                                                    sum2 += rx16[i + 2 * j + offset];
                                                i += 2;
                                                break;
                                        }
                                        break;
                                }

                                if (sum2 > (float)stepSize / (2 * steps))
                                    stepwisedDigitalValue[currentWindowIndex] = 1;
                                else
                                    stepwisedDigitalValue[currentWindowIndex] = 0;

                                sum2 = 0;
                                currentWindowIndex++;
                            }
                            searchRet = searchPattern(stepwisedDigitalValue, currentWindowIndex);
                            currentWindowIndex = 0;
                            if (searchRet == 0)
                            {
                                if (diff(lastStepSize, stepSize) < jitter)
                                    lastStepSize = stepSize;
                                ReceiveText("\r\n argNum = " + argNum + "\t  stepSize = " + stepSize + "\t max stepSize = " + stepEnd * steps, true);
                                return;
                            }
                        }
                        break;
                    case 2000:  // search from (lastStepSize-jitter) to stepEnd * steps, and if no match, then go back to stepBegin * steps
                        lastStepSize = stepBegin * steps;
                        break;
                    default:
                        break;
                }
            }
        }

        private int stepSizeToItemNum(int sS)
        {
            // integral step, e.g. 2, 3, 4, 5, 6, 7, 8
            for (int j = 0; j < (stepEnd - stepBegin + 1); j++) // j < (8 - 2 + 1) etc. j < 7
            {
                if (sS == (stepBegin + j) * steps)
                    return stepBegin + j;
            }

            // +1000 is for fractional step, to differentiate from integral step
            for (int j = 0; j < (stepEnd - stepBegin); j++)     // j < (8 - 2) etc. j < 6
            {
                if (sS > (stepBegin + j) * steps && sS < (stepBegin + j + 1) * steps)
                    return 1000 + stepBegin + j + 1;            // e.g. 2+3/7 steps will return 1003
            }

            if (sS > stepEnd * steps) // e.g.  stepSize == stepEnd * steps + 1
                return 2000;
            return 0;
        }

        private int thresholdCal(int n)
        {
            return (n / 2 + 1);
        }

        private int diff(int a, int b)
        {
            if (a >= b)
                return a - b;
            else
                return b - a;
        }

        private int filterLastNValues(int xyValue)
        {
            switch (seqCount % 3)
            {
                case 0:
                    seq_a = xyValue;
                    break;
                case 1:
                    seq_b = xyValue;
                    break;
                case 2:
                    seq_c = xyValue;
                    break;
            }
            seqCount++;

            if (diff(seq_a, seq_b) < 10 && diff(seq_a, seq_c) > 10 && diff(seq_b, seq_c) > 10)
                return seq_b;
            else if (diff(seq_a, seq_b) > 10 && diff(seq_a, seq_c) < 10 && diff(seq_b, seq_c) > 10)
                return seq_a;
            else if (diff(seq_a, seq_b) > 10 && diff(seq_a, seq_c) > 10 && diff(seq_b, seq_c) < 10)
                return seq_c;

            return xyValue;
        }

        private int searchPattern(byte[] fromArray, int length)
        {
            string hash;

            byte[] windowToBeSearched = new byte[length];
            Array.ConstrainedCopy(fromArray, 0, windowToBeSearched, 0, length);

            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                hash = Convert.ToBase64String(sha1.ComputeHash(windowToBeSearched));
            }

            if (patternAxis.TryGetValue(hash, out coordinateValue))
            {
                showWin.Xvalue = filterLastNValues(coordinateValue);
                showWin.UIshow();

                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}