using System;
using System.Windows.Forms;

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

        /// <summary>
        /// Create a harmonic of an instrument
        /// </summary>
        /// <param name="amplitude">The peak amplitude of this 
        /// frequency component</param>
        /// <param name="multiplier">The harmonic, e.g. 2 for 
        /// octave, 3 for 2nd harmonic etc.</param>
        /// <param name="decay">The decay rate for the harmonic
        /// </param>
        /// <param name="attackDuration">How long it takes for
        /// the note to build up to peak amplitude at the
        /// beginning of sounding</param>
        /// <param name="releaseDuration">The time taken for
        /// the harmonic to decay to zero on release</param>
        /// <param name="decayDuration">The period of time the
        /// harmonic decays for before plateauing</param>
        
        public Harmonic(float amplitude, float multiplier, float decay,
            float attackDuration = 0, 
            float releaseDuration = 0,
            float decayDuration = float.MaxValue)
        {
            Amplitude = amplitude;
            Multiplier = multiplier;
            Decay = decay;
            AttackDuration = attackDuration;
            DecayDuration = decayDuration;
            ReleaseDuration = releaseDuration;
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

        public float Envelope(double t)
        {
            // From t = 0 up to AttackDuration, the amplitude rises
            // exponentially to Amplitude vaue at t = AttackDuration.
            // We use a square law curve: env = K*t^2 where
            // K = Amplitute/(AttackDuration^2)

            if (t > 0 && t <= AttackDuration)
                return (float)(Amplitude * t * t 
                    / (AttackDuration*AttackDuration));

            // The decayed amplitude follows an exponential decay.
            // This is calculated as Amplitude * Exp(Ln(1/Sqrt(2)) t / Decay)
            // Which gives an amplitude decay of Sqrt(2) for each Delay
            // period. Note that for Decay values of zero, there is no
            // decay, and the waveform has a continuous amplitude.

            if (t > AttackDuration && t <= DecayDuration)
                return (float)(Decay == 0 ? Amplitude 
                    : Amplitude * Math.Exp(-0.3465736 * t / Decay));

            // Between decay end and release, the amplitude remains constant

            if (t > DecayDuration)
                return Envelope(DecayDuration);
            return 0;
        }

        /// <summary>
        /// Once a note is released, the waveform tapers
        /// following a release envelope rather than just
        /// drops instantly to zero.As release could happen
        /// during attack, decay, or constant amplitude
        /// phases, we get the initial release amplitude,
        /// then decay using a square law over the
        /// release interval.
        /// </summary>
        /// <param name="t">The time offset into the note</param>
        /// <param name="duration">The music length of the note.</param>
        /// <returns>The sample at time 't' allowing for a
        /// release envelope to be applied</returns>
        
        public float ReleaseEnvelope(double t, double duration)
        {
            if (t <= duration)
                return Envelope(t);
            if (t > duration + ReleaseDuration)
                return 0;
            float aRelease = Envelope(duration);
            t = t - duration - ReleaseDuration;
            return (float)(aRelease * t * t
                / (ReleaseDuration * ReleaseDuration));
        }

        /// <summary>
        /// The duration of the attack 
        /// phase of the harmonic's envelope. If
        /// zero there is no attack phase.
        /// </summary>
        public float AttackDuration { get; set; }

        /// <summary>
        /// The duration of the decay phase of
        /// the harmonic's envelope. At the end
        /// of the decay phase, the envelope
        /// remains constant until release.
        /// </summary>
        public float DecayDuration { get; set; }

        /// <summary>
        /// The duration of the release phase of
        /// the harmonic's envelope. If zero the
        /// release is immediate.
        /// </summary>
        public float ReleaseDuration { get; set; }
    }
}
