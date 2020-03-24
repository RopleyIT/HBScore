using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteLib
{
    public class Harmonic
    {
        /// <summary>
        /// Scale factor for amplitude of this harmonic relative to
        /// the amplitude of the note to which it belongs. 1.0 means
        /// no amplification and no attenuation.
        /// </summary>
        
        public float Amplitude { get; private set; }

        /// <summary>
        /// Which harmonic it is. 1.0 is the fundamental
        /// frequency. Integer multiple above that are the
        /// usual harmonics. For unusual sounds, non-integral
        /// values can also be used.
        /// </summary>
        
        public float Multiplier { get; private set; }

        /// <summary>
        /// The decay rate of the harmonic. Gives the time over
        /// which the amplitude of the harmonic reduces to root
        /// two of its initial value (its power halves). The
        /// special value of zero means it is continuous at
        /// this amplitude rather than instantaneously gone.
        /// </summary>
        
        public float Decay { get; private set; }

        public Harmonic(float amplitude, float multiplier, float decay)
        {
            Amplitude = amplitude;
            Multiplier = multiplier;
            Decay = decay;
        }

        /// <summary>
        /// The decayed amplitude follows an exponential decay. 
        /// This is calculated as Amplitude * Exp(Ln(1/Sqrt(2)) t / Decay)
        /// Which gives an amplitude decay of Sqrt(2) for each Delay
        /// period. Note that for Decay values of zero, there is no
        /// decay, and the waveform has a continuous amplitude.
        /// </summary>
        /// <param name="t">The time into the waveform, needed
        /// so that an exponential decay can be applied.</param>
        /// <returns>The amplitude of the harmonic after the
        /// specified time delay.</returns>

        public float DecayedAmplitude(double t)
        {
            if (Decay == 0)
                return (float)Amplitude;
            else
                return (float)(Amplitude * Math.Exp(-0.3465736 * t / Decay));
        }
    }
}
