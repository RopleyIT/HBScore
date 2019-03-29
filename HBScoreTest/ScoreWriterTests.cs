using System;
using Xunit;
using HBScore;
using System.Drawing;

namespace HBScoreTest
{
    public class ScoreWriterTests
    {
        [Fact]
        public void CreateEmptyScore()
        {
            ScoreWriter sw = new ScoreWriter(22, 3, false, 6, false);
            Image img = sw.RenderScorePage(0);
            img.Save("c:\\tmp\\myScore.bmp");
            Assert.NotNull(img);
        }

        [Fact]
        public void CreateOddBarScore()
        {
            ScoreFactory sf = new ScoreFactory();
            IScore score = sf.CreateScore();
            score.Measures.Add(sf.CreateMeasure(4, false));
            score.Measures.Add(sf.CreateMeasure(3, false));
            score.Measures.Add(sf.CreateMeasure(4, false));
            score.Measures.Add(sf.CreateMeasure(3, false));
            score.Measures.Add(sf.CreateMeasure(3, false));
            score.Measures.Add(sf.CreateMeasure(4, false));
            ScoreWriter sw = new ScoreWriter(score, 6);
            Image img = sw.RenderScorePage(0);
            img.Save("c:\\tmp\\varBarScore.bmp");
            Assert.NotNull(img);
        }

        [Fact]
        public void CreateScoreWithSharps()
        {
            ScoreFactory sf = new ScoreFactory();
            IScore score = sf.CreateScore();
            score.Measures.Add(sf.CreateMeasure(4, false));
            score.Measures.Add(sf.CreateMeasure(3, false));
            score.Measures[0].Notes.Add(sf.CreateNote(0, 13, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(2, 14, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(4, 15, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(14, 39, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(14, 40, 4));
            score.Measures[1].Notes.Add(sf.CreateNote(0, 42, 4));
            ScoreWriter sw = new ScoreWriter(score, 6);
            Image img = sw.RenderScorePage(0);
            img.Save("c:\\tmp\\sharpScore.bmp");
            Assert.NotNull(img);
        }

        [Fact]
        public void CreateScoreWithFlats()
        {
            ScoreFactory sf = new ScoreFactory();
            IScore score = sf.CreateScore(true);
            score.Measures.Add(sf.CreateMeasure(4, false));
            score.Measures.Add(sf.CreateMeasure(3, false));
            score.Measures[0].Notes.Add(sf.CreateNote(0, 13, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(2, 14, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(4, 15, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(14, 39, 4));
            score.Measures[0].Notes.Add(sf.CreateNote(14, 40, 4));
            score.Measures[1].Notes.Add(sf.CreateNote(0, 42, 4));
            ScoreWriter sw = new ScoreWriter(score, 6);
            Image img = sw.RenderScorePage(0);
            img.Save("c:\\tmp\\flatScore.bmp");
            Assert.NotNull(img);
        }
    }
}
