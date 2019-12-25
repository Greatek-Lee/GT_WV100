using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GT_WV100;
using System.IO;

namespace GT_WV100
{
    public partial class ProjectManage : Form
    {
        MF ff;
        File_Class F = new File_Class();
        public ProjectManage()
        {
            InitializeComponent();
        }

        public ProjectManage(MF mf_this)
        {
            InitializeComponent();
            ff = mf_this;
        }

        private void ProjectManage_Load(object sender, EventArgs e)
        {
            Refresh_DevList();
        }

        public void Refresh_DevList()
        {
            string DEV_Path = ff.路徑_產品資料夾;
            string[] DEVList = System.IO.Directory.GetDirectories(DEV_Path);
            ListView2.Items.Clear();
            ListView2.Columns[0].Width = 250;
            ListView2.Columns[1].Width = 250;
            ListView2.Columns[2].Width = 60;
            string Comment = "";
            string time;
            string JobName;
            int PosF;
            for (int i = 0; i <= DEVList.Length - 1; i++)
            {
                PosF = DEVList[i].LastIndexOf("\\");
                JobName = DEVList[i].Substring(PosF + 1, DEVList[i].Length - PosF - 1);
                time = System.IO.Directory.GetLastWriteTime(DEVList[i]).ToString();
                string TempPath = DEV_Path + JobName + "\\" + JobName + "_Comment.txt";
                if (System.IO.File.Exists(TempPath))
                {
                    StreamReader sr = new StreamReader(TempPath, false);
                    Comment = sr.ReadToEnd();
                    sr.Dispose();
                }
                ListViewItem TempItem = new ListViewItem(JobName);

                TempItem.SubItems.Add(time);
                TempItem.SubItems.Add(Comment);
                ListView2.Items.Add(TempItem);
            }
        }
        
        private void button4_Click(object sender, EventArgs e)
        {
            string Result = "";
            MF.InputBox("複製為:", "新產品名稱:", ref Result);
            int selectindex = ListView2.SelectedItems.Count;
            string SrcDir;
            string DstDir;
            string SrcFile;
            string DstFile = "";
            string SrcName = ListView2.SelectedItems[0].SubItems[0].Text;
            if (selectindex != 0)
            {
                SrcDir = ff.路徑_產品資料夾 + ListView2.SelectedItems[0].SubItems[0].Text;
                DstDir = ff.路徑_產品資料夾 + Result;
            }
            else
            {
                return;
            }

            if (Result.CompareTo("") != 0)
            {
                if (Directory.Exists(DstDir))
                {
                    MessageBox.Show("此專案已經存在,請重新命名");
                    return;
                }
                else
                {
                    Directory.CreateDirectory(DstDir);
                    string[] files = System.IO.Directory.GetFiles(SrcDir);

                    // Copy the files and overwrite destination files if they already exist.
                    foreach (string s in files)
                    {
                        // Use static Path methods to extract only the file name from the path.
                        SrcFile = System.IO.Path.GetFileName(s);
                        DstFile = System.IO.Path.Combine(DstDir, SrcFile);
                        System.IO.File.Copy(s, DstFile, true);
                    }
                }
            }
            Refresh_DevList();
            ff.F.Load(ref ff.PDArray, DstDir + "\\Para.ini");
            ff.F.SetV(ref ff.PDArray, "產品代號", Result);
            ff.F.Save(ref ff.PDArray, DstDir + "\\Para.ini");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (ListView2.SelectedItems[0].SubItems[0].Text.CompareTo("Default") == 0)
            {
                MessageBox.Show("不可刪除系統內建類別!");
                return;
            }

            int index = ListView2.SelectedItems.Count;
            if (index != 0)
            {
                string DelPath = ff.路徑_產品資料夾 + ListView2.SelectedItems[0].SubItems[0].Text;
                if (Directory.Exists(DelPath))
                {
                    DirectoryInfo DIFO = new DirectoryInfo(DelPath);
                    DIFO.Delete(true);
                }
            }
            Refresh_DevList();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int index = ListView2.SelectedItems.Count;
            if (index != 0)
            {
                ff.Load_PD(ListView2.SelectedItems[0].SubItems[0].Text);
            }

            this.Close();
            ff.RefreshImage_動態影像(ref ff.img即時影像);
            ff.RefreshPanel();
        }
    }
}