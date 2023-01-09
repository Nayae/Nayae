using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Extensions;

namespace Nayae.Editor;

public class ConsoleViewEntry
{
    public float Height { get; set; }
    public float Offset { get; set; }

    public LogEntry Log { get; }

    public int Count { get; set; } = 1;

    public string LevelString { get; }
    public string FirstTimeString { get; }
    public string LastTimeString { get; set; }
    public string CountText { get; set; }

    public ConsoleViewEntry(LogEntry entry)
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

public static class ConsoleView
{
    private static readonly Queue<ConsoleViewEntry> _pendingEntries;
    private static readonly List<ConsoleViewEntry> _allLogEntries;
    private static readonly List<ConsoleViewEntry> _filteredEntries;
    private static readonly Dictionary<int, ConsoleViewEntry> _uniqueEntries;

    private static readonly LogLevel[] _levels = Enum.GetValues<LogLevel>();
    private static readonly Dictionary<LogLevel, bool> _enabledLevels = new();

    private static int _previousColumnWidth;

    private static bool _isAutoScroll = true;
    private static bool _isCollapsed;

    private static string _searchText = string.Empty;

    private static Vector2 _topConsoleDummyVector = Vector2.Zero;
    private static Vector2 _bottomConsoleDummyVector = Vector2.Zero;

    static ConsoleView()
    {
        _pendingEntries = new Queue<ConsoleViewEntry>();
        _allLogEntries = new List<ConsoleViewEntry>();
        _filteredEntries = new List<ConsoleViewEntry>(_allLogEntries);
        _uniqueEntries = new Dictionary<int, ConsoleViewEntry>();

        foreach (var level in _levels)
        {
            _enabledLevels[level] = true;
        }
    }

    public static void Render()
    {
        if (ImGui.Begin("Console"))
        {
            // START -> Clear
            if (ImGui.Button("Clear"))
            {
                _uniqueEntries.Clear();
                _allLogEntries.Clear();

                RegenerateFilteredList();
            }
            // END -> Clear

            // START -> Levels
            ImGui.SameLine();
            ImGui.Text("Levels:");
            ImGui.SameLine();

            foreach (var level in _levels)
            {
                ImGui.SameLine();
                if (ImGuiUtility.ToggleButton(level.ToString(), GetEntryColor(level), _enabledLevels[level]))
                {
                    _enabledLevels[level] = !_enabledLevels[level];

                    RegenerateFilteredList();
                }
            }
            // END -> Levels

            // START -> Options
            ImGui.SameLine();
            ImGui.Text("Options:");
            ImGui.SameLine();
            ImGuiUtility.ToggleButton("Auto-Scroll", ref _isAutoScroll);
            ImGui.SameLine();
            if (ImGuiUtility.ToggleButton("Collapsed", ref _isCollapsed))
            {
                RegenerateFilteredList();
            }
            // END -> Options


            // START -> Search
            ImGui.SameLine();
            ImGui.Text("Search:");
            ImGui.SameLine();

            ImGui.PushItemWidth(-ImGui.GetStyle().FramePadding.Y);
            var previousSearchText = _searchText;
            ImGui.InputText(string.Empty, ref _searchText, 28);
            if (previousSearchText != _searchText)
            {
                RegenerateFilteredList();
            }

            ImGui.PopItemWidth();
            // END -> Search

            ImGui.Separator();

            const ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit |
                                          ImGuiTableFlags.Borders |
                                          ImGuiTableFlags.ScrollY |
                                          ImGuiTableFlags.Hideable;

            if (ImGui.BeginTable("ConsoleViewTable", _isCollapsed ? 4 : 3, flags))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                if (_isCollapsed)
                {
                    ImGui.TableSetupColumn("Count");
                }

                ImGui.TableSetupColumn("Level");
                ImGui.TableSetupColumn(_isCollapsed ? "Last Time" : "Time");
                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                var currentColumnWidth = ImGui.GetColumnWidth(_isCollapsed ? 3 : 2);
                var currentWindowHeight = ImGui.GetWindowHeight();
                var currentScrollY = ImGui.GetScrollY();

                if (_previousColumnWidth != (int)currentColumnWidth)
                {
                    _previousColumnWidth = (int)currentColumnWidth;

                    foreach (var entry in _filteredEntries)
                    {
                        entry.Height = GetLogEntryHeight(entry.Log.text, currentColumnWidth);
                    }

                    RegenerateFilteredList();
                }

                while (_pendingEntries.Count > 0)
                {
                    var entry = _pendingEntries.Dequeue();
                    _allLogEntries.Add(entry);

                    entry.Height = GetLogEntryHeight(entry.Log.text, currentColumnWidth);

                    AddToFiltered(entry);
                }

                if (TryGetFirstVisibleEntryIndex(currentScrollY, out var currentIndex))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    _topConsoleDummyVector.Y = _filteredEntries[currentIndex].Offset;
                    ImGui.Dummy(_topConsoleDummyVector);

                    for (; currentIndex < _filteredEntries.Count; currentIndex++)
                    {
                        var entry = _filteredEntries[currentIndex];

                        if (entry.Offset > currentWindowHeight + currentScrollY)
                        {
                            break;
                        }

                        ImGui.PushStyleColor(ImGuiCol.Text, GetEntryColor(entry.Log.level));
                        ImGui.TableNextRow();

                        if (_isCollapsed)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text(entry.CountText);
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text(entry.LevelString);
                        ImGui.TableNextColumn();
                        ImGui.Text(_isCollapsed ? entry.LastTimeString : entry.FirstTimeString);
                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(entry.Log.text);
                        ImGui.PopStyleColor();
                    }

                    if (currentIndex < _filteredEntries.Count - 1)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        _bottomConsoleDummyVector.Y = _filteredEntries[^1].Offset -
                                                      _filteredEntries[currentIndex].Offset;
                        ImGui.Dummy(_bottomConsoleDummyVector);
                    }
                }

