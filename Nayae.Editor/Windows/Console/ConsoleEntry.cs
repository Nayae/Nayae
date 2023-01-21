using Nayae.Engine;

namespace Nayae.Editor.Windows.Console;

public class ConsoleEntry
{
    public float Height { get; set; }
    public float Offset { get; set; }

    public LogEntry Log { get; }

    public int Count { get; set; } = 1;

    public string LevelString { get; }
    public string FirstTimeString { get; }
    public string LastTimeString { get; set; }
    public string CountText { get; set; }

    public ConsoleEntry(LogEntry entry)
    {
        Log = entry;

        LevelString = entry.level.ToString();
        FirstTimeString = LastTimeString = entry.time.ToString("HH:mm:ss.fff");
        CountText = Count.ToString();
    }

    public void AddOccurence(DateTime time)
    {
        Count++;
        CountText = Count > 9999 ? "9999+" : Count.ToString();
        LastTimeString = time.ToString("HH:mm:ss.fff");
    }

    public override int GetHashCode()
    {
        return Log.level.GetHashCode() + Log.text.GetHashCode();
    }
}