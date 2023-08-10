using System.Collections.Generic;
using System.Linq;

namespace NoteLib
{
    public class Note
    {
        /// <summary>
        /// Constructor for an immutable note object
        /// </summary>
        /// <param name="instrument">The harmonic description of the
        /// instrument being played</param>
        /// <param name="amplitude">The amplitude or loudness of this
        /// instrument for this note</param>
        /// <param name="pitch">The pitch of the note</param>
        /// <param name="start">The start beat of the note</param>
        /// <param name="duration">The duration of the note</param>

        public Note(Instrument instrument, float amplitude, float pitch, float start, float duration)
        {
            Instrument = instrument;
            Amplitude = amplitude;
            Pitch = pitch;
            Start = start;
            Duration = duration;
            Instrument.InitSamplesForPitch(Pitch);
        }

        /// <summary>
        /// Scale factor to amplify/attenuate a note relative to
        /// the other notes being played. 1.0 is the normal
        /// amplitude, so this is a scale factor relative to that.
        /// </summary>

        public float Amplitude { get; private set; }

        /// <summary>
        /// The combination of harmonics with their amplitudes,
        /// decay rates and pitches
        /// </summary>

        public Instrument Instrument { get; private set; }

        /// <summary>
        /// The pitch of the note. The value is logarithmic, so
        /// that common pitch notes are on the integer boundaries.
        /// A = 440 Hz has the value 60.0. This gives the A = 13.75
        /// Hz the value 0.0 as the lowest note supported. This is
        /// two tones below the 32ft pedal CC of a large organ.
        /// </summary>

        public float Pitch { get; private set; }

        /// <summary>
        /// Normalised start time for the note. Easiest
        /// way to use this is to make each unit one beat
        /// of the bar.
        /// </summary>

        public float Start { get; private set; }

        /// <summary>
        /// The duration of the note before it is silenced.
        /// Uses the same unites as Start.
        /// </summary>

        public float Duration { get; private set; }

        /// <summary>
        /// Obtain the waveform samples as a sequence of
        /// 32 bit IEEE floating point numbers
        /// </summary>
        /// <param name="sampleRate">The sample rate, e.g. 44k1
        /// or 48k</param>
        /// <param name="metre">Metronome marking</param>
        /// <returns>The set of samples for this note.</returns>

        public IEnumerable<float> Samples(float sampleRate, int metre)
        {
            double samplesPerBeat = sampleRate * 60.0 / metre;
            int start = StartSampleIndex(sampleRate, metre);
            int duration = EndSampleIndex(sampleRate, metre) - start;
            return Enumerable
                .Repeat(0f, start)
                .Concat(UnshiftedSamples(duration)
                .Select(s => Amplitude * s));
        }

        /// <summary>
        /// The samples for the note itself, starting as soon
        /// as the note sound begins, rather than delayed to
        /// the point at which it appears in the score. Note that
        /// the enumerable might return more than sampleCount
        /// smaples, as there is a release envelope on the end
        /// of the waveform.
        /// </summary>

        public IEnumerable<float> UnshiftedSamples(int sampleCount)
            => Instrument.SamplesForPitch(Pitch, sampleCount);

        /// <summary>
        /// The sample number at which the note's sound begins
        /// </summary>
        /// <param name="sampleRate">The sample rate, e.g. 44k1
        /// or 48k</param>
        /// <param name="metre">Metronome marking</param>
        /// <returns>The sample number</returns>

        public int StartSampleIndex(float sampleRate, int metre)
            => (int)(Start * sampleRate * 60.0 / metre);

        /// <summary>
        /// The sample number just beyond the last non-zero
        /// sample in the note.
        /// </summary>
        /// <param name="sampleRate">The sample rate, e.g. 44k1
        /// or 48k</param>
        /// <param name="metre">Metronome marking</param>
        /// <returns>The sample number</returns>

        public int EndSampleIndex(float sampleRate, int metre)
            => (int)((Start + Duration) * sampleRate * 60.0 / metre);
    }
}
