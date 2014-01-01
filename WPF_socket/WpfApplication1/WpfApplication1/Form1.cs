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
        public int xvalue = 0;
        public int yvalue = 0;

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawEllipse(Pens.Red, xvalue*(this.Width)/640, yvalue*(this.Height)/640, circleDiameter, circleDiameter);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //e.Graphics.DrawEllipse(Pens.Red, xvalue, yvalue, circleDiameter, circleDiameter);

            Invalidate();
            this.Refresh();
        }
    }
}
