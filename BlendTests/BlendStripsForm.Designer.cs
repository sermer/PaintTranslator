namespace PaintTranslator.BlendTests
{
    partial class BlendStripsForm
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
            this.stripsFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            //
            // stripsFlowPanel
            //
            this.stripsFlowPanel.AutoScroll = true;
            this.stripsFlowPanel.BackColor = System.Drawing.Color.White;
            this.stripsFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stripsFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.stripsFlowPanel.Location = new System.Drawing.Point(0, 0);
            this.stripsFlowPanel.Name = "stripsFlowPanel";
            this.stripsFlowPanel.Size = new System.Drawing.Size(684, 761);
            this.stripsFlowPanel.TabIndex = 0;
            this.stripsFlowPanel.WrapContents = false;
            //
            // BlendStripsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 761);
            this.Controls.Add(this.stripsFlowPanel);
            this.Name = "BlendStripsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Paint Blend Gradient Strips";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel stripsFlowPanel;
    }
}
