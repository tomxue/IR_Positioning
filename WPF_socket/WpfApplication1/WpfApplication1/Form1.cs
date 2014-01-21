using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WpfApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            timer2.Interval = 500;
            timer2.Tick += new EventHandler(timer2_Tick);
            timer2.Start();
            g = this.CreateGraphics();
            g.Clear(Color.Transparent);
            this.BackColor = Color.WhiteSmoke;
            this.TransparencyKey = Color.WhiteSmoke;
        }

        const int circleDiameter = 10;
        public int x_pixel = 0;
        public int y_pixel = 0;
        public double len_pixel = 0;
        double x_screen = 0;
        double y_screen = 0;
        public bool winlenMatched = false;

        // Create solid brush.
        SolidBrush redBrush = new SolidBrush(Color.Red);
        // Create location and size of ellipse.
        int width = circleDiameter;
        int height = circleDiameter;
        Graphics g = null;
        List<Point> list = new List<Point>();

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (winlenMatched)
                {
                    x_screen = (640 - x_pixel) * (19.2 * 3.45 / len_pixel);
                    y_screen = 320;// y_pixel * (this.Height) / 640;
                    list.Add(new Point((int)x_screen, (int)y_screen));
                }
                // Fill ellipse on screen.

                for (int i = 0; i < list.Count; i++)
                {
                    g.FillEllipse(redBrush, list[i].X, list[i].Y, width, height);
                    if (i > 0)
                        g.DrawLine(new Pen(Brushes.Blue), list[i - 1], list[i]);
                }

                //if (winlenMatched)
                //{
                //    x_screen = (640 - x_pixel) * (19.2 * 3.45 / len_pixel);
                //    y_screen = y_pixel * (this.Height) / 640;
                //    g.FillEllipse(redBrush, (int)x_screen, (int)y_screen, width, height);
                //    //g.DrawLine(
                //}
            }
            catch (Exception e2)
            {
                //处理除零错误
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            Invalidate();
            //this.Refresh();
        }
    }
}
