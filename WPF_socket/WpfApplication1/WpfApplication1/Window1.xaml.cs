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
    public partial class Window1 : Window
    {
        int xValue = -1;
        Graphics g;

        public Window1()
        {
            InitializeComponent();

            Console.WriteLine("ShowForm coordinate = " + xValue);

            ReceiveUIparamEvent += this.UIshow;

            UIshow();
        }

        public void UIshow()
        {
            ReceiveUIparam(Convert.ToString(xValue), true, xValue);
        }

        public void setter(int val)
        {
            xValue = val;
        }

        public delegate void ReceiveUIparamHandler(string text, bool showIt, int value);
        public event ReceiveUIparamHandler ReceiveUIparamEvent;   // Tom: 去掉event效果一样
        private void ReceiveUIparam(string text, bool showIt, int value)
        {
            if (ReceiveUIparamEvent != null)
            {
                ReceiveUIparamEvent(text, showIt, xValue);
            }
        }

        private void UIshow(string text, bool showIt, int value)
        {
            if (System.Threading.Thread.CurrentThread != textBox3.Dispatcher.Thread)
            {
                if (setUI == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    // ShowTextHandler.ShowTextHandler(void (string) target)
                    setUI = new UIShowHandler(UIshow);
                }

                object[] myArray = new object[3];
                myArray[0] = text;
                myArray[1] = showIt;
                myArray[2] = xValue;
                //textBox.Dispatcher.BeginInvoke(setText, DispatcherPriority.Normal, myArray);
                textBox3.Dispatcher.Invoke(setUI, DispatcherPriority.Normal, myArray);
                slider1.Dispatcher.Invoke(setUI, DispatcherPriority.Normal, myArray);
            }
            else
            {
                if (showIt)
                {
                    slider1.Value = xValue;
                    textBox3.AppendText(text + " ");
                    textBox3.ScrollToEnd();
                    // Set some limitation, otherwise the program needs to refresh all the old data (accumulated) and cause performance down
                    if (textBox3.LineCount > 500)
                        textBox3.Clear();
                }
            }
        }

        // ShowTextHandler is a delegate class/type
        public delegate void UIShowHandler(string text, bool showIt, int value);
        UIShowHandler setUI;
    }
}
