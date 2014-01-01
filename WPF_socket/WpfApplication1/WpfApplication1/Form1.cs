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

            timer2.Interval = 1;
            timer2.Tick += new EventHandler(timer2_Tick);
            timer2.Start();
        }

        int circleDiameter = 10;
        public int x_raw = 0;
        public int y_raw = 0;
        int x_screen = 0;
        int y_screen = 0;

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            x_screen = x_raw * (this.Width) / 640;
            y_screen = y_raw * (this.Height) / 640;
            e.Graphics.DrawEllipse(Pens.Red, x_screen, y_screen, circleDiameter, circleDiameter);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            Invalidate();
            this.Refresh();
        }
    }
}
