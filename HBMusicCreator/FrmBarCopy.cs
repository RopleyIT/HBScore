using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HBMusicCreator
{
    public partial class FrmBarCopy : Form
    {
        int barCount = 0;

        public int First { get; private set; }
        public int Count { get; private set; }
        public int InsertionPoint { get; private set; }

        public FrmBarCopy(int bc)
        {
            InitializeComponent();
            barCount = bc;
        }

        // For designer UI
        public FrmBarCopy() : this(0) { }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            var val = ValueOf(txtFirst.Text);
            if (val < 0 || val >= barCount)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this,
                    "Starting bar number not digits, or not in score", 
                    "Bad bar number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
                First = val;
            val = ValueOf(txtLast.Text);
            if (val >= barCount || val < First)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this,
                    "Ending bar number not digits, or not after starting bar",
                    "Bad bar number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
                Count = val - First + 1;
            val = ValueOf(txtDest.Text);
            if (val > barCount || val < 0)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this,
                    "Insertion point not digits, or not in score",
                    "Bad bar number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
                InsertionPoint = val;
        }

        int ValueOf(string s)
        {
            int value = 0;
            if (int.TryParse(s, out value))
                return value-1;
            else
                return -1;
        }

        private void FrmBarCopy_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
                e.Cancel = true;
        }
    }
}
