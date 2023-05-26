using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBScore
{
    public interface IMeasure
    {
        /// <summary>
        /// The number of beats in a bar for this bar.
        /// </summary>

        int BeatsPerBar { get; }

        /// <summary>
        /// The number of quarter beats in the bar. Used
        /// for compound time, where time signatures like
        /// 9:8 have nine half beats in a bar
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

        /// <summary>
        /// True if any note in this bar is 
        /// followed by a double bar line
        /// </summary>

        bool HasDoubleBarLine { get; }

        /// <summary>
        /// Create a clone of the entire bar
        /// </summary>
        /// <returns>The cloned bar</returns>
        
        IMeasure Clone();
    }
}
