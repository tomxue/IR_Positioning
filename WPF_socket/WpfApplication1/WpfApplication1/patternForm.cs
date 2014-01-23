using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace WpfApplication1
{
    public partial class patternForm : Form
    {
        public patternForm()
        {
            InitializeComponent();
        }

        public int currIndex = 0;//索引值

        public void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Image.FromFile(imageList[currIndex], true);
            //currIndex += 1;
            //if (currIndex >= imageList.Count)
            //{
            //    currIndex = 0;
            //}
        }

        public void SwitchPicture(int index)
        {
            pictureBox1.Image = Image.FromFile(imageList[index], true);
        }

        private readonly List<string> imageList = new List<string> 
        { 
            //三张图片的地址
            @"x.png",
            @"y.png",
        };

    }
}
