using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GT_WV100;
using System.Drawing;
using System.Windows.Forms;
namespace GT_WV100
{
    public class File_Class
    {
        public IO_Structure[,] InMatrix = new IO_Structure[6, 16];//輸入陣列
        public IO_Structure[,] OutMatrix = new IO_Structure[3, 16];//輸出陣列
        public struct Para_Structure
        {
            public string Name;
            public string Value;
        }

        //****************PLC********************
        public struct IO_Structure
        {
            public string N;//名稱
            public string T;//種類(I/O)
            public string BIT;//BIT
            public string CH;//通道
            public int V;          //值

            //public string Unit;       //單位
            //public string Precision;    //精度
            //public string CH_count;  //使用通道數
            //public bool ReadCH;//bit/ch/dw
            public object CTRL;//此點位在人機上所連結的元件 可能是LABEL..等等
        }

        public class Alarm_Condition_Class
        {
            public class Alarm_Node_Class
            {
                //種類:0:NOP 1.IN 2.CMP
                public string 種類 = "NOP";
                public int 軸目標位置 = 0;
                public int 軸編號 = 0;
                public string 比較元 = "0";
                public int 點通道 = 0;
                public int 點位元 = 0;
                //點通道
                public int 點目標狀態 = 0;
                public string 註解 = "";
                public int 延遲 = 0;//此動作完成後到下一個動作的延遲
                public bool 節點成立 = false;//紀錄此節點條件當下是否成立
            }
            public List<Alarm_Node_Class> ConList = new List<Alarm_Node_Class>();
            public string 異常碼 = "";
            public string 異常敘述 = "";
            public bool 條件成立 = false;//當所有節點成立 則條件成立 觸發警報
        }

        public class Motion_Step_Class
        {
            public class Motion_Node_Class
            {
                //1.Int ConType => //種類:0:NOP 1.IN 2.CMP 3.OUT 4.MOV_ABS 5.MOV_RLA 6.DELAY
                //2. Int 軸位置(軸用) ex: 3500
                //3. Int 軸編號(軸用) ex:1
                //4. Int 比較元(軸用) ex."==" "<=" ">="
                //5. Int 點位址ex.5.12
                //6. Int 點目標狀態 ex. 0/1
                //7.String 註解ex.8吋汽缸到位
                //8. int 異常時間(全自動狀態下此動作停留多久無法前進叫異常)
                public string 種類 = "NOP";
                public int 軸目標位置 = 0;
                public int 軸編號 = 0;
                public string 比較元 = "0";
                public int 點通道 = -1;
                public int 點位元 = -1;
                public int 點目標狀態 = 0;
                public string 註解 = "";
                public string 位置參數連結 = "";
                public string 軸相對距離 = "";
                public int 軸目標速度 = 0;
                public int 延遲;//給單純延遲動作使用
                public bool 節點成立 = false;//紀錄此節點條件當下是否成立
            }
            public List<Motion_Node_Class> ConList = new List<Motion_Node_Class>();
            public string 動作名稱 = "";
            public bool 條件成立 = false;//當所有節點成立 則條件成立 可以進入下一步
            public int 已延遲時間;//此步序條件成立後已過了多久時間
            public int 目標延遲時間;//此步序條件成立後延遲多久進行下一動作
            public int 已等待時間;//進入此步序已經過的時間
            public int 等待上限時間;//切到此步序後多久沒有達成條件成立就產生異常
            public bool 等待外部條件 = false;//某些需要跑副程式的週期動作, 會在副程式做完時直接IDX++,
                                       //不參考此處的判斷條件, 因此當此旗標ON時, 僅需等待外部條件成立並IDX++即可
            public string 條件註解 = "";
            public string 完整字串 = "";
        }

        public struct Language_Structure
        {
            public string Ncht;
            public string Neng;
            public double SizeCht;
            public double SizeEng;
            public int Pageindex;
        }

