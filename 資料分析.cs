using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections;
using System.IO;

namespace GT_WV100
{
    public partial class 資料分析 : Form
    {
        public 資料分析(MF ff)
        {
            mf = ff;
            InitializeComponent();
        }
        MF mf;

        DateTime 開始時間;
        DateTime 結束時間;

        private void FindFile(string dir, string STR)
        {
            //在指定目錄下查詢文件，若符合查詢條件，將檔案寫入lsFile控制元件
            DirectoryInfo Dir = new DirectoryInfo(dir);
            try
            {
                foreach (DirectoryInfo d in Dir.GetDirectories())//查詢子目錄    
                {
                    FindFile(Dir + d.ToString() + "\\", STR);
                }
                foreach (FileInfo f in Dir.GetFiles("*.*"))//查詢附檔名為xls的文件  
                {
                    Regex regex = new Regex(STR, RegexOptions.IgnoreCase);//查詢檔案名稱中有關鍵字friend的文件
                    Match m = regex.Match(f.ToString());

                    if (m.Success == true)
                    {
                        CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
                        int FindIDX = Compare.IndexOf(f.FullName, STR, CompareOptions.IgnoreCase);

                        string HeadString = f.FullName.Substring(0, FindIDX);
                        string FindString = STR;
                        string EndString = f.FullName.Substring(FindIDX + FindString.Length,
                                                                f.FullName.Length - (FindIDX + FindString.Length));
                        richTextBox2.AppendText(HeadString, Color.Black);
                        richTextBox2.AppendText(FindString, Color.Red);
                        richTextBox2.AppendText(EndString + "\r\n", Color.Black);

                        // listBox1.Items.Add(Dir + f.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Application.DoEvents();
        }



        private void button6_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.SelectedPath = "D:\\";
            path.ShowDialog();
            label5.Text = path.SelectedPath;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            richTextBox2.Clear();
            FindFile(label5.Text, textBox2.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            label總數.Text = "0";
            labelOK.Text = "0";
            labelNG.Text = "0";
            richTextBox1.Clear();
            string 矽批前五碼 = textBox1.Text;
            string dir = "";
            if (radioButton1.Checked)
                dir = "D:\\WV_LOG";
            else
                dir = "Y:";
            int OK總片數 = 0;
            int NG總片數 = 0;

            //尋找所有在範圍內的檔案
            //在指定目錄下查詢文件，若符合查詢條件，將檔案寫入lsFile控制元件
            DirectoryInfo Dir = new DirectoryInfo(dir);
            //string[] 搜尋結果 = new string[500];
            int 總片數 = 0;
            try
            {
                foreach (DirectoryInfo d in Dir.GetDirectories())//查詢子目錄    
                {
                    Regex regex = new Regex(矽批前五碼, RegexOptions.IgnoreCase);//查詢檔案名稱中有關鍵字friend的文件
                    Match m = regex.Match(d.Name.ToString());
                    if (m.Success == true)
                    {
                        //進入資料夾判斷時間
                        foreach (FileInfo f in d.GetFiles("*.log"))//查詢附檔名為xls的文件  
                        {
                            Application.DoEvents();
                            if ((f.CreationTime > 開始時間) && (f.CreationTime < 結束時間))
                            {
                                總片數++;
                                StreamReader SR = new StreamReader(f.FullName, System.Text.Encoding.Default);
                                while (!SR.EndOfStream)
                                {
                                    string Data = SR.ReadLine();
                                    if (Data.IndexOf("檢測結果") >= 0)
                                    {
                                        string Final = Data.Substring(Data.Length - 2, 2);
                                        if (Final.CompareTo("OK") == 0)
                                            OK總片數++;
                                        else if (Final.CompareTo("NG") == 0)
                                            NG總片數++;

                                    }
                                }
                                SR.Dispose();
                                richTextBox1.AppendText(f.FullName + "\r\n", Color.Black);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Application.DoEvents();
            label總數.Text = 總片數.ToString();
            labelOK.Text = OK總片數.ToString();
            labelNG.Text = NG總片數.ToString();
        }

        private void 開始時間ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            開始時間 = monthCalendar1.SelectionStart;
            label8.Text = 開始時間.ToString();

        }

        private void 結束時間ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            結束時間 = monthCalendar1.SelectionStart;
            label9.Text = 結束時間.ToString();
        }

        private void 開啟檔案位置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (richTextBox1.SelectedText.Length < 30)
                return;
            string 檔案名稱 = richTextBox1.SelectedText.Substring(0, richTextBox1.SelectedText.Length - 5);
            Process.Start(檔案名稱 + ".log");
            Process.Start(檔案名稱 + ".jpg");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            label總數.Text = "0";
            labelOK.Text = "0";
            labelNG.Text = "0";
            richTextBox1.Clear();
            string 矽批前五碼 = textBox1.Text;
            string dir = "";
            if (radioButton1.Checked)
                dir = "D:\\WV_LOG";
            else
                dir = "Y:";
            
            int OK總片數 = 0;
            int NG總片數 = 0;

            //尋找所有在範圍內的檔案
            //在指定目錄下查詢文件，若符合查詢條件，將檔案寫入lsFile控制元件
            DirectoryInfo Dir = new DirectoryInfo(dir);
            //string[] 搜尋結果 = new string[500];
            int 總片數 = 0;
            try
            {
                foreach (DirectoryInfo d in Dir.GetDirectories())//查詢子目錄    
                {
                    Regex regex = new Regex(矽批前五碼, RegexOptions.IgnoreCase);//查詢檔案名稱中有關鍵字friend的文件
                    Match m = regex.Match(d.Name.ToString());
                    if (m.Success == true)
                    {
                        //進入資料夾判斷時間
                        foreach (FileInfo f in d.GetFiles("*.log"))//查詢附檔名為xls的文件  
                        {
                            Application.DoEvents();
                            if ((f.CreationTime > 開始時間) && (f.CreationTime < 結束時間))
                            {
                                總片數++;
                                StreamReader SR = new StreamReader(f.FullName, System.Text.Encoding.Default);
                                while (!SR.EndOfStream)
                                {
                                    string Data = SR.ReadLine();
                                    if (Data.IndexOf("檢測結果") >= 0)
                                    {
                                        string Final = Data.Substring(Data.Length - 2, 2);
                                        if (Final.CompareTo("OK") == 0)
                                            OK總片數++;
                                        else if (Final.CompareTo("NG") == 0)
                                        {
                                            NG總片數++;
                                            richTextBox1.AppendText(f.FullName + "\r\n", Color.Black);
                                        }

                                    }
                                }
                                SR.Dispose();
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Application.DoEvents();
            label總數.Text = 總片數.ToString();
            labelOK.Text = OK總片數.ToString();
            labelNG.Text = NG總片數.ToString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            //path.SelectedPath = "D:\\";
            path.ShowDialog();
            label10.Text = path.SelectedPath;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //全部儲存
            for (int i = 0; i < richTextBox1.Lines.Count(); i++)
            {
                int Len = richTextBox1.Lines[i].ToString().Length;
                if (Len == 0) return;
                string LogPath = richTextBox1.Lines[i].ToString();
                string JpgPath = richTextBox1.Lines[i].ToString().Substring(0, Len-3) + "jpg";
                string [] aaa = LogPath.Split('\\');
                string[] bbb = JpgPath.Split('\\');
                string LogName = aaa[2];
                string JpgName = bbb[2];
                if (checkBox1.Checked)
                    System.IO.File.Copy(LogPath, label10.Text + "\\" + LogName,true);
                if (checkBox2.Checked)
                    System.IO.File.Copy(JpgPath, label10.Text + "\\" + JpgName,true);
            }
        }
    }
}
