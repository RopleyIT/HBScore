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
    public partial class FrmTranspose : Form
    {
        public int Interval
        {
            get
            {
                switch(lbxInterval.SelectedIndex)
                {
                    case 0: return -7;
                    case 1: return -4;
                    case 2: return -2;
                    case 3: return -1;
                    case 4: return 1;
                    case 5: return 2;
                    case 6: return 4;
                    case 7: return 7;
                    default: return 0;
                }
            }
        }

        public FrmTranspose()
        {
            InitializeComponent();
        }
    }
}
