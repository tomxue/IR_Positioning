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
        public MainWindow()
        {
            InitializeComponent();

            Bitmap bitmap = new Bitmap(1280, 800);  // Coolux resolution
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);
            g.DrawLine(new Pen(Color.White), 0, 0, 0, 800);
            g.DrawLine(new Pen(Color.White), 2, 0, 2, 800);
            g.DrawLine(new Pen(Color.White), 10, 0, 10, 800);
            g.Save();
            g.Dispose();
            //bitmap.MakeTransparent(Color.Red);
            bitmap.Save("dd.png", ImageFormat.Png);
        }
    }
}
