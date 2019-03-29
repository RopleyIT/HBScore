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
                RenderedPages.Add(RenderScorePage(i));
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

        private int IndexOfFirstBarOfPage(int page)
            => page >= pageBoundaries.Count ?
            barBoundaries.Count :
            barBoundaries.IndexOf(pageBoundaries[page]);

        private int IndexOfLastBarOfPage(int page)
            => IndexOfFirstBarOfPage(page + 1) - 1;

        private int IndexOfFirstBarOfSystem(int system)
            => system >= systemBoundaries.Count ?
            barBoundaries.Count :
            barBoundaries.IndexOf(systemBoundaries[system]);

        private int IndexOfLastBarInSystem(int system)
            => IndexOfFirstBarOfSystem(system + 1) - 1;

        IEnumerable<IMeasure> MeasuresOnPage(int page)
        {
            int firstBarOfPageIdx = IndexOfFirstBarOfPage(page);
            int firstBarOfNextPageIdx = IndexOfFirstBarOfPage(page + 1);
            return Score
                .Measures
                .Skip(firstBarOfPageIdx)
                .Take(firstBarOfNextPageIdx - firstBarOfPageIdx);
        }

        private int IndexOfSystemOnPageForBeat(int beat) 
            => IndexOfSystemForBeat(beat) % SystemsPerPage;

        private int IndexOfSystemForBeat(int beat)
        {
            int i = 0;
            while (i < systemBoundaries.Count && systemBoundaries[i] <= beat)
                i++;
            return i - 1;
        }

        private int IndexOfPageForBeat(int beat)
        {
            int i = 0;
            while (i < pageBoundaries.Count && pageBoundaries[i] <= beat)
                i++;
            return i - 1;
        }

        int BeatsInSystem(int system)
        {
            if (system < 0 || system >= systemBoundaries.Count)
                throw new ArgumentException("Invalid system index");
            int firstBeat = systemBoundaries[system];
            if (system == systemBoundaries.Count - 1)
                return totalBeats - firstBeat;
            else
                return systemBoundaries[system + 1] - firstBeat;
        }

        public Point GetTLHCOfSquare(int beat, int square)
        {
            // Origin for top left corner square on page

            Point point = new Point(179 - PixelsMargin, 190 - PixelsMargin);

            // Offset vertically by distance to top of
            // selected square

            point.Y += PixelsPerSquare * 
                (IndexOfSystemOnPageForBeat(beat) * (VerticalSquares + 1) + square);

            // Offset horizontally by distance to left of
            // selected square

            point.X += PixelsPerSquare *
                (beat - systemBoundaries[IndexOfSystemForBeat(beat)]);
            return point;
        }

        public Image RenderScorePage(int page)
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

            Bitmap bmp = new Bitmap
                (3508 - 2*PixelsMargin, 2480 - 2*PixelsMargin, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.White, 0, 0, 3507, 2480);
                for (int system = page * SystemsPerPage; system < systemBoundaries.Count; system++)
                {
                    int systemInPage = system % SystemsPerPage;

                    // Draw the border around the whole system

                    Pen thickPen = new Pen(Color.Black, 10);
                    Pen thinPen = new Pen(Color.Black, 4);

                    int firstBeat = systemBoundaries[systemInPage + page * SystemsPerPage];
                    int beatsInSystem = BeatsInSystem(systemInPage + page * SystemsPerPage);
                    Size systemSize = new Size
                        (
                            PixelsPerSquare * beatsInSystem,
                            PixelsPerSquare * VerticalSquares
                        );
                    Rectangle systemBorder = new Rectangle
                        (
                            GetTLHCOfSquare(firstBeat, 0),
                            systemSize
                        );
                    g.DrawRectangle(thickPen, systemBorder);

                    // Draw each vertical line in the system

                    for (int beat = firstBeat + 1; beat < firstBeat + beatsInSystem; beat++)
                    {
                        Point upper = GetTLHCOfSquare(beat, 0);
                        Point lower = new Point(upper.X, upper.Y + VerticalSquares * PixelsPerSquare);
                        Pen p = barBoundaries.Contains(beat) ? thickPen : thinPen;
                        g.DrawLine(p, upper, lower);
                    }

                    // Draw each horizontal line in the system

                    for(int square = 1; square < VerticalSquares; square++)
                    {
                        Point left = GetTLHCOfSquare(firstBeat, square);
                        Point right = new Point(left.X + beatsInSystem * PixelsPerSquare, left.Y);
                        g.DrawLine(thinPen, left, right);
                    }
                }
            }
            for (int i = IndexOfFirstBarOfPage(page); i < IndexOfFirstBarOfPage(page + 1); i++)
            {
                IMeasure m = Score.Measures[i];
                int firstBeatOfBar = barBoundaries[i];
                foreach(INote note in m.Notes)
                    BlankBackgroundForNote(bmp, note, firstBeatOfBar, Score.UseFlats);
                foreach (INote note in m.Notes)
                    AddNote(bmp, note, firstBeatOfBar, Score.UseFlats);
            }
            return bmp;
        }

        public Image AddNote(Image image, INote note, int firstBeatOfBar, bool useFlats)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                // Calculate horizontal position of note
                int pixelsIntoBar = note.Offset * PixelsPerSquare / 4;
                Point noteCentre = GetTLHCOfSquare(firstBeatOfBar, 0);
                noteCentre.Offset(pixelsIntoBar + PixelsPerSquare / 2,
                    (note.VerticalOffset(useFlats) - Score.MinVerticalOffset) * PixelsPerSquare / 3);
                Font font = new Font("Arial Rounded MT", 48, FontStyle.Bold);
                Font accFont = new Font("Arial Rounded MT", 32, FontStyle.Bold);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                string noteStr = note.ToString(useFlats);
                var size = g.MeasureString(noteStr.Substring(0, 1), font);
                g.DrawString(noteStr.Substring(0,1), font, Brushes.Black, noteCentre, sf);
                if(noteStr.Length > 1)
                    g.DrawString(noteStr.Substring(1), accFont, Brushes.Black, 
                        new PointF(noteCentre.X + size.Width/2, noteCentre.Y-16), sf);
            }
            return null;
        }
        public Image BlankBackgroundForNote(Image image, INote note, int firstBeatOfBar, bool useFlats)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                // Calculate horizontal position of note
                int pixelsIntoBar = note.Offset * PixelsPerSquare / 4;
                Point noteCentre = GetTLHCOfSquare(firstBeatOfBar, 0);
                noteCentre.Offset(pixelsIntoBar + PixelsPerSquare / 2,
                    (note.VerticalOffset(useFlats) - Score.MinVerticalOffset) * PixelsPerSquare / 3);
                Font font = new Font("Arial Rounded MT", 48, FontStyle.Bold);
                Font accFont = new Font("Arial Rounded MT", 32, FontStyle.Bold);
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
                        noteCentre.Y - 16 - accSize.Height / 2, 
                        accSize.Width, accSize.Height);
                }
            }
            return null;
        }
    }
}
