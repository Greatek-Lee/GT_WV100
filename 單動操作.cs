using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BDaqOcxLib;
using System.IO;
using System.Collections;
using Motion_W32;

namespace GT_WV100
{
    public partial class 單動操作 : Form
    {
        public GTS_Draw D = new GTS_Draw();

        //InstantDiCtrl DI_1758 = new InstantDiCtrl();
        //InstantDoCtrl DO_1758 = new InstantDoCtrl();
        string DeviceName = "PCI-1758UDIO,BID#0";
        string Para_Path = Application.StartupPath + "\\..\\..\\..\\DATA\\SYS\\IO.txt";

        DAQ_Structure[] 點位總表 = new DAQ_Structure[0];
        DAQ_Structure[] BTNArray;
        DAQ_Structure[] IOArray;
        short AxisID = -1;
        bool Bz_Flag = false;
        public 單動操作()
        {
            InitializeComponent();
        }
        public 單動操作(MF ff)
        {
            mf = ff;
            InitializeComponent();
        }
        MF mf;

        public void io_Navi_Output(int Mode, int Port, int Bit, byte Value)
        {
            if (Mode == 1)
            {
                //axInstantDoCtrl1.WriteBit(Port, Bit, Value);
                mf.輸出1758(Port, Bit, Value);
            }
            else if (Mode == 2)
            {
                //開關一次點位
                mf.輸出1758(Port, Bit, 1);
                System.Threading.Thread.Sleep(100);
                mf.輸出1758(Port, Bit, 0);
            }
        }

        public void io_Navi_Input(int Mode, int Port, int Bit, ref byte Value)
        {
            ErrorCode a = mf.DI_1758.ReadBit(Port, Bit, ref Value);
        }

        public void Button_Click_Event(string Button_Name, ref object sender)
        {
            int cutA = Button_Name.IndexOf("_");
            int cutB = Button_Name.IndexOf("_", cutA + 1);
            string PORT = Button_Name.Substring(cutA + 1, cutB - cutA - 1);
            //string BIT = Button_Name.Substring(cutA + 2, cutB - cutA - 1);
            string BIT = Button_Name.Substring(cutB + 1, Button_Name.Length - cutB - 1);
            Button BTN = (Button)sender;
            byte Value = 0;
            //io_Navi_Input(0, Convert.ToInt32(PORT), Convert.ToInt32(BIT), ref Value);
            mf.DO_1758.ReadBit(Convert.ToInt32(PORT), Convert.ToInt32(BIT), ref Value);

            //蜂鳴器點位(需要持續ON\OFF切換)
            if (PORT == "02" && BIT == "04")
            {
                Bz_Flag = !Bz_Flag;
                if (!Bz_Flag)
                {
                    BTN.BackColor = Color.FromKnownColor(KnownColor.Control);
                    timer2.Stop();
                    io_Navi_Output(1, Convert.ToInt32(PORT), Convert.ToInt32(BIT), 0);
                }
                else
                {
                    BTN.BackColor = Color.Yellow;
                    timer2.Start();
                }
                return;
            }

            if (Value == 0)
                io_Navi_Output(1, Convert.ToInt32(PORT), Convert.ToInt32(BIT), 1);
            else
                io_Navi_Output(1, Convert.ToInt32(PORT), Convert.ToInt32(BIT), 0);
            mf.DO_1758.ReadBit(Convert.ToInt32(PORT), Convert.ToInt32(BIT), ref Value);
            if (Value == 0)
                BTN.BackColor = Color.FromKnownColor(KnownColor.Control);
            else
                BTN.BackColor = Color.Yellow;

            ////開關一次點位
            //io_1758U_Output(0,Convert.ToInt32(PORT), Convert.ToInt32(BIT), 1);
            //System.Threading.Thread.Sleep(1000);
            //io_1758U_Output(0,Convert.ToInt32(PORT), Convert.ToInt32(BIT), 0);
        }

        public struct DAQ_Structure
        {
            public string TYPE;         //類別
            public string PORT;         //通道
            public string BIT;          //位元
            public string NAME;         //名稱
            public string VALUE;        //值
            public string Unit;         //單位
            public string Precision;    //精度
            public string CH_count;     //使用通道數
            public bool ReadCH;         //bit/ch/dw
            public object CTRL;         //此點位在人機上所連結的元件 可能是LABEL、BUTTON..等等
        }

