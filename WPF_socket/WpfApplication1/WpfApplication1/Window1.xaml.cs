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

            ReceiveTextEvent += this.ShowText;
            //sliderEvent += this.ShowValue;

            UIshow();
        }

        public void UIshow()
        {
            ReceiveText(Convert.ToString(xValue), true, xValue);
            //sliderAction(xValue);
        }

        public void setter(int val)
        {
            xValue = val;
        }

        public delegate void ReceiveTextHandler(string text, bool showIt, int value);
        public event ReceiveTextHandler ReceiveTextEvent;   // Tom: 去掉event效果一样
        private void ReceiveText(string text, bool showIt, int value)
        {
            if (ReceiveTextEvent != null)
            {
                ReceiveTextEvent(text, showIt, xValue);
            }
        }

        private void ShowText(string text, bool showIt, int value)
        {
            if (System.Threading.Thread.CurrentThread != textBox3.Dispatcher.Thread)
            {
                if (setText == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    // ShowTextHandler.ShowTextHandler(void (string) target)
                    setText = new ShowTextHandler(ShowText);
                }

                object[] myArray = new object[3];
                myArray[0] = text;
                myArray[1] = showIt;
                myArray[2] = xValue;
                //textBox.Dispatcher.BeginInvoke(setText, DispatcherPriority.Normal, myArray);
                textBox3.Dispatcher.Invoke(setText, DispatcherPriority.Normal, myArray);
                slider1.Dispatcher.Invoke(setText, DispatcherPriority.Normal, myArray);
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
        public delegate void ShowTextHandler(string text, bool showIt, int value);
        ShowTextHandler setText;

        public delegate void sliderHandler(int value);
        public event sliderHandler sliderEvent;   // Tom: 去掉event效果一样
        private void sliderAction(int value)
        {
            if (sliderEvent != null)
            {
                sliderEvent(value);
            }
        }

        public delegate void ShowValueHandler(int value);
        ShowValueHandler setValue;

        private void ShowValue(int value)
        {
            if (System.Threading.Thread.CurrentThread != slider1.Dispatcher.Thread)
            {
                if (setText == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    // ShowTextHandler.ShowTextHandler(void (string) target)
                    setValue = new ShowValueHandler(ShowValue);
                }

                //textBox.Dispatcher.BeginInvoke(setText, DispatcherPriority.Normal, myArray);
                slider1.Dispatcher.Invoke(setValue, DispatcherPriority.Normal, xValue);
            }
            else
            {
                slider1.Value = xValue;
            }
        }


    }
}
