using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace NoteLib
{
    public class NoteSampleProvider : ISampleProvider
    {
        public const float MasterVolume = 0.5f;

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
        private float[] outputSamples;
        private int idx;

        public NoteSampleProvider(int metre, IEnumerable<Note> notes)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            Metre = metre;
            Notes = notes;
            foreach (Note n in Notes)
                MergeSamples(n);
            NormaliseAmplitude();
            outputSamples = samples.ToArray();
            idx = 0;
        }

        private void NormaliseAmplitude()
        {
            float maxAmplitude = samples.Select(s => Math.Abs(s)).Max() * MasterVolume;
            for (int i = 0; i < samples.Count; i++)
                samples[i] /= maxAmplitude;
        }

        private void MergeSamples(Note n)
        {
            int start = n.StartSampleIndex(44100f, Metre);
            int end = n.EndSampleIndex(44100f, Metre);
            if (start > samples.Count)
                samples.AddRange(Enumerable.Repeat(0.0f, start - samples.Count));
            foreach (float f in n.UnshiftedSamples(end - start))
                if (samples.Count > start)
                    samples[start++] += f;
                else
                {
                    samples.Add(f);
                    start++;
                }
        }

        public WaveFormat WaveFormat { get; private set; }

        public int Read(float[] buffer, int offset, int count)
        {
            int numSamplesRead = 0;
            while (numSamplesRead < count && idx < outputSamples.Length)
            {
                buffer[offset++] = outputSamples[idx++];
                numSamplesRead++;
            }
            return numSamplesRead;
        }
    }
}
