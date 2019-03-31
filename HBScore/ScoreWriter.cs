using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace HBScore
{
    public class ScoreWriter
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
        {}

        private static IScore CreateEmptyScore
            (int measures, int beatsPerBar, bool compound, bool useFlats)
        { 
            if(beatsPerBar < 2 || beatsPerBar > 6)
                throw new ArgumentException
                    ("Only support 2 to 6 beats per bar");
            var score = new Score(useFlats);
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
            ListPages();
            RenderedPages = new List<Image>(pageBoundaries.Count);
            for (int i = 0; i < pageBoundaries.Count; i++)
                RenderedPages.Add(RenderPage(i));
        }

        List<int> pageBoundaries;
        List<int> systemBoundaries;
        List<int> barBoundaries;
        int totalBeats;

        private void ListPages()
        {
            barBoundaries = new List<int>();
            int beats = 0;
            systemBoundaries = new List<int>() { 0 };
            pageBoundaries = new List<int>() { 0 };
            foreach(Measure m in Score.Measures)
            {
                barBoundaries.Add(beats);
                if (beats + m.BeatsPerBar - systemBoundaries.Last() > MaxBeatsPerSystem)
                {
                    systemBoundaries.Add(beats);
                    if (systemBoundaries.Count % SystemsPerPage == 0)
                        pageBoundaries.Add(systemBoundaries.Last());
                }
                beats += m.BeatsPerBar;
            }
            totalBeats = beats;
        }

        private int IndexOfFirstBarOfSystem(int system)
            => system >= systemBoundaries.Count ?
            barBoundaries.Count :
            barBoundaries.IndexOf(systemBoundaries[system]);

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
                var measures = Score.Measures
                    .Skip(firstBarOfSystemIdx)
                    .Take(firstBarBeyondSystemIdx - firstBarOfSystemIdx);
                Point tlhc = new Point(179 - PixelsMargin, 190 - PixelsMargin);
                tlhc.Offset(0, PixelsPerSquare * (VerticalSquares + 1) * (system % SystemsPerPage));
                RenderMeasures(bmp, tlhc, PixelsPerSquare, firstBarOfSystemIdx, Score.UseFlats, measures);
                system++;
            }
            return bmp;
        }

        public void RenderMeasures(Image img, Point tlhc, int pixelsPerSquare, 
            int barNum, bool useFlats, IEnumerable<IMeasure> measures)
        {
            Font barNumFont = new Font("Arial Rounded MT", pixelsPerSquare/6);
            Pen thickPen = new Pen(Color.Black, pixelsPerSquare/15);
            Pen thinPen = new Pen(Color.Black, pixelsPerSquare/36);
            Graphics g = Graphics.FromImage(img);

            var beatsInSystem = measures.Sum(m => m.BeatsPerBar);

            // Draw the border around the whole system

            Size systemSize = new Size
                (
                    pixelsPerSquare * beatsInSystem,
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
                for (int beat = 0; beat < m.BeatsPerBar; beat++)
                {
                    if (beat == 0)
                    {
                        g.DrawString((++barNum).ToString(),
                            barNumFont, Brushes.Black, new Point
                            (upper.X, upper.Y - pixelsPerSquare/4 - 4));
                        if (m != measures.First())
                            g.DrawLine(thickPen, upper, lower);
                    }
                    else
                        g.DrawLine(thinPen, upper, lower);
                    upper.Offset(pixelsPerSquare, 0);
                    lower.Offset(pixelsPerSquare, 0);
                }
            }

            // Draw each horizontal line in the system

            Point left = tlhc;
            Point right = new Point(left.X + beatsInSystem * pixelsPerSquare, left.Y);
            for (int square = 1; square < VerticalSquares; square++)
            {
                left.Offset(0, pixelsPerSquare);
                right.Offset(0, pixelsPerSquare);
                g.DrawLine(thinPen, left, right);
            }

            // White out any areas where characters will be written

            Point barStart = tlhc;
            foreach(IMeasure m in measures)
            {
                foreach(INote note in m.Notes)
                    InsertBlank(img, note, barStart, useFlats, pixelsPerSquare);
                barStart.Offset(m.BeatsPerBar * pixelsPerSquare, 0);
            }

            // Write the notes to the score

            barStart = tlhc;
            foreach (IMeasure m in measures)
            {
                foreach (INote note in m.Notes)
                    InsertNote(img, note, barStart, useFlats, pixelsPerSquare);
                barStart.Offset(m.BeatsPerBar * pixelsPerSquare, 0);
            }
            g.Dispose();
            barNumFont.Dispose();
            thinPen.Dispose();
            thickPen.Dispose();
        }

        private void InsertNote(Image img, INote note, Point barStart, bool useFlats, int pixelsPerSquare)
        {
            using (Graphics g = Graphics.FromImage(img))
            {
                // Calculate horizontal position of note
                int pixelsIntoBar = note.Offset * pixelsPerSquare / 4;
                Point noteCentre = barStart;
                noteCentre.Offset(pixelsIntoBar + pixelsPerSquare / 2,
                    (note.VerticalOffset(useFlats) - Score.MinVerticalOffset) * pixelsPerSquare / 3);
                Font font = new Font("Arial Rounded MT", pixelsPerSquare / 3 - 2, FontStyle.Bold);
                Font accFont = new Font("Arial Rounded MT", pixelsPerSquare / 4, FontStyle.Bold);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                string noteStr = note.ToString(useFlats);
                var size = g.MeasureString(noteStr.Substring(0, 1), font);
                g.DrawString(noteStr.Substring(0, 1), font, Brushes.Black, noteCentre, sf);
                if (noteStr.Length > 1)
                    g.DrawString(noteStr.Substring(1), accFont, Brushes.Black,
                        new PointF(noteCentre.X + size.Width / 2, noteCentre.Y - pixelsPerSquare / 10), sf);
            }
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
                Font font = new Font("Arial Rounded MT", pixelsPerSquare / 3 - 2, FontStyle.Bold);
                Font accFont = new Font("Arial Rounded MT", pixelsPerSquare / 4, FontStyle.Bold);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                string noteStr = note.ToString(useFlats);
                var size = g.MeasureString(noteStr.Substring(0, 1), font);
                g.FillRectangle(Brushes.White, noteCentre.X - size.Width / 2,
                    noteCentre.Y - size.Height / 2, size.Width, size.Height);
                if (noteStr.Length > 1)
                {
                    var accSize = g.MeasureString(noteStr.Substring(1), accFont);
                    g.FillRectangle(Brushes.White,
                        noteCentre.X + size.Width / 2 - accSize.Width / 2,
                        noteCentre.Y - pixelsPerSquare / 10 - accSize.Height / 2,
                        accSize.Width, accSize.Height);
                }
            }
        }
    }
}
