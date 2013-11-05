using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace StructureLightGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        byte[] values = new byte[1280];

        public MainWindow()
        {
            InitializeComponent();

            Random random = new Random();
            ShowRandomNumbers(random);

            Bitmap bitmap = new Bitmap(1280, 800);  // Coolux resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);
            for (int i = 0; i < 1280; i++)
            {
                if(values[i] %2 == 1)
                    g.DrawLine(new Pen(Color.White), i, 0, i, 1280);

                //g.DrawLine(new Pen(Color.White), 0, 0, 0, 800);
                //g.DrawLine(new Pen(Color.White), 2, 0, 2, 800);
                //g.DrawLine(new Pen(Color.White), 10, 0, 10, 800);
            }
            g.Save();
            g.Dispose();
            //bitmap.MakeTransparent(Color.Red);
            bitmap.Save("dd.png", ImageFormat.Png);
        }

        private void ShowRandomNumbers(Random rand)
        {
            Console.WriteLine();
            rand.NextBytes(values);
            foreach (var value in values)
                Console.Write("{0, 5}", value);

            Console.WriteLine();
        }
    }
}
