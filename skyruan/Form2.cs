using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace skyruan
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            richTextBox1.AppendText("个人站，头条号，微信公众号同步更新。\n");
            richTextBox1.AppendText("个人站网址：http://www.skyruan.com\n");
            richTextBox1.AppendText("头条号：天空阮站长\n");
            richTextBox1.AppendText("QQ群：645325764\n");
            richTextBox1.AppendText("微信公众号：\n");
            string path = @".\image\skyruan.jpg";
            Clipboard.Clear();
            Bitmap bmp = new Bitmap(path);
            Clipboard.SetImage(bmp);
            richTextBox1.Paste();
            Clipboard.Clear();
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }
    }
}
