using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Motion_W32;

namespace GT_WV100
{
    public partial class WARNING : Form
    {
        public WARNING()
        {
            InitializeComponent();
        }

        public WARNING(MF temp)
        {
            InitializeComponent();
            ff = temp;
        }

        MF ff;
        private void button1_Click(object sender, EventArgs e)
        {
            ff.ST.續做模式 = 1;//載台有料續做
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ff.ST.續做模式 = 2;//載台無料續做
            this.Close();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            int IDX = Convert.ToInt32(textBox7.Text);
            //走到料格高度
            Motion._8164_start_ta_move(2, ff.CASArray[IDX].軌道格位高度,
                                    ff.MV.彈匣初速, ff.MV.彈匣常速, 0.1, 0.1);

            while (Motion._8164_motion_done(2) > 0)
                Application.DoEvents();

            ff.ST.當前格位 = IDX;
        }
    }
}
