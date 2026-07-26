namespace PaintTranslator
{
    partial class MainForm
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
            this.toolbarPanel = new System.Windows.Forms.Panel();
            this.loadImageButton = new System.Windows.Forms.Button();
            this.generateWheelButton = new System.Windows.Forms.Button();
            this.columnsLabel = new System.Windows.Forms.Label();
            this.columnsNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.rowsLabel = new System.Windows.Forms.Label();
            this.rowsNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.showGridCheckBox = new System.Windows.Forms.CheckBox();
            this.magnifierCheckBox = new System.Windows.Forms.CheckBox();
            this.imageCanvas = new PaintTranslator.Controls.ImageCanvas();
            this.palettePanel = new System.Windows.Forms.Panel();
            this.selectAllCheckBox = new System.Windows.Forms.CheckBox();
            this.paintsCheckedListBox = new PaintTranslator.Controls.PaintCheckedListBox();
            this.editPaletteButton = new System.Windows.Forms.Button();
            this.ditherCheckBox = new System.Windows.Forms.CheckBox();
            this.convertPhotoButton = new System.Windows.Forms.Button();
            this.toolbarPanel.SuspendLayout();
            this.palettePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.columnsNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rowsNumericUpDown)).BeginInit();
            this.SuspendLayout();
            //
            // toolbarPanel
            //
            this.toolbarPanel.Controls.Add(this.loadImageButton);
            this.toolbarPanel.Controls.Add(this.generateWheelButton);
            this.toolbarPanel.Controls.Add(this.columnsLabel);
            this.toolbarPanel.Controls.Add(this.columnsNumericUpDown);
            this.toolbarPanel.Controls.Add(this.rowsLabel);
            this.toolbarPanel.Controls.Add(this.rowsNumericUpDown);
            this.toolbarPanel.Controls.Add(this.showGridCheckBox);
            this.toolbarPanel.Controls.Add(this.magnifierCheckBox);
            this.toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolbarPanel.Location = new System.Drawing.Point(0, 0);
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.Size = new System.Drawing.Size(984, 54);
            this.toolbarPanel.TabIndex = 0;
            //
            // loadImageButton
            //
            this.loadImageButton.Location = new System.Drawing.Point(12, 12);
            this.loadImageButton.Name = "loadImageButton";
            this.loadImageButton.Size = new System.Drawing.Size(110, 30);
            this.loadImageButton.TabIndex = 1;
            this.loadImageButton.Text = "Load Image...";
            this.loadImageButton.UseVisualStyleBackColor = true;
            this.loadImageButton.Click += new System.EventHandler(this.LoadImageButton_Click);
            //
            // generateWheelButton
            //
            this.generateWheelButton.Location = new System.Drawing.Point(128, 12);
            this.generateWheelButton.Name = "generateWheelButton";
            this.generateWheelButton.Size = new System.Drawing.Size(160, 30);
            this.generateWheelButton.TabIndex = 2;
            this.generateWheelButton.Text = "Generate Color Wheel";
            this.generateWheelButton.UseVisualStyleBackColor = true;
            this.generateWheelButton.Click += new System.EventHandler(this.GenerateWheelButton_Click);
            //
            // columnsLabel
            //
            this.columnsLabel.AutoSize = true;
            this.columnsLabel.Location = new System.Drawing.Point(312, 19);
            this.columnsLabel.Name = "columnsLabel";
            this.columnsLabel.Size = new System.Drawing.Size(58, 15);
            this.columnsLabel.TabIndex = 3;
            this.columnsLabel.Text = "Columns:";
            //
            // columnsNumericUpDown
            //
            this.columnsNumericUpDown.Location = new System.Drawing.Point(376, 16);
            this.columnsNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.columnsNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.columnsNumericUpDown.Name = "columnsNumericUpDown";
            this.columnsNumericUpDown.Size = new System.Drawing.Size(60, 23);
            this.columnsNumericUpDown.TabIndex = 4;
            this.columnsNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.columnsNumericUpDown.ValueChanged += new System.EventHandler(this.GridSettingsChanged);
            //
            // rowsLabel
            //
            this.rowsLabel.AutoSize = true;
            this.rowsLabel.Location = new System.Drawing.Point(452, 19);
            this.rowsLabel.Name = "rowsLabel";
            this.rowsLabel.Size = new System.Drawing.Size(38, 15);
            this.rowsLabel.TabIndex = 5;
            this.rowsLabel.Text = "Rows:";
            //
            // rowsNumericUpDown
            //
            this.rowsNumericUpDown.Location = new System.Drawing.Point(496, 16);
            this.rowsNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.rowsNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.rowsNumericUpDown.Name = "rowsNumericUpDown";
            this.rowsNumericUpDown.Size = new System.Drawing.Size(60, 23);
            this.rowsNumericUpDown.TabIndex = 6;
            this.rowsNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.rowsNumericUpDown.ValueChanged += new System.EventHandler(this.GridSettingsChanged);
            //
            // showGridCheckBox
            //
            this.showGridCheckBox.AutoSize = true;
            this.showGridCheckBox.Location = new System.Drawing.Point(576, 18);
            this.showGridCheckBox.Name = "showGridCheckBox";
            this.showGridCheckBox.Size = new System.Drawing.Size(80, 19);
            this.showGridCheckBox.TabIndex = 7;
            this.showGridCheckBox.Text = "Show grid";
            this.showGridCheckBox.UseVisualStyleBackColor = true;
            this.showGridCheckBox.CheckedChanged += new System.EventHandler(this.GridSettingsChanged);
            //
            // magnifierCheckBox
            //
            this.magnifierCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            this.magnifierCheckBox.Location = new System.Drawing.Point(672, 12);
            this.magnifierCheckBox.Name = "magnifierCheckBox";
            this.magnifierCheckBox.Size = new System.Drawing.Size(100, 30);
            this.magnifierCheckBox.TabIndex = 8;
            this.magnifierCheckBox.Text = "🔍 Zoom";
            this.magnifierCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.magnifierCheckBox.UseVisualStyleBackColor = true;
            this.magnifierCheckBox.CheckedChanged += new System.EventHandler(this.MagnifierCheckBox_CheckedChanged);
            //
            // imageCanvas
            //
            this.imageCanvas.AllowDrop = true;
            this.imageCanvas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.imageCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageCanvas.Location = new System.Drawing.Point(0, 54);
            this.imageCanvas.Name = "imageCanvas";
            this.imageCanvas.Size = new System.Drawing.Size(984, 607);
            this.imageCanvas.TabIndex = 8;
            this.imageCanvas.TabStop = false;
            this.imageCanvas.DragDrop += new System.Windows.Forms.DragEventHandler(this.ImageDragDrop);
            this.imageCanvas.DragEnter += new System.Windows.Forms.DragEventHandler(this.ImageDragEnter);
            this.imageCanvas.Paint += new System.Windows.Forms.PaintEventHandler(this.ImageCanvas_Paint);
            this.imageCanvas.MouseLeave += new System.EventHandler(this.ImageCanvas_MouseLeave);
            this.imageCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ImageCanvas_MouseMove);
            this.imageCanvas.ViewChanged += new System.EventHandler(this.ImageCanvas_ViewChanged);
            //
            // palettePanel
            //
            this.palettePanel.Controls.Add(this.paintsCheckedListBox);
            this.palettePanel.Controls.Add(this.selectAllCheckBox);
            this.palettePanel.Controls.Add(this.editPaletteButton);
            this.palettePanel.Controls.Add(this.ditherCheckBox);
            this.palettePanel.Controls.Add(this.convertPhotoButton);
            this.palettePanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.palettePanel.Location = new System.Drawing.Point(734, 54);
            this.palettePanel.Name = "palettePanel";
            this.palettePanel.Size = new System.Drawing.Size(250, 607);
            this.palettePanel.TabIndex = 9;
            //
            // editPaletteButton
            //
            this.editPaletteButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.editPaletteButton.Location = new System.Drawing.Point(0, 0);
            this.editPaletteButton.Name = "editPaletteButton";
            this.editPaletteButton.Size = new System.Drawing.Size(250, 28);
            this.editPaletteButton.TabIndex = 14;
            this.editPaletteButton.Text = "Edit Palette...";
            this.editPaletteButton.UseVisualStyleBackColor = true;
            this.editPaletteButton.Click += new System.EventHandler(this.EditPaletteButton_Click);
            //
            // selectAllCheckBox
            //
            this.selectAllCheckBox.Checked = true;
            this.selectAllCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.selectAllCheckBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.selectAllCheckBox.Location = new System.Drawing.Point(0, 0);
            this.selectAllCheckBox.Name = "selectAllCheckBox";
            this.selectAllCheckBox.Padding = new System.Windows.Forms.Padding(6, 2, 0, 0);
            this.selectAllCheckBox.Size = new System.Drawing.Size(250, 24);
            this.selectAllCheckBox.TabIndex = 10;
            this.selectAllCheckBox.Text = "Select all";
            this.selectAllCheckBox.UseVisualStyleBackColor = true;
            this.selectAllCheckBox.CheckedChanged += new System.EventHandler(this.SelectAllCheckBox_CheckedChanged);
            //
            // paintsCheckedListBox
            //
            this.paintsCheckedListBox.CheckOnClick = true;
            this.paintsCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paintsCheckedListBox.IntegralHeight = false;
            this.paintsCheckedListBox.Location = new System.Drawing.Point(0, 24);
            this.paintsCheckedListBox.Name = "paintsCheckedListBox";
            this.paintsCheckedListBox.Size = new System.Drawing.Size(250, 583);
            this.paintsCheckedListBox.TabIndex = 11;
            this.paintsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.PaintsCheckedListBox_ItemCheck);
            //
            // ditherCheckBox
            //
            this.ditherCheckBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ditherCheckBox.Location = new System.Drawing.Point(0, 549);
            this.ditherCheckBox.Name = "ditherCheckBox";
            this.ditherCheckBox.Padding = new System.Windows.Forms.Padding(6, 2, 0, 0);
            this.ditherCheckBox.Size = new System.Drawing.Size(250, 24);
            this.ditherCheckBox.TabIndex = 13;
            this.ditherCheckBox.Text = "Dither (smoother blending)";
            this.ditherCheckBox.UseVisualStyleBackColor = true;
            //
            // convertPhotoButton
            //
            this.convertPhotoButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.convertPhotoButton.Location = new System.Drawing.Point(0, 573);
            this.convertPhotoButton.Name = "convertPhotoButton";
            this.convertPhotoButton.Size = new System.Drawing.Size(250, 34);
            this.convertPhotoButton.TabIndex = 12;
            this.convertPhotoButton.Text = "Convert Photo to Paints";
            this.convertPhotoButton.UseVisualStyleBackColor = true;
            this.convertPhotoButton.Click += new System.EventHandler(this.ConvertPhotoButton_Click);
            //
            // MainForm
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.imageCanvas);
            this.Controls.Add(this.palettePanel);
            this.Controls.Add(this.toolbarPanel);
            this.MinimumSize = new System.Drawing.Size(720, 480);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Paint Translator";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.ImageDragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.ImageDragEnter);
            this.toolbarPanel.ResumeLayout(false);
            this.toolbarPanel.PerformLayout();
            this.palettePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.columnsNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rowsNumericUpDown)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel toolbarPanel;
        private System.Windows.Forms.Button loadImageButton;
        private System.Windows.Forms.Button generateWheelButton;
        private System.Windows.Forms.Label columnsLabel;
        private System.Windows.Forms.NumericUpDown columnsNumericUpDown;
        private System.Windows.Forms.Label rowsLabel;
        private System.Windows.Forms.NumericUpDown rowsNumericUpDown;
        private System.Windows.Forms.CheckBox showGridCheckBox;
        private System.Windows.Forms.CheckBox magnifierCheckBox;
        private PaintTranslator.Controls.ImageCanvas imageCanvas;
        private System.Windows.Forms.Panel palettePanel;
        private System.Windows.Forms.CheckBox selectAllCheckBox;
        private System.Windows.Forms.Button editPaletteButton;
        private PaintTranslator.Controls.PaintCheckedListBox paintsCheckedListBox;
        private System.Windows.Forms.CheckBox ditherCheckBox;
        private System.Windows.Forms.Button convertPhotoButton;
    }
}
