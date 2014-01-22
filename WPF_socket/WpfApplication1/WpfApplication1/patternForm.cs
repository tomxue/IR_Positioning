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
    public partial class patternForm : Form
    {
        public patternForm()
        {
            InitializeComponent();
        }

        public int currIndex = 0;//索引值

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Image.FromFile(imageList[currIndex], true);
            currIndex += 1;
            if (currIndex >= imageList.Count)
            {
                currIndex = 0;
            }
            //string str = System.Environment.CurrentDirectory;
            //Console.Write(str);
        }

        private readonly List<string> imageList = new List<string> 
        { 
            //三张图片的地址
            @"x.png",
            @"y.png",
        };

    }
}
