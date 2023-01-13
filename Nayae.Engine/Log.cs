using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nayae.Engine;

public enum LogLevel
{
    Verbose,
    Debug,
    Info,
    Warn,
    Error
}

public readonly struct LogEntry
{
    public readonly LogLevel level;
    public readonly DateTime time;
    public readonly string text;

    public LogEntry(LogLevel level, DateTime time, string text)
    {
        this.level = level;
        this.time = time;
        this.text = text;
    }
}

public static class Log
{
    public static event Action<LogEntry> Entry;

    private static readonly Stack<Stopwatch> _stopwatches;

    static Log()
    {
        _stopwatches = new Stack<Stopwatch>();
    }

    public static void Verbose(params object[] values)
    {
        Format(LogLevel.Verbose, values);
    }

    public static void Debug(params object[] values)
    {
        Format(LogLevel.Debug, values);
    }

    public static void Info(params object[] values)
    {
        Format(LogLevel.Info, values);
    }

    public static void Warn(params object[] values)
    {
        Format(LogLevel.Warn, values);
    }

    public static void Error(params object[] values)
    {
        Format(LogLevel.Error, values);
    }

    public static void TimeStart()
    {
        _stopwatches.Push(Stopwatch.StartNew());
    }

    public static void TimeEnd()
    {
        Info(_stopwatches.Pop().Elapsed.TotalMilliseconds);
    }

    private static void Format(LogLevel level, params object[] values)
    {
        Entry?.Invoke(
            new LogEntry(
                level,
                DateTime.Now,
                string.Join(' ', values)
            )
        );
    }
}