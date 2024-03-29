﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class patternWindow : Window
    {
        BitmapImage image_x = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "x.png"));
        BitmapImage image_y = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "y.png"));
        public patternWindow()
        {
            InitializeComponent();
            image1.Source = image_x;

            this.Left = Screen.AllScreens[0].Bounds.Width;
            this.Top = 0;
        }

        public void SwitchPicture(bool index)
        {
            if (index)
                image1.Source = image_y;
            else
                image1.Source = image_x;
        }
    }
}
