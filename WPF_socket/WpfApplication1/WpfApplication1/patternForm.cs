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

        private void button1_Click(object sender, EventArgs e)
        {
            if (currIndex >= imageList.Count)
            {
                currIndex = 0;
            }
            pictureBox1.Image = Image.FromFile(imageList[currIndex], true);
            currIndex += 1;
        }

        public int currIndex = 0;//索引值

        private readonly List<string> imageList = new List<string> 
        { 
            //三张图片的地址
            @"c:\x.png",
            @"c:\y.png",
        };

    }
}
