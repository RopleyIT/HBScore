using NoteLib;
using System.Text.RegularExpressions;
using NAudio.Wave;

namespace Synthesiser
{
    public partial class Form1 : Form
    {
        private static Regex posInt = RxPosInt();
        private static Regex posReal = RxReal();

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            string err = ValidateDataEntry();
            if (!string.IsNullOrWhiteSpace(err))
            {
                MessageBox.Show(err, "Invalid values",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Unison harmonic, 1s decay, 10ms attack and release
            Harmonic[] h = new Harmonic[]
            {
                new Harmonic(1.0f, 1.0f, 1.0f, 0.01f, 0.01f)
            };

            Instrument inst = new Instrument(h, 44100);
            Note[] n = new Note[]
            { 
                new Note(inst, 1.0f,
                    (float)fundamental, (float)duration, (float)duration),
                 new Note(inst, 1.0f,
                    (float)fundamental + 4, (float)(2*duration), 
                    (float)duration),
                 new Note(inst, 1.0f,
                    (float)fundamental + 7, (float)(3*duration),
                    (float)duration),
            };
            NoteSampleProvider sampleProvider = new NoteSampleProvider(60, n);
            using (WaveOutEvent outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(sampleProvider);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    //if (stopRequested)
                    //{
                    //    outputDevice.Stop();
                    //    AllowPlayback(true);
                    //    return;
                    //}
                    await Task.Delay(1000);//.ConfigureAwait(false);
                }
            }
        }

        double fundamental;
        double duration;

        private string ValidateDataEntry()
        {
            if (posInt.IsMatch(txtFundamental.Text))
                fundamental = double.Parse(txtFundamental.Text);
            else
                return "Note should be an integer, 0 to 88 (A440 = 60)";

            if (posReal.IsMatch(txtDuration.Text))
                duration = double.Parse(txtDuration.Text);
            else
                return "Duration should be a floating point number (seconds)";

            return string.Empty;
        }

        [GeneratedRegex("^\\d+$")]
        private static partial Regex RxPosInt();
        [GeneratedRegex("^\\d+(.\\d+)?$")]
        private static partial Regex RxReal();
    }
}