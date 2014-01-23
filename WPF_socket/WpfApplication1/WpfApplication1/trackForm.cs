using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WpfApplication1
{
    public partial class trackForm : Form
    {
        Recognizer.Dollar.Geometric.MainForm unistrokeForm = new Recognizer.Dollar.Geometric.MainForm();
        //WMPLib.WindowsMediaPlayer player = new WMPLib.WindowsMediaPlayer();
        musicplay mpl = new musicplay("applause1.wav");

        public trackForm()
        {
            InitializeComponent();

            unistrokeForm.Show();
            unistrokeForm._rec.LoadGesture("1.xml");

            timer2.Interval = 50;
            timer2.Tick += new EventHandler(timer2_Tick);
            timer2.Start();
            g = this.CreateGraphics();
            g.Clear(Color.Transparent);
            this.BackColor = Color.WhiteSmoke;
            this.TransparencyKey = Color.WhiteSmoke;

            //player.URL = @"applause1.wav";
            //player.uiMode = "None";
            //player.settings.volume = 100;
            //player.settings.playCount = 1;
        }

        const int circleDiameter = 10;
        const int xy_distance = 100;
        public int x_pixel = 0;
        public int y_pixel = 0;
        public double len_pixel = 0;
        double x_screen = 0;
        double y_screen = 0;
        public bool winlenMatched = false;
        int listIndex = 0;

        // Create solid brush.
        SolidBrush redBrush = new SolidBrush(Color.Red);
        // Create location and size of ellipse.
        int width = circleDiameter;
        int height = circleDiameter;
        Graphics g = null;
        List<Point> list = new List<Point>();

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        [System.Runtime.InteropServices.DllImport("user32")]
        //[System.Runtime.InteropServices.DllImport("wmp")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        const int MOUSEEVENTF_MOVE = 0x0001;
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        private void timer2_Tick(object sender, EventArgs e)
        {
            int _x1 = 0, _y1 = 0;
            int _x2 = 0, _y2 = 0;
            int _x3 = 0, _y3 = 0;
            double len_x1y1 = 0;
            double len_x2y2 = 0;
            double len_x3y3 = 0;

            try
            {
                if (winlenMatched)
                {
                    x_screen = (640 - x_pixel - 250) * 3.5; // (19.2 * 3.45 / len_pixel);
                    y_screen = (640 - y_pixel - 400) * 3.5; // (10.8 * 3.45 / len_pixel);
                    list.Add(new Point((int)x_screen, (int)y_screen));

                    if (listIndex >= 3)
                    {
                        _x1 = System.Math.Abs(list[listIndex - 1].X - list[listIndex].X);
                        _x2 = System.Math.Abs(list[listIndex - 2].X - list[listIndex].X);
                        _x3 = System.Math.Abs(list[listIndex - 3].X - list[listIndex].X);
                        _y1 = System.Math.Abs(list[listIndex - 1].Y - list[listIndex].Y);
                        _y2 = System.Math.Abs(list[listIndex - 2].Y - list[listIndex].Y);
                        _y3 = System.Math.Abs(list[listIndex - 3].Y - list[listIndex].Y);
                        len_x1y1 = Math.Sqrt(_x1 * _x1 + _y1 * _y1);
                        len_x2y2 = Math.Sqrt(_x2 * _x2 + _y2 * _y2);
                        len_x3y3 = Math.Sqrt(_x3 * _x3 + _y3 * _y3);

                        // 过滤条件3：先后两个点距离 < 某个值
                        if (len_x1y1 < xy_distance && len_x2y2 < 2 * xy_distance && len_x3y3 < 3 * xy_distance)
                        {
                            g.FillEllipse(redBrush, list[listIndex].X, list[listIndex].Y, width, height);
                            g.DrawLine(new Pen(Brushes.Blue), list[listIndex - 1], list[listIndex]);
                            if (listIndex == 3)
                                unistrokeForm.MainForm_dummyDown((float)list[listIndex].X, (float)list[listIndex].Y);

                            unistrokeForm.MainForm_dummyMove((float)list[listIndex].X, (float)list[listIndex].Y);
                        }
                        else
                            list.RemoveAt(list.Count - 1);
                    }

                    if (list.Count >= 80)
                    {
                        unistrokeForm.MainForm_dummyUp();
                        if (unistrokeForm._result > 0.80)
                        {
                            unistrokeForm._result = 0;
                            MessageBox.Show("Good");
                            //g.Clear(Color.Green);
                            //player.play();
                            //mpl.play();   

                            g.FillRectangle(Brushes.Yellow, list[listIndex].X, list[listIndex].Y, 100, 100);
                        }

                        list.Clear();
                        listIndex = 0;

                        Invalidate();
                    }

                    listIndex++;
                }

                //mouse_event(MOUSEEVENTF_MOVE, list[i].X, list[i].Y, 0, 0);
                //mouse_event(MOUSEEVENTF_LEFTDOWN, list[i].X, list[i].Y, 0, 0);
                //mouse_event(MOUSEEVENTF_LEFTUP, list[i].X, list[i].Y, 0, 0);

                //下面是模拟双击的  
                //mouse_event(MOUSEEVENTF_LEFTDOWN,0,0,0,0);  
                //mouse_event(MOUSEEVENTF_LEFTUP,0,0,0,0);              

                //mouse_event(MOUSEEVENTF_LEFTDOWN,0,0,0,0);  
                //mouse_event(MOUSEEVENTF_LEFTUP,0,0,0,0);       
            }
            catch (Exception e2)
            {
                //处理除零错误
            }
        }
    }

    class musicplay
    {
        private string filename;
        private string m = @"";
        private long t;

        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        private static extern long mciSendString(string lpstrCommand, string lpstrReturnString, long length, long hwndcallback);
        public musicplay(string name)
        {
            filename = name;
        }
        public void play()
        {
            t = mciSendString(@"open   " + filename, m, 0, 0);
            t = mciSendString(@"play  " + filename + @"  repeat", m, 0, 0);
        }

        public void Stop()
        {
            t = mciSendString(@"close   " + filename, m, 0, 0);
            // t= mciSendString("stop song","",0,0);
        }

    }
}
