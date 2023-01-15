using ImGuiNET;
using Nayae.Engine;

namespace Nayae.Editor.Console;

public class ConsoleService
{
    public bool IsCollapsedMode { get; private set; }
    public float TextWrapWidth { get; private set; }

    private readonly Queue<ConsoleEntry> _pendingEntries;
    private readonly List<ConsoleEntry> _allLogEntries;
    private readonly List<ConsoleEntry> _filteredEntries;
    private readonly Dictionary<int, ConsoleEntry> _uniqueEntries;

    private static readonly LogLevel[] _levels = Enum.GetValues<LogLevel>();
    private static readonly Dictionary<LogLevel, bool> _enabledLevels = new();

    private string _searchText = string.Empty;

    public ConsoleService()
    {
        _pendingEntries = new Queue<ConsoleEntry>();
        _allLogEntries = new List<ConsoleEntry>();
        _filteredEntries = new List<ConsoleEntry>(_allLogEntries);
        _uniqueEntries = new Dictionary<int, ConsoleEntry>();

        foreach (var level in _levels)
        {
            _enabledLevels[level] = true;
        }
    }

    public void OnLoggerEntry(LogEntry entry)
    {
        _pendingEntries.Enqueue(new ConsoleEntry(entry));
    }

    public void Update()
    {
        while (_pendingEntries.Count > 0)
        {
            var entry = _pendingEntries.Dequeue();
            _allLogEntries.Add(entry);

            entry.Height = CalculateEntryHeight(entry.Log.text);

            AppendToFiltered(entry);
        }
    }

    public void ToggleLevel(LogLevel level)
    {
        _enabledLevels[level] = !_enabledLevels[level];
        RecalculateEntryInformation();
    }

    public void ToggleCollapsed()
    {
        IsCollapsedMode = !IsCollapsedMode;
        RecalculateEntryInformation();
    }

    public void SetTextWrapWidth(float width)
    {
        TextWrapWidth = width;
        RecalculateEntryInformation();
    }

    public void ClearAllLogs()
    {
        _allLogEntries.Clear();
        RecalculateEntryInformation();
    }

    public void RecalculateEntryInformation()
    {
        _filteredEntries.Clear();
        _uniqueEntries.Clear();

        foreach (var entry in _allLogEntries)
        {
            entry.Height = CalculateEntryHeight(entry.Log.text);
            entry.Offset = 0;
            AppendToFiltered(entry);
        }
    }

    public void AppendToFiltered(ConsoleEntry entry)
    {
        if (IsCollapsedMode)
        {
            if (_uniqueEntries.TryGetValue(entry.GetHashCode(), out var uniqueEntry))
            {
                uniqueEntry.AddOccurence(entry.Log.time);
                return;
            }

            entry.Count = 1;
            _uniqueEntries.Add(entry.GetHashCode(), entry);
        }

        if (DoesEntryPassFilters(entry))
        {
            if (_filteredEntries.Count > 0)
            {
                entry.Offset = _filteredEntries[^1].Offset + _filteredEntries[^1].Height;
            }

            _filteredEntries.Add(entry);
        }
    }

    public float CalculateEntryHeight(string text)
    {
        return ImGui.CalcTextSize(text, TextWrapWidth).Y + ImGui.GetStyle().ItemInnerSpacing.Y;
    }

    public bool DoesEntryPassFilters(ConsoleEntry entry)
    {
        return entry.Log.text.ToLower().Contains(_searchText.ToLower()) && _enabledLevels[entry.Log.level];
    }

    public bool TryGetFirstVisibleEntryIndex(float scrollY, out int index)
    {
        int start = 0, end = _filteredEntries.Count - 1, offset = -1;
        while (start <= end)
        {
            var mid = (start + end) / 2;
            if (_filteredEntries[mid].Offset <= scrollY)
            {
                start = mid + 1;
            }
            else
            {
                end = mid - 1;
            }

            offset = mid;
        }

        index = end;
        return offset >= 0;
    }

    public ref string SearchTextReference()
    {
        return ref _searchText;
    }

    public bool IsLevelEnabled(LogLevel level)
    {
        return _enabledLevels[level];
    }

    public LogLevel[] GetLevels()
    {
        return _levels;
    }

    public List<ConsoleEntry> GetEntries()
    {
        return _filteredEntries;
    }
}