                if (_isAutoScroll && ImGui.GetScrollY() < ImGui.GetScrollMaxY())
                {
                    ImGui.SetScrollHereY(1.0f);
                }

                if (_isAutoScroll && ImGui.GetIO().MouseWheel > 0 && ImGui.IsWindowHovered())
                {
                    _isAutoScroll = false;
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }
    }

    public static void OnLoggerEntry(LogEntry entry)
    {
        _pendingEntries.Enqueue(new ConsoleViewEntry(entry));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void RegenerateFilteredList()
    {
        _filteredEntries.Clear();
        _uniqueEntries.Clear();

        foreach (var entry in _allLogEntries)
        {
            entry.Offset = 0;
            AddToFiltered(entry);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void AddToFiltered(ConsoleViewEntry entry)
    {
        if (_isCollapsed)
        {
            if (_uniqueEntries.TryGetValue(entry.GetHashCode(), out var uniqueEntry))
            {
                uniqueEntry.AddOccurence(entry.Log.time);
                return;
            }

            entry.Count = 1;
            _uniqueEntries.Add(entry.GetHashCode(), entry);
        }

        if (DoesMatchFilter(entry))
        {
            if (_filteredEntries.Count > 0)
            {
                entry.Offset = _filteredEntries[^1].Offset + _filteredEntries[^1].Height;
            }

            _filteredEntries.Add(entry);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static float GetLogEntryHeight(string text, float wrapWidth)
    {
        return ImGui.CalcTextSize(text, wrapWidth).Y + ImGui.GetStyle().ItemInnerSpacing.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool DoesMatchFilter(ConsoleViewEntry entry)
    {
        return entry.Log.text.ToLower().Contains(_searchText.ToLower()) && _enabledLevels[entry.Log.level];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TryGetFirstVisibleEntryIndex(float scrollY, out int index)
    {
        int start = 0, end = _filteredEntries.Count - 1;

        var offset = -1;
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

        index = offset == -1 ? _filteredEntries.Count - 1 : Math.Max(0, offset - 1);
        return index >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector4 GetEntryColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Verbose => Color.FromArgb(255, 138, 190, 183).ToVector(),
            LogLevel.Debug => Color.FromArgb(255, 178, 148, 187).ToVector(),
            LogLevel.Info => Color.FromArgb(255, 181, 189, 104).ToVector(),
            LogLevel.Warn => Color.FromArgb(255, 240, 198, 116).ToVector(),
            LogLevel.Error => Color.FromArgb(255, 204, 102, 102).ToVector(),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }
}