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
                    .OrderBy(i => i)
                    .Select(p => Note.NoteString(p));
                return string.Join(", ", notesPitches);
            }
        }

        public IList<IMeasure> Measures { get; private set; }
        public bool UseFlats { get; set; }

        public int MinVerticalOffset
        {
            get
            {
                IEnumerable<INote> notes = Measures.SelectMany(m => m.Notes);
                if (notes.Any())
                    return notes.Min(n => n.VerticalOffset(UseFlats));
                else return 0;
            }
        }

        public int MaxVerticalOffset
        {
            get
            {
                IEnumerable<INote> notes = Measures.SelectMany(m => m.Notes);
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
    }

    /// <summary>
    /// One bar in the score
    /// </summary>

    [Serializable]
    public class Measure : IMeasure
    {
        public int BeatsPerBar { get; private set; }

        public bool CompoundTime { get; private set; }

        public IList<INote> Notes { get; private set; }

        public Measure(int beats, bool compound)
        {
            BeatsPerBar = beats;
            CompoundTime = compound;
            Notes = new List<INote>();
        }
    }

    [Serializable]
    public class Note : INote
    {
        /// <summary>
        /// Distance into bar. Measured in 1/4 of a beat, hence
        /// use multiples of 4 for consecutive beats, or multiples
        /// of 2 for quavers. When using compound time, each 'beat'
        /// is 6 units, as it corresponds to a beat and a half of
        /// non-compound time.
        /// </summary>

        public int Offset { get; private set; }

        /// <summary>
        /// The notes themselves. These are formatted as follows:
        /// 1 = 01C, 2 = 02B, 3 = 03A#, 4 = 03A, 5 = 04G#, 6 = 04G,
        /// 7 = 05F#, 8 = 05F, 9 = 06E, 10 = 07D#, 11 = 07D, 12 = 1C#.
        /// Each octave below this adds multiples of 12 to the note value.
        /// Hence 37 = 15C, and 42 = 18G.
        /// </summary>

        public int Pitch { get; private set; }

        /// <summary>
        /// The length of the note in 1/4 beats
        /// </summary>

        public int Duration { get; set; }

        public Note(int offset, int pitch, int duration)
        {
            Offset = offset;
            Pitch = pitch;
            Duration = duration;
        }

        [NonSerialized]
        private static readonly string[] sharpStrings =
        {
            "C",
            "B",
            "A#",
            "A",
            "G#",
            "G",
            "F#",
            "F",
            "E",
            "D#",
            "D",
            "C#"
        };

        [NonSerialized]
        private static readonly string[] flatStrings =
        {
            "C",
            "B",
            "B\u266D",
            "A",
            "A\u266D",
            "G",
            "G\u266D",
            "F",
            "E",
            "E\u266D",
            "D",
            "D\u266D"
        };

        public static string NoteName(int pitch, bool useFlats)
        {
            int noteIndex = (pitch - 1) % 12;
            return useFlats ?
                flatStrings[noteIndex] : sharpStrings[noteIndex];
        }

        public string ToString(bool useFlats) => NoteName(Pitch, useFlats);

        public override string ToString() => ToString(false);

        [NonSerialized]
        private static readonly int[] sharpOffsets =
        {
            0, 1, 2, 2, 3, 3, 4, 4, 5, 6, 6, 7
        };

        [NonSerialized]
        private static readonly int[] flatOffsets =
        {
            0, 1, 1, 2, 2, 3, 3, 4, 5, 5, 6, 6
        };

        public int VerticalOffset(bool useFlats) => BellNumber(Pitch, useFlats);

        public static int BellNumber(int pitch, bool useFlats)
        {
            int noteIndex = (pitch - 1) % 12;
            return 7 * ((pitch - 1) / 12) +
                (useFlats ? flatOffsets[noteIndex] : sharpOffsets[noteIndex]);
        }

        public static string NoteString(int pitch)
        {
            string noteString;
            int bellNum = BellNumber(pitch, false);
            if (bellNum < 7)
                noteString = "0" + (1 + bellNum) + NoteName(pitch, false);
            else
                noteString = (bellNum - 6) + NoteName(pitch, false);
            if (noteString.EndsWith("#"))
            {
                bellNum = BellNumber(pitch, true);
                if (bellNum < 7)
                    noteString += "/0" + (1 + bellNum) + NoteName(pitch, true);
                else
                    noteString += "/" + (bellNum - 6) + NoteName(pitch, true);
            }
            return noteString;
        }

        public virtual Color ForeColour { get; set; } = Color.Black;

        public virtual Color BackColour { get; set; } = Color.White;
    }

    [Serializable]
    public class ColouredNote : Note
    {
        public ColouredNote(int offset, int pitch, int duration)
            : base(offset, pitch, duration)
        {
            ForeColour = Color.Black;
            BackColour = Color.White;
        }

        public override Color ForeColour
        {
            get;
            set;
        }

        public override Color BackColour
        {
            get;
            set;
        }
    }
}