        public void Point_IO_Create(ref DAQ_Structure[] PArray, string File_Path)
        {
            string Buffer = "";
            StreamReader Sr = new StreamReader(File_Path);
            int idx = 0;
            for (int i = 0; !Sr.EndOfStream; i++)
            {
                //先將每行當中的tab/空格轉為#字方便處理
                Buffer = Sr.ReadLine();
                Buffer = Buffer.Replace('\t', '#');
                Buffer = Buffer.Replace(' ', '_');
                Buffer = Buffer.Replace("##", "#");
                int SegCount = System.Text.RegularExpressions.
                                          Regex.Matches(Buffer, "#").Count;

                if (SegCount == 3)
                {
                    Array.Resize(ref PArray, PArray.Length + 1);
                    int Cut0 = Buffer.IndexOf("#", 0);
                    int Cut1 = Buffer.IndexOf("#", Cut0 + 1);
                    int Cut2 = Buffer.IndexOf("#", Cut1 + 1);
                    int Cut3 = Buffer.IndexOf("#", Cut2 + 1);

                    PArray[idx].TYPE = Buffer.Substring(0, Cut0);
                    PArray[idx].PORT = Buffer.Substring(Cut0 + 1, 2);
                    PArray[idx].BIT = Buffer.Substring(Cut0 + 4, 2);
                    PArray[idx].NAME = Buffer.Substring(Cut1 + 1, Cut2 - Cut1 - 1);
                    PArray[idx].VALUE = Buffer.Substring(Cut2 + 1);
                    idx++;
                }
            }
        }

        public void Create_IO_Button(DAQ_Structure[] Button_Array)
        {
            BTNArray = new DAQ_Structure[Button_Array.Length];
            // 避免重覆操作，無法顯示正確結果
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel1.Padding = new Padding(20, 10, 10, 10);
            flowLayoutPanel1.Margin = new Padding(10, 10, 10, 10);
            GC.Collect();

            int idx = 0;
            for (int i = 0; i < Button_Array.Length; i++)
            {
                string TYPE = Button_Array[i].TYPE;

                if (TYPE == "O")
                {
                    string PORT = Button_Array[i].PORT;
                    string BIT = Button_Array[i].BIT;
                    string NAME = Button_Array[i].NAME;

                    Button bt = new Button();
                    bt.Name = "Button_" + PORT + "_" + BIT;
                    bt.Text = PORT + "." + BIT + " " + NAME;
                    bt.TextAlign = ContentAlignment.MiddleCenter;

                    bt.Margin = new Padding(6, 6, 3, 3);
                    bt.Size = new Size(150, 30);
                    bt.Click += Button_Click;

                    flowLayoutPanel1.Controls.Add(bt);
                    byte V = 0;
                    mf.DO_1758.ReadBit(Convert.ToInt32(PORT), Convert.ToInt32(BIT), ref V);
                    if (V == 1)
                        bt.BackColor = Color.Yellow;
                    else
                        bt.BackColor = Color.FromKnownColor(KnownColor.Control);

                    BTNArray[idx].TYPE = TYPE;
                    BTNArray[idx].PORT = PORT;
                    BTNArray[idx].BIT = BIT;
                    BTNArray[idx].ReadCH = true;
                    BTNArray[idx].VALUE = "0";
                    BTNArray[idx].NAME = TYPE + PORT + "_" + BIT;
                    BTNArray[idx].CTRL = bt;

                    idx++;
                }
            }
        }

