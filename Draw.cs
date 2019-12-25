using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
namespace GT_WV100
{
    //繪圖專用
    public class GTS_Draw
    {
        public int x_pitch, y_pitch;
        public int x_draw, y_draw;
        public int selectMRK, selectSlice;
        public int xAmount, yAmount;
        //框格按照基本係數畫到指定位置後,
        //再擴張的參數
        public int growX = 0;
        public int growY = 0;
        public int 繪圖格寬 = 250;
        public int 繪圖格高 = 30;
        public int 繪圖起始x = 10;
        public int 繪圖起始y = 10;
        public int PLC_AmountY = 7;

        public int rectSHTX = 10, rectSHTY = 15;//讓方格與座標數字能對齊的更好

        Font font = new Font("新細明體", 10, FontStyle.Bold);
        Graphics g;

        public void Draw_Dynamic_Map_IO(PictureBox DstImage, Color CC,
                                                ref File_Class.IO_Structure[] Array,
                                                int W, int H, int pitchX, int pitchY, int STX, int STY, int stride, Font FFF)
        {
            DstImage.Image = new Bitmap(DstImage.Width, DstImage.Height);
            Graphics g = Graphics.FromImage(DstImage.Image);
            Pen PPP = new Pen(Brushes.Black, 2);
            int CurrX = STX;
            int CurrY = STY;

            for (int i = 0; i <= Array.Length - 1; i++)
            {
                if (Array[i].V == null) break;

                if (Array[i].CH.IndexOf(".") == 1)//判斷是否為實體IO
                {

                    if ((((i) % stride) == 0) && (i != 0))//換行
                    {
                        CurrY += (H + pitchY);
                        CurrX = STX;
                    }
                    g.DrawRectangle(PPP, CurrX, CurrY, W, H);
                    if (Array[i].V==1)
                    {
                        g.FillRectangle(Brushes.Gold, CurrX + 1, CurrY + 1, W - 2, H - 2);
                    }

                    g.DrawString("[" + Array[i].CH + "] " + Array[i].N, FFF, Brushes.Black, CurrX + (10), CurrY + (H / 3));
                    // g.DrawString( Array[i].N , FFF, Brushes.Black, CurrX + (70), CurrY + (H / 3));
                    CurrX += (W + pitchX);
                }
            }
            DstImage.Refresh();
        }

        //動態繪出此區所要監控的所有IO點位
        public void Draw_Dynamic_Map_IO(PictureBox DstImage, Color CC,
                                                       ref File_Class.IO_Structure[] Array)
        {
            DstImage.Image = new Bitmap(DstImage.Width, DstImage.Height);
            Graphics g = Graphics.FromImage(DstImage.Image);
            Pen PPP = new Pen(Brushes.Black, 2);
            int CurrX = 繪圖起始x;
            int CurrY = 繪圖起始y;
            Font FFF = new Font("新細明體", 12);

            for (int i = 0; i <= Array.Length - 1; i++)
            {
                if (Array[i].V == null) break;
                if ((CurrY + 繪圖格高 >= DstImage.Height) && (i != 0))//換行
                {
                    CurrY = 繪圖起始y;
                    CurrX += 繪圖格寬 + 繪圖起始x;
                }
                g.DrawRectangle(PPP, CurrX, CurrY, 繪圖格寬, 繪圖格高);
                if (Array[i].V==1)
                {
                    g.FillRectangle(Brushes.Gold, CurrX + 1, CurrY + 1, 繪圖格寬 - 2, 繪圖格高 - 2);
                }
                g.DrawString("[" + Array[i].CH + "." + Array[i].BIT + "] " + Array[i].N, FFF, Brushes.Black, CurrX + 5, CurrY + 5);
                // g.DrawString(Array[i].N, FFF, Brushes.Black, CurrX + 5, CurrY + 2);
                CurrY += 繪圖格高 + 繪圖起始y;
            }
            DstImage.Refresh();
        }

        //動態繪出此區所要監控的所有IO點位(DAQ)
        public void Draw_Dynamic_Map_IO(PictureBox DstImage, Color CC, ref 單動操作.DAQ_Structure[] Array)
        {
            DstImage.Image = new Bitmap(DstImage.Width, DstImage.Height);
            Graphics g = Graphics.FromImage(DstImage.Image);
            Pen PPP = new Pen(Brushes.Black, 2);
            int CurrX = 繪圖起始x;
            int CurrY = 繪圖起始y;
            Font FFF = new Font("新細明體", 12);

            for (int i = 0; i <= Array.Length - 1; i++)
            {
                if (Array[i].VALUE == null) break;
                if ((CurrY + 繪圖格高 >= DstImage.Height) && (i != 0))//換行
                {
                    CurrY = 繪圖起始y;
                    CurrX += 繪圖格寬 + 繪圖起始x;
                }
                g.DrawRectangle(PPP, CurrX, CurrY, 繪圖格寬, 繪圖格高);
                if (Array[i].VALUE.CompareTo("1") == 0)
                {
                    g.FillRectangle(Brushes.Gold, CurrX + 1, CurrY + 1, 繪圖格寬 - 2, 繪圖格高 - 2);
                }
                g.DrawString("[" + Array[i].PORT + "." + Array[i].BIT + "] " + Array[i].NAME, FFF, Brushes.Black, CurrX + 5, CurrY + 5);
                // g.DrawString(Array[i].N, FFF, Brushes.Black, CurrX + 5, CurrY + 2);
                CurrY += 繪圖格高 + 繪圖起始y;
            }
            DstImage.Refresh();
        }

        //動態繪出所有需要設定的項目(通道)
        public void Draw_Dynamic_Map_CH(PictureBox DstImage, Color CC,
                                                ref File_Class.IO_Structure[] Array)
        {
            DstImage.Image = new Bitmap(DstImage.Width, DstImage.Height);
            Graphics g = Graphics.FromImage(DstImage.Image);
            Pen PPP = new Pen(Brushes.Black, 2);
            int CurrX = 繪圖起始x;
            int CurrY = 繪圖起始y;
            Font FFF = new Font("新細明體", 12);

            for (int i = 0; i <= Array.Length - 1; i++)
            {
                if (Array[i].V == 0) break;
                if ((((i) % PLC_AmountY) == 0) && (i != 0))//換行
                {
                    CurrY = 繪圖起始y;
                    CurrX += 繪圖格寬 + 繪圖起始x;
                }
                g.DrawRectangle(PPP, CurrX, CurrY, 繪圖格寬, 繪圖格高);
                if (Array[i].V.CompareTo("1") == 0)
                {
                    g.FillRectangle(Brushes.Gold, CurrX + 1, CurrY + 1, 繪圖格寬 - 2, 繪圖格高 - 2);
                }
                g.DrawString("[" + Array[i].CH + "] " + Array[i].N, FFF, Brushes.Black, CurrX + 5, CurrY + 5);
                CurrY += 繪圖格高 + 繪圖起始y;
            }
            DstImage.Refresh();
        }

        public void Draw_ORG_Chart(ref PictureBox Canvas, int x, int y)
        {
            xAmount = x;
            yAmount = y;
            Canvas.Image = null;
            Canvas.Image = new Bitmap(Canvas.Width, Canvas.Height);
            Graphics g = Graphics.FromImage(Canvas.Image);

            x_pitch = Convert.ToInt32(Canvas.Width / ((xAmount * 2) + 1));
            y_pitch = Convert.ToInt32(Canvas.Height / ((yAmount * 2) + 1));
            x_draw = x_pitch;
            y_draw = y_pitch;

            for (int j = 1; j <= y; j++)
            {
                g.DrawString(j.ToString(), font, Brushes.Black, 1, (float)y_draw);

                for (int i = 1; i <= x; i++)
                {
                    g.DrawString(i.ToString(), font, Brushes.Black, x_draw, 1);
                    Draw_Rect_By_Pos(Canvas, x_draw, y_draw, x_pitch, y_pitch, "", Brushes.DarkGray);
                    x_draw += x_pitch * 2;
                }
                y_draw += y_pitch * 2;
                x_draw = x_pitch;
            }

            g.Dispose();
        }

        public void Draw_Test_By_Index(PictureBox DstImage, int Y_index,
                              int X_index, Brush RectColor, string SS,
                              int TextX, int TextY, Brush TestColor)
        {
            g = Graphics.FromImage(DstImage.Image);

            x_draw = X_index * x_pitch * 2 - x_pitch;
            y_draw = Y_index * y_pitch * 2 - y_pitch;

            x_draw = x_draw - growX;
            y_draw = y_draw - growY;

            g.DrawRectangle(Pens.Black, x_draw, y_draw, x_pitch + (2 * growX), y_pitch + (2 * growY));
            g.FillRectangle(RectColor, x_draw + 1, y_draw + 1,
                            x_pitch - 2 + (2 * growX), y_pitch - 2 + (2 * growY));
            PointF aa = new PointF(x_draw + TextX, y_draw + TextY);
            g.DrawString(SS, font, TestColor, aa);

            g.Dispose();
        }


        public void Draw_Rect_By_Pos(PictureBox DstImage, int X_Pos,
                              int Y_Pos, int Width, int Height,
                                string Text, Brush RectColor)
        {

            Graphics g;
            g = Graphics.FromImage(DstImage.Image);
            g.DrawRectangle(Pens.Black, X_Pos - growX, Y_Pos - growY, Width + (growX * 2), Height + (growY * 2));
            g.FillRectangle(RectColor, X_Pos + 1, Y_Pos + 1, Width - 2, Height - 2);
            g.Dispose();
        }


        public void Draw_Rect_By_Index(PictureBox DstImage, int Y_index,
                              int X_index, Brush RectColor)
        {
            //Graphics g;
            g = Graphics.FromImage(DstImage.Image);

            // x_pitch = Convert.ToInt32(DstImage.Width / ((xAmount * 2) + 1));
            // y_pitch = Convert.ToInt32(DstImage.Height / ((yAmount * 2) + 1));
            x_draw = X_index * x_pitch * 2 - x_pitch;
            y_draw = Y_index * y_pitch * 2 - y_pitch;



            g.DrawRectangle(Pens.Black, x_draw, y_draw, x_pitch, y_pitch);
            g.FillRectangle(RectColor, x_draw + 1, y_draw + 1, x_pitch - 2, y_pitch - 2);
            g.Dispose();
        }
        public void Select_Product_Rect(PictureBox DstImage, int x, int y)
        {
            if ((xAmount == 0) || (yAmount == 0))
                return;

            //將原本選擇的方塊取消
            Target_One_Product(DstImage, selectMRK, selectSlice, Color.LemonChiffon);

            selectMRK = 0;
            selectSlice = 0;
            x_pitch = Convert.ToInt32(DstImage.Width / ((xAmount * 2) + 1));
            y_pitch = Convert.ToInt32(DstImage.Height / ((yAmount * 2) + 1));

            int XDiff = (x) % (x_pitch * 2);
            int YDiff = (y) % (y_pitch * 2);
            if ((XDiff > x_pitch) && (YDiff > y_pitch))
            {
                selectMRK = x / (x_pitch * 2) + 1;
                selectSlice = y / (y_pitch * 2) + 1;
                Target_One_Product(DstImage, selectMRK, selectSlice, Color.Blue);
            }
        }

        public void Target_One_Product(PictureBox DstImage, int xindex, int yindex, Color CC)
        {

            Graphics g = Graphics.FromImage(DstImage.Image);
            Pen PP = new Pen(CC);

            x_pitch = Convert.ToInt32(DstImage.Width / ((xAmount * 2) + 1));
            y_pitch = Convert.ToInt32(DstImage.Height / ((yAmount * 2) + 1));
            x_draw = xindex * x_pitch * 2 - x_pitch;
            y_draw = yindex * y_pitch * 2 - y_pitch;


            PP.Width = 3;
            g.DrawRectangle(PP, x_draw - 4, y_draw - 4, x_pitch + 8, y_pitch + 8);
            DstImage.Refresh();
            g.Dispose();
        }

        public void Draw_Circle(PictureBox DstImage, Brush TestColor,int Cwidth,int CHeight)
        {
            DstImage.BackColor = Color.LemonChiffon;
            DstImage.Image = new Bitmap(DstImage.Width, DstImage.Height);
            Pen pp = new Pen(TestColor, 3.0f);
            Rectangle rr = new Rectangle(1, 1, Cwidth, CHeight);
            g = Graphics.FromImage(DstImage.Image);

            g.FillEllipse(Brushes.PowderBlue, rr);
            
            g.Dispose();
        }
    }
}
