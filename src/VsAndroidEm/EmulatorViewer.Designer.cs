namespace VsAndroidEm
{
    partial class EmulatorViewer
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
            this.childContainer = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // childContainer
            // 
            this.childContainer.Location = new System.Drawing.Point(81, 45);
            this.childContainer.Name = "childContainer";
            this.childContainer.Size = new System.Drawing.Size(259, 212);
            this.childContainer.TabIndex = 0;
            // 
            // EmulatorViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.childContainer);
            this.Name = "EmulatorViewer";
            this.Size = new System.Drawing.Size(740, 675);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel childContainer;
    }
}