        //**********載入異常條件檔***************************
        public void Load_ALARM(ref Alarm_Condition_Class[] ALArray, string FileName)
        {
            for (int i = 0; i <= ALArray.Length - 1; i++)
                ALArray[i] = new Alarm_Condition_Class();
            StreamReader sr = new StreamReader(FileName, Encoding.UTF8);
            string Buffer;
            int IDX = 0;

            while (!sr.EndOfStream)
            {
                Alarm_Condition_Class AL = ALArray[IDX];
                Buffer = sr.ReadLine();
                if (Buffer.IndexOf("//") == 0) continue;//前面的註解 不用處理
                //取得異常碼
                int 切點_異常碼 = Buffer.IndexOf(",");
                AL.異常碼 = Buffer.Substring(0, 切點_異常碼);
                int 切點_異常敘述 = Buffer.LastIndexOf("&");
                AL.異常敘述 = Buffer.Substring(切點_異常敘述 + 1, Buffer.Length - 切點_異常敘述 - 1);
                //讀取此異常所有構成條件(最多5個,要更多改這邊就好)
                for (int i = 0; i <= 4; i++)
                {
                    //建構出條件關鍵字
                    string ConHead = i + "(";
                    int 切點_條件頭 = Buffer.IndexOf(ConHead);
                    if (切點_條件頭 >= 0)//有此條件
                    {
                        //切割出此條件字串部分
                        int 切點_條件尾 = Buffer.IndexOf(")", 切點_條件頭 + 1);
                        if (切點_條件尾 == -1)
                        {
                            MessageBox.Show("參數檔格式異常"); return;
                        }
                        //ex.1(軸_彈匣升降<1000)2(3.02,ON)3(3.05,ON)&彈匣升降卡料異常
                        string ConBody = Buffer.Substring(切點_條件頭 + 2, 切點_條件尾 - 切點_條件頭 - 2);
                        //建立節點
                        Alarm_Condition_Class.Alarm_Node_Class TempNode =
                            new Alarm_Condition_Class.Alarm_Node_Class();
                        string[] SegArray = ConBody.Split(',');
                        if (ConBody.IndexOf("軸") >= 0)//軸位置判定
                        {
                            //ex.軸_彈匣升降<1000)
                            int a = ConBody.IndexOf("<");
                            int b = ConBody.IndexOf("=");
                            int c = ConBody.IndexOf(">");
                            int Cut比較元 = -1;
                            if (a >= 0) { TempNode.比較元 = "<"; Cut比較元 = a; }
                            else if (b >= 0) { TempNode.比較元 = "="; Cut比較元 = b; }
                            else if (c >= 0) { TempNode.比較元 = ">"; Cut比較元 = c; }
                            TempNode.軸編號 =Convert.ToInt32( ConBody.Substring(0, Cut比較元));
                            TempNode.軸目標位置 = Convert.ToInt32(ConBody.Substring(Cut比較元 + 1, ConBody.Length - (Cut比較元 + 1)));
                            TempNode.種類 = "CMP";
                        }
                        else if (ConBody.IndexOf(",") >= 0)//點位狀態判定
                        {
                            string[] ChBit = SegArray[0].Split('.');
                            TempNode.點通道 = Convert.ToInt32(ChBit[0]);
                            TempNode.點位元 = Convert.ToInt32(ChBit[1]);
                            TempNode.點目標狀態 = Convert.ToInt32(SegArray[1]);
                            TempNode.種類 = "IN";
                        }
                        //將條件節點加入LIST
                        ALArray[IDX].ConList.Add(TempNode);
                    }

                }
                IDX++;
            }
            sr.Dispose();
        }


