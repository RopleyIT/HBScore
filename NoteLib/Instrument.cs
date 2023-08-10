using System;
using System.Collections.Generic;
using System.Linq;

namespace NoteLib
{
    /// <summary>
    /// A musical instrument has a combination of harmonics that remain
    /// the same for that instrument across the range of frequencies.
    /// </summary>

    public class Instrument
    {
        private const float MaxToneLength = 15.0f;
        private readonly List<Harmonic> timbre;
        private readonly Dictionary<float, float[]> noteSamples = null;
        private readonly int sampleRate;

        private void AddSamplesForPitch(float pitch)
        {
            float frequency = Frequency(pitch);
            int sampleCount = (int)(MaxToneLength * sampleRate + 0.5);
            float[] samples = new float[sampleCount];
            for (int s = 0; s < sampleCount; s++)
                samples[s] = Sample(s, frequency);
            noteSamples.Add(pitch, samples);
        }

        /// <summary>
        /// The frequency of a note is given by 
        /// 13.75 * 2 ** (Pitch/12.0). Since the tuning system
        /// assumes equal temperament. This makes integer
        /// values of Pitch align with conventional semitones.
        /// </summary>

        public static float Frequency(float pitch)
            => (float)(13.75 * Math.Pow(2.0, pitch / 12.0));

        /// <summary>
        /// Calculate the sample amplitude at time 
        /// i/sampleRate seconds after the start of the waveform.
        /// </summary>
        /// <param name="i">The index of the sample at the 
        /// selected sample rate</param>
        /// <returns>The sample amplitude at the selected
        /// time, consisting of the added amplitudes of
        /// all the decayed harmonics.</returns>

        private float Sample(int i, float fundamental)
        {
            double sample = 0;
            foreach (Harmonic h in Harmonics)
            {
                double t = i / (double)sampleRate;
                double frequency = fundamental * h.Multiplier;
                double amplitude = h.Envelope(t);
                sample += amplitude * Math.Sin(2 * Math.PI * frequency * t);
            }
            return (float)sample;
        }

        private float ReleaseSample(int i, float fundamental, int length)
        {
            if(i < length)
                throw new ArgumentException("i < length");
            double sample = 0;
            double duration = length/ (double)sampleRate;
            double t = i / (double)sampleRate;
            if (t > duration + ReleaseDuration)
                return 0;
            foreach (Harmonic h in Harmonics)
            {
                double frequency = fundamental * h.Multiplier;
                double amplitude = h.ReleaseEnvelope(t, duration);
                sample += amplitude * Math.Sin(2 * Math.PI * frequency * t);
            }
            return (float)sample;
        }

        private IEnumerable<float> ReleaseSamplesForPitch(float pitch, int length) 
            => Enumerable
                .Range(length, ReleaseSampleCount)
                .Select(i => ReleaseSample(i, pitch, length));

        /// <summary>
        /// The voiced tail of the note after release
        /// </summary>

        public float ReleaseDuration =>
            Harmonics.Select(h => h.ReleaseDuration).Max();

        /// <summary>
        /// The number of samples taken for the
        /// release envelope to decay to zero
        /// </summary>
        
        public int ReleaseSampleCount =>
            (int)(ReleaseDuration * sampleRate);

        /// <summary>
        /// Given the pitch, return the table of signal
        /// samples for that pitch
        /// </summary>
        /// <param name="pitch">The pitch for which we want samples</param>
        /// <returns>The table of sound samples for this instrument and pitch</returns>

        public IEnumerable<float> SamplesForPitch(float pitch, int sampleCount)
        {
            InitSamplesForPitch(pitch);
            return noteSamples[pitch]
                .Take(sampleCount)
                .Concat(ReleaseSamplesForPitch(pitch, sampleCount));
        }

        /// <summary>
        /// Force early computation of the samples
        /// table for this instrument at this pitch
        /// </summary>
        /// <param name="pitch">The pitch for which
        /// we need samples</param>

        public void InitSamplesForPitch(float pitch)
        {
            if (!noteSamples.ContainsKey(pitch))
                AddSamplesForPitch(pitch);
        }

        public IEnumerable<Harmonic> Harmonics => timbre;

        public Instrument(IEnumerable<Harmonic> harmonics, int sampleRate)
        {
            timbre = harmonics.ToList();
            this.sampleRate = sampleRate;
            noteSamples = new Dictionary<float, float[]>();
        }
    }
}
