using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HBScore
{
    /// <summary>
    /// Representation of the data structures for a piece of music
    /// </summary>

    public class Score : IScore
    {
        public IList<IMeasure> Measures { get; private set; }
        public bool UseFlats { get; private set; }

        public int MinVerticalOffset
        {
            get
            {
                var notes = Measures.SelectMany(m => m.Notes);
                if (notes.Any())
                    return notes.Min(n => n.VerticalOffset(UseFlats));
                else return 0;
            }
        }

        public int MaxVerticalOffset
        {
            get
            {
                var notes = Measures.SelectMany(m => m.Notes);
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
        /// 7 = 05F#, 8 = 05F, 9 = 06E, 10 = 07D#, 11 = 07D, 12 = 08C#.
        /// Each octave below this adds multiples of 12 to the note value.
        /// Hence 25 = 15C, and 30 = 18G.
        /// </summary>

        public int Pitch { get; private set; }

        /// <summary>
        /// The length of the note in 1/4 beats
        /// </summary>

        public int Duration { get; private set; }

        public Note(int offset, int pitch, int duration)
        {
            Offset = offset;
            Pitch = pitch;
            Duration = duration;
        }

        private static string[] sharpStrings =
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

        private static string[] flatStrings =
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

        public string ToString(bool useFlats)
        {
            int noteIndex = (Pitch - 1) % 12;
            return useFlats ? 
                flatStrings[noteIndex] : sharpStrings[noteIndex];
        }

        public override string ToString()
        {
            return ToString(false);
        }

        private static int[] sharpOffsets =
        {
            0, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 7, 7, 8
        };

        private static int[] flatOffsets =
        {
            0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 6, 6, 7, 7
        };

        public int VerticalOffset(bool useFlats)
        {
            int noteIndex = (Pitch - 1) % 12;
            return 8 * ((Pitch - 1) / 12) + 
                (useFlats ? flatOffsets[noteIndex] : sharpOffsets[noteIndex]);
        }
    }
}