        //**************載入動作流程檔*************************
        public void Load_STEP(ref Motion_Step_Class[] SArray, string FileName)
        {
            for (int i = 0; i <= SArray.Length - 1; i++)
            {
                SArray[i] = new Motion_Step_Class();
            }

            StreamReader sr = new StreamReader(FileName, Encoding.Default);
            string Buffer;
            int IDX = 0;
            while (!sr.EndOfStream)
            {
                Buffer = sr.ReadLine();
                SArray[IDX].完整字串 = Buffer;
                if (Buffer.Length == 0) //空行
                    continue;//空行
                if (Buffer.IndexOf("//") == 0)
                {
                    SArray[IDX].條件註解 = Buffer;
                    IDX++;
                    continue;//純註解 
                }                                      
                
                if (Buffer.IndexOf("*") == 0)//需等待外部條件的特殊標籤
                {
                    SArray[IDX].條件註解 = Buffer.Substring(1, Buffer.Length - 1);
                    SArray[IDX].等待外部條件 = true;
                    IDX++;
                    continue;
                }

                int cutWait = Buffer.LastIndexOf("*");
                int cutCommand = Buffer.LastIndexOf("//");
                //if (cutCommand == 0)//此行為純註解
                //    continue;

                if (cutWait != -1)
                {
                    if (cutCommand > -1)//是否有註解
                        SArray[IDX].等待上限時間 = Convert.ToInt32(Buffer.Substring(cutWait + 1, cutCommand - cutWait - 1));
                    else
                        SArray[IDX].等待上限時間 = Convert.ToInt32(Buffer.Substring(cutWait + 1, Buffer.Length - cutWait - 1));
                    int cutDelay = Buffer.LastIndexOf("*", cutWait - 1);
                    if (cutDelay != -1)
                        SArray[IDX].目標延遲時間 = Convert.ToInt32(Buffer.Substring(cutDelay + 1, cutWait - cutDelay - 1));
                }
                int ConHeaxidx = -1;
                //讀取此步驟所有構成條件(最多5個,要更多改這邊就好)
                for (int i = 0; i <= 4; i++)
                {
                    //建構出條件關鍵字
                    ConHeaxidx = Buffer.IndexOf("[", ConHeaxidx + 1);

                    if (ConHeaxidx >= 0)//有此條件
                    {
                        //切割出此條件字串部分
                        int ConEndidx = Buffer.IndexOf("]", ConHeaxidx + 1);
                        if (ConEndidx == -1)
                        {
                            MessageBox.Show("參數檔格式異常"); return;
                        }
                        string ConBody = Buffer.Substring(ConHeaxidx + 1, ConEndidx - ConHeaxidx - 1);
                        //建立節點
                        Motion_Step_Class.Motion_Node_Class TempNode =
                            new Motion_Step_Class.Motion_Node_Class();
                        string[] SegArray = ConBody.Split(',');

                        if ((ConBody.IndexOf("IN") == 0) || (ConBody.IndexOf("OUT") == 0))
                        {
                            TempNode.種類 = SegArray[0];
                            string[] ChBit = SegArray[1].Split('.');
                            TempNode.點通道 = Convert.ToInt32(ChBit[0]);
                            TempNode.點位元 = Convert.ToInt32(ChBit[1]);
                            TempNode.點目標狀態 = Convert.ToInt32(SegArray[2]);
                            //分析出點位址中的CH/BIT 然後去總表矩陣內取得點位註解
                            if (TempNode.種類.CompareTo("IN") == 0)
                                TempNode.註解 = "(IN:" + SegArray[1] + "," + SegArray[2] + ") : " + InMatrix[TempNode.點通道, TempNode.點位元].N;
                            else
                            {
                                TempNode.註解 = "(OUT:" + SegArray[1] + "," + SegArray[2] + ") : " + OutMatrix[TempNode.點通道, TempNode.點位元].N;
                                if (TempNode.點目標狀態 == 0)
                                    TempNode.註解 += "解除";
                            }
                        }
                        else if ((ConBody.IndexOf("MOV_ABS") == 0) || (ConBody.IndexOf("MOV_RLA") == 0))
                        {
                            TempNode.種類 = SegArray[0];
                            TempNode.軸編號 = Convert.ToInt32(SegArray[1]);
                            if (int.TryParse(SegArray[2], out TempNode.軸目標位置))
                                TempNode.位置參數連結 = "";//軸目標位置為數字, 不用參考參數檔內容
                            else
                                TempNode.位置參數連結 = SegArray[2];//軸目標位置為文字,直接帶入參數值名稱
                            TempNode.註解 = ConBody;
                        }
                        else if (ConBody.IndexOf("MOV_CTN") == 0)
                        {
                            TempNode.種類 = SegArray[0];
                            TempNode.軸編號 = Convert.ToInt32(SegArray[1]);
                            TempNode.註解 = ConBody;
                        }
                        else if (ConBody.IndexOf("CMP") == 0)
                        {
                            TempNode.種類 = SegArray[0];
                            TempNode.軸編號 = Convert.ToInt32(SegArray[1]);
                            if (int.TryParse(SegArray[2], out TempNode.軸目標位置))
                                TempNode.位置參數連結 = "";//軸目標位置為數字, 不用參考參數檔內容
                            else
                                TempNode.位置參數連結 = SegArray[2];//軸目標位置為文字,直接帶入參數值名稱
                            TempNode.比較元 = SegArray[3];
                            TempNode.註解 = ConBody;
                        }
                        else if (ConBody.IndexOf("DELAY") == 0)
                        {
                            TempNode.種類 = SegArray[0];
                            TempNode.延遲 = Convert.ToInt32(SegArray[1]);
                            TempNode.註解 = ConBody;
                        }
                        else if (ConBody.IndexOf("STOP") == 0)
                        {
                            TempNode.種類 = SegArray[0];
                            TempNode.軸編號 = Convert.ToInt32(SegArray[1]);
                            TempNode.註解 = ConBody;
                            SArray[IDX].條件註解 = "STOP";
                        }
                        else if (ConBody.IndexOf("SPD") == 0)
                        {
                            TempNode.種類 = SegArray[0];
                            TempNode.軸編號 = Convert.ToInt32(SegArray[1]);
                            if (int.TryParse(SegArray[2], out TempNode.軸目標速度))
                                TempNode.位置參數連結 = "";//軸目標位置為數字, 不用參考參數檔內容
                            else
                                TempNode.位置參數連結 = SegArray[2];//軸目標位置為文字,直接帶入參數值名稱

                            TempNode.註解 = ConBody;
                        }
                        //SArray[IDX].目標延遲時間 = 
                        //將條件節點加入LIST
                        SArray[IDX].ConList.Add(TempNode);

                    }
                    else
                        break;
                }
                IDX++;
            }
            sr.Dispose();

        }