        public void Show_IO_List(DAQ_Structure[] IO_Array)
        {
            //第1組IO
            IOArray = new DAQ_Structure[0];
            //將所有需要顯示的點位資料轉到IO陣列當中
            //去全機台總表裡面找到要顯示的點位
            int idx = 0;
            for (int i = 0; i <= 點位總表.Length - 1; i++)
            {
                string Type = IO_Array[i].TYPE;
                //bool find = false;
                if (Type == "I")
                {
                    Array.Resize(ref IOArray, IOArray.Length + 1);

                    IOArray[idx].TYPE = IO_Array[i].TYPE;
                    IOArray[idx].PORT = IO_Array[i].PORT;
                    IOArray[idx].BIT = IO_Array[i].BIT;
                    IOArray[idx].NAME = IO_Array[i].NAME;
                    IOArray[idx].VALUE = IO_Array[i].VALUE;
                    //find = true;

                    idx++;
                }
                //if (!find)
                //    MessageBox.Show("總表中找不到IO:");
            }
            D.Draw_Dynamic_Map_IO(pictureBox1, Color.Yellow, ref IOArray);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Button bt = (Button)(sender);
            string btName = bt.Name;
            Button_Click_Event(btName, ref sender);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            //byte Value = 0;
            // io_Navi_Input(1, 0, 0, ref Value);
            //一次讀取所有input
            timer1.Enabled = false;
            byte[] Buffer = new byte[6];
            mf.DI_1758.ReadPort(0, ref Buffer[0]);
            mf.DI_1758.ReadPort(1, ref Buffer[1]);
            mf.DI_1758.ReadPort(2, ref Buffer[2]);
            mf.DI_1758.ReadPort(3, ref Buffer[3]);
            mf.DI_1758.ReadPort(4, ref Buffer[4]);
            mf.DI_1758.ReadPort(5, ref Buffer[5]);

            string[] BArray = new string[6];

            //******劃出input狀態**********
            pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(pictureBox1.Image);
            Pen PPP = new Pen(Brushes.Black, 2);
            int CurrX = D.繪圖起始x;
            int CurrY = D.繪圖起始y;
            Font FFF = new Font("新細明體", 10);
            D.繪圖格寬 = 180;
            for (int i = 0; i <= BArray.Length - 1; i++)
            {

                BArray[i] = Convert.ToString(Convert.ToInt32(Buffer[i]), 2);
                BArray[i] = BArray[i].PadLeft(8, '0');
                char[] PortDATA = BArray[i].ToCharArray();

                for (int bIDX = 7; bIDX >= 0; bIDX--)
                {
                    g.DrawRectangle(PPP, CurrX, CurrY, D.繪圖格寬, D.繪圖格高);
                    if (PortDATA[bIDX] == 49)//input ON
                    {
                        g.FillRectangle(Brushes.Gold, CurrX + 1, CurrY + 1, D.繪圖格寬 - 2, D.繪圖格高 - 2);
                    }
                    else//input OFF
                        g.FillRectangle(Brushes.Gray, CurrX + 1, CurrY + 1, D.繪圖格寬 - 2, D.繪圖格高 - 2);
                    File_Class.IO_Structure tempNode = mf.F.InMatrix[i, 7 - bIDX];
                    g.DrawString("[" + i + "." + (7 - bIDX) + "] " + tempNode.N, FFF, Brushes.Black, CurrX + 5, CurrY + 5);
                    CurrY += D.繪圖格高 + D.繪圖起始y;
                }
                CurrY = D.繪圖起始y;
                CurrX += D.繪圖格寬 + D.繪圖起始x;
                //CurrY += D.繪圖格高 + D.繪圖起始y;
            }
            int pos = 0;
            Motion._8164_get_command(AxisID, ref pos);
            label2.Text = pos.ToString();
            pictureBox1.Refresh();

            //******畫出servo狀態**********
            pictureBox2.Image = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            g = Graphics.FromImage(pictureBox2.Image);
            CurrX = D.繪圖起始x;
            CurrY = D.繪圖起始y;
            for (short IDX = 0; IDX<=7; IDX++)
            {
                if (IDX == 3) continue;
                //取得軸狀態
                ushort V = 0;
                Motion._8164_get_io_status(IDX, ref V);
                string StateString = Convert.ToString(V, 2);
                StateString = StateString.PadLeft(16, '0');
                for (int i = 0; i <= 3; i++)
                {
                    Brush DrawColor = Brushes.Gray;
                    switch (i)
                    {
                        case (0)://ALARM
                            if (StateString[14].CompareTo('1') == 0)
                                DrawColor = Brushes.Gold;
                            break;
                        case (1)://EL-
                            if (IDX == 0)
                            {
                                if (StateString[13].CompareTo('1') == 0)
                                    DrawColor = Brushes.Gold;
                            }
                            else
                            {
                                if (StateString[12].CompareTo('1') == 0)
                                    DrawColor = Brushes.Gold;
                            }
                            break;
                        case (2)://ORG
                            if (StateString[11].CompareTo('1') == 0)
                                DrawColor = Brushes.Gold;
                            break;
                        case (3)://EL+
                            if (IDX == 0)
                            {
                                if (StateString[12].CompareTo('1') == 0)
                                    DrawColor = Brushes.Gold;
                            }
                            else
                            {
                                if (StateString[13].CompareTo('1') == 0)
                                    DrawColor = Brushes.Gold;
                            }
                            break;
                        default:
                            break;
                    }
                    g.DrawRectangle(PPP, CurrX, CurrY, 50, 15);
                    g.FillRectangle(DrawColor, CurrX + 1, CurrY + 1, 50 - 2, 15 - 2);
                    CurrX += 50 + D.繪圖起始x;
                }
                CurrX = D.繪圖起始x;
                CurrY += 15 + 13;
            }
            pictureBox2.Refresh();
            //for (int i = 0; i <= 3; i++)
            //{
            //    BArray[i] = Convert.ToString(Convert.ToInt32(Buffer[i]), 2);
            //    BArray[i] = BArray[i].PadLeft(8, '0');
            //    char[] PortDATA = BArray[i].ToCharArray();

            //    for (int bIDX = 7; bIDX >= 0; bIDX--)
            //    {
            //        g.DrawRectangle(PPP, CurrX, CurrY, D.繪圖格寬, D.繪圖格高);
            //        if (PortDATA[bIDX] == 49)//input ON
            //        {
            //            g.FillRectangle(Brushes.Gold, CurrX + 1, CurrY + 1, D.繪圖格寬 - 2, D.繪圖格高 - 2);
            //        }
            //        else//input OFF
            //            g.FillRectangle(Brushes.Gray, CurrX + 1, CurrY + 1, D.繪圖格寬 - 2, D.繪圖格高 - 2);
            //        File_Class.IO_Structure tempNode = mf.F.InMatrix[i, 7 - bIDX];
            //        g.DrawString("[" + i + "." + (7 - bIDX) + "] " + tempNode.N, FFF, Brushes.Black, CurrX + 5, CurrY + 5);
            //        CurrY += D.繪圖格高 + D.繪圖起始y;
            //    }
            //    CurrY = D.繪圖起始y;
            //    CurrX += D.繪圖格寬 + D.繪圖起始x;
            //    //CurrY += D.繪圖格高 + D.繪圖起始y;
            //}
            //label2.Text = pos.ToString();
            //pictureBox1.Refresh();

            timer1.Enabled = true;
        }

