#define PCI1758
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using GT_PLC;
using System.Diagnostics;
using AxOvkBase;
using Motion_W32;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using BDaqOcxLib;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections;

namespace GT_WV100
{
    public partial class MF : Form
    {
        public MF()
        {
            InitializeComponent();
        }


        //多執行續timer
        private System.Timers.Timer timer_Alarm_Thread;//警報狀態偵測timer
        private System.Timers.Timer timer_IOM_Thread;//所有點位/軸狀態更新
        private System.Timers.Timer timer_ST_Thread;//機台狀態控制
        private System.Timers.Timer timer_ServoPos_Thread;//伺服位置寫入
        private System.Timers.Timer timer_輸出LOG;//輸出LOG檔案
        //運動控制用資料結構
        //步序用節點結構       
        public File_Class.Motion_Step_Class[] StepArray = new File_Class.Motion_Step_Class[150];
        public File_Class.Alarm_Condition_Class[] ALMArray = new File_Class.Alarm_Condition_Class[200];
        public int IDX_Step步序 = 0;
        public int IDX_End步序 = -1;
        public InstantDiCtrl DI_1758 = new InstantDiCtrl();
        public InstantDoCtrl DO_1758 = new InstantDoCtrl();
        AxOvkBase.TxAxHitHandle DragHitHandleR1;
        Boolean DragFlagR1 = false;

        AxOvkBase.TxAxHitHandle DragHitHandleG;
        Boolean DragFlagG = false;

        bool 主畫面拖曳 = false;

        public int ActiveHandle;
        //int ActiveHandleRT;
        public float zoom分析;
        public float zoom;

        public bool Show_TempRect = false;
        bool Data_Loaded = false;
        public PLC P = new PLC();
        public File_Class F = new File_Class(); //存取檔案相關功能
        public CassetteClass[] CASArray = new CassetteClass[13];
        public MapClass MAP = new MapClass();//MAP資料類別
        public CheckClass CK = new CheckClass();//檢測紀錄類別
        public MotionClass MV = new MotionClass();
        public sysClass SYS = new sysClass();//資訊類別包括LOG等等
        public devClass PD = new devClass();//產品配方類別
        public StateClass ST = new StateClass();//機台狀態類別,包括各項參數
        public string NullChar;//無效字元
        public string FailChar;//留料字元
        public bool flag_輸出LOG;//存檔
        public bool flag_輸出格位資料;//存檔
        //***紀錄影像滑鼠點擊座標
        public int MX, MY;
        public int DBcount = 0;
        public bool DBstate = false;
        //****各式旗標*******
        public bool Flag_ENG = false;
        public string 路徑_程式DATA資料夾;
        public string 路徑_系統SYS資料夾;
        public string Dir_SYS_Default;
        public string 路徑_產品資料夾;
        public string Dir_DEV_Default;
        public string 路徑_點位總表;
        public string 路徑_動作流程8;
        public string 路徑_動作流程12;
        public string 路徑_警報條件;
        public string 路徑_生產批資料;
        //宣告系統參數陣列
        public int[] AVG_Y_ARRAY;//紀錄檢測階段每一ROW的Y座標
        public int[] AVG_Y_ARRAY_NMP;//紀錄檢測階段每一ROW的Y座標(無MAP測試模式)
        public CheckClass.DieInfoStruct[,] Matrix_NMP;// 當前晶圓上的內容矩陣(無MAP測試模式)
        public File_Class.Para_Structure[] sysArray = new File_Class.Para_Structure[150];
        public File_Class.Para_Structure[] PDArray = new File_Class.Para_Structure[50];
        public Point[,] 象限補償矩陣 = new Point[5, 5];
        public static File_Class.Language_Structure[] TextArray = new File_Class.Language_Structure[300];//載入中英切換字串
        //**** 顏色定義
        public int BAD_Ept = 0xFA;      //影像空料 MAP留DIE 紅色
        public int BAD_DEV = 0xFA00;    //影像有料 MAP留DIE 綠色
        public int GOOD_Ept = 0xEEA000; //影像有料 MAP該取  黃色
        public int GOOD_DEV = 0xFAFA;   //影像空料 MAP該取  淡藍色

        public Graphics g;

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
                        richTextBox1.AppendText(HeadString, Color.Black);
                        richTextBox1.AppendText(FindString, Color.Red);
                        richTextBox1.AppendText(EndString + "\r\n", Color.Black);

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


        //此處非常重要 所有機台汽缸/繼電器輸出前都會在此檢查安全性
        public string 輸出條件判定(int port, int bit, byte V)
        {
            string ErrorMSG = "";
            //列出所有汽缸作動前必須檢查的安全條件
            //若不合理直接輸出異常 不管自動或單動狀態下

            //if (ST.CurrentSTATE.IDX == 5)//彈匣推缸沒有歸位
            //{
            //    ErrorMSG = "停止異常後方能作動";
            //}
            if (ST.Curr_STATE.IDX != 1)//非自動狀態
            {

                //if (F.InMatrix[1, 1].V == 0)//彈匣推缸沒有歸位
                //{
                //    if ((port == 0) && (bit == 5))//2.04材料推缸回
                //        ErrorMSG = "彈匣推缸退出前限位缸禁止作動";
                //}

            }


            return ErrorMSG;
        }

        public void 自保持狀態處裡()
        {

            ////輸出條件成功, 若要設計自保持條件可以加在這邊
            //if (這個狀態下開啟自保)
            //    ST.保持條件[0] = true;


            //if (這個狀態下解除自保)
            //    ST.保持條件[0] = false;
        }

        public int 輸出1758(int port, int bit, byte V)
        {
            if (!無保護模式ToolStripMenuItem.Checked)
            {
                string ErrorMSG = "";
                ErrorMSG = 輸出條件判定(port, bit, V);
                if (ErrorMSG.Length > 0)
                {
                    ST.Req_STATE.IDX = 5;
                    ST.Req_STATE.StateColor = Color.Red;
                    ST.Req_STATE.Context = ErrorMSG;
                    return -1;
                }
            }
            return (int)DO_1758.WriteBit(port, bit, V);//材料推缸回

        }

        //此處非常重要 所有機台軸運動都會在此檢查安全性
        public string 運動條件判定(int IDX)
        {
            if (無保護模式ToolStripMenuItem.Checked)
                return "";
            //判斷是哪一軸要動, 是否符合啟動條件
            string ErrorMSG = "";
            if (ST.Curr_STATE.IDX == 5)
                ErrorMSG = ST.Curr_STATE.Context;
            if (ST.Curr_STATE.IDX != 1)//非自動狀態
            {
                switch (IDX)
                {
                    case (0)://軌道
                        if (F.InMatrix[3, 1].V == 1)//
                            ErrorMSG = "軌道上有材料禁止軌道開合動作";
                        break;
                    case (1)://天車
                             //撥料氣缸不在上位 除非自動狀態否則不可動
                        if (F.InMatrix[4, 2].V == 0)//
                            ErrorMSG = "撥料稈未在上位禁止移動天車";
                        if (F.InMatrix[3, 2].V == 0) //
                            ErrorMSG = "拉料小汽缸未在上位禁止移動天車";
                        if (F.InMatrix[3, 4].V == 0)
                            ErrorMSG = "吸料大氣缸未在上位禁止移動天車";
                        break;
                    case (2)://彈匣
                        if (F.InMatrix[3, 1].V == 1)
                            ErrorMSG = "軌道上有材料禁止彈匣動作";
                        break;
                    case (4)://X
                        if (F.InMatrix[3, 4].V == 0)
                            ErrorMSG = "吸料大氣缸未在上位禁止動作";
                        if (F.InMatrix[3, 2].V == 0)
                            ErrorMSG = "拉料小汽缸未在上位禁止移動X";
                        break;
                    case (5)://Y
                        if (F.InMatrix[3, 4].V == 0)
                            ErrorMSG = "吸料大氣缸未在上位禁止動作";
                        if (F.InMatrix[3, 2].V == 0)
                            ErrorMSG = "拉料小汽缸未在上位禁止移動Y";
                        break;
                    case (6)://R
                        if (F.InMatrix[3, 4].V == 0)
                            ErrorMSG = "吸料大氣缸未在上位禁止動作";
                        if (F.InMatrix[3, 2].V == 0)
                            ErrorMSG = "拉料小汽缸未在上位禁止移動R";
                        break;
                    case (7)://CCD
                        break;
                    default:
                        break;
                }

                if (ST.安全光幕觸發)
                    ErrorMSG = "請退出安全光幕";
                if (ST.安全門開啟)
                    ErrorMSG = "請關閉安全門";
            }

            return ErrorMSG;
        }

        public int 絕對運動(short IDX, double POS, double 初速, double 常速, double acc, double dec)
        {

            string ERRMSG = 運動條件判定(IDX);
            if (ERRMSG.Length > 0)
            {
                ST.Req_STATE.IDX = 5;
                ST.Req_STATE.StateColor = Color.Red;
                ST.Req_STATE.Context = ERRMSG;
                return -1;
            }

            return Motion._8164_start_ta_move(IDX, POS, 初速, 常速, acc, dec);

        }
        public int 相對運動(short IDX, double POS, double 初速, double 常速, double acc, double dec)
        {
            string ERRMSG = 運動條件判定(IDX);
            if (ERRMSG.Length > 0)
            {
                ST.Req_STATE.IDX = 5;
                ST.Req_STATE.StateColor = Color.Red;
                ST.Req_STATE.Context = ERRMSG;
                return -1;
            }
            return Motion._8164_start_tr_move(IDX, POS, 初速, 常速, acc, dec);
        }
        public int 連續運動(short IDX, double 初速, double 常速, double acc)
        {
            string ERRMSG = 運動條件判定(IDX);
            if (ERRMSG.Length > 0)
            {
                ST.Req_STATE.IDX = 5;
                ST.Req_STATE.StateColor = Color.Red;
                ST.Req_STATE.Context = ERRMSG;
                return -1;
            }

            return Motion._8164_tv_move(IDX, 初速, 常速, acc);
        }

        public int 原點運動(short IDX, double 初速, double 常速, double acc)
        {
            string ERRMSG = 運動條件判定(IDX);
            if (ERRMSG.Length > 0)
            {
                ST.Req_STATE.IDX = 5;
                ST.Req_STATE.StateColor = Color.Red;
                ST.Req_STATE.Context = ERRMSG;
                return -1;
            }

            return Motion._8164_home_move(IDX, 初速, 常速, acc);
        }
        public void Process_ServerMAP(string FilePath)
        {
            //20141209
            MAP.Matrix = Process_Final_MAP(FilePath);
            AVG_Y_ARRAY = new int[MAP.Matrix.GetLength(0)];//宣告每一行的檢測Y平均座標暫存陣列,供後面畫圖用

            //取得ref產品在矩陣當中的位置
            MAP.LineWidth = MAP.Matrix.GetLength(1);
            MAP.LineHeight = MAP.Matrix.GetLength(0);

            if (PD.左參考dieIDX_Y > 0)
            {
                MAP.FstX = PD.左參考dieIDX_X;
                MAP.FstY = PD.左參考dieIDX_Y;
                goto FIND_REF;
            }

            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {

                    if (MAP.Matrix[y, x].MapString.CompareTo(PD.參考DIE字元) == 0)
                    {
                        MAP.FstX = x;
                        MAP.FstY = y;
                        goto FIND_REF;
                    }
                }
            }
        FIND_REF:

            CK.Matrix = new CheckClass.DieInfoStruct[MAP.Matrix.GetLength(0), MAP.Matrix.GetLength(1)];
            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {
                    CK.Matrix[y, x] = new CheckClass.DieInfoStruct();
                }
            }
        }

        public MapClass.DieInfoStruct[,] Process_Final_MAP(string FilePath)
        {
            //取得wafer檔案在map中的基本資訊
            StreamReader SR = new StreamReader(FilePath);
            //取得map寬高
            SR = new StreamReader(FilePath);
            string line;

            int Temp = 0;

            string tempLine;
            int RowIndex = 0;
            int LineWidth = 0;
            int LineHeight = 0;
            char[] temp = new char[1];
            temp[0] = '.';
            //抓出整個map檔的寬高
            //從檔案第一行開始不停讀取直到找到 RowData開始的第一行
            LineWidth = 0;
            while ((line = SR.ReadLine()) != null)
            {
                Temp = line.IndexOf("RowData:", 0);
                if (Temp > -1)
                {
                    if (LineWidth == 0)
                    {
                        //紀錄RowData寬度
                        int a = line.Length - 8 + 1;
                        if ((a % 4) != 0)
                        {
                            LineWidth = (line.Length - 8) / 4;

                            //MessageBox.Show("MAP File Row Width Error! Load Fail!");
                            //return null;
                        }
                        else
                            LineWidth = (line.Length - 8 + 1) / 4;
                    }
                    //累加RowData高度
                    LineHeight += 1;
                }
            }

            SR.Dispose();
            //依照上面取得的寬高宣告matrix並將資料存入
            MapClass.DieInfoStruct[,] Matrix = new MapClass.DieInfoStruct[LineHeight, LineWidth];
            //將MAP資料放入記憶體矩陣
            RowIndex = 0;
            SR = new StreamReader(FilePath);
            //252
            while ((line = SR.ReadLine()) != null)
            {
                line = line.Trim();
                Temp = line.IndexOf("RowData:", 0);
                if (Temp > -1)
                {
                    line = line.Substring(8, line.Length - 8);
                    tempLine = line;
                    for (int i = 0; i <= LineWidth - 1; i++)
                    {
                        int L = tempLine.Length;
                        Matrix[RowIndex, i].MapString = tempLine.Substring(i * 4, 3);
                    }

                    RowIndex += 1;
                }
            }
            SR.Dispose();
            return Matrix;

            //進行server map的繪圖
            //RefreshMap(ref axAxImageC241, true);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            zoom分析 = 1;
            zoom = 0.057f;
            this.Top = 5;
            axAxAltairU1.WatchDogTimerState = AxAltairUDrv.TxAxauWatchDogTimerState.AXAU_WATCH_DOG_TIMER_STATE_ENABLED;
            ActiveHandle = axAxAltairU1.ActiveSurfaceHandle;
            axAxAltairU1.QuickCreateChannel();
            if ((!axAxAltairU1.IsPortCreated))
            {
                MessageBox.Show("系統開啟失敗,請檢查CCD連線後重開程式");
                menuStrip主選單.Enabled = false;
                tabControl1.Enabled = false;
            }
            //取得系統預設路徑
            string temppath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            路徑_程式DATA資料夾 = temppath + "..\\..\\..\\DATA\\";
            路徑_生產批資料 = temppath + "..\\..\\..\\DATA\\生產批資料\\";
            路徑_系統SYS資料夾 = temppath + "..\\..\\..\\DATA\\SYS\\";
            路徑_產品資料夾 = temppath + "..\\..\\..\\DATA\\DEV\\";
            Dir_SYS_Default = temppath + "..\\..\\..\\DATA\\SYS\\GT8.ini";
            Dir_DEV_Default = temppath + "..\\..\\..\\DATA\\DEV\\Default\\Para.ini";
            路徑_點位總表 = temppath + "..\\..\\..\\DATA\\SYS\\IO.txt";
            路徑_動作流程8 = temppath + "..\\..\\..\\DATA\\SYS\\STEP8.txt";
            路徑_動作流程12 = temppath + "..\\..\\..\\DATA\\SYS\\STEP12.txt";
            路徑_警報條件 = temppath + "..\\..\\..\\DATA\\SYS\\ALARM.txt";

            Load_Sys(8);
            Load_PD("Default");
            F.Load(ref TextArray, temppath + "..\\..\\..\\DATA\\SYS\\Language.ini");
            Data_Loaded = true;
            Refreshimage_靜態影像();

            eng_IN();
            eng_OUT();
            //textBox_程式.Text = "";
            //ROI1.ParentHandle = ActiveHandle;
            //ROI2.ParentHandle = ActiveHandle;

            MV.ini(this);
            F.Load_PLCMatrix(ref F.InMatrix, ref F.OutMatrix, 路徑_點位總表);
            F.Load_STEP(ref StepArray, 路徑_動作流程8);
            更新步序參數值();
            //載入目前狀態
            using (StreamReader sr = new StreamReader(路徑_系統SYS資料夾 + "STATEIDX.txt"))
                ST.Curr_STATE.IDX = Convert.ToInt32(sr.ReadLine());
            using (StreamReader sr = new StreamReader(路徑_系統SYS資料夾 + "STEPIDX.txt"))
                ST.Curr_STEP = Convert.ToInt32(sr.ReadLine());
            ST.Req_STATE.IDX = 2;//要求復歸
            //彈匣狀態示意圖
            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                CASArray[i] = new CassetteClass();
            }
            CASArray[0].panel = F0_panel;
            CASArray[1].panel = F1_panel;
            CASArray[2].panel = F2_panel;
            CASArray[3].panel = F3_panel;
            CASArray[4].panel = F4_panel;
            CASArray[5].panel = F5_panel;
            CASArray[6].panel = F6_panel;
            CASArray[7].panel = F7_panel;
            CASArray[8].panel = F8_panel;
            CASArray[9].panel = F9_panel;
            CASArray[10].panel = F10_panel;
            CASArray[11].panel = F11_panel;
            CASArray[12].panel = F12_panel;

            CASArray[0].LB = F0_LB;
            CASArray[1].LB = F1_LB;
            CASArray[2].LB = F2_LB;
            CASArray[3].LB = F3_LB;
            CASArray[4].LB = F4_LB;
            CASArray[5].LB = F5_LB;
            CASArray[6].LB = F6_LB;
            CASArray[7].LB = F7_LB;
            CASArray[8].LB = F8_LB;
            CASArray[9].LB = F9_LB;
            CASArray[10].LB = F10_LB;
            CASArray[11].LB = F11_LB;
            CASArray[12].LB = F12_LB;
            讀取彈匣狀態(ref CASArray);
            //計算出所有軌道格位的絕對位置
            for (int i = 0; i <= CASArray.Length - 1; i++)
                CASArray[i].軌道格位高度 = SYS.彈匣軌道第一點 + (SYS.彈匣間距 * i);
            //計算出所有數片格位的絕對位置
            for (int i = 0; i <= CASArray.Length - 1; i++)
                CASArray[i].數片格位高度 = SYS.彈匣數片第一點 + (SYS.彈匣間距 * i);
            //寫入彈匣狀態();

            //宣告異常掃描timer, 等自動中才啟動
            //TimerCallback Alarm_callback = new TimerCallback(timer_AL_Tick);
            //timer_Alarm_Thread = new System.Threading.Timer(Alarm_callback, null, -1, 100);
            timer_Alarm_Thread = new System.Timers.Timer();
            timer_Alarm_Thread.Interval = 100;
            timer_Alarm_Thread.Elapsed += new System.Timers.ElapsedEventHandler(timer_ALM_Tick);
            this.timer_Alarm_Thread.SynchronizingObject = this;


            //宣告 狀態控制timer 直接啟動
            timer_ST_Thread = new System.Timers.Timer();
            timer_ST_Thread.Interval = 100;
            timer_ST_Thread.Elapsed += new System.Timers.ElapsedEventHandler(timer_ST_Tick);
            this.timer_ST_Thread.SynchronizingObject = this;
#if PCI1758
            timer_ST_Thread.Start();
#endif
            //宣告I/O 狀態讀取timer 直接啟動
            timer_IOM_Thread = new System.Timers.Timer();
            timer_IOM_Thread.Interval = 100;
            timer_IOM_Thread.Elapsed += new System.Timers.ElapsedEventHandler(timer_IOM_Tick);
            this.timer_IOM_Thread.SynchronizingObject = this;
#if PCI1758
            timer_IOM_Thread.Start();
            timer_介面.Enabled = true;//啟動狀態更新timer

            ////寫入伺服位置timer 開機讀取後啟動
            //timer_ServoPos_Thread = new System.Timers.Timer();
            //timer_ServoPos_Thread.Interval = 200;
            //timer_ServoPos_Thread.Elapsed += new System.Timers.ElapsedEventHandler(寫入所有伺服位置);
            //this.timer_ServoPos_Thread.SynchronizingObject = this;
            //timer_ServoPos_Thread.Start();

            //輸出LOG timer 開機讀取後啟動
            timer_輸出LOG = new System.Timers.Timer();
            timer_輸出LOG.Interval = 300;
            timer_輸出LOG.Elapsed += new System.Timers.ElapsedEventHandler(存檔工作);
            this.timer_輸出LOG.SynchronizingObject = this;
            timer_輸出LOG.Start();

            //下面一選擇 所有點位會歸零  要先做準備
            string DeviceName = "PCI-1758UDIO,BID#0";
            DI_1758.setSelectedDevice(DeviceName, AccessMode.ModeWriteWithReset, 0);
            DO_1758.setSelectedDevice(DeviceName, AccessMode.ModeWriteWithReset, 0);
