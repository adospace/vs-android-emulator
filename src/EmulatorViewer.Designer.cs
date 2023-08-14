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
            this.childInternalContainer = new System.Windows.Forms.Panel();
            this.childContainer.SuspendLayout();
            this.emulatorContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // childContainer
            // 
            this.childContainer.BackColor = System.Drawing.Color.Transparent;
            this.childContainer.Controls.Add(this.childInternalContainer);
            this.childContainer.Location = new System.Drawing.Point(46, 38);
            this.childContainer.Name = "childContainer";
            this.childContainer.Size = new System.Drawing.Size(453, 425);
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
            // childInternalContainer
            // 
            this.childInternalContainer.BackColor = System.Drawing.Color.Transparent;
            this.childInternalContainer.Location = new System.Drawing.Point(86, 57);
            this.childInternalContainer.Name = "childInternalContainer";
            this.childInternalContainer.Size = new System.Drawing.Size(200, 100);
            this.childInternalContainer.TabIndex = 0;
            // 
            // EmulatorViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.emulatorContainer);
            this.Controls.Add(this.toolContainer);
            this.Name = "EmulatorViewer";
            this.Size = new System.Drawing.Size(740, 675);
            this.childContainer.ResumeLayout(false);
            this.emulatorContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel childContainer;
        private System.Windows.Forms.Panel toolContainer;
        private System.Windows.Forms.Panel emulatorContainer;
        private System.Windows.Forms.Panel childInternalContainer;
    }
}
