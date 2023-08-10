using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HBScore;
using System.Drawing.Drawing2D;

namespace DisplayScore
{
    public partial class FrmDisplayScore : Form
    {
        private IScore score = null;

        public FrmDisplayScore()
        {
            InitializeComponent();
        }

        private readonly BoundedList<Image> pageImages = new BoundedList<Image>();

        private void FrmDisplayScore_Load(object sender, EventArgs e)
        {
            ofdScoreFile.FileName = Properties.Settings.Default.FilePath;
            ofdScoreFile.DefaultExt = "mus";
            ofdScoreFile.CheckPathExists = true;
            ofdScoreFile.CheckFileExists = true;
            ofdScoreFile.Filter =
                "Music Scores (*.mus)|*.mus|All Files (*.*)|*.*";
            ofdScoreFile.FilterIndex = 0;
            var dlgResult = ofdScoreFile.ShowDialog(this);
            if (dlgResult != DialogResult.OK)
                Application.Exit();
            else
                LoadScore(ofdScoreFile.FileName);
        }


        private void LoadScore(string filePath)
        {
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (Stream iStream = new FileStream
                    (ofdScoreFile.FileName, FileMode.Open, FileAccess.Read))
                    score = (Score)bf.Deserialize(iStream);

                // Adjust old style note classes

                ScoreFactory sf = new ScoreFactory();
                foreach (IMeasure m in score.Measures)
                    for (int i = 0; i < m.Notes.Count(); i++)
                        if (!(m.Notes[i] is ColouredNote))
                            m.Notes[i] = sf.CreateNote
                                (m.Notes[i].Offset, m.Notes[i].Pitch, m.Notes[i].Duration);
                filePath = ofdScoreFile.FileName;
                Properties.Settings.Default.FilePath = filePath;
                Properties.Settings.Default.Save();
                LoadImages();
            }
        }

        private void LoadImages()
        {
            using (var sw = new ScoreWriter(score, true))
            {
                Size musicRect = new Size(sw.ScoreWidthPixels + 58, sw.ScoreHeightPixels + 80);
                double pageAspect = musicRect.Width
                / (double)musicRect.Height;
                double pbxAspect = pbxScore.ClientSize.Width
                    / (double)pbxScore.ClientSize.Height;
                Rectangle content = Rectangle.Empty;
                Rectangle lpNumber = Rectangle.Empty;
                Rectangle rpNumber = Rectangle.Empty;
                int stepSize = 0;
                if (pageAspect < pbxAspect)
                {
                    double scale = pbxScore.ClientSize.Height
                        / (double)musicRect.Height;
                    content.Width = (int)(scale * musicRect.Width);
                    content.Height = pbxScore.ClientSize.Height;
                    content.X = (pbxScore.ClientSize.Width - content.Width) / 2;
                    content.Y = 0;
                    lpNumber.Width = content.X;
                    lpNumber.Height = 96;
                    rpNumber = lpNumber;
                    rpNumber.X = content.Right;
                    stepSize = pbxScore.ClientSize.Height/sw.PageImages.Count();
                }
                else
                {
                    double scale = pbxScore.ClientSize.Width
                        / (double)musicRect.Width;
                    content.Width = pbxScore.ClientSize.Width;
                    content.Height = (int)(scale * musicRect.Height);
                    content.X = 0;
                    content.Y = (pbxScore.ClientSize.Height - content.Height) / 2;
                    lpNumber.Width = (int)(content.Y * 1.5);
                    lpNumber.Height = content.Y;
                    rpNumber = lpNumber;
                    rpNumber.Y = content.Bottom;
                    stepSize = (pbxScore.ClientSize.Width - lpNumber.Width)
                        / (sw.PageImages.Count() - 1);
                }
                var src = new Rectangle(Point.Empty, musicRect);
                int pageNum = 0;
                foreach (var page in sw.PageImages)
                {
                    Bitmap bmp = new Bitmap
                        (pbxScore.ClientSize.Width, pbxScore.ClientSize.Height);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.FillRectangle(Brushes.Gray, 0, 0, bmp.Width, bmp.Height);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        if (pageNum > 0)
                        {
                            PaintPageNumbers(g, lpNumber, rpNumber, stepSize, pageNum);
                            g.DrawImage(page, content, src, GraphicsUnit.Pixel);
                        }
                        else
                            g.DrawImage(page, content);
                    }
                    pageImages.Items.Add(bmp);
                    pageNum++;
                }
            }
            if (pageImages.Items.Count > 0)
                pbxScore.Image = pageImages.Items[0];
        }

        private RectangleF RectFloat(Rectangle r)
        {
            return new RectangleF(r.X, r.Y, r.Width, r.Height);
        }

        private void PaintPageNumbers(Graphics g, Rectangle lpNumber, Rectangle rpNumber, int stepSize, int pageNum)
        {
            int offset = stepSize * (pageNum - 1);
            if (lpNumber.Y == rpNumber.Y)
            {
                lpNumber.Y = offset;
                rpNumber.Y = offset;
            }
            else
            {
                lpNumber.X = offset;
                rpNumber.X = offset;
            }

            using (Font f = new Font("Arial", lpNumber.Height, GraphicsUnit.Pixel))
            {
                g.DrawString(pageNum.ToString(), f, Brushes.White, RectFloat(lpNumber));
                g.DrawString(pageNum.ToString(), f, Brushes.White, RectFloat(rpNumber));
            }
        }

        private void PbxScore_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.X > pbxScore.Width / 4)
                pbxScore.Image = pageImages.Next();
            else if(e.Y < pbxScore.Height/6)
            {
                ctxMenu.Visible = true;
            }
            else
                pbxScore.Image = pageImages.Prev();
        }

        private void RewindToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pbxScore.Image = pageImages.FirstAfterTitle();
            ctxMenu.Visible = false;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
