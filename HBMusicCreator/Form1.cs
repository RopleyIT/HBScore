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
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        readonly string[] noteNames =
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
            foreach(var i in Enumerable.Range(0, 12))
                score.Measures.Add(sf.CreateMeasure(3, false));
            measureOffset = 8;
            PaintScore();
        }

        int measureOffset = 0;

        IEnumerable<IMeasure> MeasuresFromOffset
        {
            get
            {
                int beats = 0;
                for (int i = measureOffset; i < score.Measures.Count(); i++)
                    if (beats + score.Measures[i].BeatsPerBar > BeatsInDisplay)
                        return score.Measures.Skip(measureOffset).Take(i);
                    else
                        beats += score.Measures[i].BeatsPerBar;
                return score.Measures.Skip(measureOffset);
            }
        }

        int BeatsInDisplay
        {
            get
            {
                return (pbxScore.Width - 4)/ 48;
            }
        }

        IMeasure MeasureContainingOffset(int offset)
        {
            int beats = 0;
            foreach (IMeasure m in score.Measures.Skip(offset))
                if (2 * (beats + m.BeatsPerBar) > selectedHalfBeat)
                    return m;
                else beats += m.BeatsPerBar;
            return null;
        }

        int StartOfMeasureHalfBeat(IMeasure measure)
        {
            int beats = 0;
            foreach (IMeasure m in score.Measures.Skip(measureOffset))
                if (measure == m)
                    return beats * 2;
                else
                    beats += m.BeatsPerBar;
            return -1;
        }

        private void PaintScore()
        {
            ScoreWriter sw = new ScoreWriter(score);
            Image img = new Bitmap(pbxScore.Width, pbxScore.Height);
            using (Graphics g = Graphics.FromImage(img))
                g.FillRectangle(Brushes.White, 0, 0, pbxScore.Width, pbxScore.Height);
            sw.RenderMeasures
                (img, new Point(2, 18), 48, measureOffset, true, MeasuresFromOffset);
            pbxScore.Image = img;
            pnlPointer.Invalidate();
        }

        int selectedHalfBeat = -1;

        private void PnlPointer_Paint(object sender, PaintEventArgs e)
        {
            if(selectedHalfBeat >= 0)
                using (Graphics g = pnlPointer.CreateGraphics())
                {
                    Point[] vertices = new Point[]
                    {
                        new Point((1 + selectedHalfBeat) * 24, 30),
                        new Point((selectedHalfBeat) * 24 + 9, 0),
                        new Point((2 + selectedHalfBeat) * 24 - 9, 0)
                    };
                    g.FillPolygon(Brushes.Red, vertices);
                }
        }

        private void PnlPointer_MouseClick(object sender, MouseEventArgs e)
        {
            selectedHalfBeat = (e.X - 12 - 2) / 24;
            pnlPointer.Invalidate();
        }

        private void Note_Click(object sender, EventArgs e)
        {
            int noteOffset = 0;
            int noteNumber = int.Parse((sender as Button).Tag.ToString());
            IMeasure currMeasure = MeasureContainingOffset(measureOffset);
            if (currMeasure != null)
                noteOffset = 2*(selectedHalfBeat - StartOfMeasureHalfBeat(currMeasure));
            var sf = new ScoreFactory();
            currMeasure.Notes.Add(sf.CreateNote(noteOffset, noteNumber, 4));
            PaintScore();
        }

        private void BtnFirst_Click(object sender, EventArgs e)
        {
            measureOffset = 0;
            PaintScore();
        }

        private void BtnLast_Click(object sender, EventArgs e)
        {
            measureOffset = score.Measures.Count() - 3;
            if (measureOffset < 0)
                measureOffset = 0;
            PaintScore();
        }

        private void BtnLeft_Click(object sender, EventArgs e)
        {
            measureOffset -= 3; // Good enough for 3, 4, 5, and even 6 beat bars
            if (measureOffset < 0)
                measureOffset = 0;
            PaintScore();
        }

        private void BtnRight_Click(object sender, EventArgs e)
        {
            measureOffset += 3; // Good enough for 3, 4, 5, and even 6 beat bars
            if (measureOffset >= score.Measures.Count() - 3)
                measureOffset = score.Measures.Count() - 3;
            if (measureOffset < 0)
                measureOffset = 0;
            PaintScore();
        }
    }
}
