using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBScore
{
    [Serializable]
    public class ColouredNote : Note
    {
        public ColouredNote(int offset, int pitch, int duration)
            : base(offset, pitch, duration)
        {
            ForeColour = Color.Black;
            BackColour = Color.White;
        }

        public override INote Clone()
        {
            var clone = new ColouredNote(Offset, Pitch, Duration)
            {
                ForeColour = ForeColour,
                BackColour = BackColour
            };
            return clone;
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
