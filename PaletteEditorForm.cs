using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PaintTranslator.Data;
using PaintTranslator.Pigments;

namespace PaintTranslator
{
    /// <summary>
    /// Modal dialog for choosing which paints from the full Golden catalog belong
    /// to the user's personal palette. Lists every paint with a check mark next to
    /// the ones currently in the palette; OK confirms the new selection.
    /// </summary>
    public partial class PaletteEditorForm : Form
    {
        /// <summary>
        /// Suppresses the check handlers while the select-all checkbox and the
        /// paint list synchronize each other, so a programmatic change on one side
        /// doesn't re-trigger the other in a loop.
        /// </summary>
        private bool suppressCheckEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteEditorForm"/> class,
        /// listing the full paint catalog with the current palette pre-checked.
        /// </summary>
        /// <param name="currentPaletteNames">The names of the paints currently in
        /// the user's palette, used to set the initial check states.</param>
        public PaletteEditorForm(ISet<string> currentPaletteNames)
        {
            InitializeComponent();
            UiTheme.Apply(this);
            buttonPanel.BackColor = UiTheme.SurfaceRaised;
            UiTheme.StylePrimaryButton(okButton);

            // Populating fires ItemCheck per added item; suppress the select-all
            // sync until the list is complete, then set it once.
            suppressCheckEvents = true;
            try
            {
                foreach (PigmentCoefficients paint in PigmentLibrary.Selectable)
                {
                    allPaintsCheckedListBox.Items.Add(paint, currentPaletteNames.Contains(paint.Name));
                }

                selectAllCheckBox.Checked =
                    allPaintsCheckedListBox.CheckedItems.Count == allPaintsCheckedListBox.Items.Count;
            }
            finally
            {
                suppressCheckEvents = false;
            }
        }

        /// <summary>
        /// Gets the names of the paints the user checked, in catalog order.
        /// </summary>
        public List<string> SelectedPaintNames
        {
            get
            {
                var names = new List<string>(allPaintsCheckedListBox.CheckedItems.Count);
                foreach (object item in allPaintsCheckedListBox.CheckedItems)
                {
                    if (item is PigmentCoefficients paint)
                    {
                        names.Add(paint.Name);
                    }
                }

                return names;
            }
        }

        /// <summary>
        /// Confirms the dialog if at least one paint is checked; otherwise warns
        /// and keeps it open, since an empty palette would leave the app unusable.
        /// </summary>
        /// <param name="sender">The OK button.</param>
        /// <param name="e">The event arguments.</param>
        private void OkButton_Click(object sender, EventArgs e)
        {
            if (allPaintsCheckedListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show(this, "Select at least one paint for your palette.",
                    "No paints selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Mirrors the list state onto the select-all checkbox when a paint is
        /// checked or unchecked.
        /// </summary>
        /// <param name="sender">The checked list box whose item changed.</param>
        /// <param name="e">The event arguments describing the pending check change.</param>
        private void AllPaintsCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // A select-all bulk update fires this once per item; skip the per-item sync.
            if (suppressCheckEvents)
            {
                return;
            }

            // ItemCheck fires before the state commits, so adjust the current
            // count by the pending change to get the post-toggle total.
            int checkedCount = allPaintsCheckedListBox.CheckedItems.Count
                + (e.NewValue == CheckState.Checked ? 1 : 0)
                - (e.CurrentValue == CheckState.Checked ? 1 : 0);

            suppressCheckEvents = true;
            try
            {
                selectAllCheckBox.Checked = checkedCount == allPaintsCheckedListBox.Items.Count;
            }
            finally
            {
                suppressCheckEvents = false;
            }
        }

        /// <summary>
        /// Checks or unchecks every paint in the list when the select-all checkbox
        /// is toggled.
        /// </summary>
        /// <param name="sender">The select-all checkbox.</param>
        /// <param name="e">The event arguments.</param>
        private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Programmatic syncs from ItemCheck must not fan out over the list.
            if (suppressCheckEvents)
            {
                return;
            }

            suppressCheckEvents = true;
            try
            {
                for (int i = 0; i < allPaintsCheckedListBox.Items.Count; i++)
                {
                    allPaintsCheckedListBox.SetItemChecked(i, selectAllCheckBox.Checked);
                }
            }
            finally
            {
                suppressCheckEvents = false;
            }
        }
    }
}
