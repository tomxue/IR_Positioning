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
        private PictureBox pictureBox1 = new PictureBox();

        public Window1()
        {
            InitializeComponent();

            Console.WriteLine("ShowForm coordinate = " + xValue);
            // Dock the PictureBox to the form and set its background to white.
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.BackColor = System.Drawing.Color.Yellow;
            // Connect the Paint event of the PictureBox to the event handler method.
            pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);

            // Add the PictureBox control to the Form. 
            //this.Controls.Add(pictureBox1);

            ReceiveTextEvent += this.ShowText;
            //while (true)
            //{
            //    paintPoint();
            //    Thread.Sleep(2000);
            //}
            UIshow();
        }

        private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // Create a local version of the graphics object for the PictureBox.
            Graphics g;

            g = e.Graphics;

            // Draw a line in the PictureBox.
            g.DrawEllipse(System.Drawing.Pens.Red, xValue, 200,
                20, 20);
        }

        public void UIshow()
        {
            // Draw a line in the PictureBox.
            //g.DrawEllipse(System.Drawing.Pens.Red, xValue, 200,
            //    20, 20);

            ReceiveText(Convert.ToString(xValue), true);
        }

        public void setter(int val)
        {
            xValue = val;
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
            if (System.Threading.Thread.CurrentThread != textBox3.Dispatcher.Thread)
            {
                if (setText == null)
                {
                    // Tom Xue: Delegates are used to pass methods as arguments to other methods.
                    // ShowTextHandler.ShowTextHandler(void (string) target)
                    setText = new ShowTextHandler(ShowText);
                }

                object[] myArray = new object[2];
                myArray[0] = text;
                myArray[1] = showIt;
                //textBox.Dispatcher.BeginInvoke(setText, DispatcherPriority.Normal, myArray);
                textBox3.Dispatcher.Invoke(setText, DispatcherPriority.Normal, myArray);
            }
            else
            {
                if (showIt)
                {
                    textBox3.AppendText(text + " ");
                    textBox3.ScrollToEnd();
                    // Set some limitation, otherwise the program needs to refresh all the old data (accumulated) and cause performance down
                    if (textBox3.LineCount > 500)
                        textBox3.Clear();
                }
            }
        }
    }
}
