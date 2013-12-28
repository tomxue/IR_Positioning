using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            GeneratePattern();
        }

        private void GeneratePattern()
        {
            string PATH = System.IO.Directory.GetCurrentDirectory() + @"\pattern.txt";

            const int resolutionX = 640;
            const int windowSize = 16;  // 128/16 = 8, means the steps can be 8
            const int segLength = 16;

            byte[] randomData = new byte[resolutionX];
            byte[] patternData = new byte[resolutionX];
            byte[] randomData16 = new byte[16];
            byte[] toBeCompared = new byte[windowSize];

            //Bitmap bitmap = new Bitmap(2 * resolutionX, 2 * resolutionY);  // Coolux DLP projector's resolution
            //Graphics g = Graphics.FromImage(bitmap);
            //g.Clear(System.Drawing.Color.Black);

            Random random = new Random();
            random.NextBytes(randomData16);
            Array.ConstrainedCopy(randomData16, 0, randomData, 0, 16);

            for (int segCounter = 1; segCounter < 40; segCounter++)
            {

            GenerateBarLoop:

                random = new Random();
                random.NextBytes(randomData16);
                Array.ConstrainedCopy(randomData16, 0, randomData, segCounter * segLength, segLength);

                for (int i = 0; i < (segCounter + 1) * 16; i++)
                {
                    if (randomData[i] % 2 == 1)
                    {
                        if (i >= 3 && ((randomData[i - 1] % 2) == 1) && ((randomData[i - 2] % 2) == 1) && ((randomData[i - 3] % 2) == 1))
                        {
                            patternData[i] = 0;
                            randomData[i] = 0;  // will change the input data: randomData accordingly, important!
                        }
                        else
                        {
                            patternData[i] = 1;
                            randomData[i] = 1;
                        }
                    }
                    else
                    {
                        if (i >= 3 && ((randomData[i - 1] % 2) == 0) && ((randomData[i - 2] % 2) == 0) && ((randomData[i - 3] % 2) == 0))
                        {
                            patternData[i] = 1;
                            randomData[i] = 1;
                        }
                        else
                        {
                            patternData[i] = 0;
                            randomData[i] = 0;
                        }
                    }
                }

                int diffCount;
                diffCount = 0;

                for (int i = 0; i <= (segCounter + 1) * 16 - windowSize; i++)
                {
                    // prepare the window "k" (base is "i") to be compared with all the other windows
                    for (int k = 0; k < windowSize; k++)
                        toBeCompared[k] = patternData[i + k];

                    // prepare another window "n" (base is "m") and compare it with window "k"
                    for (int m = 0; m <= ((segCounter + 1) * 16 - windowSize); m++)
                    {
                        if (m != i)
                        {
                            for (int n = 0; n < windowSize; n++)
                            {
                                if (toBeCompared[n] != patternData[m + n])
                                    diffCount++;
                            }
                            if (diffCount < 2) // if no diffrence, continue to regenerate
                            {
                                Console.WriteLine("segCounter=" + segCounter);
                                goto GenerateBarLoop;
                            }
                            else
                                diffCount = 0;
                        }
                    }
                }
            }

            //g.Save();
            //g.Dispose();
            //bitmap.Save("BarCode.png", ImageFormat.Png);

            File.WriteAllBytes(PATH, patternData);
        }
    }
}
