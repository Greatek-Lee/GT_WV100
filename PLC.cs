using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GT_WV100;


namespace GT_PLC
{
    public class PLC
    {
        public string writecj_bit(ref System.IO.Ports.SerialPort SPort, string relay_no, string bit_value, string state)
        {
            string plc_station_head = "@00FA0";
            string ICF = "00";
            string DA2 = "00";
            string SA2 = "00";
            string SID = "00";
            string com = "0102";
            string e = "";


            int b = relay_no.Length;
            for (int c = 0; c <= b - 1; c++)
            {
                string d = relay_no.Substring(c, 1);
                double aa;
                bool mycheck = double.TryParse(d, System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo, out aa);
                if (mycheck) e = e + d;
            }


            string com_wr = "";
            string x_type = relay_no.Substring(0, 1);
            switch (x_type)
            {
                case ("W"):
                    com_wr = "31";
                    break;
                case ("H"):
                    com_wr = "32";
                    break;
                default:
                    break;
            }


            int kk = Convert.ToInt32(e);
            string F = System.Convert.ToString(kk, 16);
            string a = "";
            int buf;
            buf = F.Length;
            if (buf <= 4)
            {
                switch (buf)
                {
                    case (1):
                        a = "000" + F;
                        break;
                    case (2):
                        a = "00" + F;
                        break;
                    case (3):
                        a = "0" + F;
                        break;
                    case (4):
                        a = F;
                        break;
                    default:
                        break;
                }
            }

            kk = Convert.ToInt32(bit_value);
            F = System.Convert.ToString(kk, 16);

            string bit = "0" + F;
            string ch = "0001";
            string t = plc_station_head + ICF + DA2 + SA2 + SID + com + com_wr + a + bit + ch + state;
            string fcschk = FCS(t);
            fcschk = fcschk.ToUpper();
            string SXD = t + fcschk + "*";

            string RXD = send_recieve(ref SPort, SXD, 0, 1);
            return RXD;
        }

        public string FCS(string t)
        {
            int L = t.Length;
            int a = 0;
            for (int j = 0; j <= L - 1; j++)
            {
                string tj = t.Substring(j, 1);
                int ttj = Convert.ToInt32(tj[0]);
                a = ttj ^ a;


            }
            string fcsx = Convert.ToString(a, 16);
            if (fcsx.Length == 1)
            {
                fcsx = "0" + fcsx;

            }
            return fcsx;
        }

        public string WriteDW(ref System.IO.Ports.SerialPort SPort, string data, string nch1, string nch2)
        {
            int buf = data.Length;

            string data1 = data.Substring(4, data.Length - 4);
            string data2;
            int buf1 = data1.Length;
            int buf2;
            if (buf > 4)
            {
                data2 = data.Substring(0, 4);
                buf2 = data2.Length;


            }
            else
            {
                buf2 = 4;
                data2 = "0000";

            }

            if (buf1 <= 4)
            {
                switch (buf1)
                {
                    case (1):
                        {
                            data1 = "000" + data1;
                            break;
                        }
                    case (2):
                        {
                            data1 = "00" + data1;
                            break;
                        }
                    case (3):
                        {
                            data1 = "0" + data1;
                            break;
                        }
                    case (4):
                        {
                            break;
                        }
                    default:
                        return "0000";
                }
            }
            else
            {
                return "0000";
            }


            if (buf2 <= 4)
            {
                switch (buf2)
                {
                    case (1):
                        {
                            data2 = "000" + data2;
                            return (Write_nch_OMRON(ref SPort, nch2, data2) + Write_nch_OMRON(ref SPort, nch1, data1));
                        }
                    case (2):
                        {
                            data2 = "00" + data2;
                            return (Write_nch_OMRON(ref SPort, nch2, data2) + Write_nch_OMRON(ref SPort, nch1, data1));
                        }
                    case (3):
                        {
                            data2 = "0" + data2;
                            return (Write_nch_OMRON(ref SPort, nch2, data2) + Write_nch_OMRON(ref SPort, nch1, data1));
                        }
                    case (4):
                        {
                            return (Write_nch_OMRON(ref SPort, nch2, data2) + Write_nch_OMRON(ref SPort, nch1, data1));
                        }
                    default:
                        return "0000";
                }
            }
            else
            {
                return "0000";
            }
        }


