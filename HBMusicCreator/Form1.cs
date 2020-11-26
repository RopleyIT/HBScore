using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HBScore;
using NAudio.Wave;
using NoteLib;

namespace HBMusicCreator
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
            cbTimeSignature.SelectedIndex = 4;
            cbMetronome.SelectedIndex = 3;
            cbxBeats.SelectedIndex = 0;
        }

        public int Metronome
        {
            get
            {
                if (cbMetronome.SelectedIndex >= 0 &&
                    int.TryParse(cbMetronome.SelectedItem.ToString(), out int metronome))
                {
                    return metronome;
                }
                else
                    return 72;
            }
        }

        //readonly string[] noteNames =
        //{
        //    "06E", "07D#", "07D", "1C#",
        //    "1C", "2B", "3A#", "3A", "4G#", "4G", "5F#", "5F", "6E", "7D#", "7D", "8C#",
        //    "8C", "9B", "10A#", "10A", "11G#", "11G", "12F#", "12F", "13E", "14D#", "14D", "15C#",
        //    "15C", "16B", "17A#", "17A", "18G#", "18G"
        //};

        private IScore score = null;
        private bool scoreModified = false;

        private void FrmMain_Load(object sender, EventArgs e)
        {
            filePath = Properties.Settings.Default.FilePath;
            if (string.IsNullOrWhiteSpace(filePath))
                ResetFilePath();
            lastAddedNote = null;
            cbTimeSignature.Focus();
        }

        private void ResetFilePath()
        {
            filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            filePath = Path.Combine(filePath, "NewMusic.mus");
            Properties.Settings.Default.FilePath = filePath;
            Properties.Settings.Default.Save();
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (UnsavedFileCheck())
            {
                ScoreFactory sf = new ScoreFactory();
                score = sf.CreateScore();
                score.Measures.Add(sf.CreateMeasure(BeatsPerBar, Compound));
                SetTimeSignature(score.Measures.First());
                measureOffset = 0;
                selectedHalfBeat = 0;
                scoreModified = false;
                ResetFilePath();
                PaintScore();
                chkUseFlats.Checked = score.UseFlats;
            }
            lastAddedNote = null;
        }

        private bool UnsavedFileCheck()
        {
            if (scoreModified)
            {
                DialogResult result = MessageBox.Show
                    (this, "You have unsaved changes. Are you sure you want"
                    + " to discard them?", "Unsaved changes",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return result == DialogResult.Yes;
            }
            return true;
        }

        private int measureOffset = 0;

        private IEnumerable<IMeasure> MeasuresFromOffset
        {
            get
            {
                int quarterBeats = 0;
                for (int i = measureOffset; i < score.Measures.Count(); i++)
                    if (quarterBeats + score.Measures[i].QuarterBeatsPerBar > QuarterBeatsInDisplay)
                        return score.Measures.Skip(measureOffset).Take(i - measureOffset);
                    else
                        quarterBeats += score.Measures[i].QuarterBeatsPerBar;
                return score.Measures.Skip(measureOffset);
            }
        }

        private int QuarterBeatsInDisplay => (pbxScore.Width - 4) / 12;

        private IMeasure MeasureContainingCursor(int offset)
        {
            if (selectedHalfBeat < 0)
                return null;
            int quarterBeats = 0;
            foreach (IMeasure m in score.Measures.Skip(offset))
                if ((quarterBeats + m.QuarterBeatsPerBar)/2 > selectedHalfBeat)
                    return m;
                else quarterBeats += m.QuarterBeatsPerBar;
            return null;
        }

        private int StartOfMeasureHalfBeat(IMeasure measure)
        {
            int quarterBeats = 0;
            foreach (IMeasure m in score.Measures.Skip(measureOffset))
                if (measure == m)
                    return quarterBeats/2;
                else
                    quarterBeats += m.QuarterBeatsPerBar;
            return -1;
        }

        private void PaintScore()
        {
            ScoreWriter sw = new ScoreWriter(score);
            Image img;
            if (pbxScore.Image != null
                && (pbxScore.Image.Width != pbxScore.Width
                || pbxScore.Image.Height != pbxScore.Height))
            {
                var oldImage = pbxScore.Image;
                pbxScore.Image = null;
                oldImage.Dispose();
            }

            if(pbxScore.Image == null)
                img = new Bitmap(pbxScore.Width, pbxScore.Height);
            else
                img = pbxScore.Image;

            using (var g = Graphics.FromImage(img))
                g.FillRectangle(Brushes.White, 0, 0, img.Width, img.Height);
            sw.RenderMeasures
                (img, new Point(2, 18), 48, measureOffset,
                score.UseFlats, MeasuresFromOffset, selectedNote);
            pbxScore.Image = img;
            pnlPointer.Invalidate();
        }

        private int selectedHalfBeat = -1;

        private void PnlPointer_Paint(object sender, PaintEventArgs e)
        {
            if (selectedHalfBeat >= 0)
                using (Graphics g = pnlPointer.CreateGraphics())
                {
                    Point[] vertices = new Point[]
                    {
                        new Point((1 + selectedHalfBeat) * 24, 30),
                        new Point((selectedHalfBeat) * 24 + 9, 0),
                        new Point((2 + selectedHalfBeat) * 24 - 9, 0)
                    };
                    g.FillPolygon(Brushes.Red, vertices);
                }
        }

        private INote lastAddedNote = null;
        private void Note_Click(object sender, EventArgs e) 
            => ClickNote(int.Parse((sender as Button).Tag.ToString()));

        private void startRepeatToolStripMenuItem_Click(object sender, EventArgs e) 
            => ClickNote(HBScore.Note.StartRepeat);

        private void endRepeatToolStripMenuItem_Click(object sender, EventArgs e) 
            => ClickNote(HBScore.Note.EndRepeat);

        private void RemoveSpecialNotes(IList<INote> notes, int noteNumber)
        {
            var deletees = notes.Where(n => n.Pitch == noteNumber).ToList();
            foreach (var note in deletees)
                notes.Remove(note);
        }

        private void ClickNote(int noteNumber)
        {
            IMeasure currMeasure = MeasureContainingCursor(measureOffset);
            if (currMeasure != null)
            {
                // Handle repeat deletion

                if (noteNumber >= HBScore.Note.StartRepeat && currMeasure.Notes.Any(n => n.Pitch == noteNumber))
                    RemoveSpecialNotes(currMeasure.Notes, noteNumber);
                else
                {
                    int noteOffset = 2 * (selectedHalfBeat - StartOfMeasureHalfBeat(currMeasure));
                    ScoreFactory sf = new ScoreFactory();
                    INote note = currMeasure.Notes.FirstOrDefault
                        (n => n.Offset == noteOffset && n.Pitch == noteNumber);
                    if (note == null)
                    {
                        if (noteNumber == HBScore.Note.StartRepeat)
                            noteOffset = 0;
                        else if (noteNumber == HBScore.Note.EndRepeat)
                            noteOffset = currMeasure.QuarterBeatsPerBar - 4;
                        INote newNote = sf.CreateNote(noteOffset, noteNumber, 4);
                        (newNote as ColouredNote).ForeColour = noteColour;
                        (newNote as ColouredNote).BackColour = noteBackground;
                        currMeasure.Notes.Add(newNote);
                        lastAddedNote = note;
                    }
                    else
                    {
                        currMeasure.Notes.Remove(note);
                        lastAddedNote = null;
                    }
                }
                SetSpecialMenuItems(currMeasure);
                scoreModified = true;
            }
            PaintScore();
        }

        private void BtnFirst_Click(object sender, EventArgs e)
        {
            measureOffset = 0;
            lastAddedNote = null;
            PaintScore();
        }

        private void BtnLast_Click(object sender, EventArgs e)
        {
            measureOffset = score.Measures.Count() - 3;
            if (measureOffset < 0)
                measureOffset = 0;
            lastAddedNote = null;
            PaintScore();
        }

        private void BtnLeft_Click(object sender, EventArgs e)
        {
            measureOffset -= 3; // Good enough for 3, 4, 5, and even 6 beat bars
            if (measureOffset < 0)
                measureOffset = 0;
            lastAddedNote = null;
            PaintScore();
        }

        private void BtnRight_Click(object sender, EventArgs e)
        {
            measureOffset += 3; // Good enough for 3, 4, 5, and even 6 beat bars
            if (measureOffset >= score.Measures.Count() - 3)
                measureOffset = score.Measures.Count() - 3;
            if (measureOffset < 0)
                measureOffset = 0;
            lastAddedNote = null;
            PaintScore();
        }

        private string filePath;

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (UnsavedFileCheck())
            {
                openFileDialog.FileName = filePath;
                openFileDialog.DefaultExt = "mus";
                openFileDialog.CheckPathExists = true;
                openFileDialog.CheckFileExists = true;
                openFileDialog.Filter =
                    "Music Scores (*.mus)|*.mus|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 0;
                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    using (Stream iStream = new FileStream
                        (openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                        score = (Score)bf.Deserialize(iStream);

                    // Adjust old style note classes

                    List<IMeasure> newMeasures = new List<IMeasure>();
                    ScoreFactory sf = new ScoreFactory();

                    foreach (IMeasure m in score.Measures)
                    {
                        for (int i = 0; i < m.Notes.Count(); i++)
                        {
                            if (!(m.Notes[i] is ColouredNote))
                                m.Notes[i] = sf.CreateNote
                                    (m.Notes[i].Offset, m.Notes[i].Pitch, m.Notes[i].Duration);
                        }
                    }

                    measureOffset = 0;
                    scoreModified = false;
                    selectedHalfBeat = -1;
                    PaintScore();
                    chkUseFlats.Checked = score.UseFlats;
                    filePath = openFileDialog.FileName;
                    Properties.Settings.Default.FilePath = filePath;
                    Properties.Settings.Default.Save();
                    Text = "HB Music Creator - " + filePath;
                }
            }
            lastAddedNote = null;
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                SaveAsToolStripMenuItem_Click(sender, e);
            else
            {
                // Adjustment for old format serialisation files

                BinaryFormatter bf = new BinaryFormatter();
                using (Stream oStream = new FileStream
                    (filePath, FileMode.Create, FileAccess.Write))
                    bf.Serialize(oStream, (Score)score);
                scoreModified = false;
                PaintScore();
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog.FileName = filePath;
            saveFileDialog.AddExtension = true;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.DefaultExt = "mus";
            saveFileDialog.Filter =
                "Music Scores (*.mus)|*.mus|All Files (*.*)|*.*";
            saveFileDialog.FilterIndex = 0;
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                filePath = saveFileDialog.FileName;
                Properties.Settings.Default.FilePath = filePath;
                Properties.Settings.Default.Save();
                SaveToolStripMenuItem_Click(sender, e);
            }
        }

        private void BeforeCurrentBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IMeasure m = MeasureContainingCursor(measureOffset);
            if (m != null)
            {
                int idx = score.Measures.IndexOf(m);
                score.Measures.Insert(idx, (new ScoreFactory()).CreateMeasure(BeatsPerBar, Compound));
                measureOffset = idx;
                selectedHalfBeat = -1;
                scoreModified = true;
                PaintScore();
            }
        }

        private void AfterCurrentBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IMeasure m = MeasureContainingCursor(measureOffset);
            if (m != null)
            {
                int idx = score.Measures.IndexOf(m);
                score.Measures.Insert(idx + 1, (new ScoreFactory()).CreateMeasure(BeatsPerBar, Compound));
                measureOffset = idx;
                selectedHalfBeat = m.QuarterBeatsPerBar/2;
                scoreModified = true;
                PaintScore();
            }
        }

        private bool Compound => 
            cbTimeSignature.SelectedItem != null 
            ? cbTimeSignature.SelectedItem.ToString().EndsWith(":8")
            : false;

        private int BeatsPerBar
        {
            get
            {
                string ts = "4:4";
                if(cbTimeSignature.SelectedItem != null)
                    ts = cbTimeSignature.SelectedItem.ToString();
                switch(ts)
                {
                    case "2:4": 
                    case "6:8": 
                        return 2;
                    case "3:4":
                    case "9:8":
                        return 3;
                    case "4:4":
                    case "12:8":
                        return 4;
                    case "5:4":
                        return 5;
                    case "6:4":
                        return 6;
                    case "7:4":
                        return 7;
                    default:
                        return 4;
                };
            }
        }

        private void SetTimeSignature(IMeasure m)
        {
            if (m.CompoundTime)
            {
                if (m.BeatsPerBar == 2)
                    cbTimeSignature.SelectedItem = "6:8";
                else if (m.BeatsPerBar == 3)
                    cbTimeSignature.SelectedItem = "9:8";
                else if (m.BeatsPerBar == 4)
                    cbTimeSignature.SelectedItem = "12:8";
            }
            else
            {
                if (m.BeatsPerBar == 2)
                    cbTimeSignature.SelectedItem = "2:4";
                else if (m.BeatsPerBar == 3)
                    cbTimeSignature.SelectedItem = "3:4";
                else if (m.BeatsPerBar == 4)
                    cbTimeSignature.SelectedItem = "4:4";
                else if (m.BeatsPerBar == 5)
                    cbTimeSignature.SelectedItem = "5:4";
                else if (m.BeatsPerBar == 7)
                    cbTimeSignature.SelectedItem = "7:4";
            }
        }

        private void ChkUseFlats_Click(object sender, EventArgs e)
        {
            if (score != null)
            {
                score.UseFlats = chkUseFlats.Checked;
                scoreModified = true;
                PaintScore();
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e) => e.Cancel = !UnsavedFileCheck();

        private void PrintToPDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string pdfPath = filePath;
            if (pdfPath.EndsWith(".mus"))
                pdfPath = pdfPath.Replace(".mus", ".pdf");
            else
                pdfPath += ".pdf";
            savePDFDialog.FileName = pdfPath;
            savePDFDialog.AddExtension = true;
            savePDFDialog.CheckPathExists = true;
            savePDFDialog.DefaultExt = "pdf";
            savePDFDialog.Filter =
                "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
            savePDFDialog.FilterIndex = 0;
            if (savePDFDialog.ShowDialog(this) == DialogResult.OK)
            {
                ScoreWriter sw = new ScoreWriter(score);
                sw.SavePDF(pdfPath);
            }
        }

        private void DeleteBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IMeasure m = MeasureContainingCursor(measureOffset);
            if (m != null)
            {
                int idx = score.Measures.IndexOf(m);
                score.Measures.Remove(m);
                if (!score.Measures.Any())
                    (new ScoreFactory()).CreateMeasure(BeatsPerBar, Compound);
                measureOffset = idx;
                if (score.Measures.Count() <= idx)
                    measureOffset -= 1;
                selectedHalfBeat = -1;
                scoreModified = true;
                PaintScore();
            }

        }

        private INote selectedNote = null;

        private void PbxScore_MouseClick(object sender, MouseEventArgs e)
        {
            PnlPointer_MouseClick(sender, e);
            selectedHalfBeat = (e.X - 12 - 2) / 24;
            ScoreWriter sw = new ScoreWriter(score);

            selectedNote = sw.FindNoteFromMouseCoordinates
                (e.Location, pbxScore.Image, new Point(2, 18), 48,
                    score.UseFlats, MeasuresFromOffset);
            if (recolourOnClickToolStripMenuItem.Checked)
            {
                ColouredNote n = selectedNote as ColouredNote;
                if (n != null)
                {
                    n.ForeColour = noteColour;
                    n.BackColour = noteBackground;
                }
                selectedNote = null;
            }
            PaintScore();
            if (selectedNote != null)
                cbxBeats.SelectedIndex = selectedNote.Duration / 4 - 1;
            else
                cbxBeats.SelectedIndex = 0;
        }

        private void PnlPointer_MouseClick(object sender, MouseEventArgs e)
        {
            selectedHalfBeat = (e.X - 12 - 2) / 24;
            IMeasure currMeasure = MeasureContainingCursor(measureOffset);
            if (currMeasure != null)
                SetTimeSignature(currMeasure);
            else
                SetTimeSignature(score.Measures.First());
            SetSpecialMenuItems(currMeasure);
            lastAddedNote = null;
            pnlPointer.Invalidate();
        }

        private void SetSpecialMenuItems(IMeasure currMeasure)
        {
            if (currMeasure != null && currMeasure.StartsRepeat)
                startRepeatToolStripMenuItem.Text = "Delete start repeat";
            else
                startRepeatToolStripMenuItem.Text = "Start repeat";
            if (currMeasure != null && currMeasure.EndsRepeat)
                endRepeatToolStripMenuItem.Text = "Delete end repeat";
            else
                endRepeatToolStripMenuItem.Text = "End repeat";
        }

        private void CbxBeats_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (selectedNote != null)
            {
                selectedNote.Duration = 4 * int.Parse(cbxBeats.SelectedItem.ToString());
                PaintScore();
            }
        }

        private readonly FrmScoreInfo scoreInfo = new FrmScoreInfo();

        private void ScoreinformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scoreInfo.Title = score.Title;
            scoreInfo.Composer = score.Composer;
            scoreInfo.Information = score.Information;
            if (scoreInfo.ShowDialog(this) == DialogResult.OK)
            {
                score.Title = scoreInfo.Title;
                score.Composer = scoreInfo.Composer;
                score.Information = scoreInfo.Information;
                scoreModified = true;
            }
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e) => MessageBox.Show(this,
                "Grid-based handbell music authoring programme,\r\n"
                + "Ver. 20.6.19. Licence: free to use, not for\r\n"
                + "resale. All enquiries to: sdsmith@ropley.com.\r\n"
                + "\r\n(c) 2020 S D Smith", "About HBScore",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

        private Color noteColour = Color.Black;
        private Color noteBackground = Color.White;

        private void BtnColour_Click(object sender, EventArgs e)
        {
            colorDialog.Color = noteColour;
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                noteColour = colorDialog.Color;
                ColouredNote note = selectedNote as ColouredNote;
                if (note != null)
                {
                    note.ForeColour = noteColour;
                    PaintScore();
                }
            }
        }

        private void BtnBackColour_Click(object sender, EventArgs e)
        {
            colorDialog.Color = noteBackground;
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                noteBackground = colorDialog.Color;
                ColouredNote note = selectedNote as ColouredNote;
                if (note != null)
                {
                    note.BackColour = noteBackground;
                    PaintScore();
                }
            }
        }

        private bool stopRequested = false;

        // TEST & DEBUG EVENTS
        private async void playSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AllowPlayback(false);
            List<NoteLib.Note> noteList = Playback.GenerateNotesFrom(score);

            NoteSampleProvider sampleProvider = new NoteSampleProvider(Metronome, noteList);
            using (WaveOutEvent outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(sampleProvider);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    if(stopRequested)
                    {
                        outputDevice.Stop();
                        AllowPlayback(true);
                        return;
                    }
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            AllowPlayback(true);
        }

        private void transposeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var transpose = new FrmTranspose())
            {
                var dlgResult = transpose.ShowDialog(this);
                if(dlgResult == DialogResult.OK && transpose.Interval != 0)
                {
                    score.Transpose(transpose.Interval);
                    PaintScore();
                }
            }
        }

        private void stopPlayingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopRequested = true;
        }

        private void AllowPlayback(bool allow)
        {
            var f = new Action<bool>(a =>
            {
                playToolStripMenuItem.Enabled = a;
                stopPlayingToolStripMenuItem.Enabled = !a;
            });
            Invoke(f, new object[] { allow });
            stopRequested = false;
        }
    }
}
