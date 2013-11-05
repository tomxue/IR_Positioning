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
        byte[] randomValues = new byte[1280];
        byte[] patternValue = new byte[1280];
        byte[] match111 = new byte[1280];
        byte[] match000 = new byte[1280];
        byte[] match111Or000 = new byte[1280];

        public MainWindow()
        {
            int CountOf111 = 0;     // 3 consecutive identical bits, White color for one
            int CountOf000 = 0;     // 3 consecutive identical bits, Black color for zero
            int CountAftermatch111or000 = 0;

            InitializeComponent();

            Random random = new Random();
            ShowRandomNumbers(random);  // generate all the 1280 random numbers at this point

            Bitmap bitmap = new Bitmap(1280, 800);  // Coolux DLP projector's resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);

            for (int i = 0; i < 1280; i++)
            {
                if (randomValues[i] % 2 == 1)
                {
                    CountOf111++;
                    CountOf000 = 0;
                    // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three
                    if (CountOf111 == 3)
                    {
                        CountOf111 = 0;
                        g.DrawLine(new Pen(Color.Black), i, 0, i, 1280);
                        patternValue[i] = 0;

                        match111[i] = 1;
                        match111Or000[i] = 1;
                        CountAftermatch111or000 = 0; // reset the counter
                    }
                    else
                    {
                        g.DrawLine(new Pen(Color.White), i, 0, i, 1280);
                        patternValue[i] = 1;
                    }
                }
                else
                {
                    CountOf111 = 0;
                    CountOf000++;
                    // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three
                    if (CountOf000 == 3)
                    {
                        CountOf000 = 0;
                        g.DrawLine(new Pen(Color.White), i, 0, i, 1280);
                        patternValue[i] = 1;

                        match000[i] = 1;
                        match111Or000[i] = 1;
                        CountAftermatch111or000 = 0; // reset the counter
                    }
                    else
                    {
                        g.DrawLine(new Pen(Color.Black), i, 0, i, 1280);
                        patternValue[i] = 0;
                    }
                }

                // Requirement2: every window contain at least one run of length exactly one
                CountAftermatch111or000++;
                if (CountAftermatch111or000 == 21)
                {
                    if (patternValue[i] == 1)
                    {
                        randomValues[i + 1] = 0;
                        randomValues[i + 2] = 0;
                        randomValues[i + 3] = 0;
                        randomValues[i + 4] = 1;
                    }
                    else
                    {
                        randomValues[i + 1] = 1;
                        randomValues[i + 2] = 1;
                        randomValues[i + 3] = 1;
                        randomValues[i + 4] = 0;
                    }
                }
            }

            // show it
            Console.WriteLine("Show pattern below:");
            for (int i = 0; i < 1280; i++)
                Console.Write("{0, 5}", patternValue[i]);

            Console.WriteLine("\r\nShow match111 below:");
            for (int i = 0; i < 1280; i++)
                Console.Write("{0, 5}", match111[i]);

            Console.WriteLine("\r\nShow match000 below:");
            for (int i = 0; i < 1280; i++)
                Console.Write("{0, 5}", match000[i]);

            Console.WriteLine("\r\nShow match111Or000 below:");
            for (int i = 0; i < 1280; i++)
                Console.Write("{0, 5}", match111Or000[i]);

            Console.WriteLine("\r\nShow modified random below:");
            for (int i = 0; i < 1280; i++)
                Console.Write("{0, 5}", randomValues[i]);

            g.Save();
            g.Dispose();
            //bitmap.MakeTransparent(Color.Red);
            bitmap.Save("dd.png", ImageFormat.Png);
        }

        private void ShowRandomNumbers(Random rand)
        {
            Console.WriteLine();
            rand.NextBytes(randomValues);
            Console.WriteLine("\r\nShow raw random below:");
            foreach (var value in randomValues)
                Console.Write("{0, 5}", value);

            Console.WriteLine();
        }
    }
}