        public string Write_nch_OMRON(ref System.IO.Ports.SerialPort sPort, string start_ch, string ch_data)
        {
            string plc_station_head = "@00";
            int alen = start_ch.Length;
            string x_type = start_ch.Substring(0, 2);
            string x_ch = start_ch.Substring(2, alen - 2);
            x_ch = "0000" + x_ch;
            x_ch = x_ch.Substring(x_ch.Length - 4, 4);
            int ch_no = ch_data.Length / 4;
            string com = "";
            switch (x_type)
            {
                case ("IR"):
                    com = "WR";
                    break;
                case ("HR"):
                    com = "WH";
                    break;
                case ("AR"):
                    com = "WJ";
                    break;
                case ("DM"):
                    com = "WD";
                    break;
                case ("TC"):
                    com = "WC";
                    break;
                default:
                    break;

            }

            string t = plc_station_head + com + x_ch + ch_data;
            string fcschk = FCS(t);
            fcschk = fcschk.ToUpper();
            string SXD = t + fcschk + "*";
            string RXD = send_recieve(ref sPort, SXD, ch_no, 1);
            return RXD;

        }

        public string send_recieve(ref System.IO.Ports.SerialPort sPort, string SXD, int ch_no, int waitBack)
        {
            //serialPort1.rese();
            string RXD = "";
            sPort.Write(SXD + Convert.ToChar(13).ToString());
            System.Threading.Thread.Sleep(50);
            string TempChar = sPort.ReadExisting();

            RXD = RXD + TempChar;
            int a = 0;
            a = TempChar.IndexOf("@00FA");
            if (a != 0)
            {
                sPort.Close();
                System.Threading.Thread.Sleep(50);
                sPort.Open();
                sPort.Write(SXD + Convert.ToChar(13).ToString());
                System.Threading.Thread.Sleep(20);
                TempChar = sPort.ReadExisting();
                RXD = "-1";
                return RXD;

            }

            while (RXD.IndexOf("\r") == -1)
            {
                TempChar = sPort.ReadExisting();
                RXD = RXD + TempChar;
                System.Threading.Thread.Sleep(1);
                a = a + 1;
                if (a > 1000) break;

            }


            return RXD;

        }

