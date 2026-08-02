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
            this.stylePanel = new System.Windows.Forms.FlowLayoutPanel();
            this.resetStyleButton = new System.Windows.Forms.Button();
            this.styleLabel = new System.Windows.Forms.Label();
            this.styleComboBox = new PaintTranslator.Controls.ModernComboBox();
            this.blurLabel = new System.Windows.Forms.Label();
            this.blurTrackBar = new PaintTranslator.Controls.ModernTrackBar();
            this.markLabel = new System.Windows.Forms.Label();
            this.markTrackBar = new PaintTranslator.Controls.ModernTrackBar();
            this.toolbarPanel.SuspendLayout();
            this.palettePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.columnsNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rowsNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blurTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.markTrackBar)).BeginInit();
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
            this.toolbarPanel.Size = new System.Drawing.Size(1180, 64);
            this.toolbarPanel.TabIndex = 0;
            //
            // loadImageButton
            //
            this.loadImageButton.Location = new System.Drawing.Point(16, 14);
            this.loadImageButton.Name = "loadImageButton";
            this.loadImageButton.Size = new System.Drawing.Size(118, 36);
            this.loadImageButton.TabIndex = 1;
            this.loadImageButton.Text = "Open Photo";
            this.loadImageButton.UseVisualStyleBackColor = true;
            this.loadImageButton.Click += new System.EventHandler(this.LoadImageButton_Click);
            //
            // generateWheelButton
            //
            this.generateWheelButton.Location = new System.Drawing.Point(146, 14);
            this.generateWheelButton.Name = "generateWheelButton";
            this.generateWheelButton.Size = new System.Drawing.Size(150, 36);
            this.generateWheelButton.TabIndex = 2;
            this.generateWheelButton.Text = "Color Wheel...";
            this.generateWheelButton.UseVisualStyleBackColor = true;
            this.generateWheelButton.Click += new System.EventHandler(this.GenerateWheelButton_Click);
            //
            // columnsLabel
            //
            this.columnsLabel.AutoSize = true;
            this.columnsLabel.Location = new System.Drawing.Point(326, 23);
            this.columnsLabel.Name = "columnsLabel";
            this.columnsLabel.Size = new System.Drawing.Size(58, 15);
            this.columnsLabel.TabIndex = 3;
            this.columnsLabel.Text = "Grid columns";
            //
            // columnsNumericUpDown
            //
            this.columnsNumericUpDown.Location = new System.Drawing.Point(414, 19);
            this.columnsNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.columnsNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.columnsNumericUpDown.Name = "columnsNumericUpDown";
            this.columnsNumericUpDown.Size = new System.Drawing.Size(64, 27);
            this.columnsNumericUpDown.TabIndex = 4;
            this.columnsNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.columnsNumericUpDown.ValueChanged += new System.EventHandler(this.GridSettingsChanged);
            //
            // rowsLabel
            //
            this.rowsLabel.AutoSize = true;
            this.rowsLabel.Location = new System.Drawing.Point(500, 23);
            this.rowsLabel.Name = "rowsLabel";
            this.rowsLabel.Size = new System.Drawing.Size(38, 15);
            this.rowsLabel.TabIndex = 5;
            this.rowsLabel.Text = "Rows";
            //
            // rowsNumericUpDown
            //
            this.rowsNumericUpDown.Location = new System.Drawing.Point(548, 19);
            this.rowsNumericUpDown.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.rowsNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.rowsNumericUpDown.Name = "rowsNumericUpDown";
            this.rowsNumericUpDown.Size = new System.Drawing.Size(64, 27);
            this.rowsNumericUpDown.TabIndex = 6;
            this.rowsNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.rowsNumericUpDown.ValueChanged += new System.EventHandler(this.GridSettingsChanged);
            //
            // showGridCheckBox
            //
            this.showGridCheckBox.AutoSize = true;
            this.showGridCheckBox.Location = new System.Drawing.Point(632, 22);
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
            this.magnifierCheckBox.Location = new System.Drawing.Point(738, 14);
            this.magnifierCheckBox.Name = "magnifierCheckBox";
            this.magnifierCheckBox.Size = new System.Drawing.Size(100, 36);
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
            this.imageCanvas.Location = new System.Drawing.Point(0, 64);
            this.imageCanvas.Name = "imageCanvas";
            this.imageCanvas.Size = new System.Drawing.Size(1180, 696);
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
            this.palettePanel.Controls.Add(this.stylePanel);
            this.palettePanel.Controls.Add(this.resetStyleButton);
            this.palettePanel.Controls.Add(this.styleLabel);
            this.palettePanel.Controls.Add(this.styleComboBox);
            this.palettePanel.Controls.Add(this.markLabel);
            this.palettePanel.Controls.Add(this.markTrackBar);
            this.palettePanel.Controls.Add(this.blurLabel);
            this.palettePanel.Controls.Add(this.blurTrackBar);
            this.palettePanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.palettePanel.Location = new System.Drawing.Point(880, 64);
            this.palettePanel.Name = "palettePanel";
            this.palettePanel.Padding = new System.Windows.Forms.Padding(16);
            this.palettePanel.Size = new System.Drawing.Size(300, 696);
            this.palettePanel.TabIndex = 9;
            //
            // editPaletteButton
            //
            this.editPaletteButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.editPaletteButton.Location = new System.Drawing.Point(0, 0);
            this.editPaletteButton.Name = "editPaletteButton";
            this.editPaletteButton.Size = new System.Drawing.Size(250, 36);
            this.editPaletteButton.TabIndex = 14;
            this.editPaletteButton.Text = "Edit Palette...";
            this.editPaletteButton.UseVisualStyleBackColor = true;
            this.editPaletteButton.Click += new System.EventHandler(this.EditPaletteButton_Click);
            //
            // stylePanel
            //
            this.stylePanel.AutoScroll = true;
            this.stylePanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.stylePanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.stylePanel.Location = new System.Drawing.Point(0, 254);
            this.stylePanel.Name = "stylePanel";
            this.stylePanel.Size = new System.Drawing.Size(250, 160);
            this.stylePanel.TabIndex = 20;
            this.stylePanel.WrapContents = false;
            //
            // resetStyleButton
            //
            this.resetStyleButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.resetStyleButton.Location = new System.Drawing.Point(0, 374);
            this.resetStyleButton.Name = "resetStyleButton";
            this.resetStyleButton.Size = new System.Drawing.Size(250, 34);
            this.resetStyleButton.TabIndex = 21;
            this.resetStyleButton.Text = "Reset to style defaults";
            this.resetStyleButton.UseVisualStyleBackColor = true;
            this.resetStyleButton.Click += new System.EventHandler(this.ResetStyleButton_Click);
            //
            // selectAllCheckBox
            //
            this.selectAllCheckBox.Checked = true;
            this.selectAllCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.selectAllCheckBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.selectAllCheckBox.Location = new System.Drawing.Point(0, 0);
            this.selectAllCheckBox.Name = "selectAllCheckBox";
            this.selectAllCheckBox.Padding = new System.Windows.Forms.Padding(6, 2, 0, 0);
            this.selectAllCheckBox.Size = new System.Drawing.Size(250, 30);
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
            this.paintsCheckedListBox.Location = new System.Drawing.Point(0, 52);
            this.paintsCheckedListBox.Name = "paintsCheckedListBox";
            this.paintsCheckedListBox.Size = new System.Drawing.Size(250, 202);
            this.paintsCheckedListBox.TabIndex = 11;
            this.paintsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.PaintsCheckedListBox_ItemCheck);
            //
            // blurLabel
            //
            this.blurLabel.AutoSize = false;
            this.blurLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.blurLabel.Location = new System.Drawing.Point(0, 508);
            this.blurLabel.Name = "blurLabel";
            this.blurLabel.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.blurLabel.Size = new System.Drawing.Size(250, 24);
            this.blurLabel.TabIndex = 15;
            this.blurLabel.Text = "Blur: 2 px";
            this.blurLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // blurTrackBar
            //
            this.blurTrackBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.blurTrackBar.Location = new System.Drawing.Point(0, 528);
            this.blurTrackBar.Maximum = 20;
            this.blurTrackBar.Name = "blurTrackBar";
            this.blurTrackBar.Size = new System.Drawing.Size(250, 38);
            this.blurTrackBar.TabIndex = 13;
            this.blurTrackBar.TickFrequency = 2;
            this.blurTrackBar.Value = 2;
            this.blurTrackBar.ValueChanged += new System.EventHandler(this.BlurTrackBar_ValueChanged);
            //
            // styleLabel
            //
            this.styleLabel.AutoSize = false;
            this.styleLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.styleLabel.Location = new System.Drawing.Point(0, 402);
            this.styleLabel.Name = "styleLabel";
            this.styleLabel.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.styleLabel.Size = new System.Drawing.Size(250, 24);
            this.styleLabel.TabIndex = 18;
            this.styleLabel.Text = "Style";
            this.styleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // styleComboBox
            //
            this.styleComboBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.styleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.styleComboBox.FormattingEnabled = true;
            this.styleComboBox.Location = new System.Drawing.Point(0, 422);
            this.styleComboBox.Name = "styleComboBox";
            this.styleComboBox.Size = new System.Drawing.Size(250, 34);
            this.styleComboBox.TabIndex = 19;
            this.styleComboBox.SelectedIndexChanged += new System.EventHandler(this.StyleComboBox_SelectedIndexChanged);
            //
            // markLabel
            //
            this.markLabel.AutoSize = false;
            this.markLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.markLabel.Location = new System.Drawing.Point(0, 443);
            this.markLabel.Name = "markLabel";
            this.markLabel.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.markLabel.Size = new System.Drawing.Size(250, 24);
            this.markLabel.TabIndex = 16;
            this.markLabel.Text = "Brush mark: 3 px";
            this.markLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // markTrackBar
            //
            this.markTrackBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.markTrackBar.Location = new System.Drawing.Point(0, 463);
            this.markTrackBar.Minimum = 1;
            this.markTrackBar.Maximum = 128;
            this.markTrackBar.Name = "markTrackBar";
            this.markTrackBar.Size = new System.Drawing.Size(250, 38);
            this.markTrackBar.TabIndex = 17;
            this.markTrackBar.TickFrequency = 16;
            this.markTrackBar.Value = 3;
            this.markTrackBar.ValueChanged += new System.EventHandler(this.MarkTrackBar_ValueChanged);
            //
            // MainForm
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1180, 760);
            this.Controls.Add(this.imageCanvas);
            this.Controls.Add(this.palettePanel);
            this.Controls.Add(this.toolbarPanel);
            this.MinimumSize = new System.Drawing.Size(900, 600);
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
            ((System.ComponentModel.ISupportInitialize)(this.blurTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.markTrackBar)).EndInit();
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
        private System.Windows.Forms.FlowLayoutPanel stylePanel;
        private System.Windows.Forms.Button resetStyleButton;
        private PaintTranslator.Controls.PaintCheckedListBox paintsCheckedListBox;
        private System.Windows.Forms.Label styleLabel;
        private PaintTranslator.Controls.ModernComboBox styleComboBox;
        private System.Windows.Forms.Label blurLabel;
        private PaintTranslator.Controls.ModernTrackBar blurTrackBar;
        private System.Windows.Forms.Label markLabel;
        private PaintTranslator.Controls.ModernTrackBar markTrackBar;
    }
}
