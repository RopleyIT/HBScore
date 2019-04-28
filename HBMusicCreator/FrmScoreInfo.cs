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
    public partial class FrmScoreInfo : Form
    {
        public string Title
        {
            get
            {
                return txtTitle.Text;
            }
            set
            {
                txtTitle.Text = value;
            }
        }

        public string Composer
        {
            get
            {
                return txtComposer.Text;
            }
            set
            {
                txtComposer.Text = value;
            }
        }

        public string Information
        {
            get
            {
                return txtInfo.Text;
            }
            set
            {
                txtInfo.Text = value;
            }
        }

        public FrmScoreInfo()
        {
            InitializeComponent();
        }
    }
}
