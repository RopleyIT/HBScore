using System.Collections.Generic;
using NoteLib;

namespace HBScore
{
    public static class Playback
    {
        private const float Overlap = 1;

        public static List<NoteLib.Note> GenerateNotesFrom(IScore score)
        {
            Harmonic[] harmonics = new Harmonic[]
            {
                new Harmonic(1.0f, 1.0f, 0.3f, 0.02f, 0.1f),
                //new Harmonic(0.4f, 3.0f, 0.3f, 0.02f, 0.1f),
                //new Harmonic(0.1f, 5.0f, 0.3f, 0.02f, 0.1f),
                //new Harmonic(0.05f, 6.0f, 0.3f, 0.02f, 0.1f),
                //new Harmonic(0.05f, 8.0f, 0.3f, 0.02f, 0.1f)
            };

            Instrument bells = new Instrument(harmonics, 44100);
            List<NoteLib.Note> notes = new List<NoteLib.Note>();
            int firstQuarterBeatOfMeasure = 0;
            IList<IMeasure> expandedMeasures = score.MeasuresWithRepeats;
            foreach (IMeasure m in expandedMeasures)
            {
                foreach (INote note in m.Notes)
                {
                    float pitch = 88 - note.Pitch;
                    float duration = note.Duration / 4.0f + Overlap;
                    float start = (note.Offset + firstQuarterBeatOfMeasure)/4.0f;
                    notes.Add(new NoteLib.Note(bells, 0.3f, pitch, start, duration));
                }
                firstQuarterBeatOfMeasure += m.QuarterBeatsPerBar;
            }
            return notes;
        }
    }
}
