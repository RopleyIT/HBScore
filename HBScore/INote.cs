using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBScore
{
    public interface INote
    {
        /// <summary>
        /// Distance into bar. Measured in 1/4 of a beat, hence
        /// use multiples of 4 for consecutive beats, or multiples
        /// of 2 for quavers. When using compound time, each 'beat'
        /// is 6 units, as it corresponds to a beat and a half of
        /// non-compound time.
        /// </summary>

        int Offset { get; }

        /// <summary>
        /// The notes themselves. These are formatted as follows:
        /// 1 = 01C, 2 = 02B, 3 = 03A#, 4 = 03A, 5 = 04G#, 6 = 04G,
        /// 7 = 05F#, 8 = 05F, 9 = 06E, 10 = 07D#, 11 = 07D, 12 = 08C#.
        /// Each octave below this adds multiples of 12 to the note value.
        /// Hence 25 = 15C, and 30 = 18G.
        /// </summary>

        int Pitch { get; set; }

        /// <summary>
        /// The length of the note in 1/4 beats
        /// </summary>

        int Duration { get; set; }

        /// <summary>
        /// Create a deep copy of the note
        /// </summary>
        /// <returns>A duplicate of the note</returns>

        INote Clone();

        /// <summary>
        /// Generate the output string corresponding to the selected note
        /// </summary>
        /// <param name="useFlats">True for black notes to be
        /// represented by flats, false as sharps</param>
        /// <returns>The character string representing the note</returns>

        string ToString(bool useFlats);

        /// <summary>
        /// Give a vertical distance from the top of a stave or system
        /// to be used for rendering the note in the right place
        /// </summary>
        /// <param name="useFlats">True for black notes to be
        /// represented by flats, false as sharps</param>
        /// <returns>The vertical offset for rendering the note</returns>

        int VerticalOffset(bool useFlats);
    }
}