#endif

            //test

            //Load_PD("try");
        }

        //
        public void 讀取彈匣狀態(ref CassetteClass[] CasArray)
        {
            string line = "";
            short IDX = 0;
            int 片數 = 0;
            StreamReader SR = new StreamReader(路徑_系統SYS資料夾 + "\\彈匣狀態.txt");
            Font fff = new Font("新細明體", 8, FontStyle.Bold);
            while (((line = SR.ReadLine()) != null) && (IDX <= CASArray.Length - 1))
            {
                if ((line.IndexOf("//") == -1) && (line.Length > 0))
                {
                    string[] StringArray = line.Split(new Char[] { ',' });
                    CasArray[IDX].檢測結果 = Convert.ToInt32(StringArray[2]);
                    CasArray[IDX].有無材料 = Convert.ToInt32(StringArray[1]);
                    CasArray[IDX].數片格位高度 = Convert.ToInt32(StringArray[3]);
                    CasArray[IDX].軌道格位高度 = Convert.ToInt32(StringArray[4]);
                    if (CasArray[IDX].有無材料 == 1)
                    {
                        片數++;
                        CasArray[IDX].panel.BackColor = Color.LightSkyBlue;//有料
                        CasArray[IDX].LB.Text = IDX.ToString();                                                   //  Graphics g =Graphics.FromHdc( CasArray[IDX].panel.CreateGraphics());

                        switch (CasArray[IDX].檢測結果)
                        {
                            case (1)://檢測OK
                                CasArray[IDX].panel.BackColor = Color.LimeGreen;//OK
                                CasArray[IDX].LB.Text = IDX + "-OK";
                                break;
                            case (2)://檢測NG
                                CasArray[IDX].panel.BackColor = Color.HotPink;//NG
                                CasArray[IDX].LB.Text = IDX + "-NG";
                                break;
                            case (3)://條碼讀取失敗
                                CasArray[IDX].panel.BackColor = Color.HotPink;
                                CasArray[IDX].LB.Text = IDX + "-MAP";
                                break;
                            case (4)://檢測NG
                                CasArray[IDX].panel.BackColor = Color.HotPink;//NG
                                break;
                            case (5)://找不到MAP
                                CasArray[IDX].panel.BackColor = Color.HotPink;
                                CasArray[IDX].LB.Text = IDX + "-MAP";
                                break;
                            default://尚未檢測

                                break;
                        }

                        // g.Dispose();
                    }
                    else
                    {
                        CasArray[IDX].panel.BackColor = Color.LightGray;//空格
                        CasArray[IDX].LB.Text = IDX.ToString();
                    }

                    CasArray[IDX].panel.Refresh();
                    IDX++;
                }
            }
            SR.Dispose();

            //Graphics g = CASArray[ST.plIDX].panel.CreateGraphics();
            //Pen pen = new Pen(Color.Blue, 3); //畫筆
            //Rectangle RECT = new Rectangle(0, 0, CASArray[ST.plIDX].panel.Width - 2,
            //    CASArray[ST.plIDX].panel.Height - 2);
            //g.DrawRectangle(pen, RECT);
            //g.Dispose();
            label30.Text = 片數.ToString();

        }
        public void 寫入彈匣狀態()
        {
            //int[] pos = new int[8];
            //for (short i = 0; i <= 7; i++)
            //{
            //    Motion._8164_get_command(i, ref pos[i]);
            //}
            int 片數 = 0;

            StreamWriter sw = new StreamWriter(路徑_系統SYS資料夾 + "\\彈匣狀態.txt", false);
            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                if (CASArray[i].有無材料 == 0)
                    CASArray[i].檢測結果 = 0;
                sw.WriteLine(i + "," + CASArray[i].有無材料 + "," + CASArray[i].檢測結果 + "," + CASArray[i].數片格位高度 + "," + CASArray[i].軌道格位高度);
                if (CASArray[i].有無材料 == 1)
                    片數++;
            }

            sw.Dispose();
            label30.Text = 片數.ToString();
        }

        public void 存檔工作(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_輸出LOG.Stop();
            if (flag_輸出LOG)
            {

                輸出LOG檔();
                flag_輸出LOG = false;
            }
            else if (flag_輸出格位資料)
            {
                儲存當前格位資料();
                flag_輸出格位資料 = false;
            }
            timer_輸出LOG.Start();
        }

        public void 寫入所有伺服位置(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_ServoPos_Thread.Stop();
            int[] pos = new int[8];
            for (short i = 0; i <= 7; i++)
            {
                Motion._8164_get_command(i, ref pos[i]);
            }
            StreamWriter sw = new StreamWriter(路徑_系統SYS資料夾 + "\\位置保持.txt", false);
            sw.WriteLine(
                  "0:" + pos[0] + "\r\n" +
                  "1:" + pos[1] + "\r\n" +
                  "2:" + pos[2] + "\r\n" +
                  "3:" + pos[3] + "\r\n" +
                  "4:" + pos[4] + "\r\n" +
                  "5:" + pos[5] + "\r\n" +
                  "6:" + pos[6] + "\r\n" +
                  "7:" + pos[7]
                  );
            sw.Dispose();
            timer_ServoPos_Thread.Start();
        }

        public void 讀取所有伺服位置檔案()
        {
            string line = "";
            short IDX = 0;
            StreamReader SR = new StreamReader(路徑_系統SYS資料夾 + "\\位置保持.txt");
            while (((line = SR.ReadLine()) != null) && (IDX <= 7))
            {
                string[] StringArray = line.Split(new Char[] { ':' });
                int Pos = Convert.ToInt32(StringArray[1]);
                Motion._8164_set_command(IDX, Pos);
                IDX++;
            }
            SR.Dispose();
        }

        //將指標轉成bitmap
        public Bitmap PtrToBitmap(long ImgPtr, int Width, int Height)
        {

            Bitmap tempBP = new Bitmap(Width, Height);
            BitmapData sourceData = tempBP.LockBits(new Rectangle(0, 0, Width, Height),
                                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            IntPtr source_scan = sourceData.Scan0;

            unsafe
            {
                byte* source_p = (byte*)source_scan.ToPointer();
                byte* source_Img = (byte*)ImgPtr;

                for (int height = 0; height < Height; ++height)
                {
                    for (int width = 0; width < Width; ++width)
                    {
                        //source_p[0] = source_Img[0]; //A 
                        //source_p++;
                        ////source_Img++;
                        source_p[0] = source_Img[0]; //R 
                        source_p++;
                        source_Img++;
                        source_p[0] = source_Img[0]; ; //G 
                        source_Img++;
                        source_p++;
                        source_p[0] = source_Img[0]; ; //B 
                        source_Img++;
                        source_p++;
                    }
                }
            }

            tempBP.UnlockBits(sourceData);

            return tempBP;
        }


        private void liveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //axAxCanvas1.Visible = true;
            axAxAltairU1.DacCh1Switch = true;
            axAxAltairU1.DacCh1Value = SYS.光源亮度;
            axAxAltairU1.Live();
            ST.Mode_Live = true;
            ROI_Draw.ParentHandle = img即時影像.VegaHandle;
            ROI1.ParentHandle = img即時影像.VegaHandle;
            //axAdvDIO1.WriteDoChannel(1, 12);
            zoom = 0.3f;
            axAxCanvas5.CanvasWidth = 0;
            axAxCanvas5.CanvasHeight = 0;
            return;
        }

        private void snapeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axAxAltairU1.Freeze();
            axAxAltairU1.SnapAndWait();
            ST.Mode_Live = false;

            //img1.SetSurfaceObj(axAxAltairU1.ActiveSurfaceHandle);
            //RefreshImage();
        }

        private void axAxAltairU1_OnSurfaceFilled(object sender, AxAxAltairUDrv.IAxAltairUEvents_OnSurfaceFilledEvent e)
        {
            img即時影像.SetSurfaceObj(axAxAltairU1.ActiveSurfaceHandle);
            ActiveHandle = e.surfaceHandle;
            ROI_Draw.ParentHandle = img即時影像.VegaHandle;
            ROI1.ParentHandle = img即時影像.VegaHandle;
            RefreshImage_動態影像(ref img即時影像);
        }

        public void eng_IN()
        {
            tableLayoutPanel2.ContextMenuStrip = contextMenu教導;
            //tabControl1.Enabled = true;
            工程登入ToolStripMenuItem.Text = "工程登出";
            panel7.BackColor = Color.Orange;
            panel8.Enabled = true;
            label44.Text = "工程模式";
            label44.BackColor = Color.Orange;
            Flag_ENG = true;
            ROI1.ParentHandle = ActiveHandle;
            ROI1.SetPlacement(100, 100, 500, 500);

            dataGridViewSYS.Enabled = true;
            dataGridViewPD.Enabled = true;
            contextMenu教導.Enabled = true;
            產品別ToolStripMenuItem.Enabled = true;
            button13.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            listBox1.Enabled = true;
            // tableLayoutPanel3.Enabled = true;
            RefreshImage_動態影像(ref img即時影像);
        }

        public void eng_OUT()
        {
            工程登入ToolStripMenuItem.Text = "工程登入";
            panel7.BackColor = Color.LightBlue;
            label44.Text = "生產模式";
            label44.BackColor = Color.LightBlue;
            Flag_ENG = false;
            dataGridViewSYS.Enabled = false;
            dataGridViewPD.Enabled = false;
            contextMenu教導.Enabled = false;
            產品別ToolStripMenuItem.Enabled = false;
            button13.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            listBox1.Enabled = false;
            panel8.Enabled = false;
            RefreshImage_動態影像(ref img即時影像);
        }

        private void 工程登入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (工程登入ToolStripMenuItem.Text.CompareTo("工程登入") == 0)
            {
                password PA = new password(this);
                PA.Top = 100;
                PA.Left = 100;
                PA.ShowDialog();
            }
            else
            {
                eng_OUT();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //axAxAltairU1.DacCh1Switch = true;
            //axAxAltairU1.DacCh1Value = F.Vi(DEVArray, "檢測亮度");
            //System.Threading.Thread.Sleep(50);

            axAxAltairU1.SnapAndWait();
            // axAxAltairU1.DacCh1Switch = false;
            axAxAltairU1.SaveFile(axAxAltairU1.ActiveSurfaceHandle, 路徑_程式DATA資料夾 + "SnapSaveA.bmp", AxAltairUDrv.TxAxauImageFileFormat.AXAU_IMAGE_FILE_FORMAT_BMP);


        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = 路徑_程式DATA資料夾 + "History\\";
            DialogResult result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;
            img_Globe.LoadFile(openFileDialog1.FileName);

            zoom = 0.057f;
            axAxCanvas1.CanvasWidth = Convert.ToInt32(img_Globe.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(img_Globe.ImageHeight * zoom);
            Refreshimage_靜態影像();
        }

        public void RefreshImage_動態影像(ref AxAxOvkBase.AxAxImageBW8 IMG)
        {
            if (!ST.Mode_Live)
            {
                Refreshimage_靜態影像();
                return;
            }
            axAxCanvas1.ClearCanvas();
            axAxCanvas1.CanvasWidth = Convert.ToInt32(IMG.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(IMG.ImageHeight * zoom);

            IMG.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            ROI_Draw.ParentHandle = IMG.VegaHandle;

            if (Flag_ENG)
            {
                int RectColor = 0;

                ROI1.Title = "";
                RectColor = 0xFFAA00;

                //畫出教導框
                ROI1.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, RectColor);
            }

            axAxCanvas1.RefreshCanvas();
        }

        private void 框架設計說明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string temppath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            Process.Start(temppath + "..\\..\\..\\..\\DATA\\note.txt");
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            zoom = zoom - 0.005f;
            RefreshImage_動態影像(ref img即時影像);
        }

        private void oRGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            zoom = 0.4f;
            RefreshImage_動態影像(ref img即時影像);
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            if (zoom >= 0.8f)
            {
                zoom = 0.8f;
                return;
            }
            zoom = zoom + 0.005f;
            if (ST.Mode_Live)
            {
                if (((img_Globe.ImageWidth * zoom) * (img_Globe.ImageHeight * zoom) > 25000000))
                {
                    zoom = zoom - 0.005f;
                }
            }

            RefreshImage_動態影像(ref img即時影像);
        }

        public void GetRotateROI(ref AxAxOvkBase.AxAxImageBW8 newimg, ref AxAxOvkBase.AxAxROIBW8 roi)
        {
            switch (PD.Degree)
            {
                case (90):
                    roi.SetPlacement(newimg.ImageWidth - roi.OrgY - roi.ROIHeight,
                        roi.OrgX, roi.ROIHeight, roi.ROIWidth);
                    break;
                case (180):
                    roi.SetPlacement(newimg.ImageWidth - roi.OrgX - roi.ROIWidth,
                                   newimg.ImageHeight - roi.OrgY - roi.ROIHeight, roi.ROIWidth, roi.ROIHeight);
                    break;
                case (270):
                    roi.SetPlacement(roi.OrgY,
                        newimg.ImageHeight - roi.ROIWidth - roi.OrgX,
                        roi.ROIHeight, roi.ROIWidth);
                    break;
                default:

                    break;
            }
            return;
        }

        public Point GetRotatePoint(ref AxAxOvkBase.AxAxImageBW8 newimg, Point SrcPot)
        {
            Point TempPoint = new Point(0, 0);
            switch (PD.Degree)
            {
                case (90):
                    TempPoint.X = newimg.ImageWidth - SrcPot.Y;
                    TempPoint.Y = SrcPot.X;
                    break;
                case (180):
                    TempPoint.X = newimg.ImageWidth - SrcPot.X;
                    TempPoint.Y = newimg.ImageHeight - SrcPot.Y;
                    break;
                case (270):
                    TempPoint.X = SrcPot.Y;
                    TempPoint.Y = newimg.ImageHeight - SrcPot.X;
                    break;
                default:
                    TempPoint = SrcPot;
                    break;
            }
            return TempPoint;
        }

        //更新中間主畫面
        public void Refreshimage_靜態影像()
        {
            //if (zoom2 < 0.05) return;
            if (img_Globe.ImageWidth * zoom < 800)
                axAxCanvas1.CanvasWidth = 800;
            else
                axAxCanvas1.CanvasWidth = Convert.ToInt32(img_Globe.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(img_Globe.ImageHeight * zoom);

            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            ROI_Draw.ParentHandle = img_Globe.VegaHandle;
            ROI_Draw.ShowPlacement = false;

            ROI1.ShowPlacement = true;
            //畫出教導框
            ROI1.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xFF3300);

            //劃出左邊ref die搜尋框
            roi_ref.SetPlacement(PD.左參考DIE影像X - PD.參考DIE搜尋範圍,
                                    PD.左參考DIE影像Y - PD.參考DIE搜尋範圍,
                                    mch_ref.PatternWidth + 2 * PD.參考DIE搜尋範圍,
                                    mch_ref.PatternHeight + 2 * PD.參考DIE搜尋範圍);

            roi_ref.ShowPlacement = false;
            mch_ref.DstImageHandle = roi_ref.VegaHandle;
            //劃出找到的ref die
            if (SYS.條碼位置1X > 0)
            {
                ROI_Draw.Title = "barcode1";
                ROI_Draw.SetPlacement(SYS.條碼位置1X,
                                      SYS.條碼位置1Y,
                                     SYS.條碼位置1W,
                                     SYS.條碼位置1H);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb);
            }
            if (SYS.條碼位置2X > 0)
            {
                ROI_Draw.Title = "barcode2";
                ROI_Draw.SetPlacement(SYS.條碼位置2X,
                                      SYS.條碼位置2Y,
                                     SYS.條碼位置2W,
                                     SYS.條碼位置2H);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb);
            }

            if (mch_ref.EffectMatch)
            {
                //mch_target.DrawMatchedPattern(axAxCanvas1.hDC, 0, zoom2, zoom2, Convert.ToInt32(roi_G.OrgX * -zoom2), Convert.ToInt32(roi_G.OrgY * -zoom2));
                roi_ref.ShowTitle = false;
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

                ROI_Draw.Title = "LRef";
                ROI_Draw.SetPlacement(mch_ref.MatchedX + roi_ref.OrgX,
                                      mch_ref.MatchedY + roi_ref.OrgY,
                                      mch_ref.PatternWidth,
                                      mch_ref.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
            }
            roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
            // //劃出右邊ref die搜尋框
            roi_refR.SetPlacement(PD.右參考DIE影像X - PD.參考DIE搜尋範圍,
                                     PD.右參考DIE影像Y - PD.參考DIE搜尋範圍,
                                     mch_ref.PatternWidth + 2 * PD.參考DIE搜尋範圍,
                                     mch_ref.PatternHeight + 2 * PD.參考DIE搜尋範圍);

            roi_refR.ShowPlacement = false;
            mch_refR.DstImageHandle = roi_refR.VegaHandle;
            mch_refR.AbsoluteCoord = false;
            if (mch_refR.EffectMatch)
            {
                //mch_target.DrawMatchedPattern(axAxCanvas1.hDC, 0, zoom2, zoom2, Convert.ToInt32(roi_G.OrgX * -zoom2), Convert.ToInt32(roi_G.OrgY * -zoom2));
                roi_refR.ShowTitle = false;
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

                ROI_Draw.Title = "RRef";
                ROI_Draw.SetPlacement(mch_refR.MatchedX + roi_refR.OrgX,
                                      mch_refR.MatchedY + roi_refR.OrgY,
                                      mch_refR.PatternWidth,
                                      mch_refR.PatternHeight);

                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_refR.ShowTitle = true;
                roi_refR.Title = "ref die miss";
            }
            roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

            if (Show_TempRect)
            {
                roi_Temp.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00D7FF);
                Show_TempRect = false;
            }


            //顯示所有定位格
            if (CK.Matrix != null)
            {
                for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
                {
                    for (int x = 0; x <= CK.Matrix.GetLength(1) - 1; x++)
                    {
                        if (((CK.Matrix[y, x].DieColor == BAD_Ept) && (checkBox1.Checked)) ||
                           ((CK.Matrix[y, x].DieColor == GOOD_DEV) && (checkBox5.Checked)) ||
                           ((CK.Matrix[y, x].DieColor == BAD_DEV) && (checkBox6.Checked)) ||
                           ((CK.Matrix[y, x].DieColor == GOOD_Ept) && (checkBox7.Checked)))
                        {
                            ROI_Draw.SetPlacement(CK.Matrix[y, x].coordX - PD.判定區尺寸W, CK.Matrix[y, x].coordY - PD.判定區尺寸H,
                                                                        PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);
                            ROI_Draw.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, CK.Matrix[y, x].DieColor);
                        }
                    }
                }
            }

            //顯示檢測結果();
            //輸出LOG檔();
            axAxCanvas1.RefreshCanvas();
        }

        private void axAxCanvas1_OnCanvasMouseDown(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseDownEvent e)
        {
            if (Control.MouseButtons == MouseButtons.Right)
            {
                return;
            }
            主畫面拖曳 = true;
            int indexX = 0;
            int indexY = 0;
            GetIndex(e.x, e.y, ref indexX, ref indexY);
            toolStripMenuItem5.Text = indexX.ToString();
            toolStripMenuItem6.Text = indexY.ToString();
            MainMenuStrip.Refresh();

            ROI1.ParentHandle = img_Globe.VegaHandle;
            if (DBstate)
            {
                ROI1.SetPlacement(Convert.ToInt32(e.x / zoom), Convert.ToInt32(e.y / zoom), 500, 500);
                if (ST.Mode_Live)
                    RefreshImage_動態影像(ref img即時影像);
                else
                    Refreshimage_靜態影像();
                DBstate = false;
            }
            else
            {
                timerDB.Enabled = true;
                DBstate = true;
            }

            MX = e.x;
            MY = e.y;
            DragHitHandleR1 = ROI1.HitTest(e.x, e.y, zoom, zoom, 0, 0);
            if (DragHitHandleR1 != TxAxHitHandle.AX_HANDLE_NONE)
            {
                DragFlagR1 = true;
            }
            //計算目前die座標
        }

        private void axAxCanvas1_OnCanvasMouseMove(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseMoveEvent e)
        {
            //int indexX = 0;
            //int indexY = 0;
            //GetIndex(e.x, e.y, ref indexX, ref indexY);
            //toolStripMenuItem5.Text = indexX.ToString();
            //toolStripMenuItem6.Text = indexY.ToString();
            //MainMenuStrip.Refresh();


            if (DragFlagR1)
            {
                ROI1.DragROI(DragHitHandleR1, e.x, e.y, zoom, zoom, 0, 0);

                if (ST.Mode_Live)
                    RefreshImage_動態影像(ref img即時影像);
                else
                    Refreshimage_靜態影像();
                //RefreshImage(ref img1);
            }
            else if (主畫面拖曳)
            {
                axAxCanvas1.HorzScrollValue -= Convert.ToInt32((e.x - MX) * zoom);
                axAxCanvas1.VertScrollValue -= Convert.ToInt32((e.y - MY) * zoom);
                // axAxCanvas1.Refresh();

            }
        }

        private void axAxCanvas1_OnCanvasMouseUp(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseUpEvent e)
        {
            DragFlagR1 = false;
            主畫面拖曳 = false;
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        //更新介面狀態
        public void RefreshPanel()
        {

            dataGridViewSYS.Columns[0].Width = 150;
            dataGridViewSYS.Columns[1].Width = 140;
            dataGridViewPD.Columns[0].Width = 150;
            dataGridViewPD.Columns[1].Width = 140;

            axAxCanvas2.ClearCanvas();
            axAxCanvas2.CanvasWidth = img_REF.ImageWidth * 2;
            axAxCanvas2.CanvasHeight = img_REF.ImageHeight * 2;
            axAxCanvas2.Width = axAxCanvas2.CanvasWidth;
            axAxCanvas2.Height = axAxCanvas2.CanvasHeight;
            axAxCanvas2.DrawSurface(img_REF.VegaHandle, 0.7f, 0.7f, 0, 0);
            axAxCanvas2.RefreshCanvas();

            axAxCanvas3.ClearCanvas();
            axAxCanvas3.CanvasWidth = img_DIE.ImageWidth * 2;
            axAxCanvas3.CanvasHeight = img_DIE.ImageHeight * 2;
            axAxCanvas3.Width = axAxCanvas3.CanvasWidth;
            axAxCanvas3.Height = axAxCanvas3.CanvasHeight;
            axAxCanvas3.DrawSurface(img_DIE.VegaHandle, 0.7f, 0.7f, 0, 0);
            axAxCanvas3.RefreshCanvas();

            axAxCanvas4.ClearCanvas();
            axAxCanvas4.CanvasWidth = img_EPT.ImageWidth * 2;
            axAxCanvas4.CanvasHeight = img_EPT.ImageHeight * 2;
            axAxCanvas4.Width = axAxCanvas4.CanvasWidth;
            axAxCanvas4.Height = axAxCanvas4.CanvasHeight;
            axAxCanvas4.DrawSurface(img_EPT.VegaHandle, 0.7f, 0.7f, 0, 0);
            axAxCanvas4.RefreshCanvas();

            textBox_程式.Text = F.Vs(PDArray, "產品代號");

        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!Data_Loaded) return;
            string 新值 = dataGridViewSYS.Rows[e.RowIndex].Cells[1].Value.ToString();
            string 參數名稱 = dataGridViewSYS.Rows[e.RowIndex].Cells[0].Value.ToString();
            string 舊值 = F.Vs(sysArray, 參數名稱);
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            F.SetV(ref sysArray, 參數名稱, 新值);
            Save_Sys(ST.晶圓尺寸);
            StreamWriter sw = new StreamWriter("D:\\WV_Log_Parameter\\" + today + "_log.txt", true);
            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss------")
                + "參數名稱：" + 參數名稱
                + "，舊參數：" + 舊值
                + "，新參數：" + 新值);
            sw.Dispose();

            axAxAltairU1.DacCh1Value = SYS.光源亮度;
            //重新計算料格數片高度
            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                //計算出所有格位的絕對位置
                CASArray[i].數片格位高度 = SYS.彈匣數片第一點 + (SYS.彈匣間距 * i);
            }
            //重新計算料格軌道高度
            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                //計算出所有格位的絕對位置
                CASArray[i].軌道格位高度 = SYS.彈匣軌道第一點 + (SYS.彈匣間距 * i);
            }
            寫入彈匣狀態();
            //重新設定速度
            MV.setSpeed(this);
            //重新更新當前步序檔
            更新步序參數值();
        }

        private void dataGridView3_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!Data_Loaded) return;
            string 新值 = dataGridViewPD.Rows[e.RowIndex].Cells[1].Value.ToString();
            string 參數名稱 = dataGridViewPD.Rows[e.RowIndex].Cells[0].Value.ToString();
            string 舊值 = F.Vs(PDArray, 參數名稱);
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            F.SetV(ref PDArray, 參數名稱, 新值);
            PD.Name = F.Vs(PDArray, "產品代號");
            Save_PD();
            RefreshPanel();
            RefreshImage_動態影像(ref img即時影像);

            //if (!System.IO.File.Exists("D:\\WV_Log_Parameter\\" + today + "_log.txt"))
            //{
            //    System.IO.File.Create("D:\\WV_Log_Parameter\\" + today + "_log.txt");
            //}

            StreamWriter sw = new StreamWriter("D:\\WV_Log_Parameter\\" + today + "_log.txt", true);
            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss------")
                + "參數名稱：" + 參數名稱
                + "，舊參數：" + 舊值
                + "，新參數：" + 新值);
            sw.Dispose();
        }

        public void Save_PD()
        {
            string Dir_SAVE;

            Dir_SAVE = 路徑_產品資料夾 + F.Vs(PDArray, "產品代號");

            string File_para = Dir_SAVE + "\\Para.ini";
            string DIE_path = Dir_SAVE + "\\die.bmp";
            string EPT_path = Dir_SAVE + "\\ept.bmp";
            string Globe_Path = Dir_SAVE + "\\Globe.bmp";
            string REF_path = Dir_SAVE + "\\ref.bmp";

            if (!Directory.Exists(Dir_SAVE)) Directory.CreateDirectory(Dir_SAVE);
            img_Globe.SaveFile(Globe_Path, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            img_DIE.SaveFile(DIE_path, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            img_EPT.SaveFile(EPT_path, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            img_REF.SaveFile(REF_path, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);

            F.Save(ref PDArray, File_para);
            Load_PD(F.Vs(PDArray, "產品代號"));
        }

        public void Save_Sys(int 晶圓尺寸)
        {
            F.Save(ref sysArray, Dir_SYS_Default);
            Load_Sys(晶圓尺寸);
            Refreshimage_靜態影像();
        }

        public void Load_Sys(int Size)
        {
            Data_Loaded = false;
            string temppath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            if (Size == 8)
                Dir_SYS_Default = temppath + "..\\..\\..\\DATA\\SYS\\GT8.ini";
            else
                Dir_SYS_Default = temppath + "..\\..\\..\\DATA\\SYS\\GT12.ini";

            F.Load(ref sysArray, Dir_SYS_Default);
            F.Para_To_DataGrid(ref dataGridViewSYS, ref sysArray);
            mch_條碼.LoadFile(路徑_系統SYS資料夾 + "條碼對位樣本.pat");
            SYS.ReadInfo(this);
            Data_Loaded = true;
        }

        //將Default產品參數檔格式套用到所有現有產品上
        //新參數 設為Default值, 不用之舊參數刪除
        public void Update_Para()
        {
            File_Class.Para_Structure[] DefaultArray = new File_Class.Para_Structure[50];
            File_Class.Para_Structure[] TempArray = new File_Class.Para_Structure[50];
            File_Class.Para_Structure[] DEVArray = new File_Class.Para_Structure[50];
            //載入Default產品
            F.Load(ref DefaultArray, Dir_DEV_Default);

            //取得所有產品別
            string[] DEVList = System.IO.Directory.GetDirectories(路徑_產品資料夾);
            for (int i = 0; i <= DEVList.Length - 1; i++)
            {
                int PosF = DEVList[i].LastIndexOf("\\");
                string JobName = DEVList[i].Substring(PosF + 1, DEVList[i].Length - PosF - 1);
                string TempPath = 路徑_產品資料夾 + JobName + "\\Para.ini";

                //載入要更新之產品
                F.Load(ref TempArray, TempPath);

                //檢查所有需要更新之產品
                //保留與default相同之參數的原值
                //新增default存在 但當前產品缺少的參數 並使用default值
                DEVArray[0].Name = "產品代號";
                DEVArray[0].Value = JobName;

                for (int d = 1; d <= DefaultArray.Length - 1; d++)
                {
                    string dePara = DefaultArray[d].Name;
                    string deValue = DefaultArray[d].Value;

                    for (int k = 0; k <= TempArray.Length - 1; k++)
                    {
                        //找不到該參數
                        if (TempArray[k].Name == null)
                        {
                            DEVArray[d].Name = dePara;
                            DEVArray[d].Value = deValue;
                            break;
                        }
                        if (TempArray[k].Name.CompareTo(dePara) == 0)
                        {
                            DEVArray[d].Name = TempArray[k].Name;
                            DEVArray[d].Value = TempArray[k].Value;
                            break;
                        }
                    }
                }

                F.Save(ref DEVArray, TempPath);
            }
        }

        public void Load_PD(string PDName)
        {
            Data_Loaded = false;//避免重複觸發DATAGRIDW變值事件 造成又進來LOAD 資料混亂
            string LoadDir = 路徑_產品資料夾 + PDName;
            string LoadFile = 路徑_產品資料夾 + PDName + "\\Para.ini";
            string Img_Path = LoadDir + "\\die.bmp";
            string ept_path = LoadDir + "\\ept.bmp";
            string Globe_path = LoadDir + "\\Globe.bmp";
            string ref_path = LoadDir + "\\ref.bmp";
            ST.產品代號 = PDName;
            //載入設定檔
            F.Load(ref PDArray, LoadFile);
            //F.Para_To_DataGrid(ref dataGridViewPD, ref PDArray);
            //載入影像檔
            img_Globe.LoadFile(Globe_path);//0113
            img_DIE.LoadFile(Img_Path);
            img_EPT.LoadFile(ept_path);
            img_REF.LoadFile(ref_path);

            //取得無效字元集合
            NullChar = F.Vs(PDArray, "無效字元");
            string[] StringArray = NullChar.Split(new Char[] { ',' });
            PD.NullList = new List<string>();
            for (int i = 0; i <= StringArray.Length - 1; i++)
            {
                PD.NullList.Add(StringArray[i]);
            }

            //取得留料字元集合
            FailChar = F.Vs(PDArray, "留料字元");
            StringArray = FailChar.Split(new Char[] { ',' });
            PD.FailList = new List<string>();
            for (int i = 0; i <= StringArray.Length - 1; i++)
            {
                PD.FailList.Add(StringArray[i]);
            }
            //img_EPT.SaveFile("D:\\bbbb.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            //取得取料字元集合
            string BinChar = F.Vs(PDArray, "取Bin等級");
            StringArray = BinChar.Split(new Char[] { ',' });
            PD.BinList = new List<string>();
            for (int i = 0; i <= StringArray.Length - 1; i++)
            {
                PD.BinList.Add(StringArray[i]);
            }

            //依照象限大小宣告影像陣列
            ST.晶圓尺寸 = F.Vi(PDArray, "產品尺寸");

            //載入產品及定位影像
            ROI1.ParentHandle = img_Globe.VegaHandle;
            ROI1.SetPlacement(300, 300, 100, 100);

            //學習產品定位匹配
            mch_DEV.SrcImageHandle = img_DIE.VegaHandle;
            mch_DEV.LearnPattern();
            mch_Ept.SrcImageHandle = img_EPT.VegaHandle;
            mch_Ept.LearnPattern();

            //尋找靶點初始化
            mch_ref.SrcImageHandle = img_REF.VegaHandle;
            mch_ref.LearnPattern();
            mch_ref.MaxPositions = 1;
            mch_ref.MinScore = F.Vf(PDArray, "定位門檻");
            mch_ref.ToleranceAngle = 10;
            mch_ref.AbsoluteCoord = true;

            mch_refR.SrcImageHandle = mch_ref.PatternVegaHandle;
            mch_refR.LearnPattern();
            mch_refR.MaxPositions = 1;
            mch_refR.MinScore = F.Vf(PDArray, "定位門檻");
            mch_refR.ToleranceAngle = 10;
            mch_refR.AbsoluteCoord = true;
            //參考DIE搜尋框
            roi_ref.ParentHandle = img_Globe.VegaHandle;
            roi_ref.SetPlacement(PD.左參考DIE影像X, PD.左參考DIE影像Y,
                                                        mch_ref.PatternWidth,
                                                        mch_ref.PatternHeight);
            roi_refR.ParentHandle = img_Globe.VegaHandle;
            roi_refR.SetPlacement(PD.右參考DIE影像X, PD.右參考DIE影像Y,
                                                        mch_ref.PatternWidth,
                                                        mch_ref.PatternHeight);

            //Canny邊緣搜尋初始化
            Canny_Operator1.MinGreyStep = F.Vi(PDArray, "邊緣灰階門檻值");
            Canny_Operator1.DataType = AxOvkGeometry.TxAxGeometryDataType.AX_GEOMETRY_DATATYPE_RAW_COORD;

            SYS.ReadInfo(this);
            PD.ReadInfo(this);
            textBox_程式.Text = PDName;
            if (!ST.Mode_Live)
                Refreshimage_靜態影像();

            RefreshPanel();
            textBox_批號.Text = "";
            textBox_客戶.Text = "";
            label_WID.Text = "";
            ST.晶圓ID = "";
            button11.Text = "";
            ST.MAPName = "";

            //img_EPT.SaveFile("D:\\bbbb.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            F.Para_To_DataGrid(ref dataGridViewPD, ref PDArray);
            Data_Loaded = true;
        }

        //程式關閉 進行狀態存檔
        private void MF_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer_IOM_Thread.Stop();
            axAxAltairU1.Freeze();
            axAxAltairU1.DestroyChannel();

            string PNAme = F.Vs(PDArray, "產品代號");
            if (PNAme.CompareTo("N/A") != 0)
            {
                //儲存當前生產片數
                F.Save(ref PDArray, 路徑_產品資料夾 + PNAme + "\\Para.ini");
            }

#if PCI1758
            //將汽缸退回至安全位置
            輸出1758(1, 1, 0);//材料推缸回
            System.Threading.Thread.Sleep(1000);
            輸出1758(0, 5, 0);//彈匣限位下
            System.Threading.Thread.Sleep(100);
            輸出1758(2, 0, 0);//大推缸推洩氣
            System.Threading.Thread.Sleep(100);
            輸出1758(0, 6, 1);//大推缸回
            System.Threading.Thread.Sleep(500);
            輸出1758(0, 6, 0);//大推缸回洩氣
            System.Threading.Thread.Sleep(100);
#endif
        }

        private void 燈源重啟ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void btn晶圓取像_Click(object sender, EventArgs e)
        {
            //工程動作中
            ST.Req_STATE.IDX = 3;//要求工程動作模式
            Get_Globe_Img(ref img_OrgGlobe);
            string Dir_SAVE;

            Dir_SAVE = 路徑_產品資料夾 + PD.Name;
            string Globe_Path = Dir_SAVE + "\\Globe.bmp";
            img_Globe.SaveFile(Globe_Path, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            ST.Req_STATE.IDX = 0;//切回停止
        }

        //晶圓水平校正
        public void H_Correction()
        {
            axAxAltairU1.Freeze();
            axAxAltairU1.DacCh1Switch = true;
            axAxAltairU1.DacCh1Value = PD.CKLight;
            if (ST.Req_STATE.IDX == 0) return;
            //走到水平校正位置
            絕對運動(MV.軸4載台X, SYS.水平X校正點, MV.X初速, MV.X常速, 0.4, 0.4);
            絕對運動(MV.軸5載台Y, SYS.水平Y校正點, MV.Y初速, MV.Y常速, 0.4, 0.4);
            if (ST.Req_STATE.IDX == 0) return;
            //轉盤轉正
            // 絕對運動(MV.軸6載台R, info.R接料點, MV.R初速, MV.R常速, 0.4, 0.4);
            MV.等待檢測平台動作完成();
            int CNT = 0;
            while (true)
            {
                if (ST.Req_STATE.IDX == 0) return;
                CNT++;
                //CCD取像
                System.Threading.Thread.Sleep(500);
                axAxAltairU1.SnapAndWait();
                //
                img_work.SetSurfaceObj(axAxAltairU1.ActiveSurfaceHandle);
                axAxCanvas1.CanvasWidth = Convert.ToInt32(img_work.ImageWidth * 0.3f);
                axAxCanvas1.CanvasHeight = Convert.ToInt32(img_work.ImageHeight * 0.3f);
                img_work.DrawImage(axAxCanvas1.hDC, 0.3f, 0.3f, 0, 0);
                //取得水平校正區域
                roi_Temp.ParentHandle = img_work.VegaHandle;
                roi_Temp.SetPlacement(SYS.水平區X, SYS.水平區Y, SYS.水平區W, SYS.水平區H);
                //roi_Temp.SaveFile("D:\\fff.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);

                //進行sobelY運算
                axAxKernel1.SrcImageHandle = roi_Temp.VegaHandle;
                axAxKernel1.DstImageHandle = img_work.VegaHandle;
                axAxKernel1.KernelType = AxOvkImage.TxAxKernelType.AX_SOBELY_KRNL;
                axAxKernel1.MaskOper();
                ROI_Work.ParentHandle = img_work.VegaHandle;
                ROI_Work.SetPlacement(50, 50, img_work.ImageWidth - 100, img_work.ImageHeight - 100);


                //對校正區域做區塊分析, 取得所有blob資料
                OBJ.SrcImageHandle = ROI_Work.VegaHandle;
                OBJ.ObjectClass = AxOvkBlob.TxAxObjClass.AX_OBJECT_DETECT_LIGHTER_CLASS;
                OBJ.ThresholdMethod = AxOvkBlob.TxAxObjThresholdMethod.AX_OBJECT_ITERATIVE_THRESHOLD_METHOD;
                OBJ.HighThreshold = OBJ.OptimalThreshold + 10;
                OBJ.BlobAnalyze(false);
                OBJ.CalculateFeatures(0x000021FF, -1);
                OBJ.SelectObjects(AxOvkBlob.TxAxObjFeature.AX_OBJECT_FEATURE_AREA,
                    AxOvkBlob.TxAxObjFeatureOperation.AX_OBJECT_REMOVE_LESS_THAN, 500);//面積太小的刪除

                OBJ.SortObjects(AxOvkBlob.TxAxObjFeatureSortOrder.AX_OBJECT_SORT_ORDER_LARGE_TO_SMALL,
                    AxOvkBlob.TxAxObjFeature.AX_OBJECT_FEATURE_AREA, -1, -1);
                //取最大blob計算斜率
                OBJ.BlobIndex = 0;
                toolStripMenuItem1.Text = OBJ.BlobOrientation.ToString();
                // richTextBox1.AppendText(OBJ.BlobOrientation.ToString() + "\r\n");
                roi_Temp.DrawRect(axAxCanvas1.hDC, 0.3f, 0.3f, 0, 0, 0xff);
                OBJ.DrawBlobs(axAxCanvas1.hDC, 0, 0.3f, 0.3f,
            Convert.ToInt32(roi_Temp.OrgX * 0.3f),
            Convert.ToInt32(roi_Temp.OrgY * 0.3f), true, 0xff);
                axAxCanvas1.RefreshCanvas();//更新畫面
                if ((Math.Abs(OBJ.BlobOrientation) < 0.1) ||
                    (Math.Abs(OBJ.BlobOrientation - 360) < 0.1)
                    || (CNT > 10))
                {
                    break;//校正完畢
                }
                Application.DoEvents();
                double R補償脈波量 = 0;
                if (OBJ.BlobOrientation < 90)
                {
                    R補償脈波量 = -1 * OBJ.BlobOrientation * 168;
                }
                else
                {
                    R補償脈波量 = (180 - OBJ.BlobOrientation) * 168;
                }
                相對運動(MV.軸6載台R, R補償脈波量, MV.R初速, MV.R常速, 0.4, 0.4);
                System.Threading.Thread.Sleep(100);
                while ((Motion._8164_motion_done(6) > 0))
                    Application.DoEvents();
            }
            img_work.SetSurfaceObj(0);
        }

        public void Get_Globe_Img(ref AxAxOvkBase.AxAxImageBW8 SrcIMG)
        {
            GC.Collect();
            //輸出載台真空
            輸出1758(0, 0, 1);
            //天車閃開到載船點
            絕對運動(1, SYS.天車拉出點, MV.天車初速, MV.天車常速, 0.2, 0.2);
            絕對運動(MV.軸7CCDZ, SYS.CCD檢測高度, MV.Z初速, MV.Z常速, 0.4, 0.4);
            //進行水平校正
            H_Correction();
            ST.Mode_Live = false;
            //****************走伺服取像********************
            axAxAltairU1.DacCh1Switch = true;
            axAxAltairU1.DacCh1Value = PD.CKLight;
            //img_Globe.SetSize(info.象限數X * axAxAltairU1.ImageWidth, info.象限數Y * axAxAltairU1.ImageHeight);

            int BlockW = axAxAltairU1.ImageWidth - SYS.CutXR - SYS.CutXL; //取得可用範圍影像寬度
            int BlockH = axAxAltairU1.ImageHeight - SYS.CutYT - SYS.CutYB; //取得可用範圍影像高度
            if (ST.Req_STATE.IDX == 0) return;
            //伺服走到第一點位置
            絕對運動(MV.軸4載台X, SYS.X第一點, MV.X初速, MV.X常速, 0.4, 0.4);
            絕對運動(MV.軸5載台Y, SYS.Y第一點, MV.Y初速, MV.Y常速, 0.4, 0.4);
            //轉盤轉正
            //絕對運動(MV.軸6載台R, info.R接料點, MV.R初速, MV.R常速, 0.4, 0.4);
            if (ST.Req_STATE.IDX == 0) return;
            MV.等待檢測平台動作完成();
            int XPOS = 0;
            int YPOS = 0;
            for (int y = 0; y <= SYS.象限數Y - 1; y++)
            {
                if (ST.Req_STATE.IDX == 0) return;
                //走到y下一個位置
                絕對運動(MV.軸5載台Y, SYS.Y第一點 - (y * SYS.Y檢測間距),
                    MV.Y初速, MV.Y常速, 0.5, 0.5);

                if (y % 2 == 0)
                {
                    XPOS = 0;
                    for (int x = 0; x <= SYS.象限數X - 1; x++)
                    {
                        //int a = 0;
                        //Motion._8164_get_command(4,ref a);
                        //偵測到停止或是安全光幕上微分訊號   解除檢測
                        //走到x下一個位置
                        絕對運動(MV.軸4載台X, SYS.X第一點 + (x * SYS.X檢測間距),
                            MV.X初速, MV.X常速, 0.5, 0.5);
                        Application.DoEvents();
                        MV.等待檢測平台動作完成();
                        //Motion._8164_get_command(4, ref a);

                        //取像
                        System.Threading.Thread.Sleep(150);
                        axAxAltairU1.SnapAndWait();

                        axAxAltairU1.SaveFile(axAxAltairU1.ActiveSurfaceHandle,
                            "D:\\MERGE\\F" + x + "_" + y + ".jpg",
                            AxAltairUDrv.TxAxauImageFileFormat.AXAU_IMAGE_FILE_FORMAT_JPEG);
                    }
                }
                else
                {
                    XPOS = SrcIMG.ImageWidth - BlockW - SYS.CutXR;
                    for (int x = SYS.象限數X - 1; x >= 0; x--)
                    {
                        if (ST.Req_STATE.IDX == 0) return;
                        //偵測到停止或是安全光幕上微分訊號   解除檢測
                        //走到x下一個位置
                        絕對運動(MV.軸4載台X, SYS.X第一點 + (x * SYS.X檢測間距),
                                                   MV.X初速, MV.X常速, 0.4, 0.4);
                        MV.等待檢測平台動作完成();
                        //取像
                        System.Threading.Thread.Sleep(150);
                        axAxAltairU1.SnapAndWait();
                        axAxAltairU1.SaveFile(axAxAltairU1.ActiveSurfaceHandle,
                            "D:\\MERGE\\F" + x + "_" + y + ".jpg",
                            AxAltairUDrv.TxAxauImageFileFormat.AXAU_IMAGE_FILE_FORMAT_JPEG);
                    }
                }
                XPOS = 0;
                YPOS = YPOS + ROI_Draw.ROIHeight;
            }

            MV.等待檢測平台動作完成();
            //*****************進行接圖*************************************
            zoom = 0.057f;
            MergeImg(ref SrcIMG);
            //*************************************接圖完畢*************************************
            //計算晶圓角度
            //進行影像旋轉處理  MAP檔案角度 + 晶圓偏移角度
            axAxImageRotator1.RotateCenterX = 0;
            axAxImageRotator1.RotateCenterY = 0;
            axAxImageRotator1.RotateDegree = 360 - PD.Degree;
            axAxImageRotator1.RotatorMethod = AxOvkImage.TxAxImageRotatorMethod.AX_ROTATE_ANY_ANGLE_WRT_CENTER_TO_PROPER_SIZE;
            axAxImageRotator1.SrcImageHandle = SrcIMG.VegaHandle;
            axAxImageRotator1.DstImageHandle = img_Globe.VegaHandle;
            axAxImageRotator1.Rotate();
            SrcIMG.SetSize(1, 1);
            ////進行影像強化處理
            Refreshimage_靜態影像();
            axAxAltairU1.Freeze();
            //關閉載台真空
            輸出1758(0, 0, 0);
            GC.Collect();
        }

        public void 儲存當前格位資料()
        {
            if ((textBox_批號.Text.Length == 0) ||
                (textBox_客戶.Text.Length == 0) ||
                CK.晶粒總數 == 0)
                return;
            ST.批號 = textBox_批號.Text;
            ST.客批 = textBox_客戶.Text;

            string 本機LOG路徑 = "D:\\WV_LOG\\" + ST.批號 + "_" + ST.客批 + "\\" + ST.晶圓ID + ".log";
            string 存檔資料夾 = 路徑_生產批資料 + ST.當前格位;
            string 當前彈匣LOG路徑 = 存檔資料夾 + "\\檢測紀錄.log";
            img_Globe.SaveFile(存檔資料夾 + "\\Globe.jpg", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_JPG);
            System.IO.File.Copy(本機LOG路徑, 當前彈匣LOG路徑, true);
            axAxCanvas1.SaveFile(存檔資料夾 + "\\檢測結果.jpg", TxAxCanvasSaveFileType.AX_CANVAS_SAVE_FILE_TYPE_JPG);
        }

        private void 新產品ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //判斷是否為新產品
            if (F.Vs(PDArray, "產品代號").CompareTo("Default") == 0)
            {
                string value = "Default";
                if (InputBox("請輸入新產品代號:", "新產品代號:", ref value) == DialogResult.OK)
                {
                    if ((value.CompareTo("Default") != 0) && (value.CompareTo("") != 0))
                    {
                        string TempPath = 路徑_產品資料夾 + value;
                        if (Directory.Exists(TempPath))
                        {
                            DialogResult result1 =
                            MessageBox.Show("此產品已存在,是否覆蓋原始設定?",
                            "產品已存在",
                            MessageBoxButtons.YesNo);

                            if (result1 == DialogResult.Yes)
                            {
                                F.SetV(ref PDArray, "產品代號", value);
                                Save_PD();

                            }
                            else
                                return;
                        }
                        else
                        {
                            F.SetV(ref PDArray, "產品代號", value);
                            Save_PD();
                        }
                    }
                    else
                    {
                        MessageBox.Show("產品代號不合規定!請重新命名!");
                        return;
                    }
                }
                else
                {
                    //放棄新增新產品
                    return;
                }
            }
            F.Para_To_DataGrid(ref dataGridViewPD, ref PDArray);
            Refreshimage_靜態影像();
        }

        private void 載入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //顯示產品管理視窗
            ProjectManage PM = new ProjectManage(this);
            PM.Show();
        }

        private void btn單次檢測_Click(object sender, EventArgs e)
        {
            string MSG = 讀取錯誤訊息();
            //if (MSG.Length > 0)
            //{
            //    password PA = new password(this, MSG);
            //    PA.ShowDialog();
            //    return;
            //}

            //string result;
            //if (cb手動輸入.Checked)
            //    result = 條碼模組(true);
            //else
            //    result = 條碼模組(false);
            //if (result.Length > 0)
            //    return;
            ST.Req_STATE.IDX = 3;//請求工程動作

            檢測模組();
            ST.Req_STATE.IDX = 0;//請求工程動作
        }

        private void 教導空料影像ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            img_EPT.SetSize(ROI1.ROIWidth, ROI1.ROIHeight);
            axAxImageCopier1.SrcImageHandle = ROI1.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_EPT.VegaHandle;
            axAxImageCopier1.Copy();
            mch_Ept.SrcImageHandle = img_EPT.VegaHandle;
            mch_Ept.LearnPattern();
            Save_PD();
        }

        private void axAxCanvas5_OnCanvasMouseDown(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseDownEvent e)
        {
            DragHitHandleG = ROI2.HitTest(e.x, e.y, zoom分析, zoom分析, 0, 0);
            if (DragHitHandleG != TxAxHitHandle.AX_HANDLE_NONE)
                DragFlagG = true;
        }

        private void axAxCanvas5_OnCanvasMouseMove(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseMoveEvent e)
        {
            axAxImageStatistics1.SrcImageHandle = ROI2.VegaHandle;
            axAxImageStatistics1.GetStatistics();
            label17.Text = axAxImageStatistics1.BlueMean.ToString();
            if (DragFlagG)
            {
                ROI2.DragROI(DragHitHandleG, e.x, e.y, zoom分析, zoom分析, 0, 0);
                Refreshimage_Area();
            }
        }

        private void axAxCanvas5_OnCanvasMouseUp(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseUpEvent e)
        {
            DragFlagG = false;
        }


        private void btn靜態分析_Click(object sender, EventArgs e)
        {
            process_Wafer(true);
        }


        public Point Process_RowNOMAP(ref AxAxOvkBase.AxAxROIBW8 roi_m, ref Point CurrIDX, ref Point CurrPoint)
        {
            roi_m.SetPlacement(0, roi_m.OrgY - PD.ROW搜尋高度, roi_m.ROIWidth, (2 * PD.ROW搜尋高度));
            //空料Die
            mch_Ept.DstImageHandle = roi_m.VegaHandle;
            mch_Ept.Match();
            mch_Ept.PatternIndex = 0;
            //有料Die
            mch_DEV.DstImageHandle = roi_m.VegaHandle;
            mch_DEV.Match();
            mch_DEV.PatternIndex = 0;
            // mch_DEV.DrawMatchedPattern(axAxCanvas1.hDC, -1, zoom2, zoom2, 0, 0);
            ROI_Draw.ParentHandle = img_Globe.VegaHandle;
            ROI_Draw.ShowTitle = false;

            Point AnchorIDX = new Point(0, 0);//宣告錨DIE IDX
            if (CurrIDX.Y == MAP.FstY)//參考ROW的參考DIE第一顆直接設定為錨DIE
            {
                CurrIDX.X = MAP.FstX;//訂出本行起始點位置
                CurrPoint.X = Convert.ToInt32(ROI1.OrgX * zoom);
                AnchorIDX.X = MAP.FstX;
                AnchorIDX.Y = MAP.FstY;
                //存入中心點位置
                Matrix_NMP[MAP.FstY, MAP.FstX].coordX = Convert.ToInt32(ROI1.OrgX) + (mch_ref.PatternWidth / 2);//mch使用相對座標, 因此需要加上ROI基底
                Matrix_NMP[MAP.FstY, MAP.FstX].coordY = Convert.ToInt32(ROI1.OrgY) + (mch_ref.PatternHeight / 2);// 存入CK矩陣的位置使用絕對座標
                Matrix_NMP[MAP.FstY, MAP.FstX].CheckState = 99;//匹配成功 位置穩定
                Matrix_NMP[MAP.FstY, MAP.FstX].DieState = 128;//參考DIE
                AnchorIDX.X = MAP.FstX;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                AnchorIDX.Y = CurrIDX.Y;
            }

            //計算當前ROW 所有mch的平均Y座標
            int Total_Y = 0;
            int AVG_Y = 0;
            if (mch_Ept.NumMatchedPos > 0)
            {
                for (int i = 0; i <= mch_Ept.NumMatchedPos - 1; i++)
                {
                    mch_Ept.PatternIndex = i;
                    Total_Y += mch_Ept.MatchedY;
                }
                AVG_Y = Total_Y / mch_Ept.NumMatchedPos;
            }
            else if (mch_DEV.NumMatchedPos > 0)
            {
                for (int i = 0; i <= mch_DEV.NumMatchedPos - 1; i++)
                {
                    mch_DEV.PatternIndex = i;
                    Total_Y += mch_DEV.MatchedY;
                }
                AVG_Y = Total_Y / mch_DEV.NumMatchedPos;
            }
            else
            {
                AVG_Y = roi_m.ROIHeight / 2;    //原中心線位置
            }
            ROI_Draw.Title = CurrIDX.Y.ToString();
            ROI_Draw.ShowTitle = false;
            //開始從參考DIE往兩個方向定位
            //SideClass SIDE = new SideClass(); //宣告 判斷中心點在哪個IDX的預估範圍界線
            Point 預測中心點 = new Point(0, 0);
            //***************************向左走*******************************
            for (int i = MAP.FstX; i >= 0; i--)
            {
                if (CurrIDX.Y == MAP.FstY)
                    //參考ROW預期位置推導 左極限從右側錨DIE來推(最靠近的一顆有定位成功的DIE)
                    預測中心點.X = Matrix_NMP[CurrIDX.Y, AnchorIDX.X].coordX - (PD.DieW * (AnchorIDX.X - i));

                else if (CurrIDX.Y < MAP.FstY && Matrix_NMP[CurrIDX.Y + 1, i].coordX >= 0)//上半部ROW直接參考下一排
                    預測中心點.X = Matrix_NMP[CurrIDX.Y + 1, i].coordX;//- DEV.reCoAreaX;

                else if (CurrIDX.Y > MAP.FstY && Matrix_NMP[CurrIDX.Y - 1, i].coordX >= 0)//下半部ROW直接參考上一排
                    預測中心點.X = Matrix_NMP[CurrIDX.Y - 1, i].coordX;//- DEV.reCoAreaX;

                else if (Matrix_NMP[CurrIDX.Y, AnchorIDX.X].coordX > 0)//如果上/下排尚未定位，比照第一排
                    預測中心點.X = Matrix_NMP[CurrIDX.Y, AnchorIDX.X].coordX - (PD.DieW * (AnchorIDX.X - i));// -DEV.reCoAreaX;
                else                                                  //如果基準點無效，則參考起始排
                    預測中心點.X = Matrix_NMP[MAP.FstY, AnchorIDX.X].coordX - (PD.DieW * (AnchorIDX.X - i));

                預測中心點.Y = AVG_Y;//將預期Y座標設定為平均座Y座標
                                //檢查所有有料定位結果, 找到坐落在預期位置當中的樣本
                for (int k = 0; k <= mch_DEV.NumMatchedPos - 1; k++)
                {
                    mch_DEV.PatternIndex = k;
                    if ((Math.Abs(mch_DEV.MatchedX - 預測中心點.X) < PD.中心誤差X) &&
                        (Math.Abs(mch_DEV.MatchedY - 預測中心點.Y) < PD.中心誤差Y))
                    {
                        Matrix_NMP[CurrIDX.Y, i].coordX = mch_DEV.MatchedX + roi_m.OrgX;//mch使用相對座標, 因此需要加上ROI基底
                        Matrix_NMP[CurrIDX.Y, i].coordY = mch_DEV.MatchedY + roi_m.OrgY;// 存入CK矩陣的位置使用絕對座標
                        Matrix_NMP[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                        Matrix_NMP[CurrIDX.Y, i].DieState = 1;//有料
                        ROI_Draw.SetPlacement(Matrix_NMP[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                              Matrix_NMP[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                              PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                        //判定是否留料(綠色)
                        Matrix_NMP[CurrIDX.Y, i].DieColor = 0XFF00FF;
                        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0XFF00FF);
                        AnchorIDX.X = i;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                        AnchorIDX.Y = CurrIDX.Y;
                        break;
                    }
                }
                //檢查所有空格定位結果, 找到坐落在預期位置當中的樣本
                for (int k = 0; k <= mch_Ept.NumMatchedPos - 1; k++)
                {
                    mch_Ept.PatternIndex = k;
                    if ((Math.Abs(mch_Ept.MatchedX - 預測中心點.X) < PD.中心誤差X) &&
                        (Math.Abs(mch_Ept.MatchedY - 預測中心點.Y) < PD.中心誤差Y))
                    {
                        Matrix_NMP[CurrIDX.Y, i].coordX = mch_Ept.MatchedX + roi_m.OrgX;//mch使用相對座標, 因此需要加上ROI基底
                        Matrix_NMP[CurrIDX.Y, i].coordY = mch_Ept.MatchedY + roi_m.OrgY;// 存入CK矩陣的位置使用絕對座標
                        Matrix_NMP[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                        Matrix_NMP[CurrIDX.Y, i].DieState = 0;//空料
                        ROI_Draw.SetPlacement(Matrix_NMP[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                              Matrix_NMP[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                              PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);
                        Matrix_NMP[CurrIDX.Y, i].DieColor = GOOD_Ept;
                        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0X00Aaa0);
                        AnchorIDX.X = i;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                        AnchorIDX.Y = CurrIDX.Y;
                        break;
                    }
                }
                //}
            }
            //**********************向右走***************
            AnchorIDX.X = MAP.FstX; //定位Die移回參考Die
            for (int i = MAP.FstX + 1; i <= Matrix_NMP.GetLength(0) - 1; i++)
            {
                if (CurrIDX.Y == MAP.FstY)
                    //參考ROW預期位置推導 左極限從右側錨DIE來推(最靠近的一顆有訂位成功的DIE)
                    預測中心點.X = Matrix_NMP[CurrIDX.Y, AnchorIDX.X].coordX + (PD.DieW * (i - AnchorIDX.X));// - DEV.reCoAreaX;

                else if (CurrIDX.Y < MAP.FstY && Matrix_NMP[CurrIDX.Y + 1, i].coordX >= 0)//上半部ROW直接參考下一排
                    預測中心點.X = Matrix_NMP[CurrIDX.Y + 1, i].coordX;// - DEV.reCoAreaX;

                else if (CurrIDX.Y > MAP.FstY && Matrix_NMP[CurrIDX.Y - 1, i].coordX >= 0)//下半部ROW直接參考上一排
                    預測中心點.X = Matrix_NMP[CurrIDX.Y - 1, i].coordX;// - DEV.reCoAreaX;

                else if (Matrix_NMP[CurrIDX.Y, AnchorIDX.X].coordX > 0)//如果上/下排尚未定位，比照第一排
                    預測中心點.X = Matrix_NMP[CurrIDX.Y, AnchorIDX.X].coordX + (PD.DieW * (i - AnchorIDX.X));// -DEV.reCoAreaX;
                else                                                  //如果基準點無效，則參考起始排
                    預測中心點.X = Matrix_NMP[MAP.FstY, AnchorIDX.X].coordX + (PD.DieW * (i - AnchorIDX.X));

                預測中心點.Y = AVG_Y;//將預期Y座標設定為平均座Y座標

                //檢查所有有料定位結果, 找到坐落在預期位置當中的樣本
                for (int k = 0; k <= mch_DEV.NumMatchedPos - 1; k++)
                {
                    mch_DEV.PatternIndex = k;
                    if ((Math.Abs(mch_DEV.MatchedX - 預測中心點.X) < PD.中心誤差X) &&
                        (Math.Abs(mch_DEV.MatchedY - 預測中心點.Y) < PD.中心誤差Y))
                    {
                        Matrix_NMP[CurrIDX.Y, i].coordX = mch_DEV.MatchedX + roi_m.OrgX;//mch使用相對座標, 因此需要加上ROI基底
                        Matrix_NMP[CurrIDX.Y, i].coordY = mch_DEV.MatchedY + roi_m.OrgY;// 存入CK矩陣的位置使用絕對座標
                        Matrix_NMP[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                        Matrix_NMP[CurrIDX.Y, i].DieState = 1;//有料
                        ROI_Draw.SetPlacement(Matrix_NMP[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                              Matrix_NMP[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                              PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                        //判定是否留料(綠色)
                        Matrix_NMP[CurrIDX.Y, i].DieColor = 0XFF00FF;
                        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0XFF00FF);
                        AnchorIDX.X = i;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                        AnchorIDX.Y = CurrIDX.Y;
                        break;
                    }
                }
                for (int k = 0; k <= mch_Ept.NumMatchedPos - 1; k++)
                {
                    mch_Ept.PatternIndex = k;
                    if ((Math.Abs(mch_Ept.MatchedX - 預測中心點.X) < PD.中心誤差X) && (Math.Abs(mch_Ept.MatchedY - 預測中心點.Y) < PD.中心誤差Y))
                    {
                        Matrix_NMP[CurrIDX.Y, i].coordX = mch_Ept.MatchedX + roi_m.OrgX;
                        Matrix_NMP[CurrIDX.Y, i].coordY = mch_Ept.MatchedY + roi_m.OrgY;
                        Matrix_NMP[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                        Matrix_NMP[CurrIDX.Y, i].DieState = 0;//空料
                                                              //判定是否對應空料
                        ROI_Draw.SetPlacement(Matrix_NMP[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                              Matrix_NMP[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                              PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);
                        Matrix_NMP[CurrIDX.Y, i].DieColor = GOOD_Ept;
                        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0X00Aaa0);
                        AnchorIDX.X = i;
                        AnchorIDX.Y = CurrIDX.Y;

                        break;
                    }
                }
                //}
            }

            //左右都定位完畢, 從頭開始檢查那些格位還沒有定位成功, 參考最靠進的鄰居推出定位
            for (int i = 0; i < Matrix_NMP.GetLength(1); i++)
            {
                if (Matrix_NMP[CurrIDX.Y, i].CheckState != 99)
                {
                    bool finish_flag = false;
                    //非第一排參考上下鄰居
                    if (CurrIDX.Y != MAP.FstY)
                    {
                        if (CurrIDX.Y < MAP.FstY && Matrix_NMP[CurrIDX.Y + 1, i].coordX >= 0)//上半部ROW直接參考下一排
                        {
                            Matrix_NMP[CurrIDX.Y, i].coordX = Matrix_NMP[CurrIDX.Y + 1, i].coordX;
                            Matrix_NMP[CurrIDX.Y, i].coordY = Matrix_NMP[CurrIDX.Y + 1, i].coordY - PD.DieH;
                            finish_flag = true;
                        }
                        else if (CurrIDX.Y > MAP.FstY && Matrix_NMP[CurrIDX.Y - 1, i].coordX >= 0)//下半部ROW直接參考上一排
                        {
                            Matrix_NMP[CurrIDX.Y, i].coordX = Matrix_NMP[CurrIDX.Y - 1, i].coordX;
                            Matrix_NMP[CurrIDX.Y, i].coordY = Matrix_NMP[CurrIDX.Y + 1, i].coordY + PD.DieH;
                            finish_flag = true;
                        }
                        else
                        {
                            finish_flag = false;
                        }

                        if (finish_flag)
                        {
                            // Matrix_NMP[CurrIDX.Y, i].coordY = AVG_Y + roi_m.OrgY;

                        }
                    }
                    finish_flag = false;
                    //第一排或上下參考失敗參考左右鄰居
                    if (!finish_flag)
                    {
                        int CNT = 0;
                        while (Matrix_NMP[CurrIDX.Y, i + CNT].CheckState != 99)
                        {
                            if (i < MAP.FstX)
                                CNT++;
                            else
                                CNT--;

                            //如果向右找到底皆未找到，則向左找
                            if (((i + CNT) >= (10)) || ((i + CNT) <= 0))
                            {
                                CNT = 0;
                                while (Matrix_NMP[CurrIDX.Y, i + CNT].CheckState != 99)
                                {
                                    if (i < MAP.FstX)
                                        CNT--;
                                    else
                                        CNT++;

                                    if (((i + CNT) >= (10)) || ((i + CNT) <= 0))
                                    {
                                        MessageBox.Show("檢測參數設定錯誤，請檢查參數或嘗試重新教導產品。", "定位失敗");
                                        return new Point(0, 0);//回傳 X沒有意義直接設0
                                    }
                                }
                                break;
                            }
                        }

                        Matrix_NMP[CurrIDX.Y, i].coordX = Matrix_NMP[CurrIDX.Y, i + CNT].coordX - (PD.DieW * CNT);
                        Matrix_NMP[CurrIDX.Y, i].coordY = Matrix_NMP[CurrIDX.Y, i + CNT].coordY;
                    }

                    Matrix_NMP[CurrIDX.Y, i].CheckState = 70;//靠鄰居來的位置
                    ROI_Draw.SetPlacement(Matrix_NMP[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                          Matrix_NMP[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                          PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);


                    //使用CannyOperator判斷是否為空料

                    //用Die寬/高設定判定範圍
                    roi_Temp.ParentHandle = img_Globe.VegaHandle;
                    roi_Temp.SetPlacement((int)(Matrix_NMP[CurrIDX.Y, i].coordX - PD.DieW * 0.35),
                                       (int)(Matrix_NMP[CurrIDX.Y, i].coordY - PD.DieH * 0.35),
                                       (int)(PD.DieW * 0.7), (int)(PD.DieH * 0.7));
                    //用判定區尺寸判定範圍
                    ROI_Draw.ParentHandle = img_Globe.VegaHandle;

                    Canny_Operator1.SrcImageHandle = ROI_Draw.VegaHandle;
                    Canny_Operator1.DetectPrimitives();

                    if (Canny_Operator1.PointCount < PD.Canny_count)
                    {
                        Matrix_NMP[CurrIDX.Y, i].DieState = 0;//空料
                        Matrix_NMP[CurrIDX.Y, i].DieColor = GOOD_Ept;
                        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0X00Aaa0);
                        g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
                        Font fff = new Font("新細明體", 8, FontStyle.Bold);
                        g.DrawString(Canny_Operator1.PointCount.ToString(), fff, Brushes.Red,
                            ROI_Draw.OrgX * zoom + 1, ROI_Draw.OrgY * zoom + 1);
                        g.Dispose();


                    }
                    else
                    {
                        Matrix_NMP[CurrIDX.Y, i].DieState = 1;//有料
                        Matrix_NMP[CurrIDX.Y, i].DieColor = 0XFF00FF;
                        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0XFF00FF);
                        g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
                        Font fff = new Font("新細明體", 8, FontStyle.Bold);
                        g.DrawString(Canny_Operator1.PointCount.ToString(), fff, Brushes.Red,
                            ROI_Draw.OrgX * zoom + 1, ROI_Draw.OrgY * zoom + 1);
                        g.Dispose();
                    }
                    /********************************************************************************/
                }
            }
            return new Point(0, AVG_Y + roi_m.OrgY);//回傳 X沒有意義直接設0
        }

        //處理ROW
        public Point Process_Row(ref AxAxOvkBase.AxAxROIBW8 roi_m, ref Point CurrIDX, ref Point CurrPoint)
        {
            roi_m.SetPlacement(0, roi_m.OrgY - PD.ROW搜尋高度, roi_m.ROIWidth, (2 * PD.ROW搜尋高度));
            //空料Die
            mch_Ept.DstImageHandle = roi_m.VegaHandle;
            mch_Ept.Match();
            mch_Ept.PatternIndex = 0;
            //if (CurrIDX.Y == 110)
            //    mch_Ept.DrawMatchedPattern(axAxCanvas1.hDC, -1, zoom2, zoom2, 0, 0);
            //有料Die
            mch_DEV.DstImageHandle = roi_m.VegaHandle;
            mch_DEV.Match();
            mch_DEV.PatternIndex = 0;
            // mch_DEV.DrawMatchedPattern(axAxCanvas1.hDC, -1, zoom2, zoom2, 0, 0);
            ROI_Draw.ParentHandle = img_Globe.VegaHandle;
            ROI_Draw.ShowTitle = false;

            Point AnchorIDX = new Point(0, 0);//宣告錨DIE IDX
            if (CurrIDX.Y == MAP.FstY)//參考ROW的參考DIE第一顆直接設定為錨DIE
            {
                CurrIDX.X = MAP.FstX;//訂出本行起始點位置
                CurrPoint.X = mch_ref.MatchedX;
                AnchorIDX.X = MAP.FstX;
                AnchorIDX.Y = MAP.FstY;
                //存入中心點位置
                CK.Matrix[MAP.FstY, MAP.FstX].coordX = mch_ref.MatchedX + (mch_ref.PatternWidth / 2);//mch使用相對座標, 因此需要加上ROI基底
                CK.Matrix[MAP.FstY, MAP.FstX].coordY = mch_ref.MatchedY + (mch_ref.PatternHeight / 2);// 存入CK矩陣的位置使用絕對座標
                CK.Matrix[MAP.FstY, MAP.FstX].CheckState = 99;//匹配成功 位置穩定
                CK.Matrix[MAP.FstY, MAP.FstX].DieState = 128;//參考DIE
                AnchorIDX.X = MAP.FstX;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                AnchorIDX.Y = MAP.FstX;
            }


            //計算當前ROW 所有mch的平均Y座標
            int Total_Y = 0;
            int AVG_Y = 0;
            if (mch_Ept.NumMatchedPos > 0)
            {
                for (int i = 0; i <= mch_Ept.NumMatchedPos - 1; i++)
                {
                    mch_Ept.PatternIndex = i;
                    Total_Y += mch_Ept.MatchedY;
                }
                AVG_Y = Total_Y / mch_Ept.NumMatchedPos;
            }
            else if (mch_DEV.NumMatchedPos > 0)
            {
                for (int i = 0; i <= mch_DEV.NumMatchedPos - 1; i++)
                {
                    mch_DEV.PatternIndex = i;
                    Total_Y += mch_DEV.MatchedY;
                }
                AVG_Y = Total_Y / mch_DEV.NumMatchedPos;
            }
            else
            {
                AVG_Y = roi_m.ROIHeight / 2;    //原中心線位置
            }
            ROI_Draw.Title = CurrIDX.Y.ToString();
            ROI_Draw.ShowTitle = false;
            //開始從參考DIE往兩個方向定位
            //SideClass SIDE = new SideClass(); //宣告 判斷中心點在哪個IDX的預估範圍界線
            Point 預期位置 = new Point(0, 0);
            //***************************向左走*******************************
            for (int i = MAP.FstX; i >= 0; i--)
            {
                if (!PD.NullList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString) &&
                     MAP.Matrix[CurrIDX.Y, i].MapString.CompareTo(PD.參考DIE字元) != 0)
                {
                    if (CurrIDX.Y == MAP.FstY)
                        預期位置.X = CK.Matrix[CurrIDX.Y, AnchorIDX.X].coordX - (PD.DieW * (AnchorIDX.X - i));

                    else if (CurrIDX.Y < MAP.FstY && CK.Matrix[CurrIDX.Y + 1, i].coordX >= 0)//上半部ROW直接參考下一排
                        預期位置.X = CK.Matrix[CurrIDX.Y + 1, i].coordX;//- DEV.reCoAreaX;

                    else if (CurrIDX.Y > MAP.FstY && CK.Matrix[CurrIDX.Y - 1, i].coordX >= 0)//下半部ROW直接參考上一排
                        預期位置.X = CK.Matrix[CurrIDX.Y - 1, i].coordX;//- DEV.reCoAreaX;

                    else if (CK.Matrix[CurrIDX.Y, AnchorIDX.X].coordX > 0)//如果上/下排尚未定位，比照第一排
                        預期位置.X = CK.Matrix[CurrIDX.Y, AnchorIDX.X].coordX - (PD.DieW * (AnchorIDX.X - i));// -DEV.reCoAreaX;
                    else                                                  //如果基準點無效，則參考起始排
                    {
                        for (int k = i; k <= MAP.Matrix.GetLength(1) - 1; k++)
                        {
                            if (CK.Matrix[CurrIDX.Y, k].CheckState == 99)
                            {
                                預期位置.X = CK.Matrix[CurrIDX.Y, k].coordX - (PD.DieW * (k - i));
                            }
                        }
                    }
                    //預期位置.X = CK.Matrix[MAP.FstindexY, AnchorIDX.X].coordX - (PD.DieW * (AnchorIDX.X - i));

                    預期位置.Y = AVG_Y;//將預期Y座標設定為平均座Y座標
                    //檢查所有有料定位結果, 找到坐落在預期位置當中的樣本
                    for (int k = 0; k <= mch_DEV.NumMatchedPos - 1; k++)
                    {
                        mch_DEV.PatternIndex = k;
                        if ((Math.Abs(mch_DEV.MatchedX - 預期位置.X) < PD.中心誤差X) &&
                            (Math.Abs(mch_DEV.MatchedY - 預期位置.Y) < PD.中心誤差Y))
                        {
                            CK.Matrix[CurrIDX.Y, i].coordX = mch_DEV.MatchedX + roi_m.OrgX;//mch使用相對座標, 因此需要加上ROI基底
                            CK.Matrix[CurrIDX.Y, i].coordY = mch_DEV.MatchedY + roi_m.OrgY;// 存入CK矩陣的位置使用絕對座標
                            CK.Matrix[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                            CK.Matrix[CurrIDX.Y, i].DieState = 1;//有料
                            ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                                  CK.Matrix[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                                  PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                            //判定是否留料(綠色)
                            if (!PD.BinList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString))
                            {
                                CK.Matrix[CurrIDX.Y, i].DieColor = BAD_DEV;
                                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ff00);
                            }
                            else//好料未取走(黃色)
                            {
                                CK.Matrix[CurrIDX.Y, i].DieColor = GOOD_DEV;
                                // ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ffff);
                            }
                            AnchorIDX.X = i;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                            AnchorIDX.Y = CurrIDX.Y;
                            break;
                        }
                    }
                    //檢查所有空格定位結果, 找到坐落在預期位置當中的樣本
                    for (int k = 0; k <= mch_Ept.NumMatchedPos - 1; k++)
                    {
                        mch_Ept.PatternIndex = k;
                        if ((Math.Abs(mch_Ept.MatchedX - 預期位置.X) < PD.中心誤差X) &&
                            (Math.Abs(mch_Ept.MatchedY - 預期位置.Y) < PD.中心誤差Y))
                        {
                            CK.Matrix[CurrIDX.Y, i].coordX = mch_Ept.MatchedX + roi_m.OrgX;//mch使用相對座標, 因此需要加上ROI基底
                            CK.Matrix[CurrIDX.Y, i].coordY = mch_Ept.MatchedY + roi_m.OrgY;// 存入CK矩陣的位置使用絕對座標
                            CK.Matrix[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                            CK.Matrix[CurrIDX.Y, i].DieState = 0;//空料
                            ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                                  CK.Matrix[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                                  PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                            //判定是否為正常料
                            if (!PD.BinList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString))
                            {
                                //異常被取走 紅色!!!!
                                CK.Matrix[CurrIDX.Y, i].DieColor = BAD_Ept;
                                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x0000ff);
                            }
                            else//正常被取走 淺藍色
                                CK.Matrix[CurrIDX.Y, i].DieColor = GOOD_Ept;
                            AnchorIDX.X = i;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                            AnchorIDX.Y = CurrIDX.Y;
                            break;
                        }
                    }
                }
            }
            //**********************向右走***************
            AnchorIDX.X = MAP.FstX; //定位Die移回參考Die
            for (int i = MAP.FstX + 1; i <= MAP.Matrix.GetLength(1) - 1; i++)
            {
                if (!PD.NullList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString) &&
                     MAP.Matrix[CurrIDX.Y, i].MapString.CompareTo(PD.參考DIE字元) != 0)
                {
                    if (CurrIDX.Y == MAP.FstY)
                        //參考ROW預期位置推導 左極限從右側錨DIE來推(最靠近的一顆有訂位成功的DIE)
                        預期位置.X = CK.Matrix[CurrIDX.Y, AnchorIDX.X].coordX + (PD.DieW * (i - AnchorIDX.X));// - DEV.reCoAreaX;

                    else if (CurrIDX.Y < MAP.FstY && CK.Matrix[CurrIDX.Y + 1, i].coordX >= 0)//上半部ROW直接參考下一排
                        預期位置.X = CK.Matrix[CurrIDX.Y + 1, i].coordX;// - DEV.reCoAreaX;

                    else if (CurrIDX.Y > MAP.FstY && CK.Matrix[CurrIDX.Y - 1, i].coordX >= 0)//下半部ROW直接參考上一排
                        預期位置.X = CK.Matrix[CurrIDX.Y - 1, i].coordX;// - DEV.reCoAreaX;

                    else if (CK.Matrix[CurrIDX.Y, AnchorIDX.X].coordX > 0)//如果上/下排尚未定位，比照第一排
                        預期位置.X = CK.Matrix[CurrIDX.Y, AnchorIDX.X].coordX + (PD.DieW * (i - AnchorIDX.X));// -DEV.reCoAreaX;
                    else
                    {
                        for (int k = i; k >= 0; k--)
                        {
                            if (CK.Matrix[CurrIDX.Y, k].CheckState == 99)
                            {
                                預期位置.X = CK.Matrix[CurrIDX.Y, k].coordX - (PD.DieW * (k + i));
                            }
                        }
                    }
                    //如果基準點無效，則參考起始排
                    // 預期位置.X = CK.Matrix[MAP.FstY, AnchorIDX.X].coordX + (PD.DieW * (i - AnchorIDX.X));

                    預期位置.Y = AVG_Y;//將預期Y座標設定為平均座Y座標

                    //檢查所有有料定位結果, 找到坐落在預期位置當中的樣本
                    for (int k = 0; k <= mch_DEV.NumMatchedPos - 1; k++)
                    {
                        mch_DEV.PatternIndex = k;
                        if ((Math.Abs(mch_DEV.MatchedX - 預期位置.X) < PD.中心誤差X) &&
                            (Math.Abs(mch_DEV.MatchedY - 預期位置.Y) < PD.中心誤差Y))
                        {
                            CK.Matrix[CurrIDX.Y, i].coordX = mch_DEV.MatchedX + roi_m.OrgX;//mch使用相對座標, 因此需要加上ROI基底
                            CK.Matrix[CurrIDX.Y, i].coordY = mch_DEV.MatchedY + roi_m.OrgY;// 存入CK矩陣的位置使用絕對座標
                            CK.Matrix[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                            CK.Matrix[CurrIDX.Y, i].DieState = 1;//有料
                            ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                                  CK.Matrix[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                                  PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                            //判定是否留料(綠色)
                            if (!PD.BinList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString))
                            {
                                CK.Matrix[CurrIDX.Y, i].DieColor = BAD_DEV;
                                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ff00);
                            }
                            else//好料未取走(黃色)
                            {
                                CK.Matrix[CurrIDX.Y, i].DieColor = GOOD_DEV;
                                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ffff);
                            }

                            AnchorIDX.X = i;//將當前定位成功的DIE設定為推下一顆預期位置的基準DIE
                            AnchorIDX.Y = CurrIDX.Y;
                            break;
                        }
                    }
                    for (int k = 0; k <= mch_Ept.NumMatchedPos - 1; k++)
                    {
                        mch_Ept.PatternIndex = k;
                        if ((Math.Abs(mch_Ept.MatchedX - 預期位置.X) < PD.中心誤差X) && (Math.Abs(mch_Ept.MatchedY - 預期位置.Y) < PD.中心誤差Y))
                        {
                            CK.Matrix[CurrIDX.Y, i].coordX = mch_Ept.MatchedX + roi_m.OrgX;
                            CK.Matrix[CurrIDX.Y, i].coordY = mch_Ept.MatchedY + roi_m.OrgY;
                            CK.Matrix[CurrIDX.Y, i].CheckState = 99;//匹配成功 位置穩定
                            CK.Matrix[CurrIDX.Y, i].DieState = 0;//空料
                            //判定是否對應空料
                            ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                                                  CK.Matrix[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                                                  PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                            //判定是否為正常料
                            if (!PD.BinList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString))
                            {
                                //異常被取走 紅色!!!!
                                CK.Matrix[CurrIDX.Y, i].DieColor = BAD_Ept;
                                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x0000ff);
                            }
                            else//正常被取走 淺藍色
                                CK.Matrix[CurrIDX.Y, i].DieColor = GOOD_Ept;

                            AnchorIDX.X = i;
                            AnchorIDX.Y = CurrIDX.Y;

                            break;
                        }
                    }
                }
            }
            //return new Point(0, AVG_Y + roi_m.OrgY);//回傳 X沒有意義直接設0
            //左右都定位完畢, 從頭開始檢查那些格位還沒有定位成功, 參考最靠進的鄰居推出定位
            for (int i = 0; i <= MAP.Matrix.GetLength(1) - 1; i++)
            {
                if ((CK.Matrix[CurrIDX.Y, i].CheckState != 99) &&
                     (!PD.NullList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString) &&
                     MAP.Matrix[CurrIDX.Y, i].MapString.CompareTo(PD.參考DIE字元) != 0))
                {
                    bool finish_flag = false;

                    //非第一排參考上下鄰居
                    if (CurrIDX.Y != MAP.FstY)
                    {
                        if (CurrIDX.Y < MAP.FstY && CK.Matrix[CurrIDX.Y + 1, i].coordX >= 0)//上半部ROW直接參考下一排
                        {
                            CK.Matrix[CurrIDX.Y, i].coordX = CK.Matrix[CurrIDX.Y + 1, i].coordX;
                            finish_flag = true;
                        }
                        else if (CurrIDX.Y > MAP.FstY && CK.Matrix[CurrIDX.Y - 1, i].coordX >= 0)//下半部ROW直接參考上一排
                        {
                            CK.Matrix[CurrIDX.Y, i].coordX = CK.Matrix[CurrIDX.Y - 1, i].coordX;
                            finish_flag = true;
                        }
                        else
                        {
                            finish_flag = false;
                        }

                        if (finish_flag)
                        {
                            CK.Matrix[CurrIDX.Y, i].coordY = AVG_Y + roi_m.OrgY;
                        }
                    }
                    //第一排或上下參考失敗參考左右鄰居
                    //finish_flag = false;
                    if (!finish_flag)
                    {
                        int CNT = 0;
                        while (CK.Matrix[CurrIDX.Y, i + CNT].CheckState != 99)
                        {
                            if (i < MAP.FstX)
                                CNT++;
                            else
                                CNT--;

                            //如果向右找到底皆未找到，則向左找
                            if (((i + CNT) >= (MAP.Matrix.GetLength(1) - 1)) || ((i + CNT) <= 0))
                            {
                                CNT = 0;
                                while (CK.Matrix[CurrIDX.Y, i + CNT].CheckState != 99)
                                {
                                    if (i < MAP.FstX)
                                        CNT--;
                                    else
                                        CNT++;

                                    if (((i + CNT) >= (MAP.Matrix.GetLength(1) - 1)) || ((i + CNT) <= 0))
                                    {
                                        MessageBox.Show("檢測參數設定錯誤，請檢查參數或嘗試重新教導產品。", "定位失敗");
                                        return new Point(0, 0);//回傳 X沒有意義直接設0
                                    }
                                }
                                break;
                            }
                        }

                        CK.Matrix[CurrIDX.Y, i].coordX = CK.Matrix[CurrIDX.Y, i + CNT].coordX - (PD.DieW * CNT);
                        CK.Matrix[CurrIDX.Y, i].coordY = CK.Matrix[CurrIDX.Y, i + CNT].coordY;
                    }

                    CK.Matrix[CurrIDX.Y, i].CheckState = 70;//靠鄰居來的位置


                    //**************************************************************************
                    //使用CannyOperator判斷是否為空料
                    //用判定區尺寸判定範圍
                    ROI_Draw.ParentHandle = img_Globe.VegaHandle;
                    ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - PD.判定區尺寸W,
                      CK.Matrix[CurrIDX.Y, i].coordY - PD.判定區尺寸H,
                      PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);
                    Canny_Operator1.SrcImageHandle = ROI_Draw.VegaHandle;
                    Canny_Operator1.DetectPrimitives();

                    if (Canny_Operator1.PointCount < PD.Canny_count)
                    {
                        CK.Matrix[CurrIDX.Y, i].DieState = 0;//空料

                        if (!PD.BinList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString))
                        {
                            //異常被取走 紅色!!!!
                            CK.Matrix[CurrIDX.Y, i].DieColor = BAD_Ept;
                            ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x0000ff);
                        }
                        else//正常被取走 淺藍色
                            CK.Matrix[CurrIDX.Y, i].DieColor = GOOD_Ept;
                    }
                    else
                    {
                        CK.Matrix[CurrIDX.Y, i].DieState = 1;//有料
                        //ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - DEV.判定區尺寸,
                        //                      CK.Matrix[CurrIDX.Y, i].coordY - DEV.判定區尺寸,
                        //                      DEV.判定區尺寸 * 2, DEV.判定區尺寸 * 2);

                        //判定是否留料(綠色)
                        if (!PD.BinList.Contains(MAP.Matrix[CurrIDX.Y, i].MapString))
                        {
                            CK.Matrix[CurrIDX.Y, i].DieColor = BAD_DEV;
                            ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ff00);
                        }
                        else//好料未取走(黃色)
                        {
                            CK.Matrix[CurrIDX.Y, i].DieColor = GOOD_DEV;
                            ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ffff);
                        }
                    }
                    /********************************************************************************/
                }
            }
            return new Point(0, AVG_Y + roi_m.OrgY);//回傳 X沒有意義直接設0
        }
        public void ProcessAll_byROW()
        {
            GC.Collect();
            //全部計時器暫停避免干擾
            timer_IOM_Thread.Stop();
            timer_介面.Enabled = false;


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件 
            sw.Reset();//碼表歸零
            sw.Start();//碼表開始計時

            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {
                    CK.Matrix[y, x] = new CheckClass.DieInfoStruct();
                }
            }
            roi_ref.ParentHandle = img_Globe.VegaHandle;
            roi_ref.SetPlacement(PD.左參考DIE影像X - PD.參考DIE搜尋範圍,
                                    PD.左參考DIE影像Y - PD.參考DIE搜尋範圍,
                                    mch_ref.PatternWidth + PD.參考DIE搜尋範圍 * 2,
                                   mch_ref.PatternHeight + PD.參考DIE搜尋範圍 * 2);
            roi_refR.ParentHandle = img_Globe.VegaHandle;
            roi_refR.SetPlacement(PD.右參考DIE影像X - PD.參考DIE搜尋範圍,
                                    PD.右參考DIE影像Y - PD.參考DIE搜尋範圍,
                                    mch_refR.PatternWidth + PD.參考DIE搜尋範圍 * 2,
                                   mch_refR.PatternHeight + PD.參考DIE搜尋範圍 * 2);
            // roi_target.SaveFile("D:\\aa.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            //訂出ref die右邊第一顆位置
            //mch_target.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            axAxCanvas1.RefreshCanvas();

            roi_matrix.ShowPlacement = false;
            mch_ref.AbsoluteCoord = true;
            mch_ref.DstImageHandle = roi_ref.VegaHandle;
            mch_ref.Match();
            mch_refR.AbsoluteCoord = true;
            mch_refR.DstImageHandle = roi_refR.VegaHandle;
            mch_refR.Match();
            if (mch_refR.EffectMatch && mch_ref.EffectMatch)
            {
                //計算出左右參考點斜率
                double Slope = (float)(mch_refR.MatchedY - mch_ref.MatchedY) / (mch_refR.MatchedX - mch_ref.MatchedX);
                double angel = Math.Atan(Slope);
                if (angel > 0.1)
                {
                    axAxImageRotator1.SrcImageHandle = img_Globe.VegaHandle;
                    axAxImageRotator1.DstImageHandle = img_Globe.VegaHandle;
                    axAxImageRotator1.RotateCenterX = (mch_refR.MatchedX + mch_ref.MatchedX) / 2;
                    axAxImageRotator1.RotateCenterY = (mch_refR.MatchedY + mch_ref.MatchedY) / 2;
                    axAxImageRotator1.RotateDegree = Convert.ToSingle(angel * 180 / Math.PI);
                    axAxImageRotator1.InterpolationMethod = AxOvkImage.TxAxImageRotatorInterpolationMethod.AX_ROTATOR_INTERPOLATION_METHOD_LINEAR;
                    axAxImageRotator1.RotatorMethod = AxOvkImage.TxAxImageRotatorMethod.AX_ROTATE_ANY_ANGLE_WRT_ANY_POINT_TO_SAME_SIZE;
                    axAxImageRotator1.Rotate();
                    img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
                    axAxCanvas1.RefreshCanvas();
                }
            }

            //img_Globe.SaveFile("D:\\123.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            mch_ref.Match();
            mch_refR.Match();

            //劃出找到的ref die
            if (mch_ref.EffectMatch)
            {
                roi_ref.ShowTitle = false;
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

                ROI_Draw.Title = "Ref";
                ROI_Draw.SetPlacement(mch_ref.MatchedX, mch_ref.MatchedY, mch_ref.PatternWidth, mch_ref.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                goto check_end;
            }

            //劃出找到的ref die
            if (mch_refR.EffectMatch)
            {
                roi_refR.ShowTitle = false;
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

                ROI_Draw.Title = "Ref";
                ROI_Draw.SetPlacement(mch_refR.MatchedX, mch_refR.MatchedY, mch_refR.PatternWidth, mch_refR.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_refR.ShowTitle = true;
                roi_refR.Title = "ref die miss";
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                //return;
            }

            Point FstPoint = new Point(0, 0);//參考DIE影像位置
            FstPoint.X = mch_ref.MatchedX;
            FstPoint.Y = mch_ref.MatchedY;
            Point FstIDX = new Point(0, 0);//參考DIE IDX
            FstIDX.X = MAP.FstX;
            FstIDX.Y = MAP.FstY;

            Point CurrPoint = new Point(0, 0);//定位ROW ROI  Y座標
            CurrPoint.X = 0;
            CurrPoint.Y = FstPoint.Y + mch_Ept.PatternHeight / 2;
            Point CurrIDX = new Point(0, 0);//定位矩陣ROI左上角DIE IDX
            CurrIDX.X = 0;
            CurrIDX.Y = FstIDX.Y;//從參考點開始做
            roi_matrix.ShowTitle = true;
            roi_matrix.ParentHandle = img_Globe.VegaHandle;

            //設定mch元件相關參數
            mch_Ept.MinScore = PD.EPT_TH;
            mch_Ept.MaxPositions = 300;
            mch_Ept.VerticalSpacingRatio = 0.7f;
            mch_Ept.AbsoluteCoord = false;
            mch_Ept.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_Ept.MaxOverlappedRatioX = 0.2f;
            mch_Ept.MaxOverlappedRatioY = 0.2f;
            mch_Ept.SortingMethod = AxOvkPat.TxAxMatchSortingMethod.AX_MATCH_SORTING_METHOD_BY_POSITION;
            mch_DEV.MinScore = PD.DIE_TH;
            mch_DEV.MaxPositions = 200;
            mch_DEV.AbsoluteCoord = false;
            mch_DEV.VerticalSpacingRatio = 0.7f;
            mch_DEV.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_DEV.MaxOverlappedRatioX = 0.2f;
            mch_DEV.MaxOverlappedRatioY = 0.2f;
            mch_DEV.SortingMethod = AxOvkPat.TxAxMatchSortingMethod.AX_MATCH_SORTING_METHOD_BY_POSITION;

            //***向上****
            while (CurrIDX.Y >= 0)
            {
                roi_matrix.Title = CurrIDX.Y.ToString();
                roi_matrix.SetPlacement(0, CurrPoint.Y, img_Globe.ImageWidth, 0);
                AVG_Y_ARRAY[CurrIDX.Y] = CurrPoint.Y;
                CurrPoint = Process_Row(ref roi_matrix, ref CurrIDX, ref CurrPoint);//取得重新定位後的當前ROW Y座標(因為ROW橫跨整張圖,X座標永遠為零)
                if (CurrPoint.Y == 0)
                {
                    goto check_end;
                }
                CurrIDX.Y--;
                CurrPoint.Y -= PD.DieH;
            }
            //***向下****
            CurrIDX.X = FstIDX.X;
            CurrIDX.Y = FstIDX.Y + 1;//從參考點開始做
            CurrPoint.X = 0;
            CurrPoint.Y = FstPoint.Y + PD.DieH + PD.DieH / 2;

            while (CurrIDX.Y <= CK.Matrix.GetLength(0) - 1)
            {
                roi_matrix.Title = CurrIDX.X.ToString() + "_" + CurrIDX.Y.ToString();
                roi_matrix.SetPlacement(CurrPoint.X, CurrPoint.Y, img_Globe.ImageWidth, PD.DieH);//設定定位矩陣ROI位置
                AVG_Y_ARRAY[CurrIDX.Y] = CurrPoint.Y;
                CurrPoint = Process_Row(ref roi_matrix, ref CurrIDX, ref CurrPoint);//取得重新定位後的左上格"左上角點"                
                if (CurrPoint.Y == 0)
                {
                    goto check_end;
                }
                CurrIDX.Y++;
                CurrPoint.Y += PD.DieH;
            }

            //全部檢查完畢 重新整理可能發生誤判的黃色/紅色區域
            for (int y = 2; y <= CK.Matrix.GetLength(0) - 3; y++)
            {
                for (int x = 2; x <= CK.Matrix.GetLength(1) - 3; x++)
                {
                    if (((CK.Matrix[y, x].DieColor == GOOD_DEV) ||
                        (CK.Matrix[y, x].DieColor == BAD_Ept)) &&
                            CK.Matrix[y, x].CheckState != 99)//良品未取
                    {
                        //檢查周圍九宮格找鄰居重新定位
                        for (int YY = -2; YY <= 2; YY++)
                        {
                            for (int XX = -2; XX <= 2; XX++)
                            {

                                if (CK.Matrix[y + YY, x + XX].CheckState == 99)
                                {
                                    CK.Matrix[y, x].coordX = CK.Matrix[y + YY, x + XX].coordX - (PD.DieW * XX);
                                    CK.Matrix[y, x].coordY = CK.Matrix[y + YY, x + XX].coordY - (PD.DieH * YY);
                                    //進行重新判定
                                    ROI_Draw.SetPlacement(CK.Matrix[y, x].coordX - PD.判定區尺寸W,
                                                            CK.Matrix[y, x].coordY - PD.判定區尺寸H,
                                                                PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);
                                    Canny_Operator1.SrcImageHandle = ROI_Draw.VegaHandle;
                                    Canny_Operator1.DetectPrimitives();

                                    if (Canny_Operator1.PointCount < PD.Canny_count)
                                    {
                                        CK.Matrix[y, x].DieState = 0;//空料

                                        if (!PD.BinList.Contains(MAP.Matrix[y, x].MapString))
                                        {
                                            //異常被取走 紅色!!!!
                                            CK.Matrix[y, x].DieColor = BAD_Ept;
                                            ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x0000ff);
                                        }
                                        else//正常被取走 淺藍色
                                            CK.Matrix[y, x].DieColor = GOOD_Ept;
                                    }
                                    else
                                    {
                                        CK.Matrix[y, x].DieState = 1;//有料
                                                                     //ROI_Draw.SetPlacement(CK.Matrix[CurrIDX.Y, i].coordX - DEV.判定區尺寸,
                                                                     //                      CK.Matrix[CurrIDX.Y, i].coordY - DEV.判定區尺寸,
                                                                     //                      DEV.判定區尺寸 * 2, DEV.判定區尺寸 * 2);

                                        //判定是否留料(綠色)
                                        if (!PD.BinList.Contains(MAP.Matrix[y, x].MapString))
                                        {
                                            CK.Matrix[y, x].DieColor = BAD_DEV;
                                            ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ff00);
                                        }
                                        else//好料未取走(黃色)
                                        {
                                            CK.Matrix[y, x].DieColor = GOOD_DEV;
                                            ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00ffff);
                                        }
                                    }
                                }
                            }
                        }

                    }



                }
            }



        check_end:
            sw.Stop();//碼錶停止
            //印出所花費的總毫秒數
            toolStripMenuItem9.Text = sw.Elapsed.TotalMilliseconds.ToString();
            timer_IOM_Thread.Start();
            timer_介面.Enabled = true;
        }

        public void ProcessAll_byROW_bak()
        {
            GC.Collect();
            //全部計時器暫停避免干擾
            timer_IOM_Thread.Stop();
            timer_介面.Enabled = false;


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件 
            sw.Reset();//碼表歸零
            sw.Start();//碼表開始計時

            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {
                    CK.Matrix[y, x] = new CheckClass.DieInfoStruct();
                }
            }
            roi_ref.ParentHandle = img_Globe.VegaHandle;
            roi_ref.SetPlacement(PD.左參考DIE影像X - PD.參考DIE搜尋範圍,
                                    PD.左參考DIE影像Y - PD.參考DIE搜尋範圍,
                                    mch_ref.PatternWidth + PD.參考DIE搜尋範圍 * 2,
                                   mch_ref.PatternHeight + PD.參考DIE搜尋範圍 * 2);
            roi_refR.ParentHandle = img_Globe.VegaHandle;
            roi_refR.SetPlacement(PD.右參考DIE影像X - PD.參考DIE搜尋範圍,
                                    PD.右參考DIE影像Y - PD.參考DIE搜尋範圍,
                                    mch_refR.PatternWidth + PD.參考DIE搜尋範圍 * 2,
                                   mch_refR.PatternHeight + PD.參考DIE搜尋範圍 * 2);
            // roi_target.SaveFile("D:\\aa.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            //訂出ref die右邊第一顆位置
            //mch_target.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            axAxCanvas1.RefreshCanvas();

            roi_matrix.ShowPlacement = false;
            mch_ref.AbsoluteCoord = true;
            mch_ref.DstImageHandle = roi_ref.VegaHandle;
            mch_ref.Match();
            mch_refR.AbsoluteCoord = true;
            mch_refR.DstImageHandle = roi_refR.VegaHandle;
            mch_refR.Match();
            if (mch_refR.EffectMatch && mch_ref.EffectMatch)
            {
                //計算出左右參考點斜率
                double Slope = (float)(mch_refR.MatchedY - mch_ref.MatchedY) / (mch_refR.MatchedX - mch_ref.MatchedX);
                double angel = Math.Atan(Slope);
                if (angel != 0)
                {
                    axAxImageRotator1.SrcImageHandle = img_Globe.VegaHandle;
                    axAxImageRotator1.DstImageHandle = img_Globe.VegaHandle;
                    axAxImageRotator1.RotateCenterX = (mch_refR.MatchedX + mch_ref.MatchedX) / 2;
                    axAxImageRotator1.RotateCenterY = (mch_refR.MatchedY + mch_ref.MatchedY) / 2;
                    axAxImageRotator1.RotateDegree = Convert.ToSingle(angel * 180 / Math.PI);
                    axAxImageRotator1.InterpolationMethod = AxOvkImage.TxAxImageRotatorInterpolationMethod.AX_ROTATOR_INTERPOLATION_METHOD_LINEAR;
                    axAxImageRotator1.RotatorMethod = AxOvkImage.TxAxImageRotatorMethod.AX_ROTATE_ANY_ANGLE_WRT_ANY_POINT_TO_SAME_SIZE;
                    axAxImageRotator1.Rotate();
                    img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
                    axAxCanvas1.RefreshCanvas();
                }
            }

            //img_Globe.SaveFile("D:\\123.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            mch_ref.Match();
            mch_refR.Match();

            //劃出找到的ref die
            if (mch_ref.EffectMatch)
            {
                roi_ref.ShowTitle = false;
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

                ROI_Draw.Title = "Ref";
                ROI_Draw.SetPlacement(mch_ref.MatchedX, mch_ref.MatchedY, mch_ref.PatternWidth, mch_ref.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                goto check_end;
            }

            //劃出找到的ref die
            if (mch_refR.EffectMatch)
            {
                roi_refR.ShowTitle = false;
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);

                ROI_Draw.Title = "Ref";
                ROI_Draw.SetPlacement(mch_refR.MatchedX, mch_refR.MatchedY, mch_refR.PatternWidth, mch_refR.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_refR.ShowTitle = true;
                roi_refR.Title = "ref die miss";
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                //return;
            }

            Point FstPoint = new Point(0, 0);//參考DIE影像位置
            FstPoint.X = mch_ref.MatchedX;
            FstPoint.Y = mch_ref.MatchedY;
            Point FstIDX = new Point(0, 0);//參考DIE IDX
            FstIDX.X = MAP.FstX;
            FstIDX.Y = MAP.FstY;

            Point CurrPoint = new Point(0, 0);//定位ROW ROI  Y座標
            CurrPoint.X = 0;
            CurrPoint.Y = FstPoint.Y + mch_Ept.PatternHeight / 2;
            Point CurrIDX = new Point(0, 0);//定位矩陣ROI左上角DIE IDX
            CurrIDX.X = 0;
            CurrIDX.Y = FstIDX.Y;//從參考點開始做
            roi_matrix.ShowTitle = true;
            roi_matrix.ParentHandle = img_Globe.VegaHandle;

            //設定mch元件相關參數
            mch_Ept.MinScore = PD.EPT_TH;
            mch_Ept.MaxPositions = 300;
            mch_Ept.VerticalSpacingRatio = 0.7f;
            mch_Ept.AbsoluteCoord = false;
            mch_Ept.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_Ept.MaxOverlappedRatioX = 0.2f;
            mch_Ept.MaxOverlappedRatioY = 0.2f;
            mch_Ept.SortingMethod = AxOvkPat.TxAxMatchSortingMethod.AX_MATCH_SORTING_METHOD_BY_POSITION;
            mch_DEV.MinScore = PD.DIE_TH;
            mch_DEV.MaxPositions = 200;
            mch_DEV.AbsoluteCoord = false;
            mch_DEV.VerticalSpacingRatio = 0.7f;
            mch_DEV.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_DEV.MaxOverlappedRatioX = 0.2f;
            mch_DEV.MaxOverlappedRatioY = 0.2f;
            mch_DEV.SortingMethod = AxOvkPat.TxAxMatchSortingMethod.AX_MATCH_SORTING_METHOD_BY_POSITION;

            //***向上****
            while (CurrIDX.Y >= 0)
            {
                roi_matrix.Title = CurrIDX.Y.ToString();
                roi_matrix.SetPlacement(0, CurrPoint.Y, img_Globe.ImageWidth, 0);
                AVG_Y_ARRAY[CurrIDX.Y] = CurrPoint.Y;
                CurrPoint = Process_Row(ref roi_matrix, ref CurrIDX, ref CurrPoint);//取得重新定位後的當前ROW Y座標(因為ROW橫跨整張圖,X座標永遠為零)
                if (CurrPoint.Y == 0)
                {
                    goto check_end;
                }
                CurrIDX.Y--;
                CurrPoint.Y -= PD.DieH;
            }
            //***向下****
            CurrIDX.X = FstIDX.X;
            CurrIDX.Y = FstIDX.Y + 1;//從參考點開始做
            CurrPoint.X = 0;
            CurrPoint.Y = FstPoint.Y + PD.DieH + PD.DieH / 2;

            while (CurrIDX.Y <= CK.Matrix.GetLength(0) - 1)
            {
                roi_matrix.Title = CurrIDX.X.ToString() + "_" + CurrIDX.Y.ToString();
                roi_matrix.SetPlacement(CurrPoint.X, CurrPoint.Y, img_Globe.ImageWidth, PD.DieH);//設定定位矩陣ROI位置
                AVG_Y_ARRAY[CurrIDX.Y] = CurrPoint.Y;
                CurrPoint = Process_Row(ref roi_matrix, ref CurrIDX, ref CurrPoint);//取得重新定位後的左上格"左上角點"                
                if (CurrPoint.Y == 0)
                {
                    goto check_end;
                }
                CurrIDX.Y++;
                CurrPoint.Y += PD.DieH;
            }
        check_end:
            sw.Stop();//碼錶停止
            //印出所花費的總毫秒數
            toolStripMenuItem9.Text = sw.Elapsed.TotalMilliseconds.ToString();
            timer_IOM_Thread.Start();
            timer_介面.Enabled = true;
        }


        public void ProcessAll_byROW_NOMAP()
        {
            AVG_Y_ARRAY_NMP = new int[10];//宣告每一行的檢測Y平均座標暫存陣列,供後面畫圖用
            Matrix_NMP = new CheckClass.DieInfoStruct[10, 10];
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Matrix_NMP[y, x] = new CheckClass.DieInfoStruct();
                }
            }

            //訂出ref die右邊第一顆位置
            //mch_target.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);

            Point FstPoint = new Point(0, 0);//參考DIE影像位置
            FstPoint.X = Convert.ToInt32(ROI1.OrgX) + PD.DieW / 2;
            FstPoint.Y = Convert.ToInt32(ROI1.OrgY) + PD.DieH / 2;


            MAP.FstX = 5;
            MAP.FstY = 5;

            Point CurrPoint = new Point(0, 0);//定位ROW ROI  Y座標
            CurrPoint.X = 0;
            CurrPoint.Y = FstPoint.Y; //+ mch_Ept.PatternHeight / 2;
            Point CurrIDX = new Point(0, 0);//定位矩陣ROI左上角DIE IDX
            CurrIDX.X = 0;
            CurrIDX.Y = MAP.FstY;//從參考點開始做
            roi_matrix.ShowTitle = true;
            roi_matrix.ParentHandle = img_Globe.VegaHandle;

            //設定mch元件相關參數
            mch_Ept.MinScore = PD.EPT_TH;
            mch_Ept.MaxPositions = 300;
            mch_Ept.VerticalSpacingRatio = 0.7f;
            mch_Ept.AbsoluteCoord = false;
            mch_Ept.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_Ept.MaxOverlappedRatioX = 0.2f;
            mch_Ept.MaxOverlappedRatioY = 0.2f;
            mch_Ept.SortingMethod = AxOvkPat.TxAxMatchSortingMethod.AX_MATCH_SORTING_METHOD_BY_POSITION;
            mch_DEV.MinScore = PD.DIE_TH;
            mch_DEV.MaxPositions = 200;
            mch_DEV.AbsoluteCoord = false;
            mch_DEV.VerticalSpacingRatio = 0.7f;
            mch_DEV.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_DEV.MaxOverlappedRatioX = 0.2f;
            mch_DEV.MaxOverlappedRatioY = 0.2f;
            mch_DEV.SortingMethod = AxOvkPat.TxAxMatchSortingMethod.AX_MATCH_SORTING_METHOD_BY_POSITION;

            //***向上****
            while (CurrIDX.Y >= 0)
            {
                roi_matrix.Title = CurrIDX.Y.ToString();
                roi_matrix.SetPlacement(0, CurrPoint.Y, img_Globe.ImageWidth, 0);
                AVG_Y_ARRAY_NMP[CurrIDX.Y] = CurrPoint.Y;
                CurrPoint = Process_RowNOMAP(ref roi_matrix, ref CurrIDX, ref CurrPoint);//取得重新定位後的當前ROW Y座標(因為ROW橫跨整張圖,X座標永遠為零)
                if (CurrPoint.Y == 0)
                {
                    return;
                }
                CurrIDX.Y--;
                CurrPoint.Y -= PD.DieH;
            }
            //***向下****
            CurrIDX.X = MAP.FstX;
            CurrIDX.Y = MAP.FstY + 1;//從參考點開始做
            CurrPoint.X = 0;
            CurrPoint.Y = FstPoint.Y + PD.DieH;

            while (CurrIDX.Y < Matrix_NMP.GetLength(0) - 1)
            {

                roi_matrix.Title = CurrIDX.X.ToString() + "_" + CurrIDX.Y.ToString();
                roi_matrix.SetPlacement(CurrPoint.X, CurrPoint.Y, img_Globe.ImageWidth, PD.DieH);//設定定位矩陣ROI位置
                AVG_Y_ARRAY_NMP[CurrIDX.Y] = CurrPoint.Y;
                CurrPoint = Process_RowNOMAP(ref roi_matrix, ref CurrIDX, ref CurrPoint);//取得重新定位後的左上格"左上角點"                
                if (CurrPoint.Y == 0)
                {
                    return;
                }
                CurrIDX.Y++;
                CurrPoint.Y += PD.DieH;
            }
            axAxCanvas1.RefreshCanvas();
        }

        public void 輸出錯誤訊息(string 錯誤訊息)
        {

            if (錯誤訊息.Length > 0)
            {
                using (StreamWriter sw = new StreamWriter(路徑_系統SYS資料夾 + "錯誤訊息.txt", true))
                {
                    sw.WriteLine(錯誤訊息);
                }
            }
            else
            {
                using (FileStream fs = File.OpenWrite(路徑_系統SYS資料夾 + "錯誤訊息.txt"))
                {
                    fs.SetLength(0);
                }
            }

        }
        public string 讀取錯誤訊息()
        {
            using (StreamReader sr = new StreamReader(路徑_系統SYS資料夾 + "錯誤訊息.txt", true))
            {
                return sr.ReadLine();
            }
        }


        public void 顯示檢測結果()
        {
            g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
            Font fff = new Font("新細明體", 12, FontStyle.Regular);
            int 所有檢出晶粒總數 = 0;
            double 異常率 = 0;
            double PASS_RATE = 0;
            int PosHORZ = 0;
            int PosHORZ_RESULT = axAxCanvas1.HorzScrollValue + 150;
            if (CK.檢測結果 < 3)
            {
                g.FillRectangle(Brushes.Black, PosHORZ, axAxCanvas1.VertScrollValue, 140, 210);
                g.DrawString("總數: " + CK.晶粒總數.ToString(), fff, Brushes.LightCyan, PosHORZ, axAxCanvas1.VertScrollValue + 10);
                g.DrawString("良品取走: " + CK.良品取走數.ToString(), fff, Brushes.LimeGreen, PosHORZ, axAxCanvas1.VertScrollValue + 30);
                g.DrawString("壞品留置: " + CK.壞品留置數.ToString(), fff, Brushes.LimeGreen, PosHORZ, axAxCanvas1.VertScrollValue + 50);
                g.DrawString("良品留置: " + CK.良品留置數.ToString(), fff, Brushes.Goldenrod, PosHORZ, axAxCanvas1.VertScrollValue + 70);
                g.DrawString("壞品誤取: " + CK.壞品誤取數.ToString(), fff, Brushes.Red, PosHORZ, axAxCanvas1.VertScrollValue + 90);

                g.DrawString("Total: " + CK.晶粒總數.ToString(), fff, Brushes.LightCyan, PosHORZ, axAxCanvas1.VertScrollValue + 110);
                g.DrawString("Good Pick: " + CK.良品取走數.ToString(), fff, Brushes.LimeGreen, PosHORZ, axAxCanvas1.VertScrollValue + 130);
                g.DrawString("Bad Stay: " + CK.壞品留置數.ToString(), fff, Brushes.LimeGreen, PosHORZ, axAxCanvas1.VertScrollValue + 150);
                g.DrawString("Good Stay: " + CK.良品留置數.ToString(), fff, Brushes.Goldenrod, PosHORZ, axAxCanvas1.VertScrollValue + 170);
                g.DrawString("Bad Pick: " + CK.壞品誤取數.ToString(), fff, Brushes.Red, PosHORZ, axAxCanvas1.VertScrollValue + 190);


                異常率 = (((double)CK.壞品誤取數 + (double)CK.良品留置數) / (double)CK.晶粒總數) * 100;
                所有檢出晶粒總數 = CK.良品取走數 + CK.壞品留置數 + CK.良品留置數 + CK.壞品誤取數;
                PASS_RATE = 100 - ((double)所有檢出晶粒總數 / (double)CK.晶粒總數) * 100;

                if (CK.壞品誤取數 > 0)
                {
                    fff = new Font("新細明體", 48, FontStyle.Bold);
                    g.DrawString("NG", fff, Brushes.Red, PosHORZ_RESULT, 0);
                }
                else if (所有檢出晶粒總數 != CK.晶粒總數)//所有檢出晶粒數量應該要等於 CK.晶粒總數
                {
                    fff = new Font("新細明體", 28, FontStyle.Bold);
                    g.DrawString("數量異常 檢出:" + 所有檢出晶粒總數
                                     + "  MAP:" + CK.晶粒總數, fff, Brushes.Red, PosHORZ, 220);
                    CK.檢測結果 = 2;
                }
                else
                {
                    fff = new Font("新細明體", 48, FontStyle.Bold);
                    g.DrawString("OK", fff, Brushes.Green, PosHORZ_RESULT, 10);
                    CK.檢測結果 = 1;
                }
            }
            else if (CK.檢測結果 == 3)//所有檢出晶粒數量應該要等於 CK.晶粒總數
            {
                fff = new Font("新細明體", 28, FontStyle.Bold);
                g.DrawString("條碼讀取失敗", fff, Brushes.Red, PosHORZ, 220);
                CK.檢測結果 = 3;
            }
            else if (CK.檢測結果 == 4)//所有檢出晶粒數量應該要等於 CK.晶粒總數
            {
                fff = new Font("新細明體", 28, FontStyle.Bold);
                g.DrawString("資料缺失", fff, Brushes.Red, PosHORZ, 220);
                CK.檢測結果 = 4;
            }
            else if (CK.檢測結果 == 5)//所有檢出晶粒數量應該要等於 CK.晶粒總數
            {
                fff = new Font("新細明體", 28, FontStyle.Bold);
                g.DrawString("找不到MAP", fff, Brushes.Red, PosHORZ, 220);
                CK.檢測結果 = 5;
            }

            g.Dispose();
            axAxCanvas1.RefreshCanvas();

        }


        public void 輸出LOG檔()
        {
            if (CK.Matrix == null)
                return;
            if (CK.晶粒總數 == 0)
                return;
            if (!File.Exists(ST.MAPpath)) return;
            //取得device 以及 wafer ID 加上時間 組合成存檔路徑
            string 雲端LOG根目錄 = F.Vs(sysArray, "LOG位置");
            string 本機LOG根目錄 = "D:\\WV_LOG\\";

            ST.批號 = textBox_批號.Text;
            ST.客批 = textBox_客戶.Text;
            //server端log存檔路徑
            string 上位LOG路徑 = 雲端LOG根目錄 + ST.批號 + "_" + ST.客批 + "\\" + ST.晶圓ID + ".log";
            string 上位圖檔路徑 = 雲端LOG根目錄 + ST.批號 + "_" + ST.客批 + "\\" + ST.晶圓ID + ".jpg";
            string 上位批目錄 = 雲端LOG根目錄 + ST.批號 + "_" + ST.客批;

            //本機端log存檔路徑
            string 本機批目錄 = 本機LOG根目錄 + ST.批號 + "_" + ST.客批 + "\\";
            string 本機圖檔路徑 = 本機批目錄 + ST.晶圓ID + ".jpg";
            string 本機LOG路徑 = 本機批目錄 + ST.晶圓ID + ".log";

            if (!Directory.Exists(上位批目錄)) Directory.CreateDirectory(上位批目錄);
            if (!Directory.Exists(本機批目錄)) Directory.CreateDirectory(本機批目錄);

            double 異常率 = (((double)CK.壞品誤取數 + (double)CK.良品留置數) / (double)CK.晶粒總數) * 100;
            int 所有檢出晶粒總數 = CK.良品取走數 + CK.壞品留置數 + CK.良品留置數 + CK.壞品誤取數;
            double PASS_RATE = 100 - ((double)所有檢出晶粒總數 / (double)CK.晶粒總數) * 100;

            StreamReader SR = new StreamReader(ST.MAPpath);
            StreamWriter SW = new StreamWriter(上位LOG路徑, false, System.Text.Encoding.Default);
            string MAPstring = "";
            string DataLine = "";

            DateTime date1 = DateTime.Now;
            SW.WriteLine("檢測時間(Test time):" + date1.ToString());
            SW.WriteLine("Lot:" + ST.批號);
            SW.WriteLine("Customer:" + ST.客批);
            SW.WriteLine("Wafer ID:" + ST.晶圓ID);
            SW.WriteLine("Device:" + ST.產品代號);
            //SW.WriteLine("Pass Rate:" + ST.PASS_RATE + "%");
            if (CK.壞品誤取數 > 0 || 所有檢出晶粒總數 != CK.晶粒總數)//LEE
                SW.WriteLine("檢測結果(Test result): NG");
            else
                SW.WriteLine("檢測結果(Test result): OK");

            SW.WriteLine("Die總數(Total die):" + CK.晶粒總數.ToString());
            SW.WriteLine("空料數(Empty):" + CK.空格數.ToString() +
                " (" + ((CK.空格數 / CK.晶粒總數) * 100).ToString("#00.00") + "%)");

            SW.WriteLine("有料數(Stay):" + CK.有料數.ToString() +
                " (" + ((CK.有料數 / CK.晶粒總數) * 100).ToString("#00.00") + "%)");

            SW.WriteLine("合格品未取異常(Good die stay):" + CK.良品留置數.ToString() +
                " (" + ((CK.壞品留置數 / CK.晶粒總數) * 100).ToString("#00.00") + "%)");

            SW.WriteLine("NG品取走異常(Bad die pick up):" + CK.壞品誤取數.ToString() +
                " (" + ((CK.壞品誤取數 / CK.晶粒總數) * 100).ToString("#00.00") + "%)");

            while ((DataLine = SR.ReadLine()) != null)
            {
                if (DataLine.IndexOf("RowData:") == -1)
                {
                    MAPstring += DataLine;
                    //SW.WriteLine(DataLine);
                }
                else//進入資料區  
                {
                    break;
                }
            }

            for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
            {
                string ErrorMark = "";
                DataLine = "";
                for (int i = 0; i <= CK.Matrix.GetLength(1) - 1; i++)
                {
                    if (CK.Matrix[y, i].ErrorState == 1)//少die
                    {
                        ErrorMark += "*Y=" + y + ", X=" + i;
                        DataLine += MAP.Matrix[y, i].MapString + "*";
                    }
                    else if (CK.Matrix[y, i].ErrorState == 2)//多die
                    {
                        ErrorMark += "*Y=" + y + ", X=" + i;
                        DataLine += MAP.Matrix[y, i].MapString + "%";
                    }
                    else
                    {
                        DataLine += MAP.Matrix[y, i].MapString + " ";
                    }
                }
                DataLine = DataLine + ErrorMark;
                SW.WriteLine(DataLine);
            }
            SR.Dispose();
            SW.Dispose();
            System.IO.File.Copy(上位LOG路徑, 本機LOG路徑, true);
            if (SYS.存圖選用 == 1)
            {
                img_Globe.SaveFile(上位圖檔路徑, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_JPG);
                img_Globe.SaveFile(本機圖檔路徑, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_JPG);
            }
            Del_Old_File(本機LOG根目錄, SYS.圖檔保留天數);
            //確認檔案是否儲存成功
            string[] 存檔路徑 = { 本機圖檔路徑, 本機LOG路徑, 上位圖檔路徑, 上位LOG路徑 };
            string[] 錯誤訊息 ={"缺少本地端圖片檔" ,"缺少本地端LOG檔",
                                "缺少本地端LOG檔","缺少本地端LOG檔"};
            for (int i = 0; i <= 3; i++)
            {
                if (!File.Exists(存檔路徑[i]))
                {
                    輸出錯誤訊息(錯誤訊息[i]);
                    password PA = new password(this, 錯誤訊息[i]);
                    PA.ShowDialog();
                }
            }
        }

        public void Error_release()
        {
            輸出錯誤訊息("");
            return;
        }

        private void 載入樣本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string LoadDir = 路徑_產品資料夾 + PD.Name;
            string Globe_path = LoadDir + "\\Globe.bmp";
            img_Globe.LoadFile(Globe_path);
            Refreshimage_靜態影像();
        }

        private void timerDB_Tick(object sender, EventArgs e)
        {
            DBcount++;
            if (DBcount > 3)
            {
                timerDB.Enabled = false;
                DBstate = false;
                DBcount = 0;
            }
        }
        public void 走到下一有料格()
        {
            //timer_Step.Enabled = false;
            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                if ((CASArray[i].有無材料 == 1) && (CASArray[i].檢測結果 == 0))
                {
                    //走到料格高度
                    if (i == 12)
                        絕對運動(2, CASArray[i].軌道格位高度 + 300, MV.彈匣初速, MV.彈匣常速, 0.1, 0.1);
                    else
                        絕對運動(2, CASArray[i].軌道格位高度, MV.彈匣初速, MV.彈匣常速, 0.1, 0.1);
                    ST.當前格位 = i;
                    break;

                }
            }
            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();
        }
        public void 走到下一無料格()
        {
            //timer_Step.Enabled = false;
            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                if ((CASArray[i].有無材料 == 0) && (CASArray[i].檢測結果 == 0))
                {
                    //走到料格高度
                    if (i == 12)
                        絕對運動(2, CASArray[i].軌道格位高度 + 300, MV.彈匣初速, MV.彈匣常速, 0.1, 0.1);
                    else
                        絕對運動(2, CASArray[i].軌道格位高度, MV.彈匣初速, MV.彈匣常速, 0.1, 0.1);
                    ST.當前格位 = i;
                    break;

                }
            }
            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();
        }
        public void 撥料模組()
        {
            //timer_Step.Enabled = false;
            //            ///////////天車將材料撥回彈匣////////////////
            //[MOV_ABS,1, 天車撥料慢速點]//天車軸前進直到慢速點
            //        [CMP,1, 天車撥料慢速點,=]
            //        [SPD,1,100]//天車切換到慢速 準備碰觸撥料到底感測器
            //        [MOV_CTN,1]//天車持續移動
            //        [IN,5.07,1]*0*50//天車撥料到底感測
            //[STOP,1]*5*50//天車停止
            //[MOV_ABS,1, 天車軌道點]//天車退到軌道點
            //        [CMP,1, 天車軌道點,=]
            //        [OUT,1.07,0]*0*50//撥料汽缸上
            //[IN,4.02,1]*0*50//撥料汽缸上到位
            //彈匣收料高度補償
            相對運動(2, SYS.彈匣收料補償, MV.彈匣初速, MV.彈匣常速, 0.1, 0.1);

            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();

            絕對運動(1, SYS.天車撥料慢速點, 200, 1200, 0.1, 0.1);
            輸出LOG檔();
            儲存當前格位資料();
            while (Motion._8164_motion_done(1) > 0)
                Application.DoEvents();
            //切換成慢速持續移動
            連續運動(1, 100, 500, 0.1);
            int AxisPos = 0;
            while (Motion._8164_motion_done(1) > 0)
            {
                Application.DoEvents();
                byte a = 0;
                Motion._8164_get_command(1, ref AxisPos);
                DI_1758.ReadBit(5, 7, ref a);
                if (a == 1)
                {
                    Motion._8164_sd_stop(1, 0.1);
                    break;
                }
                if (AxisPos >= SYS.天車撥料保護點)//天車撥料保護點
                {
                    Motion._8164_sd_stop(1, 0.1);
                    break;
                }
            }
            if (AxisPos < SYS.天車撥料保護點 - 100)
            {
                ST.Req_STATE.IDX = 5;
                ST.Req_STATE.Context = "撥料過程異常";
            }

            ////取得位置判定目前彈匣IDX
            //Motion._8164_get_command(2, ref AxisPos);
            //for (int i = 0; i <= CASArray.Length - 1; i++)
            //{
            //    if ((AxisPos >= CASArray[i].軌道格位高度 + SYS.彈匣收料補償 - 4000) &&
            //            (AxisPos <= CASArray[i].軌道格位高度 + SYS.彈匣收料補償 + 4000))
            //    {
            //        if((CASArray[i].CK.壞品誤取數>0)||(CASArray[i].CK.良品留置數>0))
            //        {
            //            CASArray[i].檢測完畢 = 2;
            //            CASArray[i].panel.BackColor = Color.HotPink;//NG
            //        }
            //        else
            //        {
            //            CASArray[i].檢測完畢 = 1;
            //            CASArray[i].panel.BackColor = Color.LimeGreen;//OK

            //        }
            //        break;
            //    }
            //}
        }

        //機台運轉主timer  在主線當中
        private void timerStep_Tick(object sender, EventArgs e)
        {
            if ((IDX_Step步序 == -1) || (StepArray[0] == null))
            {
                timer_步序.Enabled = false;
                return;
            }

            //判斷是否做完 需要警報
            if (StepArray[IDX_Step步序].條件註解.CompareTo("//動作結束") == 0)
            {
                timer_步序.Enabled = false;
                ST.Req_STATE.IDX = 5;
                ST.Req_STATE.Context = "彈匣檢測完畢";
                IDX_Step步序 = 0;
                return;
            }

            int CurrIDX = IDX_Step步序;
            //檢查是否到達 中斷IDX 直接停止自動
            if (IDX_End步序 == IDX_Step步序)
            {
                timer_步序.Enabled = false;
                ST.Req_STATE.IDX = 0;
                IDX_End步序 = -1;
                return;
            }
            //檢查目前步序條件
            timer_步序.Enabled = false;
            File_Class.Motion_Step_Class STN = StepArray[IDX_Step步序];
            File_Class.Motion_Step_Class STN_NEXT = StepArray[IDX_Step步序 + 1];//有些狀態會參考到下一步資料
            byte V = 0;
            bool 條件成立 = true;
            if (STN.條件註解.CompareTo("//生產週期開始") == 0)
            {
                //停止供料 回到停止狀態
                if (checkBox停止供料.Checked)
                {
                    ST.Req_STATE.IDX = 0;
                    return;
                }
            }
            else if (STN.條件註解.CompareTo("//動作結束") == 0)
            {
                IDX_Step步序 = 0;
                ST.Req_STATE.IDX = 0;
                return;
            }


            if (STN.等待外部條件)
            {
                STN.目標延遲時間 = 0;
                label20.Text = STN.已等待時間 + STN.條件註解 + "[" + IDX_Step步序 + "]";
                label24.Text = STN_NEXT.條件註解 + "[" + (IDX_Step步序 + 1) + "]";
                //搭配STEP ARRAY, 當到達某些動作時, 需要配合跑完特定副程式才能下一步, 都加在此處
                switch (STN.條件註解)
                {
                    case ("條碼模組")://目前停用(步序檔中沒有規劃) 這是專程走過去掃條碼會用到的
                        string result = "";
                        result = 條碼模組(false);
                        if (result.Length > 0)//有錯誤
                        {
                            ST.Req_STATE.IDX = 5;//要求異常狀態
                            ST.Req_STATE.Context = result;
                            goto 步序結束;
                        }
                        break;
                    case ("檢測模組"):
                        檢測模組();
                        break;
                    case ("數片模組"):
                        數片模組(true);
                        break;
                    case ("蓋印模組"):
                        蓋印模組();
                        break;
                    case ("走到下一格有料格"):
                        走到下一有料格();
                        break;
                    case ("撥料模組"):
                        撥料模組();
                        寫入彈匣狀態();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (STN.目標延遲時間 <= 4)
                    STN.目標延遲時間 = 2;
                STN.已等待時間++;
                for (int i = 0; i <= STN.ConList.Count - 1; i++)
                {
                    File_Class.Motion_Step_Class.Motion_Node_Class Node = STN.ConList[i];

                    label20.Text = STN.已等待時間 + Node.註解 + "[" + IDX_Step步序 + "]";//顯示目前節點條件
                    if (STN_NEXT.ConList.Count > 0)
                        label24.Text = STN_NEXT.ConList[0].註解 + "[" + (IDX_Step步序 + 1) + "]";//顯示下一步的節點0條件
                    switch (STN.ConList[i].種類) //--------檢查目前步序條件---------------
                    {
                        case ("NOP"):// NOP:空動作 IN.輸入判定 CMP. 軸位置 OUT.輸出點位 MOV.軸移動 
                            break;
                        case ("IN"):
                            // 取得PORT/BIT 下讀取1758指令後 比較
                            DI_1758.ReadBit(Node.點通道, Node.點位元, ref V);
                            if (V == STN.ConList[i].點目標狀態)
                            {
                                STN.ConList[i].節點成立 = true;//節點成立
                            }
                            else
                            {
                                條件成立 = false;
                                break;
                            }
                            break;
                        case ("OUT"):
                            //切割STN.點位址 取得PORT/BIT 下輸出1758  ON/OFF
                            輸出1758(Node.點通道, Node.點位元, (byte)Node.點目標狀態);
                            break;
                        case ("CMP"):
                            //取得STN條件指定軸,下讀取8164指令後 取得軸位置
                            int AxisPos = 0;
                            Motion._8164_get_command((short)STN.ConList[i].軸編號, ref AxisPos);

                            //進行軸位置比較
                            switch (STN.ConList[i].比較元)
                            {
                                case ("="):
                                    if (AxisPos == STN.ConList[i].軸目標位置)
                                        STN.ConList[i].節點成立 = true;//節點成立
                                    else
                                    {
                                        條件成立 = false;
                                        break;
                                    }
                                    break;
                                case (">"):
                                    if (AxisPos > STN.ConList[i].軸目標位置)
                                        STN.ConList[i].節點成立 = true;//節點成立
                                    else
                                    {
                                        條件成立 = false;
                                        break;
                                    }
                                    break;
                                case ("<"):
                                    if (AxisPos < STN.ConList[i].軸目標位置)
                                        STN.ConList[i].節點成立 = true;//節點成立
                                    else
                                    {
                                        條件成立 = false;
                                        break;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case ("MOV_ABS"):
                            STN.目標延遲時間 = 0;//軸運動開始步序 不需要延遲 不然會重複觸發
                                           //取得STN條件指定軸,下讀取8164指令驅動軸前進到絕對位置
                            if (Motion._8164_motion_done((short)STN.ConList[i].軸編號) > 0)
                            {
                                條件成立 = false;
                            }
                            else
                            {
                                if (STN.完整字串.IndexOf("天車拉料點") >= 0)
                                {
                                    //取得目前天車位置
                                    int Position = 0;
                                    Motion._8164_get_command((short)STN.ConList[i].軸編號, ref Position);
                                    //判斷天車是否在 "天車軌道點", 若不在 表示伺服失步, 需要RESET
                                    if (Position != SYS.天車軌道點)
                                    {
                                        原點運動(1, -200, -1200, 0);
                                        while ((Motion._8164_motion_done(1) > 0))
                                        {
                                            Application.DoEvents();
                                        }
                                    }
                                }
                                if ((short)STN.ConList[i].軸編號 == 1)
                                    絕對運動((short)STN.ConList[i].軸編號,
                                                                STN.ConList[i].軸目標位置, MV.axis[Node.軸編號].初速,
                                                                MV.axis[Node.軸編號].常速, 0.4, 0.4);
                                else
                                    絕對運動((short)STN.ConList[i].軸編號,
                                                                STN.ConList[i].軸目標位置, MV.axis[Node.軸編號].初速,
                                                                MV.axis[Node.軸編號].常速, 0.1, 0.1);

                                條件成立 = true;
                            }
                            if (cb單步模式.Checked)//單動下送完運動指令直接跳下一步進行到位比對
                                IDX_Step步序++;
                            break;
                        case ("MOV_RLA"):
                            STN.目標延遲時間 = 0;
                            if (Motion._8164_motion_done((short)STN.ConList[i].軸編號) > 0)
                                條件成立 = false;
                            else
                            {
                                //取得STN條件指定軸,下讀取8164指令驅動軸前進相對位置
                                AxisPos = 0;
                                Motion._8164_get_command((short)STN.ConList[i].軸編號, ref AxisPos);
                                相對運動((short)STN.ConList[i].軸編號,
                                                            STN.ConList[i].軸目標位置, MV.axis[Node.軸編號].初速,
                                                            MV.axis[Node.軸編號].常速, 0.1, 0.1);
                                條件成立 = true;
                                //此處很重要,相對運動時, 必須檢查下一步有沒有要做CMP檢查,
                                //如果有  現在就必須將相對運動後的目標位置寫給下一個檢查步序
                                //此處暫時 不考慮 下一步是結束....應該沒這種狀況
                                if (StepArray[IDX_Step步序 + 1].ConList[0].種類.CompareTo("CMP") == 0)
                                    StepArray[IDX_Step步序 + 1].ConList[0].軸目標位置 = AxisPos + STN.ConList[i].軸目標位置;

                            }
                            if (cb單步模式.Checked)//單動下送完運動指令直接跳下一步進行到位比對
                                IDX_Step步序++;
                            break;
                        case ("MOV_CTN"):
                            STN.目標延遲時間 = 0;
                            if (Motion._8164_motion_done((short)STN.ConList[i].軸編號) > 0)
                                條件成立 = false;
                            else
                            {
                                //軸連續運動
                                連續運動((short)STN.ConList[i].軸編號, MV.axis[Node.軸編號].初速,
                                                        MV.axis[Node.軸編號].常速, 0.1);
                                條件成立 = true;
                            }
                            if (cb單步模式.Checked)//單動下送完運動指令直接跳下一步進行到位比對
                                IDX_Step步序++;
                            break;
                        case ("STOP"):
                            //取得STN條件指定軸,下讀取8164指令驅動軸前進相對位置
                            Motion._8164_sd_stop((short)STN.ConList[i].軸編號, 0.1);
                            break;
                        case ("SPD"):
                            //取得STN條件指定軸,下讀取8164指令驅動軸前進相對位置
                            MV.axis[Node.軸編號].常速 = Node.軸目標速度;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (條件成立) //檢查所有節點 若全部成立 開始進行延遲
            {
                STN.已延遲時間++;
                if (STN.已延遲時間 >= STN.目標延遲時間)//進行下一步
                {
                    //當要跳下一步時, 需要進行檢查是否已到某些重複模組的最後一步
                    //此時要將步序回到該模組第一步
                    //EX. 0進彈匣~10週期生產10片~38退彈匣,在退彈匣條件滿足前必須重覆在 10~37執行
                    if (cb單步模式.Checked)
                    {
                        //若下一步 為STOP, 且目前為單動模式, 直接跳下一步STOP執行(防止單動按太慢撞機)
                        if (STN_NEXT.條件註解.CompareTo("STOP") == 0)
                            IDX_Step步序++;
                        else if ((STN.條件註解.CompareTo("條碼模組") == 0) ||
                                (STN.條件註解.CompareTo("數片模組") == 0) ||
                                (STN.條件註解.CompareTo("走到下一格有料格") == 0) ||
                                (STN.條件註解.CompareTo("撥料模組") == 0) ||
                                (STN.條件註解.CompareTo("檢測模組") == 0) ||
                                (STN.條件註解.CompareTo("蓋印模組") == 0))
                            //若當下條件為外部模組 因為單動下IDX不會自行++ 所以要幫忙++
                            IDX_Step步序++;

                    }
                    else
                    {
                        switch (STN_NEXT.條件註解)
                        {
                            case ("////退彈匣////")://判定整個彈匣是否做完, 否則回到生產週期進下一片
                                bool finish = true;
                                for (int i = 0; i <= CASArray.Length - 1; i++)
                                {
                                    if ((CASArray[i].有無材料 == 1) && (CASArray[i].檢測結果 == 0))
                                        finish = false;
                                }
                                if (!finish)
                                {
                                    int index = 0;
                                    while (StepArray[index].條件註解.CompareTo("//生產週期開始") != 0)
                                        index++;
                                    IDX_Step步序 = index;//回到自動檢測週期第一步
                                }
                                else
                                    IDX_Step步序++;
                                break;
                            default:
                                IDX_Step步序++;
                                break;
                        }
                        STN.已延遲時間 = 0;
                        STN.已等待時間 = 0;
                    }

                }
            }
            else
            {//只要延遲時間還沒到達的情況下, 發生此步完成條件不滿足, 則延遲時間歸零
                STN.已延遲時間 = 0;
            }

            //************等待過久異常判定*****************
            if (!cb單步模式.Checked)
            {
                if ((STN.已等待時間 >= STN.等待上限時間) && (STN.等待上限時間 > 0))
                {
                    string 不成立節點 = "";
                    for (int i = 0; i <= STN.ConList.Count - 1; i++)
                    {
                        if (!STN.ConList[i].節點成立)
                        {
                            不成立節點 = STN.ConList[i].註解;
                            break;
                        }
                    }
                    ST.Req_STATE.IDX = 5;//要求異常狀態
                    ST.Req_STATE.Context = "動作逾時: " + 不成立節點;
                    STN.已延遲時間 = 0;
                    STN.已等待時間 = 0;
                    goto 步序結束;
                }
            }

            if (ST.Curr_STATE.IDX == 1)
                timer_步序.Enabled = true;
            步序結束:
            if (CurrIDX != IDX_Step步序)
            {
                listBox1.TopIndex = IDX_Step步序 - 5;
                listBox1.Refresh();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            DialogResult Name = openFileDialog1.ShowDialog();
            if (Name != DialogResult.OK)
                return;
            if ((openFileDialog1.SafeFileName.IndexOf(".TXT") == -1) &&
                (openFileDialog1.SafeFileName.IndexOf(".txt") == -1) &&
                (openFileDialog1.SafeFileName.IndexOf("log") == -1) &&
                (openFileDialog1.SafeFileName.IndexOf("LOG") == -1))
                return;

            int indexF = openFileDialog1.FileName.LastIndexOf("\\");
            int indexB = openFileDialog1.FileName.LastIndexOf(".");
            button11.Text = openFileDialog1.FileName.Substring(indexF + 1, indexB - indexF - 1);

            StreamReader SR = new StreamReader(openFileDialog1.FileName, Encoding.Default);
            string TotalString = SR.ReadToEnd();
            SR.Dispose();
            //log 格式
            if ((TotalString.IndexOf("RowData:") == -1) && (TotalString.IndexOf("___ ") >= 0)) //log檔格式
            {
                ST.MAPpath = openFileDialog1.FileName;//進場後再依照刷barcode修改
                Process_ServerLOG(ST.MAPpath);
            }//map 格式
            else if (TotalString.IndexOf("RowData:") > -1)//map檔格式
            {
                ST.MAPpath = openFileDialog1.FileName;//進場後再依照刷barcode修改
                Process_ServerMAP(ST.MAPpath);
            }
            else
            {
                MessageBox.Show("格式錯誤, 請檢查MAP資料內容");
            }
        }

        public void Process_ServerLOG(string FilePath)
        {
            //20141209
            //取得wafer檔案在map中的基本資訊
            StreamReader SR = new StreamReader(FilePath);
            //取得map寬高
            SR = new StreamReader(FilePath);
            string line;
            int Temp = 0;
            string tempLine;
            int RowIndex = 0;
            int LineWidth = 0;
            int LineHeight = 0;
            char[] temp = new char[1];
            temp[0] = '.';
            //抓出整個map檔的寬高
            //從檔案第一行開始不停讀取直到找到 RowData開始的第一行
            LineWidth = 0;
            while ((line = SR.ReadLine()) != null)
            {
                Temp = line.IndexOf("___", 0);
                if (Temp > -1)
                {
                    if (LineWidth == 0)
                    {
                        //紀錄RowData寬度
                        int a = line.Length;
                        int lastIDX = line.LastIndexOf("___ ");
                        LineWidth = (lastIDX + 4) / 4;
                    }
                    //累加RowData高度
                    LineHeight += 1;
                }
            }

            SR.Dispose();
            //依照上面取得的寬高宣告matrix並將資料存入
            MAP.Matrix = new MapClass.DieInfoStruct[LineHeight, LineWidth];
            //將MAP資料放入記憶體矩陣
            RowIndex = 0;
            SR = new StreamReader(FilePath);
            //252
            while ((line = SR.ReadLine()) != null)
            {
                line = line.Trim();
                Temp = line.IndexOf("___", 0);
                if (Temp > -1)
                {
                    //line = line.Substring(8, line.Length - 8);
                    tempLine = line;
                    for (int i = 0; i <= LineWidth - 1; i++)
                    {
                        int L = tempLine.Length;
                        MAP.Matrix[RowIndex, i].MapString = tempLine.Substring(i * 4, 3);
                    }

                    RowIndex += 1;
                }
            }
            SR.Dispose();

            //MAP.Matrix = Matrix;
            MAP.LineWidth = MAP.Matrix.GetLength(1);
            MAP.LineHeight = MAP.Matrix.GetLength(0);

            if (PD.參考DIE字元.CompareTo("___") == 0)
            {
                MAP.FstX = PD.左參考dieIDX_X;
                MAP.FstY = PD.左參考dieIDX_Y;
                goto FIND_REF;
            }

            for (int y = 0; y <= MAP.LineWidth - 1; y++)
            {
                for (int x = 0; x <= MAP.LineWidth - 1; x++)
                {
                    if (MAP.Matrix[y, x].MapString.CompareTo(PD.參考DIE字元) == 0)
                    {
                        //有BUG
                        MAP.FstX = x;
                        MAP.FstY = y;
                        goto FIND_REF;
                    }
                }
            }

        FIND_REF:
            //int Xindex = 0;
            //while (DEV.NullList.Contains(MAP.Matrix[0, Xindex].MapString))
            //{
            //    Xindex++;
            //}

            CK.Matrix = new CheckClass.DieInfoStruct[MAP.Matrix.GetLength(0), MAP.Matrix.GetLength(1)];
            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {
                    CK.Matrix[y, x] = new CheckClass.DieInfoStruct();
                }
            }
        }

        private void saveGlobeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Title = "Save an Image File";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
                img_Globe.SaveFile(saveFileDialog1.FileName, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_JPG);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            int KeyValue = Convert.ToInt32(e.KeyChar);
            if (KeyValue == 13)//按下enter
            {
                ST.批號 = textBox_批號.Text;
            }
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            int KeyValue = Convert.ToInt32(e.KeyChar);
            if (KeyValue == 13)//按下enter
            {
                ST.客批 = textBox_客戶.Text;
            }
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            int KeyValue = Convert.ToInt32(e.KeyChar);
            if (KeyValue == 13)//按下enter
            {


                string LoadDir = 路徑_產品資料夾 + textBox_程式.Text;
                if (!Directory.Exists(LoadDir) || textBox_程式.Text == "")
                {
                    DialogResult result1 =
                        MessageBox.Show("產品不存在");
                    textBox_程式.Text = ST.產品代號;
                    //,
                    //     "產品別不存在",
                    //    MessageBoxButtons.YesNo);
                    return;
                    //if (result1 == DialogResult.Yes)
                    //{
                    //    F.SetV(ref PDArray, "產品代號", ST.產品代號);
                    //    string Dir_SAVE = 路徑_產品資料夾 + ST.產品代號;
                    //    Save_PD();
                    //}
                    //else
                    //{
                    //    Load_PD("Default");
                    //    RefreshImage_動態影像(ref img即時影像);
                    //    RefreshPanel();
                    //}
                }
                else
                {
                    ST.產品代號 = textBox_程式.Text;
                    Load_PD(ST.產品代號);
                    RefreshImage_動態影像(ref img即時影像);
                    RefreshPanel();
                }
            }
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            //取得wafer ID
            int KeyValue = Convert.ToInt32(e.KeyChar);
            if (KeyValue == 13)//按下enter
            {
                ST.晶圓ID = textBox_手動條碼.Text;
                label_WID.Text = textBox_手動條碼.Text;
                ST.MAPName = ST.晶圓ID;
                ////按照waferID下載對影MAP檔案
                //ST.MAPpath = F.Vs(sysArray, "MAP資料庫位置") + "\\" + ST.晶圓ID + ".txt";
                //try
                //{
                //    StreamReader SR = new StreamReader(ST.MAPpath);
                //    string TotalString = SR.ReadToEnd();

                //    if (TotalString.IndexOf("RowData:") == -1)
                //    {
                //        MessageBox.Show("MAP Format Error!!");
                //        return;
                //    }
                //    button11.Text = ST.MAPName;
                //    Process_ServerMAP(ST.MAPpath);
                //    SR.Dispose();
                //}
                //catch (Exception)
                //{
                //    MessageBox.Show("找不到MAP檔,請檢查資料庫或waferID!");
                //    return;
                //}
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            string OutputDIR = F.Vs(sysArray, "LOG位置") + @"History\";
            System.Diagnostics.Process.Start("EXPLORER.EXE", OutputDIR);
        }


        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            //textBox_批號.Text = "";
            //textBox_客戶.Text = "";
            //label_WID.Text = "";
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ROI1.ParentHandle = img_Globe.VegaHandle;
            axAxImageCopier1.SrcImageHandle = ROI1.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_DIE.VegaHandle;
            axAxImageCopier1.Copy();
            F.SetV(ref PDArray, "DIE寬", ROI1.ROIWidth);
            F.SetV(ref PDArray, "DIE高", ROI1.ROIHeight);
            //同步更新相關檢測參數
            F.SetV(ref PDArray, "ROW搜尋高度", Convert.ToInt32(ROI1.ROIHeight * 0.8f));
            F.SetV(ref PDArray, "中心誤差X", Convert.ToInt32(ROI1.ROIWidth * 0.3f));
            F.SetV(ref PDArray, "中心誤差Y", Convert.ToInt32(ROI1.ROIHeight * 0.2f));
            F.SetV(ref PDArray, "判定區尺寸W", Convert.ToInt32(ROI1.ROIWidth * 0.3f));
            F.SetV(ref PDArray, "判定區尺寸H", Convert.ToInt32(ROI1.ROIHeight * 0.3f));
            Save_PD();
        }

        private void 教導空格影像ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ROI1.ParentHandle = img_Globe.VegaHandle;
            //學習樣本影像
            //尋找上下左右邊界
            axAxImageCopier1.SrcImageHandle = ROI1.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_EPT.VegaHandle;
            axAxImageCopier1.Copy();
            mch_Ept.SrcImageHandle = img_EPT.VegaHandle;
            mch_Ept.LearnPattern();
            Save_PD();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = 路徑_程式DATA資料夾 + "History\\";
            DialogResult result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;
            img_Globe.LoadFile(openFileDialog1.FileName);
            zoom = 0.057f;

            axAxCanvas1.CanvasWidth = Convert.ToInt32(img_Globe.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(img_Globe.ImageHeight * zoom);
            ROI1.ParentHandle = img_Globe.VegaHandle;
            RefreshImage_動態影像(ref img即時影像);
            Refreshimage_靜態影像();
        }

        //按照滑鼠作標取得滑鼠停留在影像上的哪一顆產品位置
        public void GetIndex(int mouseX, int mouseY, ref int indexX, ref int indexY)
        {
            if (CK.Matrix == null)
                return;
            if (CK.Matrix.GetLength(1) != MAP.Matrix.GetLength(1))
                return;
            int imageX = Convert.ToInt32(mouseX / zoom);
            int imageY = Convert.ToInt32(mouseY / zoom);
            bool getY = false;
            if (MAP.Matrix == null) return;
            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                if ((CK.Matrix[y, MAP.FstX].coordY < imageY) && (CK.Matrix[y, MAP.FstX].coordY + PD.DieH > imageY))
                {
                    indexY = y;
                    getY = true;
                }
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {
                    if ((CK.Matrix[y, x].coordX < imageX) && (CK.Matrix[y, x].coordX + PD.DieW > imageX))
                    {
                        indexX = x;
                        if (getY)
                        {
                            return;
                        }
                    }
                }
            }
            return;
        }
        private void button21_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
            checkBox5.Checked = true;
            checkBox6.Checked = true;
            checkBox7.Checked = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ROI_Draw.ShowPlacement = false;
            ROI_Draw.ShowTitle = false;
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            //劃出找到的ref die
            if (mch_ref.EffectMatch)
            {
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                ROI_Draw.Title = "LRef";
                ROI_Draw.SetPlacement(mch_ref.MatchedX + roi_ref.OrgX,
                                      mch_ref.MatchedY + roi_ref.OrgY,
                                      mch_ref.PatternWidth,
                                      mch_ref.PatternHeight);

                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
            }

            // //劃出右邊ref die搜尋框
            roi_refR.SetPlacement(PD.右參考DIE影像X - PD.參考DIE搜尋範圍,
                                     PD.右參考DIE影像Y - PD.參考DIE搜尋範圍,
                                     mch_ref.PatternWidth + 2 * PD.參考DIE搜尋範圍,
                                     mch_ref.PatternHeight + 2 * PD.參考DIE搜尋範圍);

            if (mch_refR.EffectMatch)
            {
                roi_refR.ShowTitle = false;
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                ROI_Draw.Title = "RRef";
                ROI_Draw.SetPlacement(mch_refR.MatchedX + roi_refR.OrgX,
                                      mch_refR.MatchedY + roi_refR.OrgY,
                                      mch_refR.PatternWidth,
                                      mch_refR.PatternHeight);

                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
            }

            //顯示所有定位格
            for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= CK.Matrix.GetLength(1) - 1; x++)
                {
                    if (((CK.Matrix[y, x].DieColor == BAD_Ept) && (checkBox1.Checked)) ||
                       ((CK.Matrix[y, x].DieColor == GOOD_DEV) && (checkBox5.Checked)) ||
                       ((CK.Matrix[y, x].DieColor == BAD_DEV) && (checkBox6.Checked)) ||
                       ((CK.Matrix[y, x].DieColor == GOOD_Ept) && (checkBox7.Checked)))
                    {
                        ROI_Draw.SetPlacement(CK.Matrix[y, x].coordX - PD.判定區尺寸W,
                                              CK.Matrix[y, x].coordY - PD.判定區尺寸H,
                                              PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                        ROI_Draw.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, CK.Matrix[y, x].DieColor);
                    }
                }
            }
            axAxCanvas1.RefreshCanvas();
        }

        private void 版本資訊ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //  Process.Start(路徑_程式DATA資料夾 + "..\\版本資訊.txt");
        }

        private void 更新參數檔格式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Update_Para();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //顯示產品管理視窗
            ProjectManage PM = new ProjectManage(this);
            PM.Show();
        }

        public void Del_Old_File(string Path, int daycount)
        {
            DateTime dtNow = DateTime.Now;
            if (!Directory.Exists(Path))  //無此資料夾則退出
            {
                return;
            }

            string[] log_PD = Directory.GetDirectories(Path);   //取得所有PD資料夾
            foreach (string str_img_PD in log_PD)
            {
                string[] PD_img_Info = Directory.GetFiles(str_img_PD);    //取得PD資料夾下每1筆圖檔資料夾
                foreach (string str_img_Info in PD_img_Info)
                {
                    DirectoryInfo img_Info = new DirectoryInfo(str_img_Info); //取得資料夾檔案資訊
                    TimeSpan ts = dtNow.Subtract(img_Info.LastWriteTime);   //取得最後修改時間與現在時間差
                    if (ts.TotalMinutes > daycount * 24 * 60)//距離在daycount天以上
                    {
                        File.Delete(img_Info.FullName);
                    }
                }
                PD_img_Info = Directory.GetFiles(str_img_PD);    //取得PD資料夾下每1筆圖檔資料夾
                if (PD_img_Info.Length == 0)
                    Directory.Delete(str_img_PD);
            }
        }

        private void lOG轉MAPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;
            string MapPath = openFileDialog1.FileName.Substring(0, openFileDialog1.FileName.Length - 4) + ".txt";
            StreamWriter sw = new StreamWriter(MapPath, true);

            StreamReader SR = new StreamReader(openFileDialog1.FileName, Encoding.Default);
            string line = "";
            while ((line = SR.ReadLine()) != null)
            {
                if (line.IndexOf("001") != -1)
                {
                    line.Replace('%', ' ');
                    //if (line.IndexOf("*Y=") != -1)
                    //{
                    //    int Cut = line.IndexOf("*Y=");
                    //    line = line.Substring(0, line.Length-( line.Length - Cut));
                    //}
                }
                sw.WriteLine(line);
            }
            sw.Close();

        }

        private void 參考DIE位置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ST.Mode_Live) return;

            //教導樣本
            ROI1.ParentHandle = img_Globe.VegaHandle;
            axAxImageCopier1.SrcImageHandle = ROI1.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_REF.VegaHandle;
            axAxImageCopier1.Copy();
            mch_ref.SrcImageHandle = img_REF.VegaHandle;
            mch_ref.LearnPattern();


            F.SetV(ref PDArray, "左參考DIE影像X", ROI1.OrgX);
            F.SetV(ref PDArray, "左參考DIE影像Y", ROI1.OrgY);
            Save_PD();
            Refreshimage_靜態影像();
        }

        private void 最右參考DIE位置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ST.Mode_Live) return;
            ROI1.ParentHandle = img_Globe.VegaHandle;
            ROI1.ParentHandle = img_Globe.VegaHandle;
            F.SetV(ref PDArray, "右參考DIE影像X", ROI1.OrgX);
            F.SetV(ref PDArray, "右參考DIE影像Y", ROI1.OrgY);

            Save_PD();

            Refreshimage_靜態影像();
        }

        public void Refreshimage_Area()
        {
            axAxCanvas5.CanvasWidth = Convert.ToInt32(img_Area.ImageWidth * zoom分析);
            axAxCanvas5.CanvasHeight = Convert.ToInt32(img_Area.ImageHeight * zoom分析);
            img_Area.DrawImage(axAxCanvas5.hDC, zoom分析, zoom分析, 0, 0);
            if (radioButton3.Checked)
            {
                mch_Ept.DstImageHandle = ROI2.VegaHandle;
                mch_Ept.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
                mch_Ept.AbsoluteCoord = true;
                mch_Ept.Match();
                mch_Ept.DrawMatchedPattern(axAxCanvas5.hDC, -1, zoom分析, zoom分析, 0, 0);
            }
            else
            {
                mch_DEV.DstImageHandle = ROI2.VegaHandle;
                mch_DEV.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
                mch_DEV.AbsoluteCoord = true;
                mch_DEV.Match();
                mch_DEV.DrawMatchedPattern(axAxCanvas5.hDC, -1, zoom分析, zoom分析, 0, 0);
            }

            roi_drawArea.ParentHandle = img_Area.VegaHandle;
            if ((PD.中心誤差X > 0) && (PD.中心誤差Y > 0))
            {
                roi_drawArea.SetPlacement(mch_Ept.MatchedX - PD.中心誤差X - mch_Ept.PatternWidth / 2,
                                          mch_Ept.MatchedY - PD.中心誤差Y - mch_Ept.PatternHeight / 2,
                                          mch_Ept.PatternWidth + (2 * PD.中心誤差X),
                                          mch_Ept.PatternHeight + (2 * PD.中心誤差Y));

                roi_drawArea.DrawSnap(axAxCanvas5.hDC, zoom分析, zoom分析, 0, 0, 0xAA00FF);
            }
            if (PD.ROW搜尋高度 > 0)
            {
                roi_drawArea.SetPlacement(0, mch_Ept.MatchedY - PD.ROW搜尋高度, img_Area.ImageWidth, PD.ROW搜尋高度 * 2);
                roi_drawArea.DrawSnap(axAxCanvas5.hDC, zoom分析, zoom分析, 0, 0, 0xAA00FF);
            }

            ROI2.DrawFrame(axAxCanvas5.hDC, zoom分析, zoom分析, 0, 0, 0xFF3300);
            axAxCanvas5.RefreshCanvas();
        }

        private void 分析區域ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axAxImageCopier1.SrcImageHandle = ROI1.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_Area.VegaHandle;
            axAxImageCopier1.Copy();

            ROI2.ParentHandle = img_Area.VegaHandle;
            ROI2.SetPlacement(10, 10, 100, 100);

            Refreshimage_Area();
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            ROI2.ParentHandle = img_Area.VegaHandle;
            //學習樣本影像
            //尋找上下左右邊界
            axAxImageCopier1.SrcImageHandle = ROI2.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_EPT.VegaHandle;
            axAxImageCopier1.Copy();
            mch_Ept.SrcImageHandle = img_EPT.VegaHandle;
            mch_Ept.LearnPattern();
            Save_PD();
        }

        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            ROI2.ParentHandle = img_Area.VegaHandle;
            axAxImageCopier1.SrcImageHandle = ROI2.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_DIE.VegaHandle;
            axAxImageCopier1.Copy();
            F.SetV(ref PDArray, "DIE寬", ROI2.ROIWidth);
            F.SetV(ref PDArray, "DIE高", ROI2.ROIHeight);
            Save_PD();
        }

        private void 位置誤差範圍ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mch_Ept.DstImageHandle = ROI2.VegaHandle;
            mch_Ept.AbsoluteCoord = true;
            mch_Ept.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;

            mch_Ept.Match();
            if (mch_Ept.NumMatchedPos == 0)
            {
                MessageBox.Show("ROI內沒有完整空格, 無法進行設定");
            }
            F.SetV(ref PDArray, "中心誤差X", mch_Ept.MatchedX - (mch_Ept.PatternWidth / 2) - ROI2.OrgX);
            F.SetV(ref PDArray, "中心誤差Y", mch_Ept.MatchedY - (mch_Ept.PatternHeight / 2) - ROI2.OrgY);
            Save_PD();
            Refreshimage_Area();
        }

        private void rOW搜尋高度ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mch_Ept.DstImageHandle = ROI2.VegaHandle;
            mch_Ept.AbsoluteCoord = true;
            mch_Ept.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
            mch_Ept.Match();
            if (mch_Ept.NumMatchedPos == 0)
            {
                MessageBox.Show("ROI內沒有完整空格, 無法進行設定");
            }
            F.SetV(ref PDArray, "ROW搜尋高度", ROI2.ROIHeight / 2);
            Save_PD();
            Refreshimage_Area();
        }

        private void 檢測結果ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            ROI_Draw.ShowPlacement = false;
            ROI_Draw.ShowTitle = false;
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            //劃出找到的ref die
            if (mch_ref.EffectMatch)
            {
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                ROI_Draw.Title = "LRef";
                ROI_Draw.SetPlacement(mch_ref.MatchedX + roi_ref.OrgX,
                                      mch_ref.MatchedY + roi_ref.OrgY,
                                      mch_ref.PatternWidth,
                                      mch_ref.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
            }

            // //劃出右邊ref die搜尋框
            roi_refR.SetPlacement(PD.右參考DIE影像X - PD.參考DIE搜尋範圍,
                                     PD.右參考DIE影像Y - PD.參考DIE搜尋範圍,
                                     mch_ref.PatternWidth + 2 * PD.參考DIE搜尋範圍,
                                     mch_ref.PatternHeight + 2 * PD.參考DIE搜尋範圍);
            if (mch_refR.EffectMatch)
            {
                roi_refR.ShowTitle = false;
                roi_refR.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
                ROI_Draw.Title = "RRef";
                ROI_Draw.SetPlacement(mch_refR.MatchedX + roi_refR.OrgX,
                                      mch_refR.MatchedY + roi_refR.OrgY,
                                      mch_refR.PatternWidth,
                                      mch_refR.PatternHeight);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb22FF);
            }
            else
            {
                roi_ref.ShowTitle = true;
                roi_ref.Title = "ref die miss";
                roi_ref.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00FF00);
            }

            //顯示所有定位格
            if (CK.Matrix != null)
            {
                for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
                {
                    for (int x = 0; x <= CK.Matrix.GetLength(1) - 1; x++)
                    {
                        if (((CK.Matrix[y, x].DieColor == BAD_Ept) && (checkBox1.Checked)) ||
                           ((CK.Matrix[y, x].DieColor == GOOD_DEV) && (checkBox5.Checked)) ||
                           ((CK.Matrix[y, x].DieColor == BAD_DEV) && (checkBox6.Checked)) ||
                           ((CK.Matrix[y, x].DieColor == GOOD_Ept) && (checkBox7.Checked)))
                        {
                            ROI_Draw.SetPlacement(CK.Matrix[y, x].coordX - PD.判定區尺寸W, CK.Matrix[y, x].coordY - PD.判定區尺寸H,
                                                                        PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);
                            ROI_Draw.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, CK.Matrix[y, x].DieColor);
                        }

                    }
                }
            }
            顯示檢測結果();

            axAxCanvas1.RefreshCanvas();
        }

        private void 所有定位格ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ROI_Draw.ShowPlacement = false;
            ROI_Draw.ShowTitle = false;
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            //顯示所有定位格
            for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= CK.Matrix.GetLength(1) - 1; x++)
                {
                    if (!PD.NullList.Contains(MAP.Matrix[y, x].MapString))
                    {
                        ROI_Draw.SetPlacement(CK.Matrix[y, x].coordX - PD.判定區尺寸W,
                                              CK.Matrix[y, x].coordY - PD.判定區尺寸H,
                                              PD.判定區尺寸W * 2, PD.判定區尺寸H * 2);

                        if (CK.Matrix[y, x].CheckState == 99)
                        {
                            ROI_Draw.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x00a5ff);
                        }
                        else
                            ROI_Draw.DrawSnap(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xff00ff);
                    }
                }
            }
            axAxCanvas1.RefreshCanvas();
        }

        private void 清除檢測結果ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Refreshimage_靜態影像();
        }

        private void 重載點位總表ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            F.Load_PLCMatrix(ref F.InMatrix, ref F.OutMatrix, 路徑_點位總表);
        }

        private void 重載動作流程ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (label彈匣尺寸.Text.CompareTo("12吋 彈匣放置中") == 0)
                F.Load_STEP(ref StepArray, 路徑_動作流程12);
            else
                F.Load_STEP(ref StepArray, 路徑_動作流程8);

            更新步序參數值();
            ST.Req_STATE.IDX = 0;
            listBox1.Refresh();
        }


        public string 條碼模組(bool 手動輸入)
        {
            Font fff = new Font("微軟正黑體", 20, FontStyle.Bold);
            int 檢測次數 = 0;
            bool 讀取成功 = false;
            if (手動輸入)
                讀取成功 = true;
            else
            {

                axAxAltairU1.DacCh1Switch = true;
                axAxAltairU1.DacCh1Value = PD.CKLight;
                axAxAltairU1.Freeze();
                mch_條碼.MinScore = 0.2f;
                mch_條碼.ToleranceAngle = 359;
            Check_2ndBarcode:
                檢測次數++;
                if (檢測次數 == 1)
                {
                    //伺服走到條碼位置
                    絕對運動(MV.軸4載台X, SYS.X條碼點, MV.X初速, MV.X常速, 0.4, 0.4);
                    絕對運動(MV.軸5載台Y, SYS.Y條碼點, MV.Y初速, MV.Y常速, 0.4, 0.4);
                }
                else if (檢測次數 == 2)
                {
                    絕對運動(MV.軸4載台X, SYS.X條碼點2, MV.X初速, MV.X常速, 0.4, 0.4);
                    絕對運動(MV.軸5載台Y, SYS.Y條碼點2, MV.Y初速, MV.Y常速, 0.4, 0.4);
                }
                絕對運動(MV.軸6載台R, SYS.R條碼點, MV.R初速, MV.R常速, 0.4, 0.4);
                絕對運動(MV.軸7CCDZ, SYS.CCD檢測高度, MV.Z初速, MV.Z常速, 0.4, 0.4);
                while ((Motion._8164_motion_done(4) > 0) || (Motion._8164_motion_done(5) > 0) ||
                        (Motion._8164_motion_done(6) > 0)) Application.DoEvents();
                System.Threading.Thread.Sleep(500);
                //取像
                axAxAltairU1.SnapAndWait();
                img即時影像.SetSurfaceObj(axAxAltairU1.ActiveSurfaceHandle);
                img即時影像.DrawImage(axAxCanvas1.hDC, 0.2f, 0.2f, 0, 0);
                if (檢測次數 == 2)
                {
                    axAxImageRotator1.RotatorMethod = AxOvkImage.TxAxImageRotatorMethod.AX_ROTATE_ANY_ANGLE_WRT_CENTER_TO_PROPER_SIZE;
                    axAxImageRotator1.SrcImageHandle = img即時影像.VegaHandle;

                    axAxImageRotator1.DstImageHandle = img即時影像.VegaHandle;
                    axAxImageRotator1.RotateCenterX = img即時影像.ImageWidth / 2;
                    axAxImageRotator1.RotateCenterY = img即時影像.ImageHeight / 2;
                    axAxImageRotator1.RotateDegree = 90;
                }
                mch_條碼.DstImageHandle = img即時影像.VegaHandle;
                mch_條碼.Match();
                mch_條碼.DrawMatchedPattern(axAxCanvas1.hDC, 0, 0.2f, 0.2f, 0, 0);
                axAxCanvas1.RefreshCanvas();
                if (!mch_條碼.EffectMatch)
                {
                    if (檢測次數 < 2)
                        goto Check_2ndBarcode;
                    MessageBox.Show("條碼搜尋失敗");
                    return "條碼搜尋失敗";
                }
                axAxImageRotator1.RotatorMethod = AxOvkImage.TxAxImageRotatorMethod.AX_ROTATE_ANY_ANGLE_WRT_CENTER_TO_PROPER_SIZE;
                axAxImageRotator1.SrcImageHandle = img即時影像.VegaHandle;

                axAxImageRotator1.DstImageHandle = img_work.VegaHandle;
                axAxImageRotator1.RotateCenterX = mch_條碼.MatchedX;
                axAxImageRotator1.RotateCenterY = mch_條碼.MatchedY;
                // if (檢測次數 == 1)
                axAxImageRotator1.RotateDegree = mch_條碼.MatchedAngle;
                // else
                // axAxImageRotator1.RotateDegree = 90 - mch_條碼.MatchedAngle;
                axAxImageRotator1.Rotate();

                img_work.DrawImage(axAxCanvas1.hDC, 0.2f, 0.2f, 0, 0);
                mch_條碼.DstImageHandle = img_work.VegaHandle;
                mch_條碼.Match();
                mch_條碼.DrawMatchedPattern(axAxCanvas1.hDC, 0, 0.2f, 0.2f, 0, 0);
                //img_work.SaveFile("D:\\bbb.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
                axAxBarcodeScanner1.SrcImageHandle = img_work.VegaHandle;
                axAxBarcodeScanner1.ToleranceMargin = 5;
                // if (檢測次數 == 1)
                axAxBarcodeScanner1.SetPlacement(mch_條碼.MatchedX, mch_條碼.MatchedY + 100
                                        , mch_條碼.MatchedX + 1550, mch_條碼.MatchedY + 100);
                //else
                //    axAxBarcodeScanner1.SetPlacement(mch_條碼.MatchedX - 100, mch_條碼.MatchedY
                //                            , mch_條碼.MatchedX - 100, mch_條碼.MatchedY + 1550);
                axAxBarcodeScanner1.HalfHeight = 90;
                axAxBarcodeScanner1.DrawFrame(axAxCanvas1.hDC, 0.2f, 0.2f, 0, 0);
                讀取成功 = axAxBarcodeScanner1.ScanBarcode();

                if (axAxBarcodeScanner1.BarcodeData.IndexOf("-") == -1)
                    讀取成功 = false;
                axAxCanvas1.RefreshCanvas();
                if (!讀取成功)// 第一次讀取失敗 檢查另外一個位置
                {
                    g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
                    g.DrawString("讀取失敗", fff, Brushes.Red,
        (ROI_Draw.OrgX + 200) * 0.2f, (ROI_Draw.OrgY + 200) * 0.2f);
                    g.Dispose();
                    if (檢測次數 < 2)
                        goto Check_2ndBarcode;
                }
            }

            g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
            if (讀取成功)
            {
                if (手動輸入)
                    ST.晶圓ID = label_WID.Text;
                else
                    ST.晶圓ID = axAxBarcodeScanner1.BarcodeData;

                label_WID.Text = ST.晶圓ID;
                ST.MAPName = ST.晶圓ID;
                //按照waferID下載對影MAP檔案
                ST.MAPpath = F.Vs(sysArray, "MAP資料庫位置") + "\\" + ST.晶圓ID + ".txt";
                try
                {
                    StreamReader SR = new StreamReader(ST.MAPpath);
                    string TotalString = SR.ReadToEnd();

                    if (TotalString.IndexOf("RowData:") == -1)
                    {
                        MessageBox.Show("MAP Format Error!!");
                        return "MAP Format Error!!";
                    }
                    button11.Text = ST.MAPName;
                    Process_ServerMAP(ST.MAPpath);
                    SR.Dispose();
                    g.DrawString(ST.晶圓ID, fff, Brushes.Red,
                       (ROI_Draw.OrgX + 200) * 0.2f, (ROI_Draw.OrgY + 200) * 0.2f);
                    g.Dispose();
                }
                catch (Exception)
                {
                    MessageBox.Show("找不到MAP檔");
                    return "找不到MAP檔";
                }

            }
            else
            {
                g.DrawString("讀取失敗", fff, Brushes.Red,
                     (ROI_Draw.OrgX + 200) * 0.2f, (ROI_Draw.OrgY + 200) * 0.2f);
                g.Dispose();
                MessageBox.Show("讀取失敗");
                return "讀取失敗";
            }


            axAxCanvas1.RefreshCanvas();
            return "";

        }

        public void ShowMSG(string MSG, Color C)
        {

            richTextBox1.AppendText(MSG + "\r\n", C);
        }
        public string process_Barcode()
        {

            bool 讀取成功 = false;
            //int 檢測次數 = 0;
            Font fff = new Font("微軟正黑體", 12, FontStyle.Bold);
            zoom = 0.057f;
            axAxCanvas1.CanvasWidth = Convert.ToInt32(img_Globe.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(img_Globe.ImageHeight * zoom);
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            ROI_Draw.ShowTitle = true;
            if (SYS.條碼位置1X > 0)
            {
                ROI_Draw.Title = "barcode1";
                ROI_Draw.SetPlacement(SYS.條碼位置1X, SYS.條碼位置1Y, SYS.條碼位置1W, SYS.條碼位置1H);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb);
            }
            if (SYS.條碼位置2X > 0)
            {
                ROI_Draw.Title = "barcode2";
                ROI_Draw.SetPlacement(SYS.條碼位置2X, SYS.條碼位置2Y, SYS.條碼位置2W, SYS.條碼位置2H);
                ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xbb);
            }
            ROI_Draw.ShowTitle = false;
            if (textBox_手動條碼.Text.Length > 0)//手動強制輸入
            {
                讀取成功 = true;
                label_WID.Text = textBox_手動條碼.Text;
                ST.晶圓ID = label_WID.Text;
                button11.Text = ST.晶圓ID;
                button11.Refresh();
                ST.MAPName = ST.晶圓ID;
                textBox_手動條碼.Text = "";
            }
            else
            {
                label_WID.BackColor = Color.LightCyan;
                label_WID.Text = "";
                for (int 檢測次數 = 1; 檢測次數 <= 2; 檢測次數++)
                {
                    ROI_Work.ParentHandle = img_Globe.VegaHandle;
                    if (檢測次數 == 1)
                        ROI_Work.SetPlacement(SYS.條碼位置1X, SYS.條碼位置1Y, SYS.條碼位置1W, SYS.條碼位置1H);
                    else
                        ROI_Work.SetPlacement(SYS.條碼位置2X, SYS.條碼位置2Y, SYS.條碼位置2W, SYS.條碼位置2H);

                    axAxImageMorphology1.SrcImageHandle = ROI_Work.VegaHandle;
                    axAxImageMorphology1.DstImageHandle = img_temp.VegaHandle;
                    axAxImageMorphology1.ErodePixel = 2;
                    axAxImageMorphology1.Erode();
                    //img_temp.SaveFile("D:\\mm.jpg", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_JPG);
                    axAxBarcodeReader1.SrcImageHandle = img_temp.VegaHandle;
                    axAxBarcodeReader1.ToleranceMargin = 10;
                    axAxBarcodeReader1.NoiseFilterLevel = 1;
                    axAxBarcodeReader1.ExtendTolerance = 10;
                    for (int i = 4; i < 25; i++)
                    {
                        try
                        {
                            axAxBarcodeReader1.ModuleWidth = i;
                            axAxBarcodeReader1.ReadBarcode();
                            if (axAxBarcodeReader1.NumOfBarcodes == 1)
                            {
                                axAxBarcodeReader1.ResultIndex = 0;
                                if ((axAxBarcodeReader1.BarcodeType == AxOvkBarcodeTools.TxAxBarcodeType.AX_BARCODE_TYPE_CODE39) ||
                                        (axAxBarcodeReader1.BarcodeType == AxOvkBarcodeTools.TxAxBarcodeType.AX_BARCODE_TYPE_CODE128))
                                {
                                    //測試檔案是否存在
                                    string 測試MAP位置 = F.Vs(sysArray, "MAP資料庫位置") + "\\" + axAxBarcodeReader1.BarcodeData + ".txt";
                                    if (File.Exists(測試MAP位置))
                                    {
                                        讀取成功 = true;
                                    }
                                    break;//讀取出東西了 但未必對 跳出本次迴圈
                                }
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    if (讀取成功)
                    {
                        label_WID.Text = axAxBarcodeReader1.BarcodeData;
                        label_WID.Refresh();
                        ST.晶圓ID = label_WID.Text;
                        button11.Text = ST.晶圓ID;
                        button11.Refresh();
                        ST.MAPName = ST.晶圓ID;
                        break;
                    }
                }

            }


        手動輸入:
            g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
            if (讀取成功)
            {
                ST.MAPpath = F.Vs(sysArray, "MAP資料庫位置") + "\\" + ST.晶圓ID + ".txt";
                try
                {
                    StreamReader SR = new StreamReader(ST.MAPpath);
                    string TotalString = SR.ReadToEnd();

                    if (TotalString.IndexOf("RowData:") == -1)
                    {
                        ShowMSG("MAP格式異常:" + ST.MAPName, Color.Red);
                        return "MAP Format Error!!";
                    }
                    button11.Text = ST.MAPName;
                    Process_ServerMAP(ST.MAPpath);
                    SR.Dispose();
                    g.DrawString(ST.晶圓ID, fff, Brushes.Red,
                        (ROI_Work.OrgX + axAxBarcodeReader1.BarcodeCenterX + 800) * zoom,
                        (ROI_Work.OrgY + axAxBarcodeReader1.BarcodeCenterY - 100) * zoom);
                    g.Dispose();
                }
                catch (Exception)
                {
                    ShowMSG("找不到MAP:" + ST.MAPName, Color.Red);
                    return "找不到MAP檔";
                }

            }
            else
            {
                //if (label_WID.Text.Length > 0)
                //{
                //    g.DrawString("讀取失敗 載入手動ID", fff, Brushes.Blue,
                //         (ROI_Draw.OrgX + 200) * zoom, (ROI_Draw.OrgY - 300) * zoom);
                //    g.Dispose();
                //    讀取成功 = true;
                //    goto 手動輸入;
                //}

                g.DrawString("讀取失敗", fff, Brushes.Red,
                (ROI_Draw.OrgX - 1200) * zoom, (ROI_Draw.OrgY) * zoom);
                g.Dispose();
                axAxCanvas1.RefreshCanvas();
                ShowMSG("讀取失敗", Color.Red);
                return "讀取失敗";
            }


            axAxCanvas1.RefreshCanvas();
            img_temp.SetSize(1, 1);
            return "";

        }

        public void process_Wafer(bool 靜態分析)
        {
            zoom = 0.057f;
            string result = process_Barcode();//讀取條碼 載入MAP
            CK.檢測結果 = 0;
            if (result.Length > 0)
            {
                // MessageBox.Show(result);
                if (result.CompareTo("找不到MAP檔") == 0)
                    CK.檢測結果 = 5;//找不到MAP檔
                else
                    CK.檢測結果 = 3;//條碼讀取失敗
                                //ST.Req_STATE.IDX = 0;
                goto 檢測完畢;
            }

            if (!靜態分析)
            {
                //檢測觸發
                if (textBox_批號.Text.CompareTo("") == 0)
                {
                    CK.檢測結果 = 4;//資料缺失
                    goto 檢測完畢;
                }

                if (textBox_客戶.Text.CompareTo("") == 0)
                {
                    CK.檢測結果 = 4;//資料缺失
                    goto 檢測完畢;
                }
            }

            //if (label_WID.Text.CompareTo("") == 0)
            //{
            //    // MessageBox.Show("缺少WAFER ID");
            //    CK.檢測結果 = 4;//資料缺失
            //                //ST.Req_STATE.IDX = 0;
            //    goto 檢測完畢;
            //}



            CK.Reset(ref MAP);//重置CK ,必須要先載入MAP才能呼叫 因為要用到MAP的尺寸
            GC.Collect();
            //***********進行逐列檢查*************** 
            ProcessAll_byROW();

            //計算晶片顆數
            for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= CK.Matrix.GetLength(1) - 1; x++)
                {
                    if (!PD.NullList.Contains(MAP.Matrix[y, x].MapString) &&
                         MAP.Matrix[y, x].MapString.CompareTo(PD.參考DIE字元) != 0)
                    {
                        CK.晶粒總數++;
                        if (CK.Matrix[y, x].DieColor == BAD_Ept)//影像空料 MAP留DIE 紅色
                        {
                            CK.壞品誤取數++;
                            CK.空格數++;
                            CK.檢測結果 = 2;
                        }
                        else if (CK.Matrix[y, x].DieColor == GOOD_DEV)//影像有料 MAP該取 黃色
                        {
                            CK.良品留置數++;
                            CK.有料數++;
                            //CK.檢測結果 = 2;
                        }
                        else if (CK.Matrix[y, x].DieColor == BAD_DEV)//影像有料 MAP留DIE 綠色
                        {
                            CK.壞品留置數++;
                            CK.有料數++;
                        }
                        else if (CK.Matrix[y, x].DieColor == GOOD_Ept)//影像空料 MAP該取 淡藍色
                        {
                            CK.良品取走數++;
                            CK.空格數++;
                        }
                    }
                }
            }

        檢測完畢:
            顯示檢測結果();
            if (CK.檢測結果 == 0)
                CK.檢測結果 = 1;//OK!!
            CASArray[ST.當前格位].檢測結果 = CK.檢測結果;


            //flag_輸出格位資料 = true;
            //flag_輸出LOG = true;
            //儲存當前格位資料();
            //輸出LOG檔();

            if (CASArray[ST.當前格位].檢測結果 >= 2)
                CASArray[ST.當前格位].panel.BackColor = Color.HotPink;//NG
            else if (CASArray[ST.當前格位].檢測結果 == 1)
                CASArray[ST.當前格位].panel.BackColor = Color.LimeGreen;//OK


            寫入彈匣狀態();
            讀取彈匣狀態(ref CASArray);
            //if (CASArray[ST.當前格位].CK.壞品誤取數 > 0)
            //{
            //    ST.Req_STATE.IDX = 5;
            //    ST.Req_STATE.Context = "檢測異常";
            //}

        }

        public void 數片模組(bool 覆蓋彈匣資料)
        {
            MV.單軸回原點(this, 1);
            //timer_Step.Enabled = false;
            ST.FrameCNT = 0;
            int Pos = 0;

            for (int i = 0; i <= CASArray.Length - 1; i++)
            {
                if (CASArray[i].panel != null)
                {
                    CASArray[i].panel.BackColor = Color.Gray;
                    CASArray[i].有無材料 = 0;
                    if (覆蓋彈匣資料)
                    {
                        CASArray[i].檢測結果 = 0;
                        CASArray[i].LB.Text = i.ToString();
                    }
                }
            }

            //將彈匣移動到數片開始位置 14000
            if (label彈匣尺寸.Text.CompareTo("12吋 彈匣放置中 ") == 0)
            {
                絕對運動(MV.軸2彈匣, 25000, 300, 12000, 0, 0);
                while (Motion._8164_motion_done(2) > 0)
                    Application.DoEvents();
            }
            else
            {
                絕對運動(MV.軸2彈匣, 14000, 300, 12000, 0, 0);
                while (Motion._8164_motion_done(2) > 0)
                    Application.DoEvents();

            }
            //彈匣開始向上 數片  走到最後一片高度
            絕對運動(MV.軸2彈匣, 170000,
                1000, 13000, 0, 0);//此處移動要搭配SENSOR可以接受的數片速度
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
                Motion._8164_get_command(MV.軸2彈匣, ref Pos);
                //定義出當前格位IDX 的檢查範圍 
                CurrPitchLOW = SYS.彈匣數片第一點 + (PitchIDX * SYS.彈匣間距) - 3000;
                CurrPitchHight = SYS.彈匣數片第一點 + (PitchIDX * SYS.彈匣間距) + 3000;
                if (Pos > CurrPitchHight)
                {
                    PitchIDX++;
                }
                DI_1758.ReadBit(3, 0, ref NewV);
                if (NewV > CurrV)//從0變成1
                {
                    if (PitchIDX == CASArray.Length)
                        break;
                    ST.FrameCNT++;
                    CASArray[PitchIDX].panel.BackColor = Color.LightSkyBlue;
                    CASArray[PitchIDX].有無材料 = 1;
                    richTextBox1.AppendText(Pos + "\r\n");
                }
                CurrV = NewV;

                //if (PitchIDX == CASArray.Length - 1)
                //    break;
            }
            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();
            絕對運動(1, SYS.天車拉出點, 200, 2000, 0.1, 0.1);
            寫入彈匣狀態();
            // timer_Step.Enabled = true;
        }
        public void 蓋印模組()
        {
            //[MOV_ABS,4,X蓋印點]//X軸到印章位置
            //[MOV_ABS,5,Y蓋印點]//Y軸到印章位置
            //[CMP,4,X蓋印點,=
            //[CMP,5,Y蓋印點,=]
            //[OUT,1.06,1]*0*50//蓋印汽缸下
            //[IN,4.01,1]*0*50//蓋印汽缸下到位
            //[OUT,1.06,0]*0*50//蓋印汽缸上
            //[IN,4.00,1]*0*50//蓋印汽缸上到位
            if (SYS.蓋印選用 == 0) return;
            if (CASArray[ST.當前格位].檢測結果 != 1) return;//NG片不進行蓋印
            絕對運動(4, SYS.X蓋印點, MV.X初速, MV.X常速, 0.2, 0.2);
            絕對運動(5, SYS.Y蓋印點, MV.Y初速, MV.Y常速, 0.2, 0.2);
            MV.等待檢測平台動作完成();
            輸出1758(1, 6, 1);
            System.Threading.Thread.Sleep(500);
            輸出1758(1, 6, 0);
        }
        public void 檢測模組()
        {
            //進行全圖取像
            GC.Collect();
            if (跑機模式ToolStripMenuItem.Checked)
            {
                // 顯示檢測結果();
                CK.檢測結果 = 2;//NG
                CASArray[ST.當前格位].檢測結果 = CK.檢測結果;

                if (CASArray[ST.當前格位].檢測結果 >= 2)
                    CASArray[ST.當前格位].panel.BackColor = Color.HotPink;//NG
                else if (CASArray[ST.當前格位].檢測結果 == 1)
                    CASArray[ST.當前格位].panel.BackColor = Color.LimeGreen;//OK

                寫入彈匣狀態();
                讀取彈匣狀態(ref CASArray);
                return;
            }

            Get_Globe_Img(ref img_OrgGlobe);
            process_Wafer(false);
            label_WID.Text = "";
            ST.晶圓ID = "";
        }



        public void 更新步序參數值()
        {
            //載入PLC步序資料後, 必須要將用參數設定的數值 帶入步序陣列當中
            //EX.假設有一個節點為CMP要比對X軸到工作位置, 則需要將參數設定中的 X_工作位置帶入
            //所以很重要的是, GT.ini當中的參數, 若是要給步序使用的, 
            //*參數名稱必須要跟節點註解 一模一樣*
            listBox1.Items.Clear();
            for (int i = 0; i <= StepArray.Length - 1; i++)
            {
                string 輸出資料字串 = "[" + i + "]";
                if (StepArray[i].ConList.Count == 0)
                    輸出資料字串 += StepArray[i].條件註解;
                else
                {
                    for (int k = 0; k <= StepArray[i].ConList.Count - 1; k++)//檢查所有節點
                    {
                        File_Class.Motion_Step_Class.Motion_Node_Class NODE = StepArray[i].ConList[k];
                        //判斷是否需要連結參數
                        if (NODE.位置參數連結.Length > 0)
                        {
                            switch (NODE.種類)
                            {
                                case ("MOV_ABS"):
                                    NODE.軸目標位置 = F.Vi(sysArray, NODE.位置參數連結);
                                    輸出資料字串 += "(" + NODE.註解 + ")  " + NODE.軸目標位置;
                                    break;
                                case ("MOV_RLA"):
                                    NODE.軸目標位置 = F.Vi(sysArray, NODE.位置參數連結);
                                    輸出資料字串 += "(" + NODE.註解 + ")  " + NODE.軸目標位置;
                                    break;
                                case ("CMP"):
                                    NODE.軸目標位置 = F.Vi(sysArray, NODE.位置參數連結);
                                    輸出資料字串 += "(" + NODE.註解 + ")  " + NODE.軸目標位置;
                                    break;
                                case ("SPD"):
                                    NODE.軸目標速度 = F.Vi(sysArray, NODE.位置參數連結);
                                    輸出資料字串 += "(" + NODE.註解 + ")  " + NODE.軸目標速度;
                                    break;
                                default:
                                    break;
                            }

                        }
                        else
                            輸出資料字串 += "(" + NODE.註解 + ")";


                    }
                }
                //將步序表輸出到介面上
                listBox1.Items.Add(輸出資料字串);

            }






        }
        private void 重載警報條件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            F.Load_ALARM(ref ALMArray, 路徑_警報條件);
        }
        private void btnSTART_Click(object sender, EventArgs e)
        {
            //判斷啟動條件 進行啟動
            ST.Req_STATE.IDX = 1;//請求開始啟動

        }

        private void btnRESET_Click(object sender, EventArgs e)
        {
            //判斷復歸條件 進行復歸
            ST.Req_STATE.IDX = 2;//請求復歸
        }

        private void button20_Click(object sender, EventArgs e)
        {

            //timer_步序.Enabled = false;
            //MV.停止所有軸();
            //判斷停止條件 進行停止
            ST.Req_STATE.IDX = 0;//請求停止
        }

        private void timer_ST_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_ST_Thread.Stop();
            //0:停止/1:自動/2:復歸/4:緊急/5:異常/6:停止供料/7:不可繼續
            //ST.CurrentSTATE.State_IDX: // 機台目前的STATE
            //ST.Req_STATE: // 從任何地方送過來的 STATE改變請求
            //下面用"當前狀態"去判斷是否能接受外部送來的"要求狀態",掌握整機狀態機切換

            int 原始狀態 = ST.Curr_STATE.IDX;
            if (ST.Req_STATE.IDX != -1)
            {
                //判斷當前狀態  並對應 要求狀態 給出反應
                switch (ST.Curr_STATE.IDX)
                {
                    case (0)://***************停止***********
                        switch (ST.Req_STATE.IDX)
                        {
                            case (0)://請求停止
                                ST.Curr_STATE.IDX = 0;//切到停止狀態
                                break;
                            case (1)://請求啟動
                                //判斷啟動條件
                                if ((textBox_批號.Text.Length == 0) || (textBox_客戶.Text.Length == 0))
                                {
                                    ST.Req_STATE.IDX = -1;
                                    goto TICK_END;
                                }
                                if (IDX_Step步序 == -1)
                                    IDX_Step步序 = 0;
                                timer_步序.Enabled = true;//啟動步序掃描timer
                                                        //timer_Alarm_Thread.Change(100, 100);//啟動異常偵測timer
                                timer_Alarm_Thread.Start();
                                ST.Curr_STATE.IDX = 1;//狀態設定為  自動中
                                break;
                            case (2)://請求復歸
                                ST.Curr_STATE.IDX = 2;//狀態設定為  復歸中
                                break;
                            case (3)://請求工程動作
                                timer_Alarm_Thread.Start();
                                ST.Curr_STATE.IDX = 3;//狀態設定為工程動作中
                                ST.Req_STATE.IDX = -1;
                                break;
                            case (5)://請求異常
                                ST.Curr_STATE.IDX = 5;//切到異常狀態
                                break;
                            default:
                                break;
                        }
                        break;
                    case (1)://***************自動中************
                        switch (ST.Req_STATE.IDX)
                        {
                            case (0)://請求停止
                                ST.Curr_STATE.IDX = 0;//切到停止狀態
                                break;
                            case (5)://異常
                                ST.Curr_STATE.IDX = 5;//切到異常狀態
                                break;
                            default:
                                break;
                        }
                        break;
                    case (2)://***************復歸中****************
                        switch (ST.Req_STATE.IDX)
                        {
                            case (0)://請求停止
                                ST.Curr_STATE.IDX = 0;//切到停止狀態
                                break;
                            case (5)://請求異常
                                ST.Curr_STATE.IDX = 5;//切到異常狀態
                                break;
                            default:
                                break;
                        }
                        break;
                    case (3)://*************工程動作中(晶圓取像/校正...非全自動的狀態)************
                        switch (ST.Req_STATE.IDX)
                        {
                            case (0)://請求停止
                                ST.Curr_STATE.IDX = 0;//切到停止狀態
                                break;
                            case (5)://異常
                                ST.Curr_STATE.IDX = 5;//切到異常狀態
                                break;
                            default:
                                break;
                        }
                        break;
                    case (4)://****************緊急*********
                        byte V = 0;
                        DI_1758.ReadBit(0, 3, ref V);//讀取緊急input
                        if (V == 1)
                            ST.Curr_STATE.IDX = 7;//當異常解除僅能跳至 [必須重置]
                        ST.Req_STATE.IDX = -1;
                        break;
                    case (5)://****************普通異常****************
                        switch (ST.Req_STATE.IDX)
                        {
                            case (0)://請求停止
                                ST.Curr_STATE.IDX = 0;//切到停止狀態
                                break;
                            case (5)://請求異常                           
                                ST.Curr_STATE.IDX = 5;//切到異常狀態
                                break;
                            default:
                                break;
                        }
                        break;
                    case (7)://不可繼續狀態 必須停止
                        if (ST.Req_STATE.IDX == 0)
                        {
                            ST.Curr_STATE.IDX = 8;
                        }
                        break;
                    case (8)://不可繼續狀態 必須重置
                        if (ST.Req_STATE.IDX == 2)
                        {
                            ST.Curr_STATE.IDX = 2;//狀態設定為  復歸中
                        }
                        break;
                    default:
                        break;
                }
                // ST.Req_STATE.IDX = -1;
            }


            //判斷狀態是否發生改變 做對應的動作\
            if (ST.Curr_STATE.IDX != 原始狀態)
            {
                ST.Req_STATE.IDX = -1;
                switch (ST.Curr_STATE.IDX)
                {
                    case (0)://進入停止
                        timer_步序.Enabled = false;//暫停步序掃描timer
                        timer_Alarm_Thread.Stop();
                        //MV.停止所有軸();
                        break;
                    case (1)://開始啟動

                        break;
                    case (2)://進入復歸狀態
                        timer_Alarm_Thread.Start();
                        MV.全軸回原點(this);
                        ST.Curr_STATE.IDX = 0;//狀態設定為  復歸中
                        break;
                    case (3):
                        break;
                    case (4):
                        break;
                    case (5)://進入異常狀態
                        timer_步序.Enabled = false;//暫停步序掃描timer
                        timer_Alarm_Thread.Stop();
                        MV.停止所有軸();
                        ST.Curr_STATE.Context = ST.Req_STATE.Context;
                        break;
                    case (6):
                        break;
                    case (7):
                        MV.停止所有軸();
                        break;
                    case (8):
                        timer_Alarm_Thread.Start();
                        MV.全軸回原點(this);
                        ST.Curr_STATE.IDX = 0;//狀態設定為  復歸中
                        break;
                    default:
                        break;
                }

            }
        TICK_END:
            timer_ST_Thread.Start();
        }

        private void timer_IOM_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (F.InMatrix[0, 0].N == null)
                return;
            //讀取更新所有軸位置
            int Position = 0;
            Motion._8164_get_command(0, ref Position);
            label_0.Text = Position.ToString();
            Motion._8164_get_command(1, ref Position);
            label_1.Text = Position.ToString();
            Motion._8164_get_command(2, ref Position);
            label_2.Text = Position.ToString();
            Motion._8164_get_command(4, ref Position);
            label_X.Text = Position.ToString();
            Motion._8164_get_command(5, ref Position);
            label_Y.Text = Position.ToString();
            Motion._8164_get_command(6, ref Position);
            label_R.Text = Position.ToString();
            Motion._8164_get_command(7, ref Position);
            label_CCD.Text = Position.ToString();

            //讀取所有IO狀態
            //填入inMatrix
            timer_IOM_Thread.Stop();
            byte[] Buffer = new byte[6];
            string[] BArray = new string[6];
            for (int i = 0; i <= 5; i++)
            {
                DI_1758.ReadPort(i, ref Buffer[i]);
                BArray[i] = Convert.ToString(Convert.ToInt32(Buffer[i]), 2);
                BArray[i] = BArray[i].PadLeft(8, '0');
                char[] PortDATA = BArray[i].ToCharArray();
                for (int k = 0; k <= PortDATA.Length - 1; k++)
                {
                    F.InMatrix[i, k].V = Convert.ToInt32(PortDATA[7 - k].ToString());
                }

            }
            //填入outMatrix
            Buffer = new byte[3];
            BArray = new string[3];
            for (int i = 0; i <= 2; i++)
            {
                DO_1758.ReadPort(i, ref Buffer[i]);
                BArray[i] = Convert.ToString(Convert.ToInt32(Buffer[i]), 2);
                BArray[i] = BArray[i].PadLeft(8, '0');
                char[] PortDATA = BArray[i].ToCharArray();
                for (int k = 0; k <= PortDATA.Length - 1; k++)
                {
                    F.OutMatrix[i, k].V = Convert.ToInt32(PortDATA[7 - k].ToString());
                }

            }

            //安全門
            ST.安全門開啟 = false;
            if (SYS.安全門選用 == 1)
            {
                if ((F.InMatrix[5, 0].V == 1) ||
                        (F.InMatrix[5, 1].V == 1) ||
                          (F.InMatrix[5, 2].V == 1) ||
                            (F.InMatrix[5, 3].V == 1) ||
                              (F.InMatrix[5, 4].V == 1) ||
                                (F.InMatrix[5, 5].V == 1))
                {
                    if (ST.Curr_STATE.IDX == 1)//自動中
                    {
                        ST.Req_STATE.IDX = 5;//切到異常狀態
                        ST.Req_STATE.Context = "安全門開啟";
                    }
                    else
                        ST.Req_STATE.IDX = 0;//回到停止狀態
                    ST.安全門開啟 = true;
                }
            }
            //安全光幕
            ST.安全光幕觸發 = false;
            if (SYS.光幕選用 == 1)
            {
                if (F.InMatrix[5, 6].V == 0)
                {
                    if (ST.Curr_STATE.IDX == 1)//自動中
                    {
                        ST.Req_STATE.IDX = 5;//切到異常狀態
                        ST.Req_STATE.Context = "安全光幕觸發";
                    }
                    else
                        ST.Req_STATE.IDX = 0;//回到停止狀態
                    ST.安全光幕觸發 = true;
                }
            }

            //外部按鈕
            //START
            if (F.InMatrix[0, 0].V == 1)
                ST.Req_STATE.IDX = 1;
            //STOP
            if (F.InMatrix[0, 1].V == 1)
                ST.Req_STATE.IDX = 0;
            //RESET
            if (F.InMatrix[0, 2].V == 1)
                ST.Req_STATE.IDX = 2;
            //緊急
            if (F.InMatrix[0, 3].V == 0)
            {
                timer_步序.Enabled = false;//暫停步序掃描timer
                timer_Alarm_Thread.Stop();
                MV.停止所有軸();
                ST.Curr_STATE.IDX = 4;
            }
            else if (ST.Curr_STATE.IDX == 4)
            {
                timer_步序.Enabled = false;//暫停步序掃描timer
                timer_Alarm_Thread.Stop();
                MV.停止所有軸();
                ST.Curr_STATE.IDX = 7;//緊急開關解除,需要停止復歸
            }
            timer_IOM_Thread.Start();
        }
        private void timer_ALM_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_Alarm_Thread.Stop();
            //警報偵測timer
            int 異常碼 = 0;
            string 異常敘述 = "";
            bool 異常發生 = false;
            //1.在此timer中判斷警報總表中的各種點位狀態,以及軸位置,若發生不合理狀態就給出[異常狀態]
            //2.在主線timer中,每一步序都有合理等待間,超過此時間就會丟出[異常狀態]
            int State = ST.Curr_STATE.IDX;
            File_Class.Motion_Step_Class STN = new File_Class.Motion_Step_Class();
            File_Class.Motion_Step_Class.Motion_Node_Class Node = new File_Class.Motion_Step_Class.Motion_Node_Class();

            if (IDX_Step步序 > 0)
            {
                STN = StepArray[IDX_Step步序];
                Node.註解 = "";
                if (StepArray[IDX_Step步序].ConList.Count > 0)
                    Node = StepArray[IDX_Step步序].ConList[0];
            }

            switch (State)
            {
                case (0)://停止中

                    break;
                case (1)://自動中
                    if ((Node.註解.CompareTo("[MOV_ABS, 1, 天車拉出點)") == 0) ||
                        (Node.註解.CompareTo("[CMP,1,天車拉出點,=]") == 0))
                    {
                        if (F.InMatrix[0, 6].V == 1)//拉料破真空
                            MV.停止所有軸();
                        異常碼 = 100;
                        異常敘述 = "拉料破真空";
                        異常發生 = true;
                    }
                    if ((Node.註解.CompareTo("[MOV_ABS, 1, 天車拉出點)") == 0) ||
                            (Node.註解.CompareTo("[CMP,1,天車拉出點,=]") == 0))
                    {
                        if (F.InMatrix[0, 5].V == 1)//8吋搬運破真空
                            MV.停止所有軸();
                        異常碼 = 101;
                        異常敘述 = "8吋搬運破真空";
                        異常發生 = true;
                    }

                    if ((Node.註解.CompareTo("[MOV_ABS, 1, 天車拉出點)") == 0) ||
        (Node.註解.CompareTo("[CMP,1,天車拉出點,=]") == 0))
                    {
                        if (F.InMatrix[0, 5].V == 1)//8吋搬運破真空
                            MV.停止所有軸();
                        異常碼 = 101;
                        異常敘述 = "8吋搬運破真空";
                        異常發生 = true;
                    }
                    break;

                default:
                    break;
            }

            if (異常發生)//警報發生
            {
                //停止Step Timer
                timer_步序.Enabled = false;//停止步序timer
                timer_Alarm_Thread.Stop();
                MV.停止所有軸();
                ST.Req_STATE.IDX = 5;//送出異常狀態請求
                ST.Curr_STATE.Context = 異常碼 + ":" + 異常敘述;
                return;
            }
            timer_Alarm_Thread.Start();
        }

        private void timer_ALM_Tick_Bak(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ALMArray[0] == null)
                return;

            timer_Alarm_Thread.Stop();
            //警報偵測timer

            //1.在此timer中判斷警報總表中的各種點位狀態,以及軸位置,若發生不合理狀態就給出[異常狀態]
            //2.在主線timer中,每一步序都有合理等待間,超過此時間就會丟出[異常狀態]
            //3.控制
            int Value = 0;

            for (int i = 0; i <= ALMArray.Length - 1; i++)
            {
                File_Class.Alarm_Condition_Class ALM = ALMArray[i];
                //預設為警報發生, 檢查此條件下所有節點, 有一個不成立則改為false跳下一條件
                ALM.條件成立 = true;
                for (int k = 0; k < ALM.ConList.Count - 1; k++)
                {
                    File_Class.Alarm_Condition_Class.Alarm_Node_Class Node = ALM.ConList[k];
                    switch (Node.種類)//--------檢查目前步序條件---------------
                    {
                        case ("IN"):
                            // 取得PORT/BIT 下讀取1758指令後 比較
                            byte V = 0;
                            DI_1758.ReadBit(Node.點通道, Node.點位元, ref V);
                            if (V.CompareTo(Node.點目標狀態) == 0)
                                Node.節點成立 = true;
                            else
                                ALM.條件成立 = false;
                            break;
                        case ("CMP"):
                            //取得STN條件指定軸,下讀取8164指令後 取得軸位置
                            int AxisPos = 0;
                            Motion._8164_get_command((short)Node.軸編號, ref AxisPos);
                            switch (Node.比較元)
                            {
                                case ("="):
                                    if (Value == Node.軸目標位置)
                                        Node.節點成立 = true;//條件達成
                                    break;
                                case (">"):
                                    if (Value > Node.軸目標位置)
                                        Node.節點成立 = true;//條件達成
                                    break;
                                case ("<"):
                                    if (Value < Node.軸目標位置)
                                        Node.節點成立 = true;//條件達成
                                    break;
                                default:
                                    break;
                            }
                            if (!Node.節點成立)
                                ALM.條件成立 = false;
                            break;
                        default:
                            break;
                    }
                }
                if (ALM.條件成立)//警報發生
                {
                    //停止Step Timer
                    timer_步序.Enabled = false;//停止步序timer
                    timer_Alarm_Thread.Stop();
                    //timer_Alarm_Thread.Change(-1, 0);//停止警報timer
                    MV.停止所有軸();
                    ST.Req_STATE.IDX = 5;//送出異常狀態請求
                    ST.Curr_STATE.Context = ALM.異常碼 + ":" + ALM.異常敘述;
                    return;
                }

            }

            timer_Alarm_Thread.Start();


        }

        //更新機台顯示狀態
        private void timer_State_Tick(object sender, EventArgs e)
        {
            bool RESET_enable = true;
            bool START_enable = true;
            bool STOP_enable = true;

            label34.Text = IDX_Step步序.ToString();
            int 燈號 = -1;//紅1 黃2 綠3
            switch (ST.Curr_STATE.IDX)
            {
                case (0):
                    ST.Curr_STATE.Title = "停止";
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.Yellow;
                    燈號 = 2;//黃燈
                    button25.Enabled = true;
                    break;
                case (1):
                    if (checkBox停止供料.Checked)
                        ST.Curr_STATE.Title = "自動運轉中: 暫停供料";
                    else if (cb單步模式.Checked)
                        ST.Curr_STATE.Title = "單動步序中";
                    else
                        ST.Curr_STATE.Title = "自動運轉中";

                    if (IDX_End步序 != -1)
                        ST.Curr_STATE.Title = "自動運轉中:執行到 STEP " + IDX_End步序;
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.MediumSeaGreen;
                    button25.Enabled = false;
                    燈號 = 3;//綠燈
                    break;
                case (2):
                    //ST.Curr_STATE.Title = "機台復歸中";
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.Pink;
                    燈號 = 2;//黃燈
                    break;
                case (3):
                    ST.Curr_STATE.Title = "工程動作中";
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.MediumSeaGreen;
                    燈號 = 3;//綠燈
                    break;
                case (4):
                    ST.Curr_STATE.Title = "緊急停止";
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.Red;
                    燈號 = 1;//紅燈
                    break;
                case (5):
                    if (ST.Curr_STATE.Context.IndexOf("彈匣檢測完畢") >= 0)
                    {
                        ST.Curr_STATE.Title = "結批";
                        label9.Text = "結批:" + ST.Curr_STATE.Context;
                        panel2.BackColor = Color.Yellow;
                        燈號 = 2;//黃燈
                    }
                    else
                    {
                        ST.Curr_STATE.Title = "異常";
                        label9.Text = "異常:" + ST.Curr_STATE.Context;
                        panel2.BackColor = Color.Red;
                        燈號 = 1;//紅燈
                    }
                    RESET_enable = false;
                    START_enable = false;
                    break;
                case (6):
                    ST.Curr_STATE.Title = "停止供料";
                    燈號 = 3;//綠燈
                    break;
                case (7):
                    ST.Curr_STATE.Title = "必須停止";
                    RESET_enable = false;
                    START_enable = false;
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.Pink;
                    燈號 = 1;//紅燈
                    break;
                case (8):
                    ST.Curr_STATE.Title = "必須復歸";
                    START_enable = false;
                    STOP_enable = false;
                    label9.Text = ST.Curr_STATE.Title;
                    panel2.BackColor = Color.Pink;
                    燈號 = 1;//紅燈
                    break;
                default:
                    break;
            }

            //切換燈號
            if (ST.Curr_STATE.Context == null)
                return;
            if ((ST.Curr_STATE.Context.IndexOf("彈匣檢測完畢") >= 0) &&
                    (ST.Curr_STATE.IDX == 5))
            {
                輸出1758(2, 1, 0);
                //輸出1758(2, 2, 1);
                輸出1758(2, 3, 0);
                if (timer_BZ.Enabled == false)
                    timer_BZ.Enabled = true;

            }
            else if (燈號 == 1)//紅燈
            {
                輸出1758(2, 1, 1);
                輸出1758(2, 2, 0);
                輸出1758(2, 3, 0);
                if (timer_BZ.Enabled == false)
                    timer_BZ.Enabled = true;
            }
            else if (燈號 == 2)
            {
                輸出1758(2, 1, 0);
                輸出1758(2, 2, 1);
                輸出1758(2, 3, 0);
                timer_BZ.Enabled = false;
                輸出1758(2, 4, 0);//BZ OFF
            }
            else
            {
                輸出1758(2, 1, 0);
                輸出1758(2, 2, 0);
                輸出1758(2, 3, 1);
                timer_BZ.Enabled = false;
                輸出1758(2, 4, 0);//BZ OFF
            }


            if (ST.安全光幕觸發 || ST.安全門開啟)
            {
                if (ST.安全光幕觸發)
                    label9.Text = "安全光幕觸發";
                else
                    label9.Text = "安全門開啟";
                panel2.BackColor = Color.Pink;
                RESET_enable = false;
                START_enable = false;
            }

            //判定是否輸入完整資料 可以開始啟動機台
            if ((textBox_批號.Text.Length == 0) ||
                (textBox_客戶.Text.Length == 0) ||
                (textBox_程式.Text.Length == 0))
                START_enable = false;


            //判定8吋/12吋/空彈匣

            if ((F.InMatrix[1, 5].V == 1) &&
              label彈匣尺寸.Text.CompareTo("12吋 彈匣放置中") != 0)//12吋彈匣放置
            {
                ST.晶圓尺寸 = 12;
                panel4.BackColor = Color.LimeGreen;
                panel5.BackColor = Color.LimeGreen;
                panel6.BackColor = Color.LimeGreen;
                label彈匣尺寸.Text = "12吋 彈匣放置中";
                label42.Text = "12吋 資料使用中";
                label33.Text = "12吋步序";
                F.Load_STEP(ref StepArray, 路徑_動作流程12);
                Load_Sys(12);
                更新步序參數值();
            }
            else if ((F.InMatrix[1, 6].V == 1) && (F.InMatrix[1, 7].V == 1) &&
                label彈匣尺寸.Text.CompareTo("8吋 彈匣放置中") != 0)//8吋彈匣放置
            {
                ST.晶圓尺寸 = 8;
                panel4.BackColor = Color.LimeGreen;
                panel5.BackColor = Color.LimeGreen;
                panel6.BackColor = Color.LimeGreen;
                label彈匣尺寸.Text = "8吋 彈匣放置中";
                label42.Text = "8吋 資料使用中";
                label33.Text = "8吋步序";
                F.Load_STEP(ref StepArray, 路徑_動作流程8);
                Load_Sys(8);
                更新步序參數值();
            }
            else if (((F.InMatrix[1, 4].V == 0) &&
                (F.InMatrix[1, 5].V == 0) &&
                (F.InMatrix[1, 6].V == 0)) &&
                label彈匣尺寸.Text.CompareTo("無彈匣") != 0)
            {
                panel4.BackColor = Color.LightGray;
                label彈匣尺寸.Text = "無彈匣";
            }

            if (label彈匣尺寸.Text.CompareTo("無彈匣") == 0)
                ST.空彈匣計時++;
            if ((ST.空彈匣計時 > 30) && (ST.Curr_STATE.IDX == 0) && (F.InMatrix[2, 7].V == 0))
            {
                //輸出1758(1, 2, 1);
                ST.空彈匣計時 = 0;
            }
            // panel2.BackColor = ST.StateTable[ST.CurrentSTATE.State_IDX].State_color;
            //label9.Text = ST.StateTable[ST.CurrentSTATE.State_IDX].State_string;
            Application.DoEvents();

            btnSTART.Enabled = START_enable;
            // button25.Enabled = START_enable;
            btnSTOP.Enabled = STOP_enable;
            btnRESET.Enabled = RESET_enable;
            if (!START_enable)
            {
                btn晶圓取像.Enabled = false;
                btn單次檢測.Enabled = false;
            }
            else
            {
                btn晶圓取像.Enabled = true;
                btn單次檢測.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            單動操作 單動視窗 = new 單動操作(this);
            單動視窗.Show();
        }
        private void button23_Click(object sender, EventArgs e)
        {

            if (label彈匣尺寸.Text.CompareTo("12吋 彈匣放置中") == 0)
                F.Load_STEP(ref StepArray, 路徑_動作流程12);
            else
                F.Load_STEP(ref StepArray, 路徑_動作流程8);
            更新步序參數值();
            listBox1.Refresh();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            if (IDX_Step步序 == -1) IDX_Step步序 = 0;
            File_Class.Motion_Step_Class STN = StepArray[IDX_Step步序];
            File_Class.Motion_Step_Class STN_NEXT = StepArray[IDX_Step步序 + 1];//有些狀態會參考到下一步資料
            if (cb單步模式.Checked)
            {
                switch (STN_NEXT.條件註解)
                {
                    case ("//進彈匣"):
                        IDX_Step步序++;
                        break;
                    case ("//生產週期"):
                        IDX_Step步序++;
                        break;
                    case ("//退彈匣"):
                        bool finish = true;
                        for (int i = 0; i <= CASArray.Length - 1; i++)
                        {
                            if ((CASArray[i].有無材料 == 1) && (CASArray[i].檢測結果 == 0))
                                finish = false;
                        }
                        if (!finish)
                        {
                            int index = 0;
                            while (StepArray[index].條件註解.CompareTo("//生產週期開始") != 0)
                                index++;
                            IDX_Step步序 = index;//回到自動檢測週期第一步
                        }
                        else
                            IDX_Step步序++;
                        break;
                    default:
                        IDX_Step步序++;
                        break;
                }
                STN.已延遲時間 = 0;
                STN.已等待時間 = 0;
            }
            listBox1.Refresh();
        }

        //global brushes with ordinary/selected colors
        private SolidBrush reportsForegroundBrushSelected = new SolidBrush(Color.Black);
        private SolidBrush reportsForegroundBrush = new SolidBrush(Color.Black);
        private SolidBrush 被選中顏色 = new SolidBrush(Color.FromKnownColor(KnownColor.Highlight));
        private SolidBrush 背景白色 = new SolidBrush(Color.White);
        private SolidBrush 當下步序顏色 = new SolidBrush(Color.Yellow);
        private SolidBrush 步序截止點顏色 = new SolidBrush(Color.DarkOrange);
        private SolidBrush Brush輸出 = new SolidBrush(Color.LightCyan);
        private SolidBrush Brush運動 = new SolidBrush(Color.MistyRose);
        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {

            e.DrawBackground();
            bool selected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);

            int index = e.Index;
            if (index >= 0 && index < listBox1.Items.Count)
            {
                string text = listBox1.Items[index].ToString();
                Graphics g = e.Graphics;
                SolidBrush 背景顏色;



                //background:
                if (index == IDX_Step步序)
                    背景顏色 = 當下步序顏色;
                else if (selected)
                    背景顏色 = 被選中顏色;
                else if (index == IDX_End步序)
                    背景顏色 = 步序截止點顏色;
                else if (text.IndexOf("MOV") >= 0)
                    背景顏色 = Brush運動;
                else if (text.IndexOf("OUT") >= 0)
                    背景顏色 = Brush輸出;
                else if ((text.IndexOf("(") == -1) && (text.IndexOf("//") == -1) && (text.Length > 5))
                    背景顏色 = new SolidBrush(Color.LightGray);
                else
                    背景顏色 = 背景白色;

                g.FillRectangle(背景顏色, e.Bounds);

                //text:
                SolidBrush foregroundBrush = (selected) ? reportsForegroundBrushSelected : reportsForegroundBrush;
                g.DrawString(text, e.Font, foregroundBrush, listBox1.GetItemRectangle(index).Location);
            }

            e.DrawFocusRectangle();

        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            IDX_Step步序 = listBox1.SelectedIndex;

            listBox1.Refresh();
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void but彈匣上一格_Click(object sender, EventArgs e)
        {
            相對運動(2, SYS.彈匣間距, MV.彈匣初速, MV.彈匣常速, 0.2, 0.2);
        }

        private void but彈匣下一格_Click(object sender, EventArgs e)
        {
            相對運動(2, -1 * SYS.彈匣間距, MV.彈匣初速, MV.彈匣常速, 0.2, 0.2);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            絕對運動(2, SYS.彈匣軌道第一點, MV.彈匣初速, MV.彈匣常速, 0.2, 0.2);
        }

        public void MergeImg(ref AxAxOvkBase.AxAxImageBW8 img_M)
        {
            //寫入接圖補償
            象限補償矩陣[0, 0].Y = 0;
            象限補償矩陣[0, 0].X = 0;
            象限補償矩陣[1, 0].Y = 0;
            象限補償矩陣[3, 0].Y = 0;

            象限補償矩陣[0, 1].X = 0;
            象限補償矩陣[0, 1].Y = 0;
            象限補償矩陣[1, 1].X = 0;
            象限補償矩陣[1, 1].X = 0;
            象限補償矩陣[1, 1].Y = 0;
            象限補償矩陣[2, 1].X = 0;
            象限補償矩陣[3, 1].X = 0;
            象限補償矩陣[4, 1].X = 0;

            象限補償矩陣[0, 2].X = 0;
            象限補償矩陣[0, 2].Y = 0;
            象限補償矩陣[1, 2].X = 0;
            象限補償矩陣[1, 2].Y = 0;
            象限補償矩陣[2, 2].X = 0;
            象限補償矩陣[2, 3].X = 0;
            象限補償矩陣[3, 2].X = 0;
            象限補償矩陣[3, 2].Y = 0;
            象限補償矩陣[4, 2].X = 0;
            象限補償矩陣[4, 2].Y = 0;

            象限補償矩陣[0, 3].X = 0;
            象限補償矩陣[1, 3].X = 0;
            象限補償矩陣[1, 3].Y = 0;
            象限補償矩陣[2, 3].X = 0;
            象限補償矩陣[3, 3].X = 0;
            象限補償矩陣[4, 3].X = 0;

            象限補償矩陣[0, 4].X = 0;
            象限補償矩陣[1, 4].X = 0;
            象限補償矩陣[1, 4].Y = 0;
            象限補償矩陣[2, 4].X = 0;
            象限補償矩陣[3, 4].X = 0;
            象限補償矩陣[4, 4].X = 0;
            //img_Merge.LoadFile("D:\\bb.bmp");

            //    img_Merge.SetSurfaceObj(0);
            //  img_Merge.SetSize(100, 100);
            //逐一載入影像
            //img_Merge.SetSize(3664 * info.象限數X, 2748 * info.象限數Y);
            //img_Merge.SetSize(10000, 10000);
            //roi_Temp.ParentHandle = img_Merge.VegaHandle;

            for (int y = 0; y <= SYS.象限數Y - 1; y++)
            {
                for (int x = 0; x <= SYS.象限數X - 1; x++)
                {
                    img_temp.LoadFile("D:\\MERGE\\F" + x + "_" + y + ".jpg");
                    roi_Temp.SetPlacement((x * 3664), (y * 2748), 3664, 2748);
                    axAxImageCopier1.SrcImageHandle = img_temp.VegaHandle;
                    axAxImageCopier1.DstImageHandle = roi_Temp.VegaHandle;
                    axAxImageCopier1.Copy();
                }
            }
            img_temp.SetSize(1, 1);
            GC.Collect();
            //img_Merge.SaveFile("D:\\bb.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            ROI_Draw.ParentHandle = img_M.VegaHandle;
            int CutW = 3664 - SYS.CutXL - SYS.CutXR;
            int CutH = 2748 - SYS.CutYT - SYS.CutYB;
            //注意接圖結果跟這邊的方向是經過MAP角度旋轉的(180)!!!!!
            img_M.SetSize((CutW * SYS.象限數X) + SYS.CutXL,
                               (CutH * SYS.象限數Y) + SYS.CutYT);
            //img_M.LoadFile("D:\\bb.bmp");
            Rectangle[,] R_Array = new Rectangle[5, 5];
            //img_M.SetSize(CutW * info.象限數X + info.CutXL,
            //               CutH * info.象限數Y + info.CutYT);
            //貼合
            for (int y = 0; y <= SYS.象限數Y - 1; y++)
            {

                for (int x = 0; x <= SYS.象限數X - 1; x++)
                {
                    img_子區塊.LoadFile("D:\\MERGE\\F" + x + "_" + y + ".jpg");
                    //roi_Temp.ParentHandle = img_子區塊.VegaHandle;
                    //roi_Temp.SetPlacement(0,0, 3664, 2748);

                    // roi_Temp.SetPlacement(x * 3664, (y * 2748), 3664, 2748);//取得原始圖象限範圍
                    roiSRC.ParentHandle = img_子區塊.VegaHandle;
                    roiDST.ParentHandle = img_M.VegaHandle;
                    int XAD = 象限補償矩陣[x, y].X;
                    int YAD = 象限補償矩陣[x, y].Y;

                    int srcX = SYS.CutXL + XAD;
                    int srcY = SYS.CutYT + YAD;
                    int srcW = CutW;
                    int srcH = CutH;
                    int dstX = x * CutW + SYS.CutXL;
                    int dstY = y * CutH + SYS.CutYT;
                    if (y == 0)
                    {
                        srcY -= SYS.CutYT;
                        srcH += SYS.CutYT;
                        dstY -= SYS.CutYT;
                    }
                    if (x == 4)
                    {
                        srcW += SYS.CutXR;

                    }

                    dstX -= SYS.CutXL;
                    roiSRC.SetPlacement(srcX, srcY, srcW, srcH);
                    roiDST.SetPlacement(dstX, dstY, roiSRC.ROIWidth, roiSRC.ROIHeight);

                    R_Array[x, y].X = roiDST.OrgX;
                    R_Array[x, y].Y = roiDST.OrgY;
                    R_Array[x, y].Width = roiDST.ROIWidth;
                    R_Array[x, y].Height = roiDST.ROIHeight;

                    axAxImageCopier1.SrcImageHandle = roiSRC.VegaHandle;
                    axAxImageCopier1.DstImageHandle = roiDST.VegaHandle;
                    axAxImageCopier1.Copy();
                    img_子區塊.SetSize(1, 1);
                    // img_M.SaveFile("D:\\11.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);

                }
            }
            //貼合完畢
            ROI_Draw.ParentHandle = img_M.VegaHandle;
            //  img_M.SetSize(R_Array[4, 4].X + R_Array[4, 4].Width,
            //          R_Array[4, 4].Y + R_Array[4, 4].Height);
            //img_M.SaveFile("D:\\11.bmp", TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_BMP);
            //Font fff = new Font("微軟正黑體", 20, FontStyle.Bold);

            //for (int y = 0; y <= 0; y++)
            //{
            //    for (int x = 0; x <= SYS.象限數X - 1; x++)
            //    {
            //        ROI_Draw.SetPlacement(R_Array[x, y].X, R_Array[x, y].Y,
            //            R_Array[x, y].Width, R_Array[x, y].Height);
            //        ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xfa);
            //        g = Graphics.FromHdc((IntPtr)axAxCanvas1.hDC);
            //        g.DrawString(x + "-" + y, fff, Brushes.DarkOrange,
            //           (ROI_Draw.OrgX + 200) * zoom, (ROI_Draw.OrgY + 200) * zoom);
            //        g.Dispose();
            //    }
            //}
        }

        private void button5_Click(object sender, EventArgs e)
        {
            zoom = 0.07f;
            MergeImg(ref img_OrgGlobe);
            //進行影像旋轉處理  MAP檔案角度 + 晶圓偏移角度
            axAxImageRotator1.RotateCenterX = 0;
            axAxImageRotator1.RotateCenterY = 0;
            axAxImageRotator1.RotateDegree = 360 - PD.Degree;
            axAxImageRotator1.RotatorMethod = AxOvkImage.TxAxImageRotatorMethod.AX_ROTATE_ANY_ANGLE_WRT_CENTER_TO_PROPER_SIZE;
            axAxImageRotator1.SrcImageHandle = img_OrgGlobe.VegaHandle;
            axAxImageRotator1.DstImageHandle = img_Globe.VegaHandle;
            axAxImageRotator1.Rotate();
            axAxCanvas1.CanvasWidth = Convert.ToInt32(img_Globe.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(img_Globe.ImageHeight * zoom);
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);


            axAxCanvas1.RefreshCanvas();

        }

        private void button8_Click(object sender, EventArgs e)
        {
            H_Correction();
        }

        //彈匣狀態顯示共用事件
        private void F7_panel_MouseClick(object sender, MouseEventArgs e)
        {
            讀取彈匣狀態(ref CASArray);

            Panel pl;
            pl = (Panel)(sender);
            string[] A = pl.Name.Split('_');
            ST.選中格位IDX = Convert.ToInt32(A[0].Substring(1, A[0].Length - 1));

            Graphics g = CASArray[ST.選中格位IDX].panel.CreateGraphics();
            Pen pen = new Pen(Color.Blue, 3); //畫筆
            Rectangle RECT = new Rectangle(0, 0, CASArray[ST.選中格位IDX].panel.Width - 2,
                CASArray[ST.選中格位IDX].panel.Height - 2);
            g.DrawRectangle(pen, RECT);
            g.Dispose();

        }

        private void 設為未檢測ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CASArray[ST.選中格位IDX].檢測結果 = 0;
            寫入彈匣狀態();
            讀取彈匣狀態(ref CASArray);
        }

        private void 設為空格ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CASArray[ST.選中格位IDX].有無材料 = 0;
            寫入彈匣狀態();
            讀取彈匣狀態(ref CASArray);
        }

        private void 設為有料ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CASArray[ST.選中格位IDX].有無材料 = 1;
            寫入彈匣狀態();
            讀取彈匣狀態(ref CASArray);

        }

        private void contextMenu彈匣狀態_Opening(object sender, CancelEventArgs e)
        {
            讀取彈匣狀態(ref CASArray);

            string Name = (sender as ContextMenuStrip).SourceControl.Name;
            string[] A = Name.Split('_');
            if (A[0].IndexOf("tableLayoutPanel") > -1) return;
            ST.選中格位IDX = Convert.ToInt32(A[0].Substring(1, A[0].Length - 1));

            Graphics g = CASArray[ST.選中格位IDX].panel.CreateGraphics();
            Pen pen = new Pen(Color.Blue, 3); //畫筆
            Rectangle RECT = new Rectangle(0, 0, CASArray[ST.選中格位IDX].panel.Width - 2,
                CASArray[ST.選中格位IDX].panel.Height - 2);
            g.DrawRectangle(pen, RECT);
            g.Dispose();
        }

        private void 設為OKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CASArray[ST.選中格位IDX].檢測結果 = 1;
            寫入彈匣狀態();
            讀取彈匣狀態(ref CASArray);
        }

        private void 設為NGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CASArray[ST.選中格位IDX].檢測結果 = 2;
            寫入彈匣狀態();
            讀取彈匣狀態(ref CASArray);
        }


        private void 載入此片檢測紀錄ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ST.當前格位 = ST.選中格位IDX;
            string 格位資料夾 = 路徑_生產批資料 + "\\" + ST.選中格位IDX;
            img_Globe.LoadFile(格位資料夾 + "\\Globe.jpg");
            // Process.Start(格位資料夾);
            Refreshimage_靜態影像();
            //string[] 讀取參數 ={"檢測時間(Test time):" ,"Lot:",
            //                    "Customer:","Wafer ID:","Device:","檢測結果(Test result):",
            //                    "Die總數(Total die):","空料數(Empty):","有料數(Stay):","",""};
            //img_Globe.LoadFile(格位資料夾 + "\\Globe.jpg");
            //using (StreamReader sr = new StreamReader(格位資料夾 + "\\檢測紀錄.log", Encoding.Default))
            //{
            //    string TotalString = sr.ReadToEnd();
            //    int Cut1 = TotalString.IndexOf("檢測時間(Test time):");
            //    int Len = "檢測時間(Test time):".Length;
            //    int Cut2 = TotalString.IndexOf("\r\n", Cut1 );
            //    CK.檢測時間 = TotalString.Substring(Cut1 + Len, Cut2 - Cut1 - Len);
            //}
        }


        private void 存圖ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Title = "Save an Image File";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
                img即時影像.SaveFile(saveFileDialog1.FileName, TxAxImageType.AX_IMAGE_FILE_TYPE_GREYLEVEL_JPG);

        }

        private void button9_Click(object sender, EventArgs e)
        {

            int IDX = Convert.ToInt32(textBox7.Text);
            //走到料格高度
            Motion._8164_start_ta_move(2, CASArray[IDX].軌道格位高度,
                                    MV.彈匣初速, MV.彈匣常速, 0.1, 0.1);

            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();

            ST.當前格位 = IDX;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Motion._8164_start_ta_move(MV.軸4載台X, SYS.X接料點, MV.X初速, MV.X常速, 0.4, 0.4);
            Motion._8164_start_ta_move(MV.軸5載台Y, SYS.Y接料點, MV.Y初速, MV.Y常速, 0.4, 0.4);
            Motion._8164_start_ta_move(MV.軸6載台R, SYS.R接料點, MV.R初速, MV.R常速, 0.4, 0.4);

            while ((Motion._8164_motion_done(4) > 0) ||
                    (Motion._8164_motion_done(5) > 0) ||
                    (Motion._8164_motion_done(6) > 0))
                Application.DoEvents();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            數片模組(true);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            輸出錯誤訊息("12314");
            string aaa = 讀取錯誤訊息();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            //if (DEV.Name.CompareTo("Default") == 0) return;
            ProcessAll_byROW_NOMAP();
        }

        private void 吋載入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ST.晶圓尺寸 = 12;
            panel4.BackColor = Color.LimeGreen;
            panel5.BackColor = Color.LimeGreen;
            panel6.BackColor = Color.LimeGreen;
            label彈匣尺寸.Text = "12吋 彈匣放置中";
            label42.Text = "12吋 資料使用中";
            label33.Text = "12吋步序";
            F.Load_STEP(ref StepArray, 路徑_動作流程12);
            Load_Sys(12);
            更新步序參數值();
        }

        private void 吋載入ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ST.晶圓尺寸 = 8;
            panel4.BackColor = Color.LimeGreen;
            panel5.BackColor = Color.LimeGreen;
            panel6.BackColor = Color.LimeGreen;
            label彈匣尺寸.Text = "8吋 彈匣放置中";
            label42.Text = "8吋 資料使用中";
            label33.Text = "8吋步序";
            F.Load_STEP(ref StepArray, 路徑_動作流程8);
            Load_Sys(8);
            更新步序參數值();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(ST.MAPpath))
                return;
            Process.Start(ST.MAPpath);
        }

        private void button29_MouseDown(object sender, MouseEventArgs e)
        {
            //取得按鈕名稱
            Button btn = sender as Button;
            short X軸 = 4;
            short Y軸 = 5;
            short 轉軸 = 6;

            if (btn.Name.IndexOf("前") > -1)
                連續運動(Y軸, MV.axis[Y軸].初速, MV.axis[Y軸].常速, 0.4);
            else if (btn.Name.IndexOf("後") > -1)
                連續運動(Y軸, -1 * MV.axis[Y軸].初速, -1 * MV.axis[Y軸].常速, 0.4);

            if (btn.Name.IndexOf("左") > -1)
                連續運動(X軸, MV.axis[X軸].初速, MV.axis[X軸].常速, 0.4);
            else if (btn.Name.IndexOf("右") > -1)
                連續運動(X軸, -1 * MV.axis[X軸].初速, -1 * MV.axis[X軸].常速, 0.4);


            if (btn.Name.IndexOf("正轉") > -1)
                連續運動(轉軸, MV.axis[轉軸].初速, MV.axis[轉軸].常速, 0.4);
            else if (btn.Name.IndexOf("反轉") > -1)
                連續運動(轉軸, -1 * MV.axis[轉軸].初速, -1 * MV.axis[轉軸].常速, 0.4);

        }

        private void button29_MouseUp(object sender, MouseEventArgs e)
        {
            short X軸 = 4;
            short Y軸 = 5;
            short 轉軸 = 6;
            Motion._8164_sd_stop(X軸, 0.2);
            Motion._8164_sd_stop(Y軸, 0.2);
            Motion._8164_sd_stop(轉軸, 0.2);
        }

        private void btn原點_Click(object sender, EventArgs e)
        {
            short X軸 = 4;
            short Y軸 = 5;
            short 轉軸 = 6;
            MV.單軸回原點(this, X軸);
            MV.單軸回原點(this, Y軸);
            MV.單軸回原點(this, 轉軸);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            GC.Collect();
            label_WID.Text = "";
            ST.晶圓ID = "";
            button11.Text = "";
            ST.MAPName = "";
            label_WID.Refresh();
            button11.Refresh();
            process_Barcode();
        }

        private void 條碼位置1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ST.Mode_Live) return;

            //教導樣本
            ROI1.ParentHandle = img_Globe.VegaHandle;
            F.SetV(ref sysArray, "條碼位置1X", ROI1.OrgX);
            F.SetV(ref sysArray, "條碼位置1Y", ROI1.OrgY);
            F.SetV(ref sysArray, "條碼位置1W", ROI1.ROIWidth);
            F.SetV(ref sysArray, "條碼位置1H", ROI1.ROIHeight);
            Save_Sys(8);
            Refreshimage_靜態影像();
        }

        private void 條碼位置2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //教導樣本
            ROI1.ParentHandle = img_Globe.VegaHandle;
            F.SetV(ref sysArray, "條碼位置2X", ROI1.OrgX);
            F.SetV(ref sysArray, "條碼位置2Y", ROI1.OrgY);
            F.SetV(ref sysArray, "條碼位置2W", ROI1.ROIWidth);
            F.SetV(ref sysArray, "條碼位置2H", ROI1.ROIHeight);
            Save_Sys(ST.晶圓尺寸);
            Refreshimage_靜態影像();
        }

        private void 資料分析toolStripMenuItem_Click(object sender, EventArgs e)
        {
            資料分析 資料分析 = new 資料分析(this);
            資料分析.Show();
        }

        private void timer_BZ_Tick(object sender, EventArgs e)
        {
            if (ST.Curr_STATE.Context == null) return;
            if (ST.Curr_STATE.Context.CompareTo("生產完畢") == 0)
                timer_BZ.Interval = 1000;
            else
                timer_BZ.Interval = 500;

            byte A = 0;
            DO_1758.ReadBit(2, 4, ref A);
            if (A == 1)
            {
                if ((ST.Curr_STATE.Context.IndexOf("彈匣檢測完畢") >= 0) &&
                        (ST.Curr_STATE.IDX == 5))
                    輸出1758(2, 2, 0);//BZ
                輸出1758(2, 4, 0);//BZ
            }
            else
            {
                if ((ST.Curr_STATE.Context.IndexOf("彈匣檢測完畢") >= 0) &&
                    (ST.Curr_STATE.IDX == 5))
                    輸出1758(2, 2, 1);//BZ
                輸出1758(2, 4, 1);//BZ
            }
        }

        private void 強制執行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            File_Class.Motion_Step_Class STN = StepArray[listBox1.SelectedIndex];
            File_Class.Motion_Step_Class STN_NEXT = StepArray[listBox1.SelectedIndex + 1];//有些狀態會參考到下一步資料

            if (STN.等待外部條件)
            {
                STN.目標延遲時間 = 0;
                label20.Text = STN.已等待時間 + STN.條件註解 + "[" + IDX_Step步序 + "]";
                label24.Text = STN_NEXT.條件註解 + "[" + (IDX_Step步序 + 1) + "]";
                //搭配STEP ARRAY, 當到達某些動作時, 需要配合跑完特定副程式才能下一步, 都加在此處
                switch (STN.條件註解)
                {
                    case ("檢測模組"):
                        檢測模組();
                        break;
                    case ("數片模組"):
                        數片模組(true);
                        break;
                    case ("走到下一格有料格"):
                        走到下一有料格();
                        break;
                    case ("撥料模組"):
                        撥料模組();
                        寫入彈匣狀態();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                File_Class.Motion_Step_Class.Motion_Node_Class Node = STN.ConList[0];

                label20.Text = STN.已等待時間 + Node.註解 + "[" + IDX_Step步序 + "]";//顯示目前節點條件
                if (STN_NEXT.ConList.Count > 0)
                    label24.Text = STN_NEXT.ConList[0].註解 + "[" + (IDX_Step步序 + 1) + "]";//顯示下一步的節點0條件
                switch (Node.種類) //--------檢查目前步序條件---------------
                {
                    case ("OUT"):
                        //切割STN.點位址 取得PORT/BIT 下輸出1758  ON/OFF
                        輸出1758(Node.點通道, Node.點位元, (byte)Node.點目標狀態);
                        break;
                    case ("MOV_ABS"):
                        if (Motion._8164_motion_done((short)Node.軸編號) == 0)
                        {
                            絕對運動((short)Node.軸編號, Node.軸目標位置, MV.axis[Node.軸編號].初速,
                                                        MV.axis[Node.軸編號].常速, 0.1, 0.1);
                        }
                        break;
                    case ("STOP"):
                        //取得STN條件指定軸,下讀取8164指令驅動軸前進相對位置
                        Motion._8164_sd_stop((short)Node.軸編號, 0.1);
                        break;
                    default:
                        break;
                }

            }
        }

        private void contextMenuStrip步序表_Opening(object sender, CancelEventArgs e)
        {
            if (ST.Curr_STATE.IDX != 0)
                強制執行ToolStripMenuItem.Enabled = false;
            else
                強制執行ToolStripMenuItem.Enabled = true;

        }

        private void ROW中心線ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            img_Globe.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            ROI_Draw.ShowPlacement = false;
            ROI_Draw.ShowTitle = true;
            //顯示ROW切割線
            for (int y = 0; y <= CK.Matrix.GetLength(0) - 1; y++)
            {
                if (y % 1 == 0)
                {
                    ROI_Draw.Title = y.ToString();
                    ROI_Draw.SetPlacement(0, AVG_Y_ARRAY[y],
                        img_Globe.ImageWidth, 1);
                    ROI_Draw.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0x008cff);
                }
            }
            axAxCanvas1.RefreshCanvas();
        }

        private void 教導REF影像ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            axAxImageCopier1.SrcImageHandle = ROI2.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_REF.VegaHandle;
            axAxImageCopier1.Copy();
            mch_ref.SrcImageHandle = img_REF.VegaHandle;
            mch_ref.LearnPattern();
            mch_refR.SrcImageHandle = img_REF.VegaHandle;
            mch_refR.LearnPattern();
            Save_PD();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            zoom = (float)trackBar1.Value / 1000;
            label14.Text = zoom.ToString();
            RefreshImage_動態影像(ref img即時影像);
        }

        private void 局部演算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessAll_byROW_NOMAP();
        }

        //載台進片
        private void button6_Click(object sender, EventArgs e)
        {
            int AxisPos = 0;
            Motion._8164_get_command(0, ref AxisPos);

            if (AxisPos != SYS.軌道寬點)
            {
                MessageBox.Show("請先將軌道寬度設定為軌道寬點");
                return;
            }
            if (F.InMatrix[3, 1].V == 0)
            {
                MessageBox.Show("軌道上無產品");
                return;
            }

            for (int i = 0; i < StepArray.Length - 1; i++)
            {
                if (StepArray[i].完整字串.IndexOf("天車到軌道點吸料") > 0)
                {
                    IDX_Step步序 = i;
                }
                else if (StepArray[i].完整字串.IndexOf("//進片週期截止") > 0)
                {
                    IDX_End步序 = i;
                    break;
                }
            }
            ST.Req_STATE.IDX = 1;//請求開始啟動
            while (ST.Curr_STATE.IDX != 1)//等待其啟動
                Application.DoEvents();

            while (ST.Curr_STATE.IDX != 0)//等待其做完
                Application.DoEvents();

            ////天車讓開
            //絕對運動(1, SYS.天車拉出點, MV.天車初速, MV.天車常速, 0.2, 0.2);
            listBox1.Refresh();
        }

        //退片至軌道
        private void button7_Click(object sender, EventArgs e)
        {
            int AxisPos = 0;
            Motion._8164_get_command(0, ref AxisPos);
            if (AxisPos != SYS.軌道寬點)
            {
                MessageBox.Show("請先將軌道寬度設定為軌道寬點");
                return;
            }
            if (F.InMatrix[3, 1].V == 1)
            {
                MessageBox.Show("軌道上有產品");
                return;
            }
            for (int i = 0; i < StepArray.Length - 1; i++)
            {
                if (StepArray[i].完整字串.IndexOf("天車將材料取回軌道上") > 0)
                {
                    IDX_Step步序 = i;
                }
                else if (StepArray[i].完整字串.IndexOf("天車將材料撥回彈匣") > 0)
                {
                    IDX_End步序 = i - 1;
                    break;
                }
            }

            ST.Req_STATE.IDX = 1;//請求開始啟動
            while (ST.Curr_STATE.IDX != 1)//等待其啟動
                Application.DoEvents();

            while (ST.Curr_STATE.IDX != 0)//等待其做完
                Application.DoEvents();

            //System.Threading.Thread.Sleep(2000);
            //天車讓開
            //絕對運動(1, SYS.天車拉出點, MV.天車初速, MV.天車常速, 0.2, 0.2);
            listBox1.Refresh();
        }

        private void 執行到此步ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IDX_End步序 = listBox1.SelectedIndex;
            listBox1.Refresh();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            絕對運動(0, SYS.軌道寬點, MV.軌道初速, MV.軌道常速, 0.2, 0.2);
        }

        private void button20_Click_1(object sender, EventArgs e)
        {
            絕對運動(0, SYS.軌道窄點, MV.軌道初速, MV.軌道常速, 0.2, 0.2);
        }

        private void 開啟記錄資料夾ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string 格位資料夾 = 路徑_生產批資料 + "\\" + ST.選中格位IDX;
            Process.Start(格位資料夾);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            絕對運動(4, SYS.X蓋印點, MV.X初速, MV.X常速, 0.2, 0.2);
            絕對運動(5, SYS.Y蓋印點, MV.Y初速, MV.Y常速, 0.2, 0.2);
            絕對運動(6, SYS.R蓋印點, MV.R初速, MV.R常速, 0.2, 0.2);
        }

        private void lANGUAGEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object ff = this;
            switch_Language(ref ff);
            if (ST.英文模式)
                ST.英文模式 = false;
            else
                ST.英文模式 = true;
        }
        public void switch_Language(ref object FFFF)
        {

            //MF FF = (MF)FFFF;

            foreach (Control c in this.Controls)
            {
                if (c is ListBox)
                {
                    continue;
                }
                FindSubControl(c, ref ST);
            }
        }
        public static void FindSubControl(Control c, ref StateClass st)
        {


            if (c is ListBox)
            {
                return;
            }
            if (c is MenuStrip)
            {
                MenuStrip A = (MenuStrip)c;
                foreach (ToolStripItem item in A.Items)
                {
                    ToolStripItem IT = item;
                    if (st.英文模式)
                        Find_MY_China_Name(ref IT);
                    else
                        Find_MY_English_Name(ref IT);
                }
                return;
            }
            //else if (c is TabControl)
            //{
            //    TabControl A = (TabControl)c;
            //    foreach (TabPage item in A.TabPages)
            //    {
            //        TabPage IT = item;
            //        if (st.英文模式)
            //            Find_MY_China_Name(ref IT);
            //        else
            //            Find_MY_English_Name(ref IT);
            //    }
            //    return;
            //}
            if (st.英文模式)
                Find_MY_China_Name(ref c);
            else
                Find_MY_English_Name(ref c);
            //判斷是否有子控制項
            if (c.Controls.Count > 0)
            {
                foreach (Control Ctl1 in c.Controls)
                {
                    //繼續往下找(遞迴)
                    FindSubControl(Ctl1, ref st);
                }
            }

            //else
            //{

            //    if (st.英文模式)
            //    {
            //        Find_MY_China_Name(ref c);
            //    }
            //    else
            //    {
            //        Find_MY_English_Name(ref c);

            //    }

            //}
        }

        public static void Find_MY_English_Name(ref ToolStripItem c)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Ncht == null) break;
                if (TextArray[i].Ncht.CompareTo(c.Text) == 0)
                {
                    c.Text = TextArray[i].Neng;
                    //c.Font = new Font("微軟正黑體", Convert.ToInt32(TextArray[i].SizeCht - 2));
                    c.Font = new Font("微軟正黑體", c.Font.Size);
                }
            }
        }

        public static void Find_MY_English_Name(ref TabPage c)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Ncht == null) break;
                if (TextArray[i].Ncht.CompareTo(c.Text) == 0)
                {
                    c.Text = TextArray[i].Neng;
                    //c.Font = new Font("微軟正黑體", Convert.ToInt32(TextArray[i].SizeCht - 2));
                    c.Font = new Font("微軟正黑體", c.Font.Size - 2);
                }
            }
        }

        public static void Find_MY_English_Name(ref Control c)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Ncht == null) break;
                if (TextArray[i].Ncht.CompareTo(c.Text) == 0)
                {
                    c.Text = TextArray[i].Neng;
                    //c.Font = new Font("微軟正黑體", Convert.ToInt32(TextArray[i].SizeCht - 2));
                    c.Font = new Font("微軟正黑體", c.Font.Size - 2);
                }
            }
        }

        public static string Find_MY_China_Name(string EngName)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Neng == null) break;
                if ((TextArray[i].Neng.CompareTo(EngName) == 0))
                {
                    return TextArray[i].Ncht;
                }
            }

            return "";
        }


        public static void Find_MY_China_Name(ref Control c)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Neng == null) break;
                if ((TextArray[i].Neng.CompareTo(c.Text) == 0))
                {
                    c.Text = TextArray[i].Ncht;
                    //c.Font = new Font("微軟正黑體", Convert.ToInt32(TextArray[i].SizeCht));
                    c.Font = new Font("微軟正黑體", c.Font.Size + 2);
                }
            }
        }
        public static void Find_MY_China_Name(ref ToolStripItem c)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Neng == null) break;
                if ((TextArray[i].Neng.CompareTo(c.Text) == 0))
                {
                    c.Text = TextArray[i].Ncht;
                    c.Font = new Font("微軟正黑體", c.Font.Size);
                }
            }
        }
        public static void Find_MY_China_Name(ref TabPage c)
        {
            for (int i = 0; i <= TextArray.Length - 1; i++)
            {
                if (TextArray[i].Neng == null) break;
                if ((TextArray[i].Neng.CompareTo(c.Text) == 0))
                {
                    c.Text = TextArray[i].Ncht;
                    c.Font = new Font("微軟正黑體", c.Font.Size + 2);
                }
            }
        }
        private void button23_Click_1(object sender, EventArgs e)
        {

        }

        private void 重載語言檔ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string temppath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            F.Load(ref TextArray, temppath + "..\\..\\..\\DATA\\SYS\\Language.ini");
        }

        private void timer_做完_Tick(object sender, EventArgs e)
        {
            byte A = 0;
            DO_1758.ReadBit(2, 4, ref A);
            if (A == 1)
                輸出1758(2, 4, 0);//BZ
            else
                輸出1758(2, 4, 1);//BZ
        }

        private void button23_Click_2(object sender, EventArgs e)
        {
            //Motion._8164_set_home_config(1, 4, 0, 0, 0, 0);
            //原點運動(1, -200, -500, 1);
            MV.單軸回原點(this, 1);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            WARNING WR = new WARNING(this);
            WR.Top = 100;
            WR.Left = 100;
            WR.ShowDialog();

            if (ST.續做模式 == 1)//載台上有料
            {
                CASArray[ST.當前格位].有無材料 = 1;
                CASArray[ST.當前格位].檢測結果 = 0;
                CASArray[ST.當前格位].panel.BackColor = Color.LightSkyBlue;//有料
                CASArray[ST.當前格位].LB.Text = ST.當前格位.ToString();
                for (int i = 0; i < StepArray.Length; i++)
                {
                    if (StepArray[i].完整字串.IndexOf("重啟後檢測開始位置") >= 0)
                    {
                        IDX_Step步序 = i;
                        break;
                    }
                }
                ST.Req_STATE.IDX = 1;//請求啟動

            }
            else if (ST.續做模式 == 2)
            {
                bool finish = true;
                //檢查做完沒
                for (int i = 0; i < CASArray.Length; i++)
                {
                    if ((CASArray[i].有無材料 == 1) &&
                        (CASArray[i].檢測結果 == 0))
                    {
                        finish = false;
                        break;
                    }

                }

                if (!finish)
                {
                    for (int i = 0; i < StepArray.Length; i++)
                    {
                        if (StepArray[i].完整字串.IndexOf("生產週期開始") >= 0)
                        {
                            IDX_Step步序 = i;
                            break;
                        }
                    }
                }
                else
                {
                    Motion._8164_start_ta_move(1, SYS.天車軌道點, MV.天車初速, MV.天車常速, 0.5, 0.5);

                    while ((Motion._8164_motion_done(1) > 0))
                    {
                        Application.DoEvents();
                    }
                    for (int i = 0; i < StepArray.Length; i++)
                    {
                        if (StepArray[i].完整字串.IndexOf("退彈匣") >= 0)
                        {
                            IDX_Step步序 = i;
                            break;
                        }
                    }
                }
                ST.Req_STATE.IDX = 1;//請求啟動
            }

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void button26_Click(object sender, EventArgs e)
        {
            double acc = 0.4;
            絕對運動(1, SYS.天車軌道點, MV.天車初速, MV.天車常速, acc, acc);
            while (Motion._8164_motion_done(1) > 0)
            {
                Application.DoEvents();
            }

            for (int i = 0; i < 30; i++)
            {
                絕對運動(1, SYS.天車拉料點, MV.天車初速, MV.天車常速, acc, acc);
                while (Motion._8164_motion_done(1) > 0)
                    Application.DoEvents();
                System.Threading.Thread.Sleep(500);
                絕對運動(1, SYS.天車軌道點, MV.天車初速, MV.天車常速, acc, acc);
                while (Motion._8164_motion_done(1) > 0)
                    Application.DoEvents();
                System.Threading.Thread.Sleep(500);
            }

        }

        private void 跑機模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 分析區域正規化ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //亮度正規化
            axAxImageLuminanceNormalizer1.SrcImageHandle = img_Globe.VegaHandle;
            axAxImageLuminanceNormalizer1.DstImageHandle = img_Normalizer.VegaHandle;
            axAxImageLuminanceNormalizer1.RefGreyMean = 128;
            axAxImageLuminanceNormalizer1.RefGreyStdDev = 200;
            axAxImageLuminanceNormalizer1.Normalize();

            ROI1.ParentHandle = img_Normalizer.VegaHandle;
            axAxImageCopier1.SrcImageHandle = ROI1.VegaHandle;
            axAxImageCopier1.DstImageHandle = img_Area.VegaHandle;
            axAxImageCopier1.Copy();
            ROI1.ParentHandle = img_Globe.VegaHandle;

            ROI2.ParentHandle = img_Area.VegaHandle;
            ROI2.SetPlacement(10, 10, 100, 100);

            Refreshimage_Area();
        }
    }
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
    public class MapClass //紀錄map資料的類別
    {
        public struct DieInfoStruct
        {
            //public char MapChar;
            public string MapString;
        }
        public int CenterLineIndex;// 紀錄最寬行的index
        public int CutNum;//紀錄map檔左面有多少多餘部份要切掉
        public int LineWidth;// Server MAP 文字檔line長度
        public int LineHeight;// Server MAP 文字檔有幾行

        //public int CountWidth;// Server MAP 文字檔中有資料的長度
        //public int CountHeight;// Server MAP 文字檔中有資料的高度

        public DieInfoStruct[,] Matrix;// Server MAP 的內容矩陣
        public int DieWidth;//晶圓寬度(畫圖用)
        public int DieHeight;//晶圓高度(畫圖用)  
        public AxAxOvkBase.AxAxImageBW8[,] ImgArray;
        public int FstX;//第一顆在Matrix當中的座標位置
        public int FstY;
        //public int indexTop;// 矩陣中第一行有料座標
        //public int indexDown;// 矩陣中最後一行有料座標
        //public string Device;//從map檔案當中讀出的Deviece名稱
        //public string ID;//從map檔案當中讀出的wafer ID
        //public string MAPpath;//map 路徑
    }


    public class MotionClass //伺服參數
    {

        public struct MV_struct
        {
            public string 軸名稱;
            public int 常速;
            public int 初速;

        }

        public MV_struct[] axis = new MV_struct[8];
        //Servo
        public Int16 Ret = 0;
        public Int16 AxisNo = 0;
        public Int16 existCards = 0;
        public Int16 pls_outmode = 4;  //Set CW/CCW mode
        public Int16 pls_iptmode = 2;  //Set 4X A/B mode
        public Int16 pls_logic = 1;    //inverse direction
        public Int16 Src = 1;          //Command pulse
        public Int16 alm_logic = 0;    //0:active LOW ; 1:active HIGH
        public Int16 alm_mode = 0;     //0:motor immediately stops ; 1:motor decelerates then stops
        public Int16 on_off = 0;       //0:servo on ; 1:servo off


        public short 軸0軌道 = 0;
        public short 軸1天車 = 1;
        public short 軸2彈匣 = 2;
        public short 軸4載台X = 4;
        public short 軸5載台Y = 5;
        public short 軸6載台R = 6;
        public short 軸7CCDZ = 7;

        public int 軌道常速 = 5000;
        public int 天車常速 = 3000;
        public int 彈匣常速 = 7000;
        public int X常速 = 5000;
        public int Y常速 = 5000;
        public int R常速 = 1000;
        public int Z常速 = 3000;

        public int 軌道初速 = 400;
        public int 天車初速 = 400;
        public int 彈匣初速 = 500;
        public int X初速 = 400;
        public int Y初速 = 400;
        public int R初速 = 250;
        public int Z初速 = 400;
        //
        public void 停止所有軸()
        {
            for (short i = 0; i <= 7; i++)
            {
                Motion._8164_sd_stop((short)i, 0.1);
            }

            //尋找停止失敗的軸連續下指令直到他停止
            for (short i = 0; i <= 7; i++)
            {
                while (Motion._8164_motion_done(i) > 0)
                    Motion._8164_sd_stop((short)i, 0.1);
            }
        }
        public void 重置所有軸()
        {

            for (short AxisIDX = 0; AxisIDX <= 7; AxisIDX++)
            {
                Motion._8164_sd_stop(AxisIDX, 0.2);
            }

            while ((Motion._8164_motion_done(0) > 0) ||
            (Motion._8164_motion_done(1) > 0) ||
            (Motion._8164_motion_done(2) > 0) ||
            (Motion._8164_motion_done(4) > 0) ||
            (Motion._8164_motion_done(5) > 0) ||
            (Motion._8164_motion_done(6) > 0) ||
             (Motion._8164_motion_done(7) > 0))
            {
                Application.DoEvents();
            }

            while ((Motion._8164_motion_done(0) > 0) ||
            (Motion._8164_motion_done(1) > 0) ||
            (Motion._8164_motion_done(2) > 0) ||
            (Motion._8164_motion_done(4) > 0) ||
            (Motion._8164_motion_done(5) > 0) ||
            (Motion._8164_motion_done(6) > 0) ||
             (Motion._8164_motion_done(7) > 0))
            {
                Application.DoEvents();
            }

            Motion._8164_set_home_config(0, 4, 0, 0, 0, 0);
            Motion._8164_set_home_config(1, 4, 0, 0, 0, 0);
            Motion._8164_set_home_config(2, 4, 0, 0, 0, 0);
            Motion._8164_set_home_config(4, 4, 0, 0, 0, 0);
            Motion._8164_set_home_config(5, 4, 0, 0, 0, 0);
            Motion._8164_set_home_config(6, 7, 0, 0, 3, 0);
            Motion._8164_set_home_config(7, 4, 0, 0, 0, 0);

        }

        public void 等待檢測平台動作完成()
        {

            while ((Motion._8164_motion_done(軸4載台X) > 0) ||
            (Motion._8164_motion_done(軸5載台Y) > 0) ||
            (Motion._8164_motion_done(軸6載台R) > 0) ||
            (Motion._8164_motion_done(軸7CCDZ) > 0))
            {
                Application.DoEvents();
            }
        }

        public void 單軸回原點(MF ff, short IDX)
        {
            switch (IDX)
            {
                case (0)://軌道
                    ff.相對運動(IDX, -1000, 300, 2000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(0, 4, 0, 0, 0, 0);
                    ff.原點運動(IDX, 800, 5000, 0.4);
                    break;
                case (1)://天車
                    ff.相對運動(IDX, 500, 200, 1000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(1, 4, 0, 0, 0, 0);
                    ff.原點運動(IDX, -100, -800, 0);
                    break;
                case (2)://彈匣
                    ff.相對運動(IDX, 1000, 300, 12000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(2, 4, 0, 0, 0, 0);
                    ff.原點運動(IDX, -2000, -12000, 0.4);
                    break;
                case (3)://
                    break;
                case (4)://X軸
                    ff.相對運動(IDX, 5000, 300, 4000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(4, 4, 0, 0, 0, 0);
                    ff.原點運動(IDX, -300, -2000, 0.4);
                    break;
                case (5)://Y軸
                    ff.相對運動(IDX, 3000, 300, 5000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(5, 4, 0, 0, 0, 0);
                    ff.原點運動(IDX, -500, -5000, 0.4);
                    break;
                case (6)://R
                    ff.相對運動(IDX, -500, 300, 1000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(6, 6, 0, 0, 3, 0);
                    ff.原點運動(6, 300, 1000, 0.4);
                    break;
                case (7)://CCD升降
                    ff.相對運動(IDX, 500, 300, 1000, 0.4, 0.4);
                    while ((Motion._8164_motion_done(IDX) > 0)) Application.DoEvents();
                    Motion._8164_set_home_config(7, 4, 0, 0, 0, 0);
                    ff.原點運動(IDX, -300, -3000, 0.4);
                    break;

                default:
                    break;
            }

        }


        public void 全軸回原點(MF ff)
        {

            byte a = 0;
            ff.DI_1758.ReadBit(3, 1, ref a);
            if (a == 1)//
            {
                ff.ST.Req_STATE.IDX = 5;
                ff.ST.Req_STATE.Context = "請先移除軌道上材料";
                return;
            }

            ff.DI_1758.ReadBit(0, 5, ref a);
            if (a == 0)//
            {
                ff.ST.Req_STATE.IDX = 5;
                ff.ST.Req_STATE.Context = "請先手動解除真空取走材料";
                return;
            }

            //撥料汽缸上
            //小汽缸上
            //大氣剛上




            //開啟真空
            ff.輸出1758(0, 0, 1);
            //System.Threading.Thread.Sleep(2000);

            ff.ST.Curr_STATE.Title = "機台復歸中";
            for (short i = 0; i <= 7; i++)
            {
                Motion._8164_sd_stop(i, 0.2);
            }

            for (short i = 0; i <= 7; i++)
            {
                int A = 0;
                while ((Motion._8164_motion_done(i) > 0))
                {
                    Application.DoEvents();
                    A++;
                    //if (A > 1000)
                    //{
                    //    Motion._8164_sd_stop(1, 0.2);
                    //    A = 0;
                    //}
                    //if (ff.ST.Curr_STATE.IDX != 2)
                    //    return;
                }
            }


            for (short i = 0; i <= 7; i++)
            {
                單軸回原點(ff, i);
            }
            for (short i = 0; i <= 7; i++)
            {
                while ((Motion._8164_motion_done(i) > 0))
                {
                    ff.ST.Curr_STATE.Title = "等待軸" + i + "復歸完成";
                    Application.DoEvents();
                    if (ff.ST.Curr_STATE.IDX != 2)
                        return;
                }
            }

            //走道載台階料點

            ff.絕對運動(ff.MV.軸4載台X, ff.SYS.X接料點, ff.MV.X初速, ff.MV.X常速, 0.4, 0.4);
            ff.絕對運動(ff.MV.軸5載台Y, ff.SYS.Y接料點, ff.MV.Y初速, ff.MV.Y常速, 0.4, 0.4);
            ff.絕對運動(ff.MV.軸6載台R, ff.SYS.R接料點, ff.MV.R初速, ff.MV.R常速, 0.4, 0.4);
            ff.絕對運動(ff.MV.軸7CCDZ, ff.SYS.CCD檢測高度, ff.MV.Z初速, ff.MV.Z常速, 0.4, 0.4);
            ff.絕對運動(ff.MV.軸0軌道, ff.SYS.軌道寬點, ff.MV.軌道初速, ff.MV.軌道常速, 0.4, 0.4);
            //ff.數片模組(false);
            while ((Motion._8164_motion_done(4) > 0) ||
                  (Motion._8164_motion_done(0) > 0) ||
                    (Motion._8164_motion_done(5) > 0) ||
                    (Motion._8164_motion_done(6) > 0) ||
                     (Motion._8164_motion_done(7) > 0))
                Application.DoEvents();

            //關閉轉盤真空
            ff.輸出1758(0, 0, 0);
        }

        public void setSpeed(MF ff)
        {

            軌道常速 = ff.F.Vi(ff.sysArray, "軌道常速");
            軌道初速 = ff.F.Vi(ff.sysArray, "軌道初速");
            天車常速 = ff.F.Vi(ff.sysArray, "天車常速");
            天車初速 = ff.F.Vi(ff.sysArray, "天車初速");
            彈匣常速 = ff.F.Vi(ff.sysArray, "彈匣常速");
            彈匣初速 = ff.F.Vi(ff.sysArray, "彈匣初速");
            X常速 = ff.F.Vi(ff.sysArray, "X常速");
            X初速 = ff.F.Vi(ff.sysArray, "X初速");
            Y常速 = ff.F.Vi(ff.sysArray, "Y常速");
            Y初速 = ff.F.Vi(ff.sysArray, "Y初速");
            R常速 = ff.F.Vi(ff.sysArray, "R常速");
            R初速 = ff.F.Vi(ff.sysArray, "R初速");
            Z常速 = ff.F.Vi(ff.sysArray, "Z常速");
            Z初速 = ff.F.Vi(ff.sysArray, "Z初速");

            axis[0].常速 = 軌道常速;
            axis[0].初速 = 軌道初速;
            axis[1].常速 = 天車常速;
            axis[1].初速 = 天車初速;
            axis[2].常速 = 彈匣常速;
            axis[2].初速 = 彈匣初速;
            axis[4].常速 = X常速;
            axis[4].初速 = X初速;
            axis[5].常速 = Y常速;
            axis[5].初速 = Y初速;
            axis[6].常速 = R常速;
            axis[6].初速 = R初速;
            axis[7].常速 = Z常速;
            axis[7].初速 = Z初速;
        }
        public void ini(MF ff)
        {
            try
            {
                Motion._8164_close();
            }
            catch (Exception)
            {
                throw;
            }
            int a = Motion._8164_initial(ref existCards);

            for (short i = 0; i <= 7; i++)
            {
                Motion._8164_set_pls_outmode(i, 4);//CW/CCW falling
                Motion._8164_set_pls_iptmode(i, 3, 0);//CW/CCW  not inverse
                Motion._8164_set_feedback_src(i, 1);
                Motion._8164_set_servo(i, 0);
                Motion._8164_set_servo(i, 1);

            }
            Motion._8164_set_home_config(0, 4, 1, 1, 3, 0);
            Motion._8164_set_home_config(1, 4, 1, 1, 3, 0);
            Motion._8164_set_home_config(2, 4, 1, 1, 0, 0);
            Motion._8164_set_home_config(4, 4, 1, 1, 2, 0);
            Motion._8164_set_home_config(5, 4, 1, 1, 2, 0);
            Motion._8164_set_home_config(6, 4, 1, 1, 2, 0);
            Motion._8164_set_home_config(7, 4, 1, 1, 2, 0);

            軌道常速 = ff.F.Vi(ff.sysArray, "軌道常速");
            軌道初速 = ff.F.Vi(ff.sysArray, "軌道初速");
            天車常速 = ff.F.Vi(ff.sysArray, "天車常速");
            天車初速 = ff.F.Vi(ff.sysArray, "天車初速");
            彈匣常速 = ff.F.Vi(ff.sysArray, "彈匣常速");
            彈匣初速 = ff.F.Vi(ff.sysArray, "彈匣初速");
            X常速 = ff.F.Vi(ff.sysArray, "X常速");
            X初速 = ff.F.Vi(ff.sysArray, "X初速");
            Y常速 = ff.F.Vi(ff.sysArray, "Y常速");
            Y初速 = ff.F.Vi(ff.sysArray, "Y初速");
            R常速 = ff.F.Vi(ff.sysArray, "R常速");
            R初速 = ff.F.Vi(ff.sysArray, "R初速");
            Z常速 = ff.F.Vi(ff.sysArray, "Z常速");
            Z初速 = ff.F.Vi(ff.sysArray, "Z初速");

            axis[0].常速 = 軌道常速;
            axis[0].初速 = 軌道初速;
            axis[1].常速 = 天車常速;
            axis[1].初速 = 天車初速;
            axis[2].常速 = 彈匣常速;
            axis[2].初速 = 彈匣初速;
            axis[4].常速 = X常速;
            axis[4].初速 = X初速;
            axis[5].常速 = Y常速;
            axis[5].初速 = Y初速;
            axis[6].常速 = R常速;
            axis[6].初速 = R初速;
            axis[7].常速 = Z常速;
            axis[7].初速 = Z初速;

            //ff.讀取所有伺服位置檔案();

        }
    }

    public class devClass
    {
        public Point RTAnchorA;//旋轉過後的定位點A
        public Point RTAnchorB;//旋轉過後的定位點b
        public string Name;
        public int StartMode;
        public int WaferSize;
        public int 參考DIE搜尋範圍;
        public int DIEArea;
        public int reCoAreaX;
        public int reCoAreaY;
        public int 中心誤差X;
        public int 中心誤差Y;
        public int ROW搜尋高度;
        public float AnchorTH;
        public float EPT_TH;
        public float DIE_TH;
        public Point AnchorA;
        public Point AnchorB;
        public int DieW;
        public int DieH;
        public int CKLight;//檢測亮度
        public int PRLight;//定位亮度
        public int Degree;//影像旋轉角度

        public int 判定區尺寸W;
        public int 判定區尺寸H;
        public int 左參考DIE影像X;
        public int 左參考DIE影像Y;
        public int 左參考dieIDX_X;
        public int 左參考dieIDX_Y;
        public int 右參考DIE影像X;
        public int 右參考DIE影像Y;
        public string 參考DIE字元;

        public int PDrefine;// 產品取樣面積調整度
        public int EPTrefine;// 空格取樣面積調整度
        public int PDexpend;// 產品取樣擴散
        public int GrayTotalTH;// 產品灰階合門檻

        public int DieJudgeMode;//產品判定依據
        public int VarianceTH;//變異系數門檻
        public int JudgeReduce;//判定取樣內縮
        public int Canny_count;//邊緣點有料門檻

        public int SHTth;//重定位偏移門檻

        public List<string> FailList;//壞品 留料字元
        public List<string> NullList;//無效字元
        public List<string> BinList;//好品 取料字元

        public void ReadInfo(MF ff)
        {
            Name = ff.F.Vs(ff.PDArray, "產品代號");
            StartMode = ff.F.Vi(ff.PDArray, "啟動模式");
            WaferSize = ff.F.Vi(ff.PDArray, "產品尺寸");
            參考DIE搜尋範圍 = ff.F.Vi(ff.PDArray, "參考DIE搜尋範圍");
            reCoAreaX = ff.F.Vi(ff.PDArray, "重定位範圍X");
            reCoAreaY = ff.F.Vi(ff.PDArray, "重定位範圍Y");
            ROW搜尋高度 = ff.F.Vi(ff.PDArray, "ROW搜尋高度");
            中心誤差X = ff.F.Vi(ff.PDArray, "中心誤差X");
            中心誤差Y = ff.F.Vi(ff.PDArray, "中心誤差Y");
            DIEArea = ff.F.Vi(ff.PDArray, "第一顆範圍");
            AnchorTH = ff.F.Vf(ff.PDArray, "定位門檻");
            AnchorA = new Point(ff.F.Vi(ff.PDArray, "定位點AX"), ff.F.Vi(ff.PDArray, "定位點AY"));
            AnchorB = new Point(ff.F.Vi(ff.PDArray, "定位點BX"), ff.F.Vi(ff.PDArray, "定位點BY"));
            DieW = ff.F.Vi(ff.PDArray, "DIE寬");
            DieH = ff.F.Vi(ff.PDArray, "DIE高");
            CKLight = ff.F.Vi(ff.PDArray, "檢測亮度");
            PRLight = ff.F.Vi(ff.PDArray, "定位亮度");
            EPT_TH = ff.F.Vf(ff.PDArray, "空料門檻");
            DIE_TH = ff.F.Vf(ff.PDArray, "有料門檻");
            Canny_count = ff.F.Vi(ff.PDArray, "邊緣點有料門檻");
            Degree = ff.F.Vi(ff.PDArray, "MAP順轉角");
            GrayTotalTH = ff.F.Vi(ff.PDArray, "無料灰階");
            判定區尺寸W = ff.F.Vi(ff.PDArray, "判定區尺寸W");
            判定區尺寸H = ff.F.Vi(ff.PDArray, "判定區尺寸H");
            左參考DIE影像X = ff.F.Vi(ff.PDArray, "左參考DIE影像X");
            左參考DIE影像Y = ff.F.Vi(ff.PDArray, "左參考DIE影像Y");
            右參考DIE影像X = ff.F.Vi(ff.PDArray, "右參考DIE影像X");
            右參考DIE影像Y = ff.F.Vi(ff.PDArray, "右參考DIE影像Y");
            左參考dieIDX_X = ff.F.Vi(ff.PDArray, "左參考dieIDX_X");
            左參考dieIDX_Y = ff.F.Vi(ff.PDArray, "左參考dieIDX_Y");
            參考DIE字元 = ff.F.Vs(ff.PDArray, "參考DIE字元");
        }
    }

    public class CassetteClass
    {//記錄此彈匣所有陣列狀況
        public int 有無材料 = 0;//是否有材料
                            // public CheckClass CK = new CheckClass();//檢測紀錄
        public Panel panel = new Panel();
        public Label LB = new Label();
        public int 數片格位高度 = 0;
        public int 軌道格位高度 = 0;
        public int 檢測結果 = 0;

    }


    public class sysClass
    {
        public string 產品尺寸;
        public int 圖檔保留天數;
        public int 象限數X;
        public int 象限數Y;
        public int 蓋印選用;
        public int 存圖選用;
        public int PASS_RATE;
        public int 光幕選用;
        public int 安全門選用;
        public int X檢測間距;
        public int Y檢測間距;
        public int X第一點;
        public int Y第一點;
        public int X蓋印點;
        public int Y蓋印點;
        public int R蓋印點;
        public int X條碼點;
        public int Y條碼點;
        public int X條碼點2;
        public int Y條碼點2;
        public int R條碼點;
        public int 彈匣數片第一點;
        public int 彈匣軌道第一點;
        public int 彈匣收料補償;
        public int 彈匣間距;
        public int X接料點;
        public int Y接料點;
        public int R接料點;
        public int 條碼位置1X;
        public int 條碼位置1Y;
        public int 條碼位置1W;
        public int 條碼位置1H;
        public int 條碼位置2X;
        public int 條碼位置2Y;
        public int 條碼位置2W;
        public int 條碼位置2H;
        public int 水平X校正點;
        public int 水平Y校正點;
        public int 水平區X;
        public int 水平區Y;
        public int 水平區W;
        public int 水平區H;


        public int 軌道寬點;
        public int 軌道窄點;

        public int 天車撥料慢速點;
        public int 天車撥料保護點;
        public int 天車軌道點;
        public int 天車載船點;
        public int 天車拉出點;
        public int 天車拉料點;

        public int CutXR;
        public int CutXL;
        public int CutYT;
        public int CutYB;
        public int StartMode;

        public int OverlapX;
        public int OverlapY;
        public double 伺服像素比X;
        public double 伺服像素比Y;
        public int 光源亮度;

        public int 軟體正極限x;
        public int 軟體負極限x;
        public int 軟體正極限y;
        public int 軟體負極限y;

        public int CCD檢測高度;

        public void ReadInfo(MF ff)
        {
            產品尺寸 = ff.F.Vs(ff.PDArray, "產品尺寸");
            圖檔保留天數 = ff.F.Vi(ff.sysArray, "圖檔保留天數");
            蓋印選用 = ff.F.Vi(ff.sysArray, "蓋印選用");
            存圖選用 = ff.F.Vi(ff.sysArray, "存圖選用");
            光幕選用 = ff.F.Vi(ff.sysArray, "光幕選用");
            安全門選用 = ff.F.Vi(ff.sysArray, "安全門選用");
            象限數X = ff.F.Vi(ff.sysArray, "象限數X");
            象限數Y = ff.F.Vi(ff.sysArray, "象限數Y");
            X檢測間距 = ff.F.Vi(ff.sysArray, "X檢測間距");
            Y檢測間距 = ff.F.Vi(ff.sysArray, "Y檢測間距");
            X第一點 = ff.F.Vi(ff.sysArray, "X第一點");
            Y第一點 = ff.F.Vi(ff.sysArray, "Y第一點");
            X蓋印點 = ff.F.Vi(ff.sysArray, "X蓋印點");
            Y蓋印點 = ff.F.Vi(ff.sysArray, "Y蓋印點");
            R蓋印點 = ff.F.Vi(ff.sysArray, "R蓋印點");
            X條碼點 = ff.F.Vi(ff.sysArray, "X條碼點");
            Y條碼點 = ff.F.Vi(ff.sysArray, "Y條碼點");
            R條碼點 = ff.F.Vi(ff.sysArray, "R條碼點");
            X條碼點2 = ff.F.Vi(ff.sysArray, "X條碼點2");
            Y條碼點2 = ff.F.Vi(ff.sysArray, "Y條碼點2");
            CCD檢測高度 = ff.F.Vi(ff.sysArray, "CCD檢測高度");
            彈匣數片第一點 = ff.F.Vi(ff.sysArray, "彈匣數片第一點");
            彈匣軌道第一點 = ff.F.Vi(ff.sysArray, "彈匣軌道第一點");
            彈匣間距 = ff.F.Vi(ff.sysArray, "彈匣間距");
            彈匣收料補償 = ff.F.Vi(ff.sysArray, "彈匣收料補償");
            軌道寬點 = ff.F.Vi(ff.sysArray, "軌道寬點");
            軌道窄點 = ff.F.Vi(ff.sysArray, "軌道窄點");

            X接料點 = ff.F.Vi(ff.sysArray, "X接料點");
            Y接料點 = ff.F.Vi(ff.sysArray, "Y接料點");
            R接料點 = ff.F.Vi(ff.sysArray, "R接料點");

            天車撥料慢速點 = ff.F.Vi(ff.sysArray, "天車撥料慢速點");
            天車撥料保護點 = ff.F.Vi(ff.sysArray, "天車撥料保護點");
            天車軌道點 = ff.F.Vi(ff.sysArray, "天車軌道點");
            天車載船點 = ff.F.Vi(ff.sysArray, "天車載船點");
            天車拉出點 = ff.F.Vi(ff.sysArray, "天車拉出點");
            天車拉料點 = ff.F.Vi(ff.sysArray, "天車拉料點");

            條碼位置1X = ff.F.Vi(ff.sysArray, "條碼位置1X");
            條碼位置1Y = ff.F.Vi(ff.sysArray, "條碼位置1Y");
            條碼位置1W = ff.F.Vi(ff.sysArray, "條碼位置1W");
            條碼位置1H = ff.F.Vi(ff.sysArray, "條碼位置1H");
            條碼位置2X = ff.F.Vi(ff.sysArray, "條碼位置2X");
            條碼位置2Y = ff.F.Vi(ff.sysArray, "條碼位置2Y");
            條碼位置2W = ff.F.Vi(ff.sysArray, "條碼位置2W");
            條碼位置2H = ff.F.Vi(ff.sysArray, "條碼位置2H");

            CutXR = ff.F.Vi(ff.sysArray, "接圖右切量X");
            CutXL = ff.F.Vi(ff.sysArray, "接圖左切量X");
            CutYT = ff.F.Vi(ff.sysArray, "接圖上切量Y");
            CutYB = ff.F.Vi(ff.sysArray, "接圖下切量Y");
            伺服像素比X = ff.F.Vd(ff.sysArray, "伺服像素比X");
            伺服像素比Y = ff.F.Vd(ff.sysArray, "伺服像素比Y");
            光源亮度 = ff.F.Vi(ff.sysArray, "光源亮度");

            軟體正極限x = ff.F.Vi(ff.sysArray, "軟體正極限x");
            軟體負極限x = ff.F.Vi(ff.sysArray, "軟體負極限x");
            軟體正極限y = ff.F.Vi(ff.sysArray, "軟體正極限y");
            軟體負極限y = ff.F.Vi(ff.sysArray, "軟體負極限y");


            水平X校正點 = ff.F.Vi(ff.sysArray, "水平X校正點");
            水平Y校正點 = ff.F.Vi(ff.sysArray, "水平Y校正點");
            水平區X = ff.F.Vi(ff.sysArray, "水平區X");
            水平區Y = ff.F.Vi(ff.sysArray, "水平區Y");
            水平區W = ff.F.Vi(ff.sysArray, "水平區W");
            水平區H = ff.F.Vi(ff.sysArray, "水平區H");

        }
    }

    public class StateClass//掌管機台各種狀態
    {
        public struct State_Structure
        {
            public int IDX;
            public string Title;
            public string Context;//狀態內容敘述
            public Color StateColor;
        }
        public string 批號;
        public string 客批;
        public string 產品代號;
        public string 晶圓ID;
        public string MAPName;
        public string MAPpath;//跟MAP檔資料庫跟目錄組合完畢的完整MAP路徑
        public bool 英文模式 = false;
        public State_Structure[] StateTable = new State_Structure[10];
        public State_Structure Curr_STATE;//目前狀態
        public State_Structure Req_STATE;//請求切換目標狀態
        public int Curr_STEP;//目前運轉步序階段
        public int 當前格位;
        public int FrameCNT = 0;//數片結果
        public bool Mode_Live;
        public int 晶圓尺寸;
        public int 選中格位IDX;

        public bool 安全光幕觸發 = false;
        public bool 安全門開啟 = false;
        public int 空彈匣計時 = 0;
        public int 續做模式 = 0;//1:載台有料續做  2:載台無料續做


        public StateClass()
        {
            選中格位IDX = 0;
            //建立狀態表格 供後續參考
            StateTable[0] = new State_Structure { IDX = 0, Title = "停止", StateColor = Color.Yellow };
            StateTable[1] = new State_Structure { IDX = 1, Title = "自動中", StateColor = Color.MediumSeaGreen };
            StateTable[2] = new State_Structure { IDX = 2, Title = "復歸中", StateColor = Color.Yellow };
            StateTable[3] = new State_Structure { IDX = 3, Title = "安全光幕", StateColor = Color.Red };
            StateTable[4] = new State_Structure { IDX = 4, Title = "緊急", StateColor = Color.Red };
            StateTable[5] = new State_Structure { IDX = 5, Title = "異常", StateColor = Color.Red };
            StateTable[6] = new State_Structure { IDX = 6, Title = "停止供料", StateColor = Color.Orange };
            StateTable[6] = new State_Structure { IDX = 7, Title = "必須停止", StateColor = Color.Pink };
            StateTable[6] = new State_Structure { IDX = 8, Title = "必須復歸", StateColor = Color.Pink };
        }
    }

    public class CheckClass //紀錄檢測到的資料
    {
        public class DieInfoStruct
        {
            public DieInfoStruct()
            {
                DieColor = 0;
                coordX = -1;
                coordY = -1;
                DieState = -1;
                CheckState = 0;
                ErrorState = 0;
            }

            public int DieColor;//此顆die檢測後應該被標記的顏色
            public char MapChar;//此顆die在MAP中的代表字元
            public int coordX;//此顆die檢測區左上角座標位置
            public int coordY;
            public int DieState;//實際檢測結果-1:尚未檢測 0:無die  1:有die
            public int CheckState;//是否已檢測完成 0:尚未檢測 1:定位成功 2:推導成功
                                  //2:空料左上角定位 3:空料右下角定位
                                  //4.空料中段定位 5.好鄰居定位
            public int ErrorState;
        }

        public DieInfoStruct[,] Matrix;// 當前晶圓上的內容矩陣

        //記錄當前檢測產品的資料
        public string LotNum;
        public string Customer;
        public string Device;
        public string WaferID;
        public string 檢測時間;
        public int 晶粒總數;
        public int 空格數;
        public int 有料數;
        public int 壞品誤取數;
        public int 壞品留置數;
        public int 良品留置數;
        public int 良品取走數;
        public int 檢測結果; //0:沒檢查 1:OK 2:NG 3:條碼失敗 4.資料缺失 5.無MAP
        public void Reset(ref MapClass MAP)
        {
            Matrix = new CheckClass.DieInfoStruct
    [MAP.Matrix.GetLength(0), MAP.Matrix.GetLength(1)];
            for (int y = 0; y <= MAP.Matrix.GetLength(0) - 1; y++)
            {
                for (int x = 0; x <= MAP.Matrix.GetLength(1) - 1; x++)
                {
                    Matrix[y, x] = new CheckClass.DieInfoStruct();
                }
            }

            晶粒總數 = 0;
            有料數 = 0;
            空格數 = 0;
            壞品誤取數 = 0;
            良品留置數 = 0;
            壞品留置數 = 0;
            良品取走數 = 0;
        }
    }
}
