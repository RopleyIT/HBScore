using System.Collections.Generic;

namespace HBScore
{
    public interface IScore
    {
        /// <summary>
        /// Information about the piece of music
        /// </summary>
        string Title { get; set; }
        string Composer { get; set; }
        string Information { get; set; }
        string NoteList { get; }

        /// <summary>
        /// The collection of bars or measures in the score
        /// </summary>

        IList<IMeasure> Measures { get; }

        /// <summary>
        /// The collection of measures in the score, as
        /// played with repeats
        /// </summary>
        
        IList<IMeasure> MeasuresWithRepeats { get; }

        /// <summary>
        /// True if black notes should be represented by
        /// the white note above followed by the flat sign.
        /// False if they should be represented by the
        /// note below followed by the sharp sign.
        /// </summary>

        bool UseFlats { get; set; }

        /// <summary>
        /// The vertical offset value for the highest note
        /// used in the score. Each increment corresponds
        /// to one note of a diatonic scale.
        /// </summary>

        int MinVerticalOffset { get; }

        /// <summary>
        /// The vertical offset value for the lowest note
        /// used in the score. Each increment corresponds
        /// to one note of a diatonic scale.
        /// </summary>

        int MaxVerticalOffset { get; }
    }

    public interface IMeasure
    {
        /// <summary>
        /// The number of beats in a bar for this bar.
        /// </summary>

        int BeatsPerBar { get; }

        /// <summary>
        /// The number of quarter beats in the bar. Used
        /// for compound time, where time signatures like
        /// 9:8 have  nine half beats in a bar
        /// </summary>
        
        int QuarterBeatsPerBar { get; }

        /// <summary>
        /// True if each beat is a compound beat, meaning
        /// it divides into thirds rather than halves
        /// </summary>

        bool CompoundTime { get; }

        /// <summary>
        /// The collection of notes that appear in this bar
        /// </summary>

        IList<INote> Notes { get; }

        /// <summary>
        /// True if this bar contains a repeat mark at the
        /// beginning of the bar
        /// </summary>
        
        bool StartsRepeat { get; }

        /// <summary>
        /// True if this bar contains a repeat mark
        /// at the end of the bar
        /// </summary>
        
        bool EndsRepeat { get; }

    }

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

        int Pitch { get; }

        /// <summary>
        /// The length of the note in 1/4 beats
        /// </summary>

        int Duration { get; set; }

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