        public string write_nch_Panasonic(ref System.IO.Ports.SerialPort SPort, string start_ch, string end_ch, string ch_data)
        {
            string x_type;
            string x_ch;
            int alen = start_ch.Length;
            string com = "";
            int ch_no;
            string t;
            string SXD;
            x_type = start_ch.Substring(0, 1);
            x_ch = start_ch.Substring(1, alen - 1);
            //string write_nch;
            string PLC_station_head = "%01#";

            if (x_type.CompareTo("D") != 0)
            {
                x_ch = "0000" + x_ch;
                x_ch = x_ch.Substring(end_ch.Length - 4, 4);

                end_ch = "0000" + end_ch;
                end_ch = end_ch.Substring(end_ch.Length - 4, 4);


            }
            else
            {
                x_ch = "0000" + x_ch;
                x_ch = x_ch.Substring(x_ch.Length - 5, 5);

                end_ch = "0000" + end_ch;
                end_ch = end_ch.Substring(end_ch.Length - 5, 5);

            }


            ch_no = ch_data.Length / 4;
            switch (x_type)
            {
                case ("R"):
                    com = "WCCR";
                    break;
                case ("D"):
                    com = "WDD";
                    break;

                default:
                    break;
            }
            t = PLC_station_head + com + x_ch + end_ch + ch_data;

            SXD = t + "**";
            send_recieve(ref SPort, SXD, ch_no, 1);
            return "";
        }
        public string read_nch(ref System.IO.Ports.SerialPort sPort, string start_ch, int n)
        {
            string plc_station_head = "@00FA0";
            string ICF = "00";
            string DA2 = "00";
            string SA2 = "00";
            string SID = "00";
            string com = "0101";
            int ch_no = 0;
            string ch = "";
            string e = "";
            int buf;
            int b = start_ch.Length;
            for (int c = 0; c <= b - 1; c++)
            {
                string d = start_ch.Substring(c, 1);
                double aa;
                bool mycheck = double.TryParse(d, System.Globalization.NumberStyles.Any,
                    System.Globalization.NumberFormatInfo.InvariantInfo, out aa);
                if (mycheck) e = e + d;
            }
            int kk = Convert.ToInt32(e);
            string F = System.Convert.ToString(kk, 16);
            string a = "";
            buf = F.Length;
            if (buf <= 4)
            {
                switch (buf)
                {

                    case (1):
                        a = "000" + F;
                        break;
                    case (2):
                        a = "00" + F;
                        break;
                    case (3):
                        a = "0" + F;
                        break;
                    case (4):
                        a = F;
                        break;
                    default:

                        break;

                }
            }

            F = Convert.ToString(n, 16);
            buf = F.Length;
            if (buf <= 4)
            {
                switch (buf)
                {

                    case (1):
                        ch = "000" + F;
                        break;
                    case (2):
                        ch = "00" + F;
                        break;
                    case (3):
                        ch = "0" + F;
                        break;
                    case (4):
                        ch = F;
                        break;
                    default:

                        break;

                }
            }

            string com_wr = "";
            string x_type = start_ch.Substring(0, 1);
            switch (x_type)
            {
                case ("W"):
                    com_wr = "B1";
                    break;
                case ("D"):
                    com_wr = "82";
                    break;
                default:
                    break;
            }
            string t = plc_station_head + ICF + DA2 + SA2 + SID + com + com_wr + a + "00" + ch;
            string fcschk = FCS(t);
            string SXD = t + fcschk + "*";
            string RXD = send_recieve(ref sPort, SXD, ch_no, 1);
            if (RXD == "-1")
            {
                return "";
            }
            string xbyte = RXD.Substring(23, n * 4);
            return xbyte;
        }

        //public string send_recieve(ref System.IO.Ports.SerialPort SPort ,string SXD, int ch_no, int waitBack)
        //{
        //    //serialPort1.rese();
        //    string RXD = "";
        //    SPort.Write(SXD + Convert.ToChar(13).ToString());
        //    System.Threading.Thread.Sleep(50);
        //    string TempChar = SPort.ReadExisting();

        //    RXD = RXD + TempChar;
        //    int a = 0;
        //    a = TempChar.IndexOf("@00FA");
        //    if (a != 0)
        //    {
        //        SPort.Close();
        //        System.Threading.Thread.Sleep(50);
        //        SPort.Open();
        //        SPort.Write(SXD + Convert.ToChar(13).ToString());
        //        System.Threading.Thread.Sleep(20);
        //        TempChar = SPort.ReadExisting();
        //        RXD = "-1";
        //        return RXD;

        //    }

        //    while (RXD.IndexOf("\r") == -1)
        //    {
        //        TempChar = SPort.ReadExisting();
        //        RXD = RXD + TempChar;
        //        System.Threading.Thread.Sleep(1);
        //        a = a + 1;
        //        if (a > 1000) break;

        //    }


        //    return RXD;

        //}


        public string trans_OMRON(int Sub)
        {
            string sub16 = System.Convert.ToString(Sub, 16); //十進位轉換十六進位


            switch (sub16.Length)
            {
                case (1):
                    sub16 = "000" + sub16;
                    break;
                case (2):
                    sub16 = "00" + sub16;
                    break;
                case (3):
                    sub16 = "0" + sub16;
                    break;
                default:
                    break;

            }

            return sub16;
        }



        public string trans_Panasonic(int Sub)
        {
            string sub16 = System.Convert.ToString(Sub, 16); //十進位轉換十六進位
            switch (sub16.Length)
            {
                case (1):
                    sub16 = "000" + sub16;
                    break;
                case (2):
                    sub16 = "00" + sub16;
                    break;
                case (3):
                    sub16 = "0" + sub16;
                    break;
                default:
                    break;

            }
            string Upchr, DownChr;

            Upchr = sub16.Substring(2, 2);
            DownChr = sub16.Substring(0, 2);
            sub16 = Upchr + DownChr;
            sub16 = sub16 + "0000";
            return sub16;
        }





    }
}
