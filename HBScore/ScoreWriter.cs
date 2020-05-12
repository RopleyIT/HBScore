using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace HBScore
{
    public class ScoreWriter : IDisposable
    {
        public const int PixelsPerInch = 300;
        public const int PixelsPerSquare = 150;
        public const int PixelsPerPageLength = 3508;
        public const int PixelsPerPageHeight = 2480;
        public const int PixelsMargin = 150;
        public const int MaxBeatsPerSystem = 21;
        public int VerticalSquares { get; private set; }

        public int SystemsPerPage
        {
            get
            {
                switch (VerticalSquares)
                {
                    case 1:
                        return 7;
                    case 2:
                        return 5;
                    case 3:
                    case 4:
                        return 3;
                    case 5:
                    case 6:
                        return 2;
                    default:
                        return 1;
                }
            }
        }

        public List<Image> RenderedPages { get; private set; }

        public IScore Score { get; set; }

        public ScoreWriter(int measures, int beatsPerBar, bool compound, int squares, bool useFlats)
            : this(CreateEmptyScore(measures, beatsPerBar, compound, useFlats), squares)
        { }

        private static IScore CreateEmptyScore
            (int measures, int beatsPerBar, bool compound, bool useFlats)
        {
            if (beatsPerBar < 2 || beatsPerBar > 7)
                throw new ArgumentException
                    ("Only support 2 to 7 beats per bar");
            Score score = new Score(useFlats);
            foreach (int i in Enumerable.Range(0, measures))
                score.Measures.Add(new Measure(beatsPerBar, compound));
            return score;
        }

        public ScoreWriter(IScore score, int squares = 0)
        {
            if (score == null)
                throw new ArgumentException("No score passed to score writer");
            if (!score.Measures.Any())
                throw new ArgumentException("Score has no bars");
            Score = score;

            // Calculate how many vertical squares are needed to represent
            // the notes used. It is permitted to use the line above and
            // below the set of squares, hence a system one square deep can
            // hold four notes vertically (plus sharps or flats). Two squares
            // can hold up to seven, three squares up to ten etc. If a hard-
            // wired non-zero squares argument is provided, override the
            // calculation of how many squares are needed with the fixed value.

            if (squares == 0)
                VerticalSquares = 1 +
                    (Score.MaxVerticalOffset - Score.MinVerticalOffset - 1) / 3;
            else
                VerticalSquares = squares;
            SetPageSystemAndBarBoundaries();
            RenderedPages = new List<Image>(pageBoundaries.Count)
            {
                RenderTitlePage()
            };
            for (int i = 0; i < pageBoundaries.Count; i++)
                RenderedPages.Add(RenderPage(i));
        }

        public void SavePDF(string outputPath) =>
            PDFScoreWriter.GeneratePDF(RenderedPages, outputPath,
                Score.Title, Score.Composer, Score.Information, Score.NoteList);

        private List<int> pageBoundaries;   // Measured in quarter beats
        private List<int> systemBoundaries; // Measured in quarter beats
        private List<int> barBoundaries;    // Measured in quarter beats

        private void SetPageSystemAndBarBoundaries()
        {
            barBoundaries = new List<int>();
            int quarterBeats = 0;
            systemBoundaries = new List<int>() { 0 };
            pageBoundaries = new List<int>() { 0 };
            foreach (Measure m in Score.Measures)
            {
                barBoundaries.Add(quarterBeats);
                if (quarterBeats + m.QuarterBeatsPerBar - systemBoundaries.Last() > 4*MaxBeatsPerSystem)
                {
                    systemBoundaries.Add(quarterBeats);
                    if (systemBoundaries.Count % SystemsPerPage == 0)
                        pageBoundaries.Add(systemBoundaries.Last());
                }
                quarterBeats += m.QuarterBeatsPerBar;
            }
        }

        private int IndexOfFirstBarOfSystem(int system)
            => system >= systemBoundaries.Count ?
            barBoundaries.Count :
            barBoundaries.IndexOf(systemBoundaries[system]);

        public Image RenderTitlePage()
        {
            int bmpHeight = 2480;
            Bitmap bmp = new Bitmap
                (3508 - 2 * PixelsMargin, bmpHeight - 2 * PixelsMargin, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(Brushes.White, 0, 0, 3507, 2480);
            Font titleFont = new Font("Arial Rounded MT", 96, FontStyle.Bold);
            Font subTitleFont = new Font("Arial Rounded MT", 48, FontStyle.Regular);

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(Score.Title, titleFont, Brushes.Black, 1753, 800, sf);
            g.DrawString(Score.Composer, subTitleFont, Brushes.Black, 1753, 960, sf);
            g.DrawString(Score.Information, subTitleFont, Brushes.Black, 1753, 1120, sf);
            g.DrawString(Score.NoteList, subTitleFont, Brushes.Black,
                new RectangleF(300, 1280, 2908, 600), sf);

            subTitleFont.Dispose();
            titleFont.Dispose();
            g.Dispose();
            return bmp;
        }

        public Image RenderPage(int page)
        {
            // Assume 300dpi on A4 paper, which is
            // 297 by 210 mm. Hence we are painting
            // into a rectangle with 297/25.4 * 300
            // = 3508 wide by 210/25.4 * 300 = 2480
            // high. Rendered area in middle of page
            // is 2100 by 3150 giving a top and bottom
            // margin of 190px and left and right margin
            // of 179px, i.e. in each case slightly
            // more than 1/2 inch.

            int bmpHeight = 2480;
            Bitmap bmp = new Bitmap
                (3508 - 2 * PixelsMargin, bmpHeight - 2 * PixelsMargin, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
                g.FillRectangle(Brushes.White, 0, 0, 3507, 2480);

            // Walk the set of systems on this page

            int system = page * SystemsPerPage;
            while (system < systemBoundaries.Count && system < (page + 1) * SystemsPerPage)
            {
                // Find the set of bars in this system

                int firstBarOfSystemIdx = IndexOfFirstBarOfSystem(system);
                int firstBarBeyondSystemIdx = IndexOfFirstBarOfSystem(system + 1);
                IEnumerable<IMeasure> measures = Score.Measures
                    .Skip(firstBarOfSystemIdx)
                    .Take(firstBarBeyondSystemIdx - firstBarOfSystemIdx);
                Point tlhc = new Point(179 - PixelsMargin, 190 - PixelsMargin);
                tlhc.Offset(0, PixelsPerSquare * (VerticalSquares + 1) * (system % SystemsPerPage));
                RenderMeasures(bmp, tlhc, PixelsPerSquare, firstBarOfSystemIdx, Score.UseFlats, measures, null);
                system++;
            }
            return bmp;
        }

        public void RenderMeasures(Image img, Point tlhc, int pixelsPerSquare,
            int barNum, bool useFlats, IEnumerable<IMeasure> measures, INote selectedNote)
        {
            Font barNumFont = new Font("Arial Rounded MT", pixelsPerSquare / 6);
            Pen thickPen = new Pen(Color.Black, pixelsPerSquare / 15);
            Pen thinPen = new Pen(Color.Black, pixelsPerSquare / 36);
            Graphics g = Graphics.FromImage(img);

            int beatsInSystem = measures.Sum(m => m.BeatsPerBar);
            int quarterBeatsInSystem = measures.Sum(m => m.QuarterBeatsPerBar);

            // Draw the border around the whole system

            Size systemSize = new Size
                (
                    (pixelsPerSquare * quarterBeatsInSystem) / 4,
                    pixelsPerSquare * VerticalSquares
                );
            Rectangle systemBorder = new Rectangle(tlhc, systemSize);
            g.DrawRectangle(thickPen, systemBorder);

            // Draw each vertical line in the system

            Point upper = tlhc;
            Point lower = new Point
                (upper.X, upper.Y + pixelsPerSquare * VerticalSquares);

            foreach (Measure m in measures)
            {
                int offset = m.CompoundTime ? 3 * pixelsPerSquare / 2 : pixelsPerSquare;
                for (int beat = 0; beat < m.BeatsPerBar; beat++)
                {
                    if (beat == 0)
                    {
                        g.DrawString((++barNum).ToString(),
                            barNumFont, Brushes.Black, new Point
                            (upper.X, upper.Y - pixelsPerSquare / 4 - 4));
                        if (m != measures.First())
                        {
                            if (m.Notes.Any(n => n.Offset == 0 && n.Pitch == Note.DoubleBar))
                                DrawDoubleBarLine(g, upper, lower, pixelsPerSquare);
                            else
                                g.DrawLine(thickPen, upper, lower);
                        }
                    }
                    else
                        g.DrawLine(thinPen, upper, lower);
                    upper.Offset(offset, 0);
                    lower.Offset(offset, 0);
                }
            }

            // Draw each horizontal line in the system

            Point left = tlhc;
            Point right = new Point(left.X + systemSize.Width, left.Y);
            for (int square = 1; square < VerticalSquares; square++)
            {
                left.Offset(0, pixelsPerSquare);
                right.Offset(0, pixelsPerSquare);
                g.DrawLine(thinPen, left, right);
            }

            // White out any areas where characters will be written

            Point barStart = tlhc;
            foreach (IMeasure m in measures)
            {
                foreach (INote note in m.Notes)
                    InsertBlank(img, note, barStart, useFlats, pixelsPerSquare);
                barStart.Offset((m.QuarterBeatsPerBar * pixelsPerSquare) / 4, 0);
            }

            // Write the notes to the score

            barStart = tlhc;
            foreach (IMeasure m in measures)
            {
                foreach (INote note in m.Notes)
                    InsertNote(img, note, barStart, useFlats, pixelsPerSquare, note == selectedNote);
                barStart.Offset((m.QuarterBeatsPerBar * pixelsPerSquare) / 4, 0);
            }
            g.Dispose();
            barNumFont.Dispose();
            thinPen.Dispose();
            thickPen.Dispose();
        }

        private void DrawDoubleBarLine(Graphics g, Point upper, Point lower, int pixelsPerSquare)
        {
            Pen thinPen = new Pen(Color.Black, pixelsPerSquare / 36);
            upper.Offset(-(int)(pixelsPerSquare / 30.0), 0);
            lower.Offset(-(int)(pixelsPerSquare / 30.0), 0);
            g.DrawLine(thinPen, upper, lower);
            upper.Offset((int)(pixelsPerSquare / 15.0), 0);
            lower.Offset((int)(pixelsPerSquare / 15.0), 0);
            g.DrawLine(thinPen, upper, lower);
        }

        private void InsertNote(Image img, INote note, Point barStart, bool useFlats, int pixelsPerSquare, bool selected)
        {
            using (Graphics g = Graphics.FromImage(img))
            {
                if (note.Pitch >= Note.StartRepeat)
                    RenderSpecial(g, note, barStart, useFlats, pixelsPerSquare, selected);
                else
                {
                    // Calculate horizontal position of note
                    int pixelsIntoBar = note.Offset * pixelsPerSquare / 4;
                    Point noteCentre = barStart;
                    noteCentre.Offset(pixelsIntoBar + pixelsPerSquare / 2,
                        (note.VerticalOffset(useFlats) - Score.MinVerticalOffset) * pixelsPerSquare / 3);
                    using (Font font = new Font("Arial Rounded MT", pixelsPerSquare / 3 - 2, FontStyle.Bold))
                    using (Font accFont = new Font("Arial Rounded MT", pixelsPerSquare / 4, FontStyle.Bold))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        string noteStr = note.ToString(useFlats);
                        SizeF size = g.MeasureString(noteStr.Substring(0, 1), font);
                        using (Brush txtBrush = selected ? new SolidBrush(Color.Red) : new SolidBrush((note as ColouredNote).ForeColour))
                        {
                            g.DrawString(noteStr.Substring(0, 1), font, txtBrush, noteCentre, sf);
                            if (noteStr.Length > 1)
                                g.DrawString(noteStr.Substring(1), accFont, txtBrush,
                                    new PointF(noteCentre.X + size.Width / 2, noteCentre.Y - pixelsPerSquare / 10), sf);
                            if (note.Duration > 4)
                                using (Pen p = new Pen(txtBrush, pixelsPerSquare / 36.0f))
                                    g.DrawLine(p, noteCentre.X + size.Width, noteCentre.Y - pixelsPerSquare / 10,
                                        noteCentre.X + pixelsPerSquare * (note.Duration / 4 - 1), noteCentre.Y - pixelsPerSquare / 10);
                        }
                    }
                }
            }
        }

        private void RenderSpecial(Graphics g, INote note, Point barStart, bool useFlats, int pixelsPerSquare, bool selected)
        {
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;

            if (note.Pitch == Note.StartRepeat || note.Pitch == Note.EndRepeat)
            {
                int pixelsIntoBar = note.Offset * pixelsPerSquare / 4;
                using (Brush txtBrush = selected ? new SolidBrush(Color.Red) : new SolidBrush((note as ColouredNote).ForeColour))
                {
                    RectangleF blob = new RectangleF(
                        barStart.X + pixelsIntoBar + pixelsPerSquare / 8f,
                        barStart.Y + pixelsPerSquare / 4f,
                        pixelsPerSquare / 8f,
                        pixelsPerSquare / 8f);
                    if (note.Pitch == Note.EndRepeat)
                        blob.Offset(5 * pixelsPerSquare / 8f, 0f);
                    g.FillEllipse(txtBrush, blob);
                    blob.Offset(0f, 3 * pixelsPerSquare / 8f);
                    g.FillEllipse(txtBrush, blob);
                    blob.Offset(0f, pixelsPerSquare * (VerticalSquares - 1));
                    g.FillEllipse(txtBrush, blob);
                    blob.Offset(0f, -3 * pixelsPerSquare / 8f);
                    g.FillEllipse(txtBrush, blob);
                }
            }

            // TODO: First and second time bars
        }

        private void InsertBlank(Image img, INote note, Point barStart, bool useFlats, int pixelsPerSquare)
        {
            using (Graphics g = Graphics.FromImage(img))
            {
                // Calculate horizontal position of note
                int pixelsIntoBar = note.Offset * pixelsPerSquare / 4;
                Point noteCentre = barStart;
                noteCentre.Offset(pixelsIntoBar + pixelsPerSquare / 2,
                    (note.VerticalOffset(useFlats) - Score.MinVerticalOffset) * pixelsPerSquare / 3);
                using (Font font = new Font("Arial Rounded MT", pixelsPerSquare / 3 - 2, FontStyle.Bold))
                using (Font accFont = new Font("Arial Rounded MT", pixelsPerSquare / 4, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    string noteStr = note.ToString(useFlats);
                    SizeF size = g.MeasureString(noteStr.Substring(0, 1), font);
                    using (Brush fillBrush = new SolidBrush((note as ColouredNote).BackColour))
                    {
                        g.FillRectangle(fillBrush, noteCentre.X - size.Width / 2,
                            noteCentre.Y - size.Height / 2, size.Width, size.Height);
                        if (noteStr.Length > 1)
                        {
                            SizeF accSize = g.MeasureString(noteStr.Substring(1), accFont);
                            g.FillRectangle(fillBrush,
                                noteCentre.X + size.Width / 2 - accSize.Width / 2,
                                noteCentre.Y - pixelsPerSquare / 10 - accSize.Height / 2,
                                accSize.Width, accSize.Height);
                        }
                    }
                }
            }
        }

        public INote FindNoteFromMouseCoordinates(Point mousePt, Image img, Point tlhc, int pixelsPerSquare,
            bool useFlats, IEnumerable<IMeasure> measures)
        {
            Point barStart = tlhc;
            foreach (IMeasure m in measures)
            {
                foreach (INote n in m.Notes)
                    if (NoteHitTest(mousePt, img, n, barStart, useFlats, pixelsPerSquare))
                        return n;
                barStart.Offset((m.QuarterBeatsPerBar * pixelsPerSquare) / 4, 0);
            }
            return null;
        }

        private bool NoteHitTest(Point mousePt, Image img, INote note, Point barStart, bool useFlats, int pixelsPerSquare)
        {
            using (Graphics g = Graphics.FromImage(img))
            {
                if (note.Pitch >= Note.StartRepeat)
                    return SpecialNoteHitTest(mousePt, g, note, barStart, useFlats, pixelsPerSquare);

                // Calculate horizontal position of note
                int pixelsIntoBar = note.Offset * pixelsPerSquare / 4;
                Point noteCentre = barStart;
                noteCentre.Offset(pixelsIntoBar + pixelsPerSquare / 2,
                    (note.VerticalOffset(useFlats) - Score.MinVerticalOffset) * pixelsPerSquare / 3);
                using (Font font = new Font("Arial Rounded MT", pixelsPerSquare / 3 - 2, FontStyle.Bold))
                using (Font accFont = new Font("Arial Rounded MT", pixelsPerSquare / 4, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    string noteStr = note.ToString(useFlats);
                    SizeF size = g.MeasureString(noteStr.Substring(0, 1), font);
                    if ((new RectangleF(noteCentre.X - size.Width / 2,
                        noteCentre.Y - size.Height / 2, size.Width, size.Height)).Contains(mousePt))
                        return true;
                    if (noteStr.Length > 1)
                    {
                        SizeF accSize = g.MeasureString(noteStr.Substring(1), accFont);
                        if ((new RectangleF(noteCentre.X + size.Width / 2 - accSize.Width / 2,
                            noteCentre.Y - pixelsPerSquare / 10 - accSize.Height / 2,
                            accSize.Width, accSize.Height)).Contains(mousePt))
                            return true;
                    }
                }
                return false;
            }
        }

        private bool SpecialNoteHitTest(Point mousePt, Graphics g, INote note, Point barStart, bool useFlats, int pixelsPerSquare)
        {
            int pixelsIntoBar = note.Offset * pixelsPerSquare / 4;
            RectangleF blob = new RectangleF(
                barStart.X + pixelsIntoBar + pixelsPerSquare / 8f,
                barStart.Y + pixelsPerSquare / 8f,
                pixelsPerSquare / 4f,
                3* pixelsPerSquare / 4f);
            RectangleF lowerBlob = blob;
            lowerBlob.Offset(0f, (VerticalSquares - 1) * pixelsPerSquare);
            if (note.Pitch == Note.StartRepeat && (blob.Contains(mousePt) || lowerBlob.Contains(mousePt)))
                return true;

            blob.Offset(pixelsPerSquare / 2f, 0f);
            lowerBlob.Offset(pixelsPerSquare / 2f, 0f);
            if (note.Pitch == Note.EndRepeat && (blob.Contains(mousePt) || lowerBlob.Contains(mousePt)))
                return true;

            // TODO: 1st and second time bars

            return false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    foreach (Image page in RenderedPages)
                        page.Dispose();

                RenderedPages.Clear();
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() => Dispose(true);
        #endregion
    }
}
