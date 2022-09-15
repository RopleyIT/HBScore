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

        /// <summary>
        /// Transpose the score up or down by a number of semitones
        /// </summary>
        /// <param name="interval">The number of semitones to
        /// transpose the score by. Positive = up.</param>
        
        void Transpose(int interval);
    }
}