        ////載入動作流程檔
        //public void Load_STEP(ref Motion_Step_Class[] SArray, string FileName)
        //{
        //    for (int i = 0; i <= SArray.Length - 1; i++)
        //    {
        //        SArray[i] = new Motion_Step_Class();
        //    }

        //    StreamReader sr = new StreamReader(FileName, Encoding.UTF8);
        //    string Buffer;
        //    int IDX = 0;
        //    while (!sr.EndOfStream)
        //    {
        //        Buffer = sr.ReadLine();
        //        if (Buffer.IndexOf("//") == 0) continue;//前面的註解 不用處理


        //        int cut0 = Buffer.IndexOf(",");
        //        int cut1 = Buffer.IndexOf(",", cut0+1);
        //        int cut2 = Buffer.IndexOf(",", cut1 + 1);
        //        int cut3 = Buffer.IndexOf(",", cut2 + 1);
        //        if ((Buffer.IndexOf("IN")==0)||(Buffer.IndexOf("OUT") == 0))
        //        {
        //            SArray[IDX].種類 = Buffer.Substring(0,cut0);
        //            SArray[IDX].點位址 = Buffer.Substring(cut0+1, cut1 - cut0 -1);
        //            SArray[IDX].點目標狀態 =Convert.ToInt32( Buffer.Substring(cut1+1, cut2 - cut1 - 1));
        //            SArray[IDX].註解 = Buffer.Substring(cut2+1, Buffer.Length - cut2 - 1);
        //        }
        //        else if ((Buffer.IndexOf("MOV_ABS") == 0)|| (Buffer.IndexOf("MOV_RLA") == 0))
        //        {
        //            SArray[IDX].種類 = Buffer.Substring(0, cut0);
        //            SArray[IDX].軸編號 = Convert.ToInt32(Buffer.Substring(cut0 + 1, cut1 - cut0 - 1));
        //            SArray[IDX].軸目標位置 = Convert.ToInt32(Buffer.Substring(cut1 + 1, cut2 - cut1 - 1));
        //            SArray[IDX].註解 = Buffer.Substring(cut2 + 1, Buffer.Length - cut2 - 1);
        //        }
        //        else if (Buffer.IndexOf("CMP") == 0)
        //        {
        //            SArray[IDX].種類 = Buffer.Substring(0, cut0);
        //            SArray[IDX].軸編號 = Convert.ToInt32(Buffer.Substring(cut0 + 1, cut1 - cut0 - 1));
        //            SArray[IDX].軸目標位置 = Convert.ToInt32(Buffer.Substring(cut1 + 1, cut2 - cut1 - 1));
        //            SArray[IDX].比較元 = Buffer.Substring(cut2 + 1, cut3 - cut2 - 1);
        //            SArray[IDX].註解 = Buffer.Substring(cut3 + 1, Buffer.Length - cut3 - 1);
        //        }
        //        else if (Buffer.IndexOf("DELAY") == 0)
        //        {
        //            SArray[IDX].種類 = Buffer.Substring(0, cut0);
        //            SArray[IDX].延遲 = Convert.ToInt32(Buffer.Substring(cut0 + 1, Buffer.Length - cut0 - 1));
        //        }
        //    }

        //}


        public void Load_PLCMatrix(ref IO_Structure[,] inMatrix,
                                    ref IO_Structure[,] outMatrix, string FileName)
        {
            //從CX-programer複製下來的點位總表
            //此function是要將此文字檔總表資料切分出來填入資料結構內
            //方便後續作輸出讀取的處理
            //文字檔格式範例如下
            //BOOL	1.04 	   正翻轉推料氣缸上 	0
            //BOOL	2.05 	   反出料撥料擋缸上 	0
            //BOOL	H0.00	   開機自保	0
            //BOOL	W9.01	   NC433U軸相對運動	  0
            for (int i = 0; i <= inMatrix.GetLength(0) - 1; i++)
            {
                for (int k = 0; k < inMatrix.GetLength(1) - 1; k++)
                {
                    inMatrix[i, k].N = null;
                    inMatrix[i, k].V = 0;//尚未使用
                    inMatrix[i, k].T = null;
                    inMatrix[i, k].CH = null;
                    inMatrix[i, k].BIT = null;
                }

            }
            for (int i = 0; i <= outMatrix.GetLength(0) - 1; i++)
            {
                for (int k = 0; k < outMatrix.GetLength(1) - 1; k++)
                {
                    outMatrix[i, k].N = null;
                    outMatrix[i, k].V = 0;//尚未使用
                    outMatrix[i, k].T = null;
                    outMatrix[i, k].CH = null;
                    outMatrix[i, k].BIT = null;
                }

            }
            StreamReader sr = new StreamReader(FileName, Encoding.UTF8);
            string Buffer;
            int cut = FileName.LastIndexOf("\\");
            string PD_Name = FileName.Substring(cut + 1);
            switch (PD_Name.IndexOf("IO.txt"))
            {
                case 0:
                    //點位總表
                    while (!sr.EndOfStream)
                    {
                        //先將每行當中的tab/空格轉為#字方便處理
                        Buffer = sr.ReadLine();
                        Buffer = Buffer.Replace('\t', '#');
                        Buffer = Buffer.Replace(' ', '_');
                        Buffer = Buffer.Replace("##", "#");
                        int SegCount = System.Text.RegularExpressions.
                                                  Regex.Matches(Buffer, "#").Count;

                        //標準格式每行只會被分成四段(5個#), 超過的為PLC保留功能區不用考慮
                        if (SegCount == 3)
                        {
                            int Cut0 = Buffer.IndexOf("#", 0);
                            int Cut1 = Buffer.IndexOf("#", Cut0 + 1);
                            int Cut2 = Buffer.IndexOf("#", Cut1 + 1);
                            int CutFloatP = Buffer.IndexOf(".", Cut0 + 1, Cut1 - Cut0 - 1);

                            //先取得IO點, 因為要把資料放到矩陣的對應座標位置
                            int IDX_ch = Convert.ToInt32(Buffer.Substring(Cut0 + 1, CutFloatP - Cut0 - 1));//取得IO(CH)點
                            int IDX_bit = Convert.ToInt32(Buffer.Substring(CutFloatP + 1, Cut1 - CutFloatP - 1));//取得IO(BIT)點
                            string IOType = Buffer.Substring(0, 1);//取得I/O 種類

                            if (IOType.CompareTo("O") == 0)//輸出點位
                            {

                                outMatrix[IDX_ch, IDX_bit].CH = IDX_ch.ToString(); //取得IO(CH)點
                                outMatrix[IDX_ch, IDX_bit].BIT = IDX_bit.ToString();//取得IO(BIT)點
                                outMatrix[IDX_ch, IDX_bit].T = IOType;
                                outMatrix[IDX_ch, IDX_bit].N = Buffer.Substring(Cut1 + 1, Cut2 - Cut1 - 1);//取得名稱
                            }
                            else if (IOType.CompareTo("I") == 0)//輸入點位
                            {
                                inMatrix[IDX_ch, IDX_bit].CH = IDX_ch.ToString(); //取得IO(CH)點
                                inMatrix[IDX_ch, IDX_bit].BIT = IDX_bit.ToString();//取得IO(BIT)點
                                inMatrix[IDX_ch, IDX_bit].T = IOType;
                                inMatrix[IDX_ch, IDX_bit].N = Buffer.Substring(Cut1 + 1, Cut2 - Cut1 - 1);//取得名稱
                            }

                            //分析IO點來取得記憶體區種類
                            //IOMatrix[IDX_ch, IDX_bit].T = IOMatrix[IDX_ch, IDX_bit].CH.Substring(0, 1);
                        }
                        //IDX++;
                    }
                    break;

                default:
                    break;
            }
            sr.Dispose();
        }





