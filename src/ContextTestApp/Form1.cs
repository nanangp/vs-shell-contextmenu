using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContextTestApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            lvFiles.MouseUp += lvFiles_MouseUp;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory);
            var fileInfos = di.EnumerateFiles();
            foreach (var fi in fileInfos)
                lvFiles.Items.Add(fi.FullName);
        }

        void lvFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                var itemInfo = lvFiles.HitTest(e.Location);
                var filename = itemInfo.Item.Text;
                Popup(filename, lvFiles.PointToScreen(e.Location));
            }
        }

        private void Popup(string filename, Point point)
        {
            //MessageBox.Show(filename);
            var contextMenu = new ShellContextMenu();
            var fileInfo = new[] { new FileInfo(filename) };
            contextMenu.ShowContextMenu(fileInfo, point);
        }
        
    }
}
