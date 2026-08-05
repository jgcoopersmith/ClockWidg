namespace ClockWidg.Faces;

public interface IClockFace
{
    void UpdateTime(DateTime time, bool showSeconds, bool use24Hour);
}
