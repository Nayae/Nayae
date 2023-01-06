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

public static class Logger
{
    public static event Action<LogEntry> Entry;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Verbose(params object[] values)
    {
        Format(LogLevel.Verbose, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Debug(params object[] values)
    {
        Format(LogLevel.Debug, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Info(params object[] values)
    {
        Format(LogLevel.Info, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Warn(params object[] values)
    {
        Format(LogLevel.Warn, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Error(params object[] values)
    {
        Format(LogLevel.Error, values);
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