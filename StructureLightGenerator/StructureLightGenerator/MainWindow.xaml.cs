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
        const int windowSize = 25;
        const int consecutiveBits = 3;
        const int resolutionX = 1280;
        const int resolutionY = 800;
        byte[] randomValues = new byte[resolutionX];
        byte[] patternValue = new byte[resolutionX];
        byte[] match111 = new byte[resolutionX];
        byte[] match000 = new byte[resolutionX];
        byte[] match111Or000 = new byte[resolutionX];

        public MainWindow()
        {
            int CountOf111 = 0;     // 3 consecutive identical bits, White color for one
            int CountOf000 = 0;     // 3 consecutive identical bits, Black color for zero
            int CountAftermatch111or000 = 0;

            InitializeComponent();

            Random random = new Random();
            ShowRandomNumbers(random);  // generate all the resolutionX random numbers at this point

            Bitmap bitmap = new Bitmap(resolutionX, resolutionY);  // Coolux DLP projector's resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);

            for (int i = 0; i < resolutionX; i++)
            {
                if (randomValues[i] % 2 == 1)
                {
                    CountOf111++;
                    CountOf000 = 0;
                    // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three
                    if (CountOf111 == consecutiveBits)
                    {
                        CountOf111 = 0;
                        g.DrawLine(new Pen(Color.Black), i, 0, i, resolutionX);
                        patternValue[i] = 0;

                        match111[i] = 1;
                        match111Or000[i] = 1;
                        CountAftermatch111or000 = 0; // reset the counter
                    }
                    else
                    {
                        g.DrawLine(new Pen(Color.White), i, 0, i, resolutionX);
                        patternValue[i] = 1;
                    }
                }
                else
                {
                    CountOf111 = 0;
                    CountOf000++;
                    // Requirement 1: limit the maximum number of con-secutive identical bits (a run of bits) to three
                    if (CountOf000 == consecutiveBits)
                    {
                        CountOf000 = 0;
                        g.DrawLine(new Pen(Color.White), i, 0, i, resolutionX);
                        patternValue[i] = 1;

                        match000[i] = 1;
                        match111Or000[i] = 1;
                        CountAftermatch111or000 = 0; // reset the counter
                    }
                    else
                    {
                        g.DrawLine(new Pen(Color.Black), i, 0, i, resolutionX);
                        patternValue[i] = 0;
                    }
                }

                // Requirement2: every window contain at least one run of length exactly one
                CountAftermatch111or000++;
                if (CountAftermatch111or000 == (windowSize - consecutiveBits - 1))
                {
                    if (patternValue[i] == 1)
                    {
                        int j;
                        for (j = 1; j <= consecutiveBits; j++)
                            randomValues[i + j] = 0;
                        randomValues[i + j] = 1;
                    }
                    else
                    {
                        int j;
                        for (j = 1; j <= consecutiveBits; j++)
                            randomValues[i + j] = 1;
                        randomValues[i + j] = 0;
                    }
                }
            }

            // show it
            Console.WriteLine("\r\nShow pattern below:");
            foreach (var value in patternValue)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\nShow match111 below:");
            foreach (var value in match111)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\nShow match000 below:");
            foreach (var value in match000)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\nShow match111Or000 below:");
            foreach (var value in match111Or000)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\nShow modified random below:");
            foreach (var value in randomValues)
                Console.Write("{0, 5}", value);

            Console.WriteLine("\r\n");

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
