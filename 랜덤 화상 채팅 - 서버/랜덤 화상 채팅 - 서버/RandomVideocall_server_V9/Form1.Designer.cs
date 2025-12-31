
namespace RandomVideocall_V2
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            panel1 = new Panel();
            button1 = new Button();
            panel2 = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel3 = new TableLayoutPanel();
            label3 = new Label();
            tableLayoutPanel2 = new TableLayoutPanel();
            panel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.BackColor = Color.White;
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Dock = DockStyle.Fill;
            textBox1.Font = new Font("Noto Sans KR", 13.7999992F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox1.Location = new Point(3, 39);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(171, 27);
            textBox1.TabIndex = 0;
            // 
            // textBox2
            // 
            textBox2.BackColor = Color.White;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Dock = DockStyle.Fill;
            textBox2.Font = new Font("Noto Sans KR", 13.7999992F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox2.Location = new Point(180, 39);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(171, 27);
            textBox2.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Noto Sans KR Medium", 13.7999992F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(171, 36);
            label1.TabIndex = 2;
            label1.Text = "IP Address";
            label1.TextAlign = ContentAlignment.BottomLeft;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Bottom;
            label2.Font = new Font("Noto Sans KR Medium", 13.7999992F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label2.Location = new Point(180, 9);
            label2.Name = "label2";
            label2.Size = new Size(171, 27);
            label2.TabIndex = 3;
            label2.Text = "PORT";
            // 
            // panel1
            // 
            panel1.BackColor = Color.FloralWhite;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Dock = DockStyle.Fill;
            panel1.Font = new Font("Noto Sans KR", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            panel1.Location = new Point(3, 31);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(5, 4, 5, 4);
            panel1.Size = new Size(348, 249);
            panel1.TabIndex = 4;
            // 
            // button1
            // 
            button1.BackColor = Color.White;
            button1.Dock = DockStyle.Top;
            button1.Font = new Font("Noto Sans KR Medium", 16.1999989F, FontStyle.Bold, GraphicsUnit.Point, 129);
            button1.Location = new Point(3, 373);
            button1.Name = "button1";
            button1.Size = new Size(352, 59);
            button1.TabIndex = 5;
            button1.Text = "서버 시작";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click_1;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(254, 249, 241);
            panel2.BackgroundImageLayout = ImageLayout.Stretch;
            panel2.Controls.Add(tableLayoutPanel1);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(39, 38);
            panel2.Margin = new Padding(0);
            panel2.MinimumSize = new Size(327, 195);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(14);
            panel2.Size = new Size(386, 491);
            panel2.TabIndex = 7;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.BackgroundImageLayout = ImageLayout.Center;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 257F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 71F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 0, 1);
            tableLayoutPanel1.Controls.Add(button1, 0, 2);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(14, 14);
            tableLayoutPanel1.Margin = new Padding(2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.Size = new Size(358, 463);
            tableLayoutPanel1.TabIndex = 8;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(label3, 0, 0);
            tableLayoutPanel3.Controls.Add(panel1, 0, 1);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(2, 85);
            tableLayoutPanel3.Margin = new Padding(2);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            tableLayoutPanel3.Size = new Size(354, 283);
            tableLayoutPanel3.TabIndex = 0;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Fill;
            label3.Font = new Font("Noto Sans KR Medium", 13.7999992F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label3.Location = new Point(3, 0);
            label3.Name = "label3";
            label3.Size = new Size(348, 28);
            label3.TabIndex = 5;
            label3.Text = "Log";
            label3.TextAlign = ContentAlignment.BottomLeft;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
            tableLayoutPanel2.Controls.Add(label1, 0, 0);
            tableLayoutPanel2.Controls.Add(textBox2, 1, 1);
            tableLayoutPanel2.Controls.Add(textBox1, 0, 1);
            tableLayoutPanel2.Controls.Add(label2, 1, 0);
            tableLayoutPanel2.Dock = DockStyle.Top;
            tableLayoutPanel2.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            tableLayoutPanel2.Location = new Point(2, 2);
            tableLayoutPanel2.Margin = new Padding(2);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(354, 72);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(254, 250, 241);
            BackgroundImage = Properties.Resources.은은한_그림자;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(464, 567);
            Controls.Add(panel2);
            DoubleBuffered = true;
            MinimumSize = new Size(377, 280);
            Name = "Form1";
            Padding = new Padding(39, 38, 39, 38);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            panel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox textBox1;
        private TextBox textBox2;
        private Label label1;
        private Label label2;
        private Panel panel1;
        private Button button1;
        private Panel panel2;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private Label label3;
    }
}

