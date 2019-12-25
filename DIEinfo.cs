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
    public partial class DIEinfo : Form
    {
        public DIEinfo()
        {
            InitializeComponent();
        }        public DIEinfo(AxAxOvkBase.AxAxImageBW8 img,
                        int MAPindexX, int MAPindexY, CheckClass CKK, devClass DEVV, MF FFF)
        {
            InitializeComponent();
            axAxImageBW81.SetSurfaceObj(img.VegaHandle);
            indexX = MAPindexX;
            indexY = MAPindexY;
            CK = CKK;
            DEV = DEVV;
            ff = FFF;
        }

        int indexX = 0;
        int indexY = 0;
        CheckClass CK;
        devClass DEV;
        MF ff;
        int CurrX = 0;
        int CurrY = 0;
        float zoom = 1;
        CheckClass.DieInfoStruct[] rowData;

        private void DIEinfo_Load(object sender, EventArgs e)
        {
            int RowX = 0;// DEV.FstShtX - (ff.MAP.FstindexX * DEV.DieW) - 200;
            int RowW = (ff.MAP.Matrix.GetLength(1) * DEV.DieW) + 400;
            int RowH = (DEV.DieH);

            CurrY = CK.RowY_array[indexY];
            CurrX = CK.RowX_array[indexY];
            roi1.ParentHandle = ff.img_Globe.VegaHandle;
            roi1.SetPlacement(0,
                              CurrY - DEV.reCoAreaY,
                              ff.img_Globe.ImageWidth,
                              RowH + (2 * DEV.reCoAreaY));

            axAxImageCopier1.SrcImageHandle = roi1.VegaHandle;
            axAxImageCopier1.DstImageHandle = axAxImageBW81.VegaHandle;
            axAxImageCopier1.Copy();
            axAxCanvas1.Width = 800;
            axAxCanvas1.Height = 200;
            RefreshImage();

            label2.Text = indexX.ToString();
            label3.Text = indexY.ToString();
            rowData = new CheckClass.DieInfoStruct[ff.CK.Matrix.GetLength(1)];

            string MapRowString = "";
            bool intoMap = false;
            for (int i = 0; i <= ff.CK.Matrix.GetLength(1) - 1; i++)
            {
                if ((!DEV.NullList.Contains(ff.MAP.Matrix[indexY, i].MapString)) || (intoMap))
                {
                    intoMap = true;
                    MapRowString += ff.MAP.Matrix[indexY, i].MapString;
                    MapRowString += " | ";
                }
            }
            richTextBox1.AppendText(MapRowString);
        }

        //更新影像畫面
        public void RefreshImage()
        {
            axAxCanvas1.CanvasWidth = Convert.ToInt32(axAxImageBW81.ImageWidth * zoom);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(axAxImageBW81.ImageHeight * zoom);
            axAxImageBW81.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            if (rowData != null)
            {
                for (int x = 0; x <= ff.MAP.Matrix.GetLength(1) - 1; x++)
                {

                    if (rowData[x].CheckState == 0) continue;
                    if ((rowData[x].DieColor == 0) && (rowData[x].infoColor == 0)) continue;


                    if (x == ff.MAP.FstindexX)
                    {
                        roi_temp.Title = "起始位置";
                        roi_temp.ShowTitle = true;

                    }
                    else
                        roi_temp.ShowTitle = false;

                    roi_temp.SetPlacement(rowData[x].coordX,
                                                         rowData[x].coordY, DEV.DieW, DEV.DieH);
                    roi_temp.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, rowData[x].DieColor);

                    //if ( rowData[ x].infoState > 0)
                    //{
                    //    roi_unit.SetPlacement( rowData[ x].coordX + (DEV.DieW / 3),
                    //                                          rowData[ x].coordY + (DEV.DieH / 3),
                    //                                         DEV.DieW - (DEV.DieW * 2 / 3),
                    //                                         DEV.DieH - (DEV.DieH * 2 / 3));
                    //    roi_unit.DrawRect(axAxCanvas1.hDC, zoom2, zoom2, 0, 0,  rowData[ x].infoColor);
                    //}
                }
            }
            axAxCanvas1.RefreshCanvas();
        }
        
        public void ProcessRow()
        {
            for (int i = 0; i <= ff.MAP.Matrix.GetLength(1) - 1; i++)
            {
                rowData[i] = new CheckClass.DieInfoStruct();
            }
            //標定留料位置
            //取得第一顆中心位置

            int EPT_sideX = ((ff.mch_Ept.PatternWidth - DEV.DieW) / 2);
            int EPT_sideY = ((ff.mch_Ept.PatternHeight - DEV.DieH) / 2);

            roi_PR.ParentHandle = axAxImageBW81.VegaHandle;
            string MapString = "";
            CurrX = CK.RowX_array[indexY];
            //以第一點X位置當作起始位置
            //roi_PR.ParentHandle = img_Globe.VegaHandle;
            //向左

            for (int i = ff.MAP.FstindexX; i > 0; i--)
            {

                MapString = ff.MAP.Matrix[indexY, i].MapString;
                if (DEV.NullList.Contains(MapString))
                {
                    rowData[i].coordX = CurrX;
                    rowData[i].coordY = 0;
                    rowData[i].CheckState = 0;
                    goto NEXT_LEFT;
                }

                roi_PR.SetPlacement(CurrX - DEV.reCoAreaX,
                                    0,
                                    DEV.DieW + (2 * DEV.reCoAreaX),
                                    axAxImageBW81.ImageHeight);

                ff.mch_DEV.DstImageHandle = roi_PR.VegaHandle;
                ff.mch_DEV.Match();
                ff.mch_Ept.DstImageHandle = roi_PR.VegaHandle;
                ff.mch_Ept.Match();

                if (ff.mch_DEV.NumMatchedPos == 1)
                {
                    rowData[i].coordX = ff.mch_DEV.MatchedX;
                    rowData[i].coordY = ff.mch_DEV.MatchedY;
                    rowData[i].CheckState = 1;
                    rowData[i].DieState = 1;
                }
                else if ((ff.mch_Ept.NumMatchedPos == 1))//&&((Math.Abs( mch_Ept.MatchedY -CurrY)<5 )))
                {
                    rowData[i].coordX = ff.mch_Ept.MatchedX + EPT_sideX;
                    rowData[i].coordY = ff.mch_Ept.MatchedY + EPT_sideY;
                    rowData[i].CheckState = 1;
                    rowData[i].DieState = 0;

                }
                else// 兩種樣本都定位失敗 進行有無料判定
                {
                    rowData[i].coordX = CurrX;
                    rowData[i].coordY = 0;
                    rowData[i].CheckState = 0;
                    //   if (i == ff.MAP.FstindexX)
                    // rowData[i].CheckState = 1;
                }
            NEXT_LEFT:
                CurrX = rowData[i].coordX - DEV.DieW;
            }

            //向右
            CurrX = CK.RowX_array[indexY] + DEV.DieW;
            for (int i = ff.MAP.FstindexX + 1; i <= ff.MAP.Matrix.GetLength(1) - 1; i++)
            {
                MapString = ff.MAP.Matrix[indexY, i].MapString;
                if (DEV.NullList.Contains(MapString))
                {
                    rowData[i].coordX = CurrX;
                    rowData[i].coordY = 0;
                    rowData[i].CheckState = 0;
                    goto NEXT_REIGHT;
                }

                roi_PR.SetPlacement(CurrX - DEV.reCoAreaX,
                                    0,
                                    DEV.DieW + (2 * DEV.reCoAreaX),
                                    axAxImageBW81.ImageHeight);

                ff.mch_DEV.DstImageHandle = roi_PR.VegaHandle;
                ff.mch_DEV.Match();
                ff.mch_Ept.DstImageHandle = roi_PR.VegaHandle;
                ff.mch_Ept.Match();


                if (ff.mch_DEV.NumMatchedPos == 1)
                {
                    rowData[i].coordX = ff.mch_DEV.MatchedX;
                    rowData[i].coordY = ff.mch_DEV.MatchedY;
                    rowData[i].CheckState = 1;
                    rowData[i].DieState = 1;

                }
                else if ((ff.mch_Ept.NumMatchedPos == 1))//&&((Math.Abs( mch_Ept.MatchedY -CurrY)<5 )))
                {
                    rowData[i].coordX = ff.mch_Ept.MatchedX + EPT_sideX;
                    rowData[i].coordY = ff.mch_Ept.MatchedY + EPT_sideY;
                    rowData[i].CheckState = 1;
                    rowData[i].DieState = 0;
                }
                else// 兩種樣本都定位失敗 進行有無料判定
                {

                    rowData[i].coordX = CurrX;
                    rowData[i].coordY = 0;
                    rowData[i].CheckState = 0;
                }
            NEXT_REIGHT:
                CurrX = rowData[i].coordX + DEV.DieW;
            }

            //定位失敗者導正
            roi_temp.ShowPlacement = false;
            roi_temp.ParentHandle = axAxImageBW81.VegaHandle;
            for (int i = 0; i <= ff.MAP.Matrix.GetLength(1) - 1; i++)
            {
                if (rowData[i].CheckState == 1)
                {
                    int k = i - 1;
                    while ((k >= 0) && (rowData[k].CheckState == 0))
                    {
                        MapString = ff.MAP.Matrix[indexY, k].MapString;
                        if (DEV.NullList.Contains(MapString))
                            break;

                        rowData[k].coordX = rowData[k + 1].coordX - DEV.DieW;
                        rowData[k].coordY = rowData[k + 1].coordY;

                        //向內收縮
                        roi_temp.SetPlacement(rowData[k].coordX + (DEV.DieW / 3),
                                              rowData[k].coordY + (DEV.DieH / 3),
                                              DEV.DieW - (DEV.DieW * 2 / 3),
                                              DEV.DieH - (DEV.DieH * 2 / 3));

                        rowData[k].infoColor = 0xFA0000;
                        rowData[k].infoState = 1;

                        roi_temp.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xFA);
                        
                        axAxImageStatistics1.SrcImageHandle = roi_temp.VegaHandle;
                        //axAxImageStatistics1.CalculateMean = true;
                        axAxImageStatistics1.GetStatistics();

                        if (axAxImageStatistics1.BlueMean > DEV.GrayTotalTH)//空白
                            rowData[k].DieState = 0;
                        else
                            rowData[k].DieState = 1;
                        rowData[k].VAR = Convert.ToInt32(axAxImageStatistics1.BlueMean);
                        rowData[k].CheckState = 2;
                        k--;
                    }

                    k = i + 1;
                    while ((k <= ff.MAP.Matrix.GetLength(1) - 1) && (rowData[k].CheckState == 0))
                    {
                        MapString = ff.MAP.Matrix[indexY, k].MapString;

                        if (DEV.NullList.Contains(MapString))
                            break;

                        rowData[k].coordX = rowData[k - 1].coordX + DEV.DieW;
                        rowData[k].coordY = rowData[k - 1].coordY;

                        roi_temp.SetPlacement(rowData[k].coordX + (DEV.DieW / 3),
                                              rowData[k].coordY + (DEV.DieH / 3),
                                              DEV.DieW - (DEV.DieW * 2 / 3),
                                              DEV.DieH - (DEV.DieH * 2 / 3));

                        rowData[k].infoColor = 0x0000FA;
                        rowData[k].infoState = 1;
                        roi_temp.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, 0xFA);
                        axAxImageStatistics1.SrcImageHandle = roi_temp.VegaHandle;
                        // axAxImageStatistics1.CalculateMean = true;
                        axAxImageStatistics1.GetStatistics();

                        if (axAxImageStatistics1.BlueMean > DEV.GrayTotalTH)//空白
                            rowData[k].DieState = 0;
                        else
                            rowData[k].DieState = 1;
                        rowData[k].VAR = Convert.ToInt32(axAxImageStatistics1.BlueMean);
                        rowData[k].CheckState = 2;
                        k++;
                    }
                }
            }

            for (int index = 0; index <= ff.MAP.Matrix.GetLength(1) - 1; index++)
            {
                if (rowData[index].CheckState == 0) continue;
                //CK.Totalnum++;
                MapString = ff.MAP.Matrix[indexY, index].MapString;
                if (!DEV.BinList.Contains(MapString) && (rowData[index].DieState == 1))//此位置為不良品 應該留料
                {
                    //壞料留下 畫出綠框
                    rowData[index].DieColor = ff.BAD_OC;
                    CK.NGDIEnum++;
                }
                else if (DEV.BinList.Contains(MapString) && (rowData[index].DieState == 1))//
                {
                    //好料未取走 畫出黃框
                    rowData[index].DieColor = ff.GOOD_OC;
                    CK.OKDIEnum++;
                }
                else if (!DEV.BinList.Contains(MapString) && (rowData[index].DieState == 0))//不良品應該留料卻空料
                {
                    //壞料被取走 畫出紅框
                    rowData[index].DieColor = ff.BAD_Ept;
                    CK.NGEPTnum++;
                }
                else   //良品被取走 藍色正常
                {
                    rowData[index].DieColor = ff.GOOD_Ept;
                    CK.OKEPTnum++;
                }
            }

            //**************顯示檢測結果
            if (rowData != null)
            {
                for (int x = 0; x <= ff.MAP.Matrix.GetLength(1) - 1; x++)
                {

                    if (rowData[x].CheckState == 0) continue;
                    if ((rowData[x].DieColor == 0) && (rowData[x].infoColor == 0)) continue;


                    if (x == ff.MAP.FstindexX)
                    {
                        roi_temp.Title = "起始位置";
                        roi_temp.ShowTitle = true;
                    }
                    else
                        roi_temp.ShowTitle = false;

                    roi_temp.SetPlacement(rowData[x].coordX,
                                                         rowData[x].coordY, DEV.DieW, DEV.DieH);
                    roi_temp.DrawRect(axAxCanvas1.hDC, zoom, zoom, 0, 0, rowData[x].DieColor);
                }
            }

            axAxCanvas1.RefreshCanvas();
            //  RefreshImage();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            axAxImageBW81.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            axAxCanvas1.RefreshCanvas();
            ProcessRow();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            axAxImageBW81.DrawImage(axAxCanvas1.hDC, zoom, zoom, 0, 0);
            axAxCanvas1.RefreshCanvas();
        }
        
    }
}