        public void Load_PLC(ref IO_Structure[] PArray, string FileName)
        {
            //從CX-programer複製下來的點位總表
            //此function是要將此文字檔總表資料切分出來填入資料結構內
            //方便後續作輸出讀取的處理
            //文字檔格式範例如下
            //BOOL	1.04 	   正翻轉推料氣缸上 	0
            //BOOL	2.05 	   反出料撥料擋缸上 	0
            //BOOL	H0.00	   開機自保	0
            //BOOL	W9.01	   NC433U軸相對運動	  0
            for (int i = 0; i <= PArray.Length - 1; i++)
            {
                PArray[i].N = null;
                PArray[i].V = 0;
                PArray[i].T = null;
                PArray[i].CH = null;
                PArray[i].BIT = null;
            }
            StreamReader sr = new StreamReader(FileName, Encoding.UTF8);
            string Buffer;
            int IDX = 0;
            int cut = FileName.LastIndexOf("\\");
            string PD_Name = FileName.Substring(cut + 1);
            switch (PD_Name.IndexOf("IO.txt"))
            {
                case 0:
                    //點位總表
                    while (!sr.EndOfStream)
                    {
                        //先將每行當中的tab/空格轉為#字方便處理
                        Buffer = sr.ReadLine();
                        Buffer = Buffer.Replace('\t', '#');
                        Buffer = Buffer.Replace(' ', '_');
                        Buffer = Buffer.Replace("##", "#");
                        int SegCount = System.Text.RegularExpressions.
                                                  Regex.Matches(Buffer, "#").Count;

                        //標準格式每行只會被分成四段(5個#), 超過的為PLC保留功能區不用考慮
                        if (SegCount == 3)
                        {
                            int Cut0 = Buffer.IndexOf("#", 0);
                            int Cut1 = Buffer.IndexOf("#", Cut0 + 1);
                            int Cut2 = Buffer.IndexOf("#", Cut1 + 1);
                            // int Cut3 = Buffer.IndexOf("#", Cut2 + 1);
                            // int Cut4 = Buffer.IndexOf("#", Cut3 + 1);
                            int cutPoint = Buffer.IndexOf(".", Cut0 + 1, Cut1 - Cut0 - 1);

                            PArray[IDX].N = Buffer.Substring(Cut1 + 1, Cut2 - Cut1 - 1);//取得名稱
                            if (cutPoint != -1)
                            {
                                PArray[IDX].CH = Buffer.Substring(Cut0 + 1, cutPoint - Cut0 - 1);//取得IO(CH)點
                                PArray[IDX].BIT = Buffer.Substring(cutPoint + 1, Cut1 - cutPoint - 1);//取得IO(BIT)點
                            }
                            //分析IO點來取得記憶體區種類
                            PArray[IDX].T = PArray[IDX].CH.Substring(0, 1);
                        }
                        IDX++;
                    }
                    break;

                default:
                    //只針對產品別的產品參數
                    PArray = new IO_Structure[0];
                    while (!sr.EndOfStream)
                    {
                        Array.Resize(ref PArray, PArray.Length + 1);
                        //先將每行當中的tab/空格轉為#字方便處理
                        Buffer = sr.ReadLine();
                        Buffer = Buffer.Replace("_", "#");
                        Buffer = Buffer.Replace("##", "#");

                        int SegCount = System.Text.RegularExpressions.
                                                  Regex.Matches(Buffer, "#").Count;

                        //標準格式每行只會被分成5段(3個#及1個=), 超過的為PLC保留功能區不用考慮
                        if (SegCount == 3)
                        {
                            int Cut0 = Buffer.IndexOf("#", 0);
                            int Cut1 = Buffer.IndexOf("#", Cut0 + 1);
                            int Cut2 = Buffer.IndexOf("#", Cut1 + 1);
                            int CutV = Buffer.IndexOf("=", 0);

                            PArray[IDX].T = Buffer.Substring(0, Cut0);//取得記憶體種類
                            PArray[IDX].CH = Buffer.Substring(Cut0 + 1, Cut1 - Cut0 - 1);//取得IO(CH)點
                            PArray[IDX].BIT = Buffer.Substring(Cut1 + 1, Cut2 - Cut1 - 1);//取得IO(BIT)點
                            PArray[IDX].N = Buffer.Substring(Cut2 + 1, CutV - Cut2 - 1);//取得名稱
                            PArray[IDX].V =Convert.ToInt32( Buffer.Substring(CutV + 1, Buffer.Length - CutV - 2)); //取得value
                        }
                        IDX++;
                    }
                    break;
            }
            sr.Dispose();
        }

