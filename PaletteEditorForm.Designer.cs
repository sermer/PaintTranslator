namespace PaintTranslator
{
    partial class PaletteEditorForm
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
            this.selectAllCheckBox = new System.Windows.Forms.CheckBox();
            this.allPaintsCheckedListBox = new PaintTranslator.Controls.PaintCheckedListBox();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // selectAllCheckBox
            //
            this.selectAllCheckBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.selectAllCheckBox.Location = new System.Drawing.Point(0, 0);
            this.selectAllCheckBox.Name = "selectAllCheckBox";
            this.selectAllCheckBox.Padding = new System.Windows.Forms.Padding(6, 2, 0, 0);
            this.selectAllCheckBox.Size = new System.Drawing.Size(340, 24);
            this.selectAllCheckBox.TabIndex = 0;
            this.selectAllCheckBox.Text = "Select all";
            this.selectAllCheckBox.UseVisualStyleBackColor = true;
            this.selectAllCheckBox.CheckedChanged += new System.EventHandler(this.SelectAllCheckBox_CheckedChanged);
            //
            // allPaintsCheckedListBox
            //
            this.allPaintsCheckedListBox.CheckOnClick = true;
            this.allPaintsCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.allPaintsCheckedListBox.IntegralHeight = false;
            this.allPaintsCheckedListBox.Location = new System.Drawing.Point(0, 24);
            this.allPaintsCheckedListBox.Name = "allPaintsCheckedListBox";
            this.allPaintsCheckedListBox.Size = new System.Drawing.Size(340, 450);
            this.allPaintsCheckedListBox.TabIndex = 1;
            this.allPaintsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.AllPaintsCheckedListBox_ItemCheck);
            //
            // buttonPanel
            //
            this.buttonPanel.Controls.Add(this.okButton);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 474);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(340, 46);
            this.buttonPanel.TabIndex = 2;
            //
            // okButton
            //
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point(166, 8);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 30);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(252, 8);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 30);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // PaletteEditorForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(340, 520);
            this.Controls.Add(this.allPaintsCheckedListBox);
            this.Controls.Add(this.selectAllCheckBox);
            this.Controls.Add(this.buttonPanel);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(280, 360);
            this.Name = "PaletteEditorForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit My Palette";
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.CheckBox selectAllCheckBox;
        private PaintTranslator.Controls.PaintCheckedListBox allPaintsCheckedListBox;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