        private void 單動操作_Load(object sender, EventArgs e)
        {
            // mf.ST.Req_STATE.IDX = 0;//進入停止狀態
            Point_IO_Create(ref 點位總表, Para_Path);
            Create_IO_Button(點位總表);
            Show_IO_List(點位總表);
            rb_1軸天車.Checked = true;
            //IO初始化
            // mf.DI_1758.setSelectedDevice(DeviceName, AccessMode.ModeWriteWithReset, 0);
            // mf.DO_1758.setSelectedDevice(DeviceName, AccessMode.ModeWriteWithReset, 0);
        }

        private void rb_7軸CCD升降_Click(object sender, EventArgs e)
        {


        }

        private void button5_Click(object sender, EventArgs e)
        {
            Motion._8164_set_servo(AxisID, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int a = Motion._8164_set_servo(AxisID, 0);
        }





        private void button1_Click(object sender, EventArgs e)
        {
            Motion._8164_sd_stop(AxisID, 0.2);
            while ((Motion._8164_motion_done(AxisID) > 0))
                Application.DoEvents();
            mf.MV.單軸回原點(mf, AxisID);

        }

        private void button6_Click(object sender, EventArgs e)
        {
            int a = Motion._8164_sd_stop(AxisID, 0.2);
        }

        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            if (AxisID < 0) return;
            mf.連續運動(AxisID, mf.MV.axis[AxisID].初速
                                            , mf.MV.axis[AxisID].常速, 0.4);

        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            int a = Motion._8164_sd_stop(AxisID, 0.2);
        }

