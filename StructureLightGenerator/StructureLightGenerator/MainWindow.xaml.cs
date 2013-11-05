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
            int threeOneCount = 0;      // 3 consecutive identical bits, White color for one
            int threeZeroCount = 0;     // 3 consecutive identical bits, Black color for zero
            InitializeComponent();

            Random random = new Random();
            ShowRandomNumbers(random);

            Bitmap bitmap = new Bitmap(1280, 800);  // Coolux resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);

            for (int i = 0; i < 1280; i++)
            {
                if (values[i] % 2 == 1)
                {
                    threeOneCount++;
                    threeZeroCount = 0;
                    if (threeOneCount == 3)
                    {
                        threeOneCount = 0;
                        g.DrawLine(new Pen(Color.Black), i, 0, i, 1280);
                    }
                    else
                        g.DrawLine(new Pen(Color.White), i, 0, i, 1280);
                }
                else
                {
                    threeOneCount = 0;
                    threeZeroCount++;
                    if (threeZeroCount == 3)
                    {
                        threeZeroCount = 0;
                        g.DrawLine(new Pen(Color.White), i, 0, i, 1280);
                    }
                    else
                        g.DrawLine(new Pen(Color.Black), i, 0, i, 1280);
                }
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
