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
            this.toolContainer = new System.Windows.Forms.Panel();
            this.emulatorContainer = new System.Windows.Forms.Panel();
            this.emulatorContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // childContainer
            // 
            this.childContainer.Location = new System.Drawing.Point(46, 38);
            this.childContainer.Name = "childContainer";
            this.childContainer.Size = new System.Drawing.Size(259, 212);
            this.childContainer.TabIndex = 0;
            // 
            // toolContainer
            // 
            this.toolContainer.BackColor = System.Drawing.Color.Transparent;
            this.toolContainer.Dock = System.Windows.Forms.DockStyle.Right;
            this.toolContainer.Location = new System.Drawing.Point(740, 0);
            this.toolContainer.Name = "toolContainer";
            this.toolContainer.Size = new System.Drawing.Size(0, 675);
            this.toolContainer.TabIndex = 1;
            // 
            // emulatorContainer
            // 
            this.emulatorContainer.BackColor = System.Drawing.Color.Transparent;
            this.emulatorContainer.Controls.Add(this.childContainer);
            this.emulatorContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.emulatorContainer.Location = new System.Drawing.Point(0, 0);
            this.emulatorContainer.Name = "emulatorContainer";
            this.emulatorContainer.Size = new System.Drawing.Size(740, 675);
            this.emulatorContainer.TabIndex = 2;
            // 
            // EmulatorViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.emulatorContainer);
            this.Controls.Add(this.toolContainer);
            this.Name = "EmulatorViewer";
            this.Size = new System.Drawing.Size(740, 675);
            this.emulatorContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel childContainer;
        private System.Windows.Forms.Panel toolContainer;
        private System.Windows.Forms.Panel emulatorContainer;
    }
}
