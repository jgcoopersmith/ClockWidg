namespace ClockWidg.Models;

public class AlarmItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Alarm";
    public int Hour { get; set; } = 7;    // 0-23
    public int Minute { get; set; } = 0;  // 0-59
    public bool Enabled { get; set; } = true;
    public bool Repeat { get; set; } = false;  // false => one-shot (auto-disables after ringing)
}
