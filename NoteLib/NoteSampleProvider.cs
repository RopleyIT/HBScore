using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace NoteLib
{
    public class NoteSampleProvider : ISampleProvider
    {
        /// <summary>
        /// The metronome marking for the piece. This
        /// defines the length of one beat for each note.
        /// </summary>

        public int Metre { get; private set; }

        /// <summary>
        /// The set of notes in the piece
        /// </summary>

        private readonly IEnumerable<Note> Notes;
        private readonly List<float> samples = new List<float>();

        public NoteSampleProvider(int metre, IEnumerable<Note> notes)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            Metre = metre;
            Notes = notes;
            foreach (Note n in Notes)
                MergeSamples(n);
            NormaliseAmplitude();
        }

        private void NormaliseAmplitude()
        {
            float maxAmplitude = samples.Select(s => Math.Abs(s)).Max();
            for (int i = 0; i < samples.Count; i++)
                samples[i] /= maxAmplitude;
        }

        private void MergeSamples(Note n)
        {
            int start = n.StartSampleIndex(44100f, Metre);
            int end = n.EndSampleIndex(44100f, Metre);
            if (start > samples.Count)
                samples.AddRange(Enumerable.Repeat(0.0f, start - samples.Count));
            foreach (float f in n.UnshiftedSamples.Take(end - start))
                if (samples.Count > start)
                    samples[start++] += f;
                else
                {
                    samples.Add(f);
                    start++;
                }
        }

        public WaveFormat WaveFormat { get; private set; }

        private IEnumerator<float> sampleIterator = null;

        public int Read(float[] buffer, int offset, int count)
        {
            if (sampleIterator == null)
                sampleIterator = samples.GetEnumerator();
            int numSamplesRead = 0;
            while (numSamplesRead < count && sampleIterator.MoveNext())
            {
                buffer[offset++] = sampleIterator.Current;
                numSamplesRead++;
            }
            return numSamplesRead;
        }
    }
}
