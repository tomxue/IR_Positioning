using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

namespace WpfApplication3
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort1;

        delegate void HandleInterfaceUpdateDelagate(string text);//委托；此为重点
        HandleInterfaceUpdateDelagate interfaceUpdateHandle;
        public MainWindow()
        {
            this.InitializeComponent();

            // 在此点下面插入创建对象所需的代码。
        }

        private void btnSart_Click(object sender, RoutedEventArgs e)
        {
            //实例化串口对象(默认：COMM1,9600,e,8,1) 
            serialPort1 = new SerialPort();
            //更改参数
            serialPort1.PortName = textBoxCOM.Text;
            serialPort1.BaudRate = 115200;
            serialPort1.Parity = Parity.None;
            serialPort1.StopBits = StopBits.One;

            //上述步骤可以用在实例化时调用SerialPort类的重载构造函数
            //SerialPort serialPort = new SerialPort("COM1", 19200, Parity.Odd, StopBits.Two);

            //打开串口(打开串口后不能修改端口名,波特率等参数,修改参数要在串口关闭后修改)
            if (!serialPort1.IsOpen)
            {
                serialPort1.Open();
            }
            else
                MessageBox.Show("Port is already open!");

            //开启接收数据线程
            ReceiveData(serialPort1);
        }

        /// <summary>
        /// 发送按钮的单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            //用字节的形式发送数据
            SendBytesData(serialPort1);
        }

        //发送二进制数据
        private void SendBytesData(SerialPort serialPortobj)
        {
            SerialPort serialPort = (SerialPort)serialPortobj;
            byte[] bytesSend = null;

            try
            {
                Action action = delegate()
                {
                    // do stuff to UI
                    bytesSend = System.Text.Encoding.Default.GetBytes(txtBoxSend.Text);
                };
                this.txtBoxSend.Dispatcher.Invoke(DispatcherPriority.Normal, action);

                serialPort.Write(bytesSend, 0, bytesSend.Length);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 开启接收数据线程
        /// </summary>
        private void ReceiveData(SerialPort serialPort)
        {
            //同步阻塞接收数据线程
            Thread threadReceive = new Thread(new ParameterizedThreadStart(SynReceiveData));
            threadReceive.Start(serialPort);

            //也可用异步接收数据线程
            //Thread threadReceiveSub = new Thread(new ParameterizedThreadStart(AsyReceiveData));
            //threadReceiveSub.Start(serialPort);
        }

        //同步阻塞读取
        private void SynReceiveData(object serialPortobj)
        {

            SerialPort serialPort = (SerialPort)serialPortobj;
            Thread.Sleep(0);
            serialPort.ReadTimeout = 1000;
            while (true)
            {
                try
                {
                    int n = serialPort.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致  
                    byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据  
                    //received_count += n;//增加接收计数  
                    serialPort.Read(buf, 0, n);//读取缓冲数据  
                    //因为要访问ui资源，所以需要使用invoke方式同步ui
                    interfaceUpdateHandle = new HandleInterfaceUpdateDelagate(UpdateTextBox);//实例化委托对象
                    Dispatcher.Invoke(interfaceUpdateHandle, new string[] { Encoding.ASCII.GetString(buf) });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    //处理超时错误
                }
                // Give other threads some time to be executed
                Thread.Sleep(1);
            }

            serialPort.Close();
        }

        private void UpdateTextBox(string text)
        {
            if (System.Threading.Thread.CurrentThread != txtBoxReceive.Dispatcher.Thread)
            {
                if (interfaceUpdateHandle == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    interfaceUpdateHandle = new HandleInterfaceUpdateDelagate(UpdateTextBox);
                }

                object[] myArray = new object[1];
                myArray[0] = text;
                txtBoxReceive.Dispatcher.Invoke(interfaceUpdateHandle, DispatcherPriority.Normal, myArray);
            }
            else
            {
                txtBoxReceive.AppendText(text);
                // Set some limitation, otherwise the program needs to refresh all the old data (accumulated) and cause performance down
                if (txtBoxReceive.LineCount > 2560)
                    txtBoxReceive.Clear();
            }
        }

        //异步读取
        private void AsyReceiveData(object serialPortobj)
        {
            SerialPort serialPort = (SerialPort)serialPortobj;
            Thread.Sleep(500);
            try
            {
                int n = serialPort.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致  
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据  
                //received_count += n;//增加接收计数  
                serialPort.Read(buf, 0, n);//读取缓冲数据  
                //因为要访问ui资源，所以需要使用invoke方式同步ui。
                interfaceUpdateHandle = new HandleInterfaceUpdateDelagate(UpdateTextBox);//实例化委托对象
                Dispatcher.BeginInvoke(interfaceUpdateHandle, new string[] { Encoding.ASCII.GetString(buf) });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                //处理错误
            }
            serialPort.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}