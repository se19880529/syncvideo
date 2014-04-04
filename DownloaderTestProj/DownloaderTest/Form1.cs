using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DownloaderTest
{
    public partial class Form1 : Form
    {
        string filePath = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            filePath = ofd.FileName;
            labelPath.Text = filePath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FileService.FileDescripter desc = FileService.FileDescripter.CreateFromFile(filePath, 4096000, 8);
            textFileDescripter.Text = desc.ToString();
        }
    }
}
