using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HBScore;

namespace HBMusicCreator
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        string[] noteNames =
        {
            "06E", "07D#", "07D", "1C#",
            "1C", "2B", "3A#", "3A", "4G#", "4G", "5F#", "5F", "6E", "7D#", "7D", "8C#",
            "8C", "9B", "10A#", "10A", "11G#", "11G", "12F#", "12F", "13E", "14D#", "14D", "15C#",
            "15C", "16B", "17A#", "17A", "18G#", "18G"
        };

        IScore score = null;

        private void FrmMain_Load(object sender, EventArgs e)
        {
        }

        private void PnlKeyboard_Paint(object sender, PaintEventArgs e)
        {

        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sf = new ScoreFactory();
            score = sf.CreateScore();
            foreach(var i in Enumerable.Range(0, 7))
                score.Measures.Add(sf.CreateMeasure(3, false));
            paintScore();
        }

        private void paintScore()
        {
            ScoreWriter sw = new ScoreWriter(score, 7);
            Image img = new Bitmap(pbxScore.Width, pbxScore.Height);
            sw.RenderMeasures(img, new Point(2, 18), 48, 0, true, score.Measures);
            pbxScore.Image = img;
        }

        private void PbxScore_MouseMove(object sender, MouseEventArgs e)
        {
            lblDbg.Text = e.X + ", " + e.Y;
        }
    }
}
