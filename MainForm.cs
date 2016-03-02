using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;


namespace Final_Project
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ToolStripMenuItem_DIP_Click(object sender, EventArgs e)
        {
            DIP DIP_process = new DIP();
            DIP_process.Show();
            DIP_process.MdiParent = this;
        }

        private void ToolStripMenuItem_DIPwork_Click(object sender, EventArgs e)
        {
            DIPwork DIPwork_process = new DIPwork();
            DIPwork_process.Show();
            DIPwork_process.MdiParent = this;
        }

        private void ToolStripMenuItem_train_Click(object sender, EventArgs e)
        {
            train train_process = new train();
            train_process.Show();
            train_process.MdiParent = this;
        }
    }
}
