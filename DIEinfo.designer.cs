namespace GT_WV100
{
    partial class DIEinfo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DIEinfo));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.axAxCanvas1 = new AxAxOvkBase.AxAxCanvas();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.axAxImageBW81 = new AxAxOvkBase.AxAxImageBW8();
            this.roi1 = new AxAxOvkBase.AxAxROIBW8();
            this.roi_PR = new AxAxOvkBase.AxAxROIBW8();
            this.img_work = new AxAxOvkBase.AxAxImageBW8();
            this.button3 = new System.Windows.Forms.Button();
            this.axAxImageStatistics1 = new AxAxOvkImage.AxAxImageStatistics();
            this.img_PR = new AxAxOvkBase.AxAxImageBW8();
            this.axAxImageCopier1 = new AxAxOvkImage.AxAxImageCopier();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.roi_temp = new AxAxOvkBase.AxAxROIBW8();
            this.button6 = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axAxCanvas1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axAxImageBW81)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.roi1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.roi_PR)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.img_work)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axAxImageStatistics1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.img_PR)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axAxImageCopier1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.roi_temp)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.roi_temp);
            this.groupBox1.Controls.Add(this.axAxImageCopier1);
            this.groupBox1.Controls.Add(this.axAxCanvas1);
            this.groupBox1.Controls.Add(this.img_PR);
            this.groupBox1.Controls.Add(this.axAxImageBW81);
            this.groupBox1.Controls.Add(this.axAxImageStatistics1);
            this.groupBox1.Controls.Add(this.roi1);
            this.groupBox1.Controls.Add(this.img_work);
            this.groupBox1.Controls.Add(this.roi_PR);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(834, 225);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "搜尋區";
            // 
            // axAxCanvas1
            // 
            this.axAxCanvas1.Location = new System.Drawing.Point(13, 24);
            this.axAxCanvas1.Name = "axAxCanvas1";
            this.axAxCanvas1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axAxCanvas1.OcxState")));
            this.axAxCanvas1.Size = new System.Drawing.Size(192, 192);
            this.axAxCanvas1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label1.Location = new System.Drawing.Point(35, 342);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "影像座標x";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label2.Location = new System.Drawing.Point(135, 342);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(16, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label3.Location = new System.Drawing.Point(135, 370);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(16, 16);
            this.label3.TabIndex = 4;
            this.label3.Text = "0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label4.Location = new System.Drawing.Point(35, 370);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 16);
            this.label4.TabIndex = 3;
            this.label4.Text = "影像座標y";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label9.Location = new System.Drawing.Point(35, 442);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(64, 16);
            this.label9.TabIndex = 9;
            this.label9.Text = "Die State";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label10.Location = new System.Drawing.Point(135, 442);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(16, 16);
            this.label10.TabIndex = 10;
            this.label10.Text = "0";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label11.Location = new System.Drawing.Point(135, 473);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(16, 16);
            this.label11.TabIndex = 12;
            this.label11.Text = "0";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label12.Location = new System.Drawing.Point(35, 473);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(72, 16);
            this.label12.TabIndex = 11;
            this.label12.Text = "定位狀態";
            // 
            // axAxImageBW81
            // 
            this.axAxImageBW81.Location = new System.Drawing.Point(489, 90);
            this.axAxImageBW81.Name = "axAxImageBW81";
            this.axAxImageBW81.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axAxImageBW81.OcxState")));
            this.axAxImageBW81.Size = new System.Drawing.Size(30, 30);
            this.axAxImageBW81.TabIndex = 13;
            this.axAxImageBW81.Visible = false;
            // 
            // roi1
            // 
            this.roi1.Location = new System.Drawing.Point(525, 90);
            this.roi1.Name = "roi1";
            this.roi1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("roi1.OcxState")));
            this.roi1.Size = new System.Drawing.Size(30, 30);
            this.roi1.TabIndex = 22;
            this.roi1.Visible = false;
            // 
            // roi_PR
            // 
            this.roi_PR.Location = new System.Drawing.Point(561, 90);
            this.roi_PR.Name = "roi_PR";
            this.roi_PR.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("roi_PR.OcxState")));
            this.roi_PR.Size = new System.Drawing.Size(30, 30);
            this.roi_PR.TabIndex = 23;
            this.roi_PR.Visible = false;
            // 
            // img_work
            // 
            this.img_work.Location = new System.Drawing.Point(489, 126);
            this.img_work.Name = "img_work";
            this.img_work.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("img_work.OcxState")));
            this.img_work.Size = new System.Drawing.Size(30, 30);
            this.img_work.TabIndex = 24;
            this.img_work.Visible = false;
            // 
            // button3
            // 
            this.button3.Font = new System.Drawing.Font("新細明體", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button3.Location = new System.Drawing.Point(573, 353);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(265, 79);
            this.button3.TabIndex = 25;
            this.button3.Text = "單排檢測";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // axAxImageStatistics1
            // 
            this.axAxImageStatistics1.Location = new System.Drawing.Point(525, 126);
            this.axAxImageStatistics1.Name = "axAxImageStatistics1";
            this.axAxImageStatistics1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axAxImageStatistics1.OcxState")));
            this.axAxImageStatistics1.Size = new System.Drawing.Size(30, 30);
            this.axAxImageStatistics1.TabIndex = 26;
            this.axAxImageStatistics1.Visible = false;
            // 
            // img_PR
            // 
            this.img_PR.Location = new System.Drawing.Point(562, 126);
            this.img_PR.Name = "img_PR";
            this.img_PR.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("img_PR.OcxState")));
            this.img_PR.Size = new System.Drawing.Size(30, 30);
            this.img_PR.TabIndex = 27;
            this.img_PR.Visible = false;
            // 
            // axAxImageCopier1
            // 
            this.axAxImageCopier1.Location = new System.Drawing.Point(562, 174);
            this.axAxImageCopier1.Name = "axAxImageCopier1";
            this.axAxImageCopier1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axAxImageCopier1.OcxState")));
            this.axAxImageCopier1.Size = new System.Drawing.Size(30, 30);
            this.axAxImageCopier1.TabIndex = 28;
            this.axAxImageCopier1.Visible = false;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label17.Location = new System.Drawing.Point(135, 509);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(16, 16);
            this.label17.TabIndex = 32;
            this.label17.Text = "0";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label18.Location = new System.Drawing.Point(35, 509);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(72, 16);
            this.label18.TabIndex = 31;
            this.label18.Text = "灰階總合";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label13.Location = new System.Drawing.Point(12, 251);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(88, 16);
            this.label13.TabIndex = 35;
            this.label13.Text = "MAP內資料";
            // 
            // roi_temp
            // 
            this.roi_temp.Location = new System.Drawing.Point(598, 90);
            this.roi_temp.Name = "roi_temp";
            this.roi_temp.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("roi_temp.OcxState")));
            this.roi_temp.Size = new System.Drawing.Size(30, 30);
            this.roi_temp.TabIndex = 37;
            this.roi_temp.Visible = false;
            // 
            // button6
            // 
            this.button6.Font = new System.Drawing.Font("新細明體", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button6.Location = new System.Drawing.Point(266, 353);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(265, 91);
            this.button6.TabIndex = 38;
            this.button6.Text = "清除結果";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(38, 279);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(759, 43);
            this.richTextBox1.TabIndex = 39;
            this.richTextBox1.Text = "";
            // 
            // DIEinfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(858, 542);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Name = "DIEinfo";
            this.Text = "DIEinfo";
            this.Load += new System.EventHandler(this.DIEinfo_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axAxCanvas1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axAxImageBW81)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.roi1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.roi_PR)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.img_work)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axAxImageStatistics1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.img_PR)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axAxImageCopier1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.roi_temp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private AxAxOvkBase.AxAxCanvas axAxCanvas1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private AxAxOvkBase.AxAxImageBW8 axAxImageBW81;
        private AxAxOvkBase.AxAxROIBW8 roi1;
        private AxAxOvkBase.AxAxROIBW8 roi_PR;
        private AxAxOvkBase.AxAxImageBW8 img_work;
        private System.Windows.Forms.Button button3;
        private AxAxOvkImage.AxAxImageStatistics axAxImageStatistics1;
        private AxAxOvkBase.AxAxImageBW8 img_PR;
        private AxAxOvkImage.AxAxImageCopier axAxImageCopier1;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label13;
        private AxAxOvkBase.AxAxROIBW8 roi_temp;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button button1;
    }
}