        //將整個語言檔載入參數陣列
        public void Load(ref Language_Structure[] PArray, string FileName)
        {
            string Line;
            int index = 0;

            for (int i = 0; i <= PArray.Length - 1; i++)
            {
                PArray[i].Ncht = null;
                PArray[i].Neng = null;
                PArray[i].SizeCht = -1;

            }
            StreamReader sr = new StreamReader(FileName, Encoding.Default);

            index = 0;
            while (!sr.EndOfStream)
            {
                Line = sr.ReadLine();
                if (Line.CompareTo("") == 0) continue;
                string[] A = Line.Split('=');
                //找到語言檔中的英文
                PArray[index].Neng = A[1];
                PArray[index].Ncht = A[0];
                index++;
            }
            sr.Dispose();
        }

        //將整個文字設定檔載入參數陣列
        public void Load(ref Para_Structure[] PArray, string FileName)
        {
            string Buffer;
            string ParaName;
            string ParaValue;
            int PosA, PosB;
            int index = 0;
            string CutChar = "";

            StreamReader sr = new StreamReader(FileName, Encoding.Default);
            try
            {
                //StreamReader sr = new StreamReader(FileName);
                Buffer = sr.ReadLine();
                PosA = Buffer.IndexOf("=");
                if (PosA != -1) CutChar = "=";
                else
                {
                    PosA = Buffer.IndexOf(":");
                    if (PosA != -1) CutChar = ":";
                }
                if (PosA == -1) return;
                PosB = Buffer.IndexOf(";");
                ParaName = Buffer.Substring(0, PosA);
                ParaValue = Buffer.Substring(PosA + 1, PosB - PosA - 1);
                PArray[index].Name = ParaName;
                PArray[index].Value = ParaValue;
                index += 1;

                while (sr.Peek() != -1)
                {
                    Buffer = sr.ReadLine();
                    PosA = Buffer.IndexOf(CutChar);


                    PosB = Buffer.IndexOf(";");
                    if ((PosA != -1) && (PosB != -1))
                    {
                        ParaName = Buffer.Substring(0, PosA);
                        ParaValue = Buffer.Substring(PosA + 1, PosB - PosA - 1);
                        PArray[index].Name = ParaName;
                        PArray[index].Value = ParaValue;
                        index += 1;
                    }
                }

                sr.Dispose();
            }
            catch (System.Exception)
            {
                sr.Dispose();
                return;
            }
            sr.Dispose();
        }

