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

        const int circleDiameter = 10;
        public int x_pixel = 0;
        public int y_pixel = 0;
        public double len_pixel = 0;
        double x_screen = 0;
        double y_screen = 0;
        // Create solid brush.
        SolidBrush redBrush = new SolidBrush(Color.Red);
        // Create location and size of ellipse.
        int width = circleDiameter;
        int height = circleDiameter;

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            //x_screen = x_pixel * (this.Width) / 640;
            //y_screen = y_pixel * (this.Height) / 640;
            try
            {
                // Calibration:
                x_screen = (x_pixel - 640 / 2) * (1920 * 2.3 / len_pixel);  // coef 2.3 should be calibrated based on measurement
                y_screen = (y_pixel - 640 / 2) * (1080 * 2.3 / len_pixel);  // my display's resolution is set as 1920*1080
            }
            catch (Exception e2)
            {
                //处理除零错误
            }

            // Fill ellipse on screen.
            e.Graphics.FillEllipse(redBrush, (int)x_screen, (int)y_screen, width, height);
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
