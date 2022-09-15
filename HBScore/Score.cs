using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HBScore
{
    /// <summary>
    /// Representation of the data structures for a piece of music
    /// </summary>

    [Serializable]
    public class Score : IScore
    {
        public string Title { get; set; }
        public string Composer { get; set; }
        public string Information { get; set; }
        public string NoteList
        {
            get
            {
                IEnumerable<string> notesPitches = Measures
                    .SelectMany(m => m.Notes)
                    .Select(n => n.Pitch)
                    .Distinct()
                    .Where(n => n < Note.StartRepeat)
                    .OrderBy(i => i)
                    .Select(p => Note.NoteString(p));
                return string.Join(", ", notesPitches);
            }
        }

        public IList<IMeasure> Measures { get; private set; }

        public IList<IMeasure> CloneMeasures(int index, int count)
        {
            int oldCount = Measures.Count;
            if (index < 0 || index >= oldCount
                || (index + count) < 0 || (index + count) > oldCount)
                return new List<IMeasure>();
            IEnumerable<IMeasure> range = Measures.Skip(index).Take(count);
            return new List<IMeasure>(range.Select(m => (m as Measure).Clone()));
        }

        public void InsertMeasures(int index, IList<IMeasure> insertees)
        {
            if(index >= 0 || index <= Measures.Count)
            {
                foreach (var m in insertees.Reverse())
                    Measures.Insert(index, m);
            }
        }

        private int EndRepeatOffset(IMeasure bar)
            => (from n in bar.Notes
                where n.Pitch == Note.EndRepeat
                select n.Offset).First();
        
        private IEnumerable<INote> NotesBeforeEndRepeat(IMeasure bar)
        {
            var repeatOffset = EndRepeatOffset(bar) 
                + (bar.CompoundTime ? 6 : 4);

            return
                from n in bar.Notes
                where n.Pitch < Note.StartRepeat
                && n.Offset < repeatOffset
                orderby n.Offset
                select n;
        }

        private int StartRepeatOffset(IMeasure bar)
            => (from n in bar.Notes
                 where n.Pitch == Note.StartRepeat
                 select n.Offset).First();

        private IEnumerable<INote> NotesAfterStartRepeat(IMeasure bar)
        {
            var repeatOffset = StartRepeatOffset(bar);

            return
                from n in bar.Notes
                where n.Pitch < Note.StartRepeat
                && n.Offset >= repeatOffset
                orderby n.Offset
                select n;
        }

        /// <summary>
        /// Create the hybrid bar that is made from
        /// the half bars at each end of a repeat
        /// </summary>
        /// <param name="earlier">The bar at the beginning of a repeat</param>
        /// <param name="later">The bar at the end of a repeat</param>
        /// <returns>The new bar</returns>
        
        private IEnumerable<IMeasure> CreateRepeatBars(IMeasure earlier, IMeasure later)
        {
            // Validation

            if (earlier == null || later == null
                || earlier.CompoundTime != later.CompoundTime)
                yield break;
            if (!earlier.Notes.Any(n => n.Pitch == Note.StartRepeat)
                || !later.Notes.Any(n => n.Pitch == Note.EndRepeat))
                yield break;

            // Filters

            int startOffset = StartRepeatOffset(earlier);
            int endOffset = EndRepeatOffset(later);
            int sqPerBeat = earlier.CompoundTime ? 6 : 4;

            if (endOffset == later.QuarterBeatsPerBar - sqPerBeat)
            {
                yield return later;

                // Earlier bar has repeat mark at beginning, later
                // bar has repeat mark at the end. Just return
                // the two bars unaltered.

                if (startOffset == 0)
                    yield return earlier;

                // Earlier bar has repeat mark part way through.
                // Return the later bar followed by a truncated
                // second half of the earlier bar.

                else
                {
                    var measure = (new ScoreFactory())
                        .CreateMeasure(earlier.BeatsPerBar - startOffset / sqPerBeat,
                        earlier.CompoundTime);
                    foreach (INote note in NotesAfterStartRepeat(earlier))
                        measure.Notes.Add(note.Clone());
                    yield return measure;
                }
            }
            else
            {

                // Later bar has repeat mark half way through.
                // Return a truncated first half of the later bar
                // followed by all of the earlier bar.

                if (startOffset == 0)
                {
                    var measure = (new ScoreFactory())
                        .CreateMeasure(1 + endOffset / sqPerBeat, later.CompoundTime);
                    foreach (INote note in NotesBeforeEndRepeat(later))
                        measure.Notes.Add(note.Clone());
                    yield return measure;
                    yield return earlier;
                }

                // Both the later and earlier bars have repeat marks
                // other than on the barlines. Create a single new
                // bar that accumulates the beats from the two half
                // bars between the repeat signs.

                else
                {
                    int repeatBarBeats = 1 +
                        (earlier.QuarterBeatsPerBar - startOffset + endOffset) / sqPerBeat;
                    var measure = (new ScoreFactory())
                        .CreateMeasure(repeatBarBeats, earlier.CompoundTime);
                    foreach (INote note in NotesBeforeEndRepeat(later))
                        measure.Notes.Add(note.Clone());
                    foreach (INote note in NotesAfterStartRepeat(earlier))
                        measure.Notes.Add(note.Clone());
                    yield return measure;
                }
            }
        }

        /// <summary>
        /// Get the bar numbers of the sequence of start repeat
        /// and end repeat markers. Note this handles markers in
        /// the same bar, which could be either way round.
        /// The returned values are the bar numbers of the start
        /// repeats interleaved with the one's complement bar
        /// numbers of the end repeats.
        /// </summary>
        
        private IEnumerable<int> NextRepeatMark
        {
            get
            {
                foreach (int i in Enumerable.Range(0, Measures.Count))
                    foreach (INote note in Measures[i].Notes.OrderBy(n => n.Offset))
                        if (note.Pitch == Note.StartRepeat)
                            yield return i;
                        else if (note.Pitch == Note.EndRepeat)
                            yield return ~i;
            }
        }

        private List<int> repeatMarks = null; 

        /// <summary>
        /// Given a repeat marker, find the next repeat marker
        /// in the whole score. Allows for one of each repeat
        /// marker within the same bar, either way round.
        /// </summary>
        /// <param name="prevRepeat"></param>
        /// <returns>The value of the next repeat marker,
        /// or null if no more markers, or if the prevRepeat
        /// argument was not a valid repeat marker</returns>
        
        private int? NextRepeat(int prevRepeat)
        {
            int prevIdx = repeatMarks.IndexOf(prevRepeat);
            if (prevIdx < 0 || prevIdx >= repeatMarks.Count - 1)
                return null;
            else
                return repeatMarks[prevIdx + 1];
        }

        /// <summary>
        /// Obtain the bar number of the first repeat
        /// marker in the score
        /// </summary>
        /// <returns>The bar number of the first
        /// repeat marker, or null if there are no
        /// repeats in the score</returns>
        
        private int? FirstRepeat()
        {
            repeatMarks = new List<int>(NextRepeatMark);
            if (repeatMarks.Count > 0)
                return repeatMarks[0];
            else
                return null;
        }

        public IList<IMeasure> MeasuresWithRepeats
        {
            get
            {
                var repeatedMeasures = new List<IMeasure>();
                var repeatStarts = new Stack<int>();
                int nextMeasure = 0;
                int? nextRepeat = FirstRepeat();
                while(nextRepeat.HasValue)
                {
                    if (nextRepeat >= 0)
                        repeatStarts.Push(nextRepeat.Value);
                    else
                    {
                        // We have encountered and end of repeat marker.
                        // First copy all the bars up to the bar preceding
                        // the bar with the repeat marker.

                        while (nextMeasure < ~nextRepeat)
                            repeatedMeasures.Add(Measures[nextMeasure++]);

                        // Pop the bar we have to jump back to

                        int startOfRepeat = repeatStarts.Pop();
                        if (startOfRepeat >= 0) // Repeat not done yet
                        {
                            // Mark the fact that the repeat has been done

                            repeatStarts.Push(-1);

                            // Manufacture and add the bar(s) that contain the repeat

                            var repeatBars = CreateRepeatBars
                                (Measures[startOfRepeat], Measures[~nextRepeat.Value]);
                            repeatedMeasures.AddRange(repeatBars);
                            nextMeasure = startOfRepeat + 1;
                            nextRepeat = startOfRepeat;
                        }
                    }
                    nextRepeat = NextRepeat(nextRepeat.Value);
                }

                // Copy any trailing bars after the last repeat

                while (nextMeasure < Measures.Count)
                    repeatedMeasures.Add(Measures[nextMeasure++]);

                // Strip out the repeat markers

                foreach(IMeasure m in repeatedMeasures)
                {
                    IList<INote> barNotes = new List<INote>(m.Notes);
                    foreach (var note in barNotes)
                        if (note.Pitch == Note.StartRepeat || note.Pitch == Note.EndRepeat)
                            m.Notes.Remove(note);
                }

                return repeatedMeasures;
            }
        }

        public bool UseFlats { get; set; }

        public int MinVerticalOffset
        {
            get
            {
                IEnumerable<INote> notes = Measures
                    .SelectMany(m => m.Notes)
                    .Where(n=>n.Pitch < Note.StartRepeat);
                if (notes.Any())
                    return notes.Min(n => n.VerticalOffset(UseFlats));
                else return 0;
            }
        }

        public int MaxVerticalOffset
        {
            get
            {
                IEnumerable<INote> notes = Measures
                    .SelectMany(m => m.Notes)
                    .Where(n => n.Pitch < Note.StartRepeat);
                if (notes.Any())
                    return notes.Max(n => n.VerticalOffset(UseFlats));
                else return 0;
            }
        }

        public Score(bool useFlats)
        {
            UseFlats = useFlats;
            Measures = new List<IMeasure>();
        }

        public void Transpose(int interval)
        {
            foreach (var measure in Measures)
                foreach (var note in measure.Notes)
                    if(note.Pitch + interval < Note.StartRepeat 
                        && note.Pitch + interval > 0)
                        note.Pitch += interval;
        }
    }
}