        //將參數陣列寫入文字設定檔
        public void Save(ref Para_Structure[] Parray,
                       string FileName)
        {
            string FinalBuffer = "";
            for (int i = 0; i <= Parray.Length - 1; i++)
            {
                if (Parray[i].Name == null)
                {
                    break;
                }
                FinalBuffer = FinalBuffer + Parray[i].Name + "=" + Parray[i].Value + ";" + "\r\n";
            }
            int PosA = FileName.LastIndexOf("\\");

            string TempPath = FileName.Substring(0, PosA);
            if (!Directory.Exists(TempPath)) Directory.CreateDirectory(TempPath);
            // System.IO.File.CreateText(FileName);

            using (FileStream fs = new FileStream(FileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                {
                    sw.Write(FinalBuffer);
                    sw.Flush();
                    sw.Dispose();
                }
                fs.Dispose();
            }

        }

        //取得特定參數值(string)
        public string Vs(Para_Structure[] PArry, string Name)
        {
            if (PArry[0].Name == null)
                return "0";


            for (int i = 0; i <= PArry.Length - 1; i++)
            {
                if (PArry[i].Name == null) return "0";


                if (PArry[i].Name.CompareTo(Name) == 0)
                {
                    return (PArry[i].Value);
                }
            }
            return "0";
        }
        //取得特定參數值(int)
        public int Vi(Para_Structure[] PArry, string Name)
        {
            if (PArry[0].Name == null)
                return 0;


            for (int i = 0; i <= PArry.Length - 1; i++)
            {
                if (PArry[i].Name == null) return 0;

                if (PArry[i].Name.CompareTo(Name) == 0)
                {
                    try
                    {
                        return Int32.Parse(PArry[i].Value);
                    }
                    catch (Exception)
                    {
                        return -1;
                        throw;
                    }

                }
            }
            return 0;
        }

        //取得特定參數值(int)
        public double Vd(Para_Structure[] PArry, string Name)
        {
            //Boolean GetPara = false;
            if (PArry[0].Name == null)
                return 0;


            for (int i = 0; i <= PArry.Length - 1; i++)
            {
                if (PArry[i].Name == null) return 0;

                if (PArry[i].Name.CompareTo(Name) == 0)
                {
                    return StrToDouble(PArry[i].Value);
                }
            }
            return 0;
        }


        //取得特定參數值(int)
        public float Vf(Para_Structure[] PArry, string Name)
        {
            //Boolean GetPara = false;
            if (PArry[0].Name == null)
                return 0;


            for (int i = 0; i <= PArry.Length - 1; i++)
            {
                if (PArry[i].Name == null) return 0;

                if (PArry[i].Name.CompareTo(Name) == 0)
                {
                    return StrToFloat(PArry[i].Value);
                }
            }
            return 0;
        }
        public float StrToFloat(object FloatString)
        {
            float result;
            if (FloatString != null)
            {
                if (float.TryParse(FloatString.ToString(), out result))
                    return result;
                else
                {
                    return (float)0.00;
                }
            }
            else
            {
                return (float)0.00;
            }
        }
        public double StrToDouble(object FloatString)
        {
            double result;
            if (FloatString != null)
            {
                if (double.TryParse(FloatString.ToString(), out result))
                    return result;
                else
                {
                    return (double)0.00;
                }
            }
            else
            {
                return (double)0.00;
            }
        }
        //設定特定參數值
        public void SetV(ref Para_Structure[] PArry, string Name, object Value)
        {
            int i = 0;
            for (; i <= PArry.Length - 1; i++)
            {

                if (PArry[i].Name == null)
                {
                    PArry[i].Name = Name;
                    PArry[i].Value = Value.ToString();
                    return;
                }
                else if (PArry[i].Name.CompareTo(Name) == 0)
                {
                    PArry[i].Value = Value.ToString();
                    return;
                }
            }

            return;

        }


        //將參數帶入指定的DataGrid
        public void Para_To_DataGrid(ref System.Windows.Forms.DataGridView DataGrid,
                                ref File_Class.Para_Structure[] JobArray)
        {
            //初始化時使用
            if (DataGrid.Rows.Count < 5)
            {
                DataGrid.DefaultCellStyle.BackColor = Color.PaleGoldenrod;
                DataGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.LemonChiffon;
                DataGrid.DefaultCellStyle.Font = new Font("新細明體", 10, FontStyle.Bold);
                //載入JOB參數
                DataGrid.Rows.Clear();
                for (int i = 0; i <= JobArray.Length - 1; i++)
                {
                    if (JobArray[i].Name != null)
                        DataGrid.Rows.Add(JobArray[i].Name, JobArray[i].Value);
                }               
            }
            else
            {
                for (int i = 0; i <= JobArray.Length - 1; i++)
                {
                    bool FindPara = false;
                    if (JobArray[i].Name != null)
                    {
                        for (int k = 0; k < DataGrid.Rows.Count; k++)
                        {
                            string newValue = "";
                            string Para = "";
                            if (DataGrid.Rows[k].Cells[1].Value != null)
                            {
                                newValue = DataGrid.Rows[k].Cells[1].Value.ToString();
                                Para = DataGrid.Rows[k].Cells[0].Value.ToString();
                            }
                            if (JobArray[i].Name.CompareTo(Para) == 0)
                            {
                                DataGrid.Rows[k].Cells[1].Value = JobArray[i].Value;
                                FindPara = true;
                            }
                        }
                        if(!FindPara)
                        {

                            MessageBox.Show(JobArray[i].Name+"參數未對應,請確認參數檔格式");
                        }
                    }
                }

            }

            DataGrid.Columns[0].ReadOnly = true;
        }

        //儲存特殊結構資料
        public void SaveClassList(ref List<object> ListData, string FileName)
        {

            string FinalBuffer = "";
            for (int i = 0; i <= ListData.Count - 1; i++)
            {
                FinalBuffer += "****Node" + i + "****";


            }

        }

    }
}