        private void button3_MouseDown(object sender, MouseEventArgs e)
        {
            if (AxisID < 0) return;
            mf.連續運動(AxisID, -1 * mf.MV.axis[AxisID].初速
                                  , -1 * mf.MV.axis[AxisID].常速, 0.4);
        }

        private void button3_MouseUp(object sender, MouseEventArgs e)
        {

            int a = Motion._8164_sd_stop(AxisID, 0.2);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            mf.MV.全軸回原點(mf);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //蜂鳴器觸發
            if (Bz_Flag)
            {
                byte Value = 0;
                mf.DO_1758.ReadBit(2, 4, ref Value);

                if (Value == 0)
                    io_Navi_Output(1, 2, 4, 1);
                else
                    io_Navi_Output(1, 2, 4, 0);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Motion._8164_start_ta_move((short)AxisID,
                                      Convert.ToInt32(textBox1.Text), mf.MV.axis[AxisID].初速,
                                   mf.MV.axis[AxisID].常速, 0.1, 0.1);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Motion._8164_start_tr_move((short)AxisID,
                      Convert.ToInt32(textBox1.Text), mf.MV.axis[AxisID].初速,
                       mf.MV.axis[AxisID].常速, 0.1, 0.1);
        }

        private void rb_7軸CCD升降_CheckedChanged(object sender, EventArgs e)
        {
            if (rb_0軸軌道.Checked) AxisID = 0;
            if (rb_1軸天車.Checked) AxisID = 1;
            if (rb_2軸彈匣升降.Checked) AxisID = 2;
            if (rb_4軸載台X.Checked) AxisID = 4;
            if (rb_5軸載台Y.Checked) AxisID = 5;
            if (rb_6軸載台R.Checked) AxisID = 6;
            if (rb_7軸CCD升降.Checked) AxisID = 7;
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                timer_Mcnt.Enabled = true;
            }
            else
            {
                timer_Mcnt.Enabled = false;
            }
        }

        private void timer_Mcnt_Tick(object sender, EventArgs e)
        {
            timer_Mcnt.Enabled = false;
            mf.ST.FrameCNT = 0;
            int Pos = 0;
            for (int i = 0; i <= mf.CASArray.Length - 1; i++)
            {
                if (mf.CASArray[i].panel != null)
                {
                    mf.CASArray[i].panel.BackColor = Color.Gray;
                    mf.CASArray[i].有無材料 = 0;
                    mf.CASArray[i].檢測結果 = 0;
                }
            }


            //這種寫法是為了 可以讓數道的產品對應到彈匣中的哪一格
            //就算跳著擺 也可以抓到對應位置
            int PitchIDX = 0;
            int CurrPitchLOW = 0;
            int CurrPitchHight = 0;
            byte CurrV = 0;
            byte NewV = 0;
            while (Pos < 170000)
            {
                Application.DoEvents();
                Motion._8164_get_command(mf.MV.軸2彈匣, ref Pos);
                //定義出當前格位IDX 的檢查範圍 
                CurrPitchLOW = 14000 + (PitchIDX * mf.SYS.彈匣間距) - 4000;
                CurrPitchHight = 14000 + (PitchIDX * mf.SYS.彈匣間距) + 4000;
                if (Pos > CurrPitchHight)
                {
                    PitchIDX++;
                }

                mf.DI_1758.ReadBit(3, 0, ref NewV);
                if (NewV > CurrV)//從0變成1
                {
                    if (PitchIDX == mf.CASArray.Length)
                        break;
                    mf.ST.FrameCNT++;
                    mf.CASArray[PitchIDX].panel.BackColor = Color.LightSkyBlue;
                    mf.CASArray[PitchIDX].有無材料 = 1;
                    mf.richTextBox1.AppendText(Pos + "\r\n");
                }
                CurrV = NewV;

                //if (PitchIDX == CASArray.Length - 1)
                //    break;
            }
            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();

            mf.寫入彈匣狀態();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }
    }
}
