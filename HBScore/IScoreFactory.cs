namespace HBScore
{
    public interface IScoreFactory
    {
        IMeasure CreateMeasure(int beats, bool compound);
        INote CreateNote(int offset, int pitch, int duration);
        IScore CreateScore(bool useFlats = false);
    }
}