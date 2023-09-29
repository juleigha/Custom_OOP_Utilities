namespace Intranet_Forms.IntranetApp.Bases
{
    partial class FormBuilder
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox = new System.Windows.Forms.FlowLayoutPanel();
            this.consoleView = new System.Windows.Forms.FlowLayoutPanel();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // groupBox
            // 
            this.groupBox.AutoScroll = true;
            this.groupBox.AutoScrollMargin = new System.Drawing.Size(0, 10);
            this.groupBox.AutoScrollMinSize = new System.Drawing.Size(0, 332);
            this.groupBox.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.groupBox.Location = new System.Drawing.Point(13, 170);
            this.groupBox.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox.Name = "groupBox";
            this.groupBox.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.groupBox.Size = new System.Drawing.Size(770, 331);
            this.groupBox.TabIndex = 1;
            // 
            // consoleView
            // 
            this.consoleView.AutoScroll = true;
            this.consoleView.Location = new System.Drawing.Point(13, 12);
            this.consoleView.Name = "consoleView";
            this.consoleView.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.consoleView.Size = new System.Drawing.Size(770, 151);
            this.consoleView.TabIndex = 2;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // FormBuilder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScrollMargin = new System.Drawing.Size(0, 30);
            this.ClientSize = new System.Drawing.Size(803, 551);
            this.Controls.Add(this.groupBox);
            this.Controls.Add(this.consoleView);
            this.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(819, 700);
            this.Name = "FormBuilder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel groupBox;
        private System.Windows.Forms.FlowLayoutPanel consoleView;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}
