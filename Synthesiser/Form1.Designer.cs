namespace Synthesiser
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
            txtFundamental = new TextBox();
            label1 = new Label();
            label2 = new Label();
            txtDuration = new TextBox();
            btnPlay = new Button();
            SuspendLayout();
            // 
            // txtFundamental
            // 
            txtFundamental.Location = new Point(80, 23);
            txtFundamental.Name = "txtFundamental";
            txtFundamental.Size = new Size(88, 23);
            txtFundamental.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 26);
            label1.Name = "label1";
            label1.Size = new Size(33, 15);
            label1.TabIndex = 1;
            label1.Text = "Note";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(201, 26);
            label2.Name = "label2";
            label2.Size = new Size(53, 15);
            label2.TabIndex = 3;
            label2.Text = "Duration";
            // 
            // txtDuration
            // 
            txtDuration.Location = new Point(260, 23);
            txtDuration.Name = "txtDuration";
            txtDuration.Size = new Size(88, 23);
            txtDuration.TabIndex = 2;
            // 
            // btnPlay
            // 
            btnPlay.Location = new Point(364, 22);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(75, 23);
            btnPlay.TabIndex = 4;
            btnPlay.Text = "Play";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(451, 451);
            Controls.Add(btnPlay);
            Controls.Add(label2);
            Controls.Add(txtDuration);
            Controls.Add(label1);
            Controls.Add(txtFundamental);
            Margin = new Padding(2, 1, 2, 1);
            Name = "Form1";
            Text = "Instrument Synthesiser";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtFundamental;
        private Label label1;
        private Label label2;
        private TextBox txtDuration;
        private Button btnPlay;
    }
}