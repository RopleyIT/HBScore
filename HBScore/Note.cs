﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBScore
{
    [Serializable]
    public class Note : INote
    {
        /// <summary>
        /// Distance into bar. Measured in 1/4 of a beat, hence
        /// use multiples of 4 for consecutive beats, or multiples
        /// of 2 for quavers. When using compound time, each 'beat'
        /// is 6 units, as it corresponds to a beat and a half of
        /// non-compound time.
        /// For repeat markers, the offset is for the first
        /// repeated beat for a start repeat. For an end repeat,
        /// the offset is to the first beat after the repeated
        /// block.
        /// </summary>

        public int Offset { get; private set; }

        /// <summary>
        /// The notes themselves. These are formatted as follows:
        /// 1 = 01C, 2 = 02B, 3 = 03A#, 4 = 03A, 5 = 04G#, 6 = 04G,
        /// 7 = 05F#, 8 = 05F, 9 = 06E, 10 = 07D#, 11 = 07D, 12 = 1C#.
        /// Each octave below this adds multiples of 12 to the note value.
        /// Hence 37 = 15C, and 42 = 18G. Values of 128 and above are
        /// special markers. 128 = start repeat, 129 = end repeat,
        /// 130 = first time bar/sequence, 131 = second time bar/sequence,
        /// 132 = double bar line section end marker.
        /// </summary>

        public int Pitch { get; set; }
        public const int StartRepeat = 128;
        public const int EndRepeat = 129;
        public const int FirstTimeSequence = 130;
        public const int SecondTimeSequence = 131;
        public const int DoubleBar = 132;

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

        /// <summary>
        /// Create a deep copy of the note
        /// </summary>
        /// <returns>A duplicate of the note</returns>

        public virtual INote Clone() => new Note(Offset, Pitch, Duration);

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

        [NonSerialized]
        private static readonly string[] specialStrings =
        {
            "|:",
            ":|",
            "||",
            "|1st|",
            "|2nd|"
        };

        public static string NoteName(int pitch, bool useFlats)
        {
            if (pitch >= Note.StartRepeat)
                return specialStrings[pitch - Note.StartRepeat];

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
}
