using System;
using System.Collections.Generic;
using System.Text;

namespace HBScore
{
    public class ScoreFactory : IScoreFactory
    {
        public IScore CreateScore(bool useFlats = false)
            => new Score(useFlats);

        public IMeasure CreateMeasure(int beats, bool compound)
            => new Measure(beats, compound);

        public INote CreateNote(int offset, int pitch, int duration)
            => new ColouredNote(offset, pitch, duration);
    }
}
