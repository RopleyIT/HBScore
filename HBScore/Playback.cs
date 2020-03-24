using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoteLib;

namespace HBScore
{
    public static class Playback
    {
        const float Overlap = 1;

        public static List<NoteLib.Note> GenerateNotesFrom(IScore score)
        {
            Harmonic[] harmonics = new Harmonic[]
            {
                new Harmonic(1.0f, 1.0f, 0.2f),
                new Harmonic(0.2f, 3.0f, 0.2f),
                new Harmonic(0.1f, 5.0f, 0.1f)
            };

            Instrument bells = new Instrument(harmonics, 44100);
            var notes = new List<NoteLib.Note>();
            int firstBeatOfMeasure = 0;
            foreach(Measure m in score.Measures)
            {
                foreach(var note in m.Notes)
                {
                    float pitch = 88 - note.Pitch;
                    float duration = note.Duration / 4.0f + Overlap;
                    float start = note.Offset / 4.0f + firstBeatOfMeasure;
                    notes.Add(new NoteLib.Note(bells, 0.3f, pitch, start, duration));
                }
                firstBeatOfMeasure += m.BeatsPerBar;
            }
            return notes;
        }
    }
}
