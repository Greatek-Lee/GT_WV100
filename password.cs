using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GT_WV100;

namespace GT_WV100
{
    public partial class password : Form
    {
        MF MF;
        string Msg = "";
        int PA_Mode = 0;

        public password()
        {
            InitializeComponent();
        }
        public password(MF temp)
        {
            InitializeComponent();
            MF = temp;
            PA_Mode = 0;
        }
        public password(MF temp, string str)
        {
            InitializeComponent();
            MF = temp;
            Msg = str;
            label1.Text = Msg;
            PA_Mode = 1;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (PA_Mode == 0)
            {
                System.IO.StreamReader SR = new System.IO.StreamReader(MF.路徑_程式DATA資料夾 + "\\PW.txt");
                string TotalString = SR.ReadToEnd();

                if (textBox1.Text.CompareTo(TotalString) == 0)
                {
                    MF.eng_IN();
                    this.Dispose();
                }
                else
                {
                    MessageBox.Show("密碼錯誤");
                    this.Dispose();
                }
            }
            else if (PA_Mode == 1)
            {
                System.IO.StreamReader SR = new System.IO.StreamReader(MF.路徑_程式DATA資料夾 + "\\HOLD.txt");
                string TotalString = SR.ReadToEnd();

                if (textBox1.Text.CompareTo(TotalString) == 0)
                {
                    MF.Error_release();
                    this.Dispose();
                }
                else
                {
                    MessageBox.Show("密碼錯誤");
                    this.Dispose();
                }
            }
        }
    }
}
