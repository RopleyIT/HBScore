using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBScore
{
    /// <summary>
    /// One bar in the score
    /// </summary>

    [Serializable]
    public class Measure : IMeasure
    {
        public int BeatsPerBar { get; private set; }

        public int QuarterBeatsPerBar =>
            CompoundTime ? BeatsPerBar * 6 : BeatsPerBar * 4;

        public int StartOfBeat(int quarterBeat)
            => quarterBeat - (CompoundTime ? quarterBeat % 6 : quarterBeat % 4);

        public bool CompoundTime { get; private set; }

        public IList<INote> Notes { get; private set; }

        public Measure(int beats, bool compound)
        {
            BeatsPerBar = beats;
            CompoundTime = compound;
            Notes = new List<INote>();
        }

        public Measure Clone()
        {
            var clone = new Measure(BeatsPerBar, CompoundTime);
            foreach (var n in Notes)
                if (n is ColouredNote)
                    clone.Notes.Add((n as ColouredNote).Clone());
                else
                    clone.Notes.Add((n as Note).Clone());
            return clone;
        }

        public bool StartsRepeat
            => Notes.Any(n => n.Pitch == Note.StartRepeat);

        public bool EndsRepeat
            => Notes.Any(n => n.Pitch == Note.EndRepeat);

        public bool HasDoubleBarLine
            => Notes.Any(n => n.Pitch == Note.DoubleBar);
    }
}
