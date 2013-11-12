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
    public partial class showWindow : Window
    {
        private int xvalue = -1;
        Graphics g;


        public showWindow()
        {
            InitializeComponent();

            Console.WriteLine("ShowForm coordinate = " + xvalue);

            ReceiveUIparamEvent += this.UIshow;

            UIshow();
        }

        public int Xvalue
        {
            get { return xvalue; }
            set { xvalue = value; }
        }

        public void UIshow()
        {
            ReceiveUIparam(Convert.ToString(xvalue), true, xvalue);
        }

        public delegate void ReceiveUIparamHandler(string text, bool showIt, int value);
        public event ReceiveUIparamHandler ReceiveUIparamEvent;   // Tom: 去掉event效果一样
        private void ReceiveUIparam(string text, bool showIt, int value)
        {
            if (ReceiveUIparamEvent != null)
            {
                ReceiveUIparamEvent(text, showIt, xvalue);
            }
        }

        private void UIshow(string text, bool showIt, int value)
        {
            if (System.Threading.Thread.CurrentThread != textBox3.Dispatcher.Thread)
            {
                if (setUI == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    setUI = new UIShowHandler(UIshow);
                }

                object[] myArray = new object[3];
                myArray[0] = text;
                myArray[1] = showIt;
                myArray[2] = xvalue;
                //textBox3.Dispatcher.Invoke(setUI, DispatcherPriority.Normal, myArray);
                slider1.Dispatcher.Invoke(setUI, DispatcherPriority.Normal, myArray);
            }
            else
            {
                if (showIt)
                {
                    slider1.Value = xvalue;
                    //textBox3.AppendText(text + " ");
                    //textBox3.ScrollToEnd();
                }
            }
        }

        // ShowTextHandler is a delegate class/type
        public delegate void UIShowHandler(string text, bool showIt, int value);
        UIShowHandler setUI;
    }
}
