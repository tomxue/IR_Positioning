using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ShowWindow : Window
    {
        private int xvalue = -1;
        private int yvalue = -1;
        //Graphics g;

        public ShowWindow()
        {
            InitializeComponent();

            Console.WriteLine("ShowForm X coordinate = " + xvalue);
            Console.WriteLine("ShowForm Y coordinate = " + yvalue);

            ReceiveUIparamEvent += this.UIshow;

            UIShow();
        }

        public int Xvalue
        {
            get { return xvalue; }
            set { xvalue = value; }
        }

        public int Yvalue
        {
            get { return yvalue; }
            set { yvalue = value; }
        }

        public void UIShow()
        {
            ReceiveUIparam("what to say", true, xvalue, yvalue);
        }

        public delegate void ReceiveUIparamHandler(string text, bool showIt, int x, int y);
        public event ReceiveUIparamHandler ReceiveUIparamEvent;   // Tom: 去掉event效果一样
        private void ReceiveUIparam(string text, bool showIt, int x, int y)
        {
            if (ReceiveUIparamEvent != null)
            {
                ReceiveUIparamEvent(text, showIt, x, y);
            }
        }

        private void UIshow(string text, bool showIt, int x, int y)
        {
            if (System.Threading.Thread.CurrentThread != textBox3.Dispatcher.Thread)
            {
                if (setUI == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    setUI = new UIShowHandler(UIshow);
                }

                object[] myArray = new object[4];
                myArray[0] = text;
                myArray[1] = showIt;
                myArray[2] = x;
                myArray[3] = y;
                //textBox3.Dispatcher.Invoke(setUI, DispatcherPriority.Normal, myArray);
                slider1.Dispatcher.Invoke(setUI, DispatcherPriority.Normal, myArray);
            }
            else
            {
                if (showIt)
                {
                    slider1.Value = x;
                    slider2.Value = 640 - y;
                    //textBox3.AppendText("x = " + x + " y = " + y + "\r\n");
                    //textBox3.ScrollToEnd();
                }
            }
        }

        // ShowTextHandler is a delegate class/type
        public delegate void UIShowHandler(string text, bool showIt, int xvalue, int yvalue);
        UIShowHandler setUI;
    }
}
