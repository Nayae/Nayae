using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Extensions;

namespace Nayae.Editor.Windows.Console;

public class ConsoleView
{
    private readonly ConsoleService _service;

    private static bool _isAutoScroll = true;

    private static Vector2 _topConsoleDummyVector = Vector2.Zero;
    private static Vector2 _bottomConsoleDummyVector = Vector2.Zero;

    public ConsoleView(ConsoleService service)
    {
        _service = service;
    }

    public void Render()
    {
        if (ImGui.Begin("Console"))
        {
            // START -> Clear
            if (ImGui.Button("Clear"))
            {
                _service.ClearAllLogs();
            }
            // END -> Clear

            // START -> Levels
            ImGui.SameLine();
            ImGui.Text("Levels:");
            ImGui.SameLine();

            foreach (var level in _service.GetLevels())
            {
                ImGui.SameLine();
                if (ImGuiUtility.ToggleButton(level.ToString(), GetEntryColor(level), _service.IsLevelEnabled(level)))
                {
                    _service.ToggleLevel(level);
                }
            }
            // END -> Levels

            // START -> Options
            ImGui.SameLine();
            ImGui.Text("Options:");
            ImGui.SameLine();
            ImGuiUtility.ToggleButton("Auto-Scroll", ref _isAutoScroll);
            ImGui.SameLine();
            if (ImGuiUtility.ToggleButton("Collapsed", _service.IsCollapsedMode))
            {
                _service.ToggleCollapsed();
            }
            // END -> Options


            // START -> Search
            ImGui.SameLine();
            ImGui.Text("Search:");
            ImGui.SameLine();

            ImGui.PushItemWidth(-ImGui.GetStyle().FramePadding.Y);
            if (ImGui.InputText(string.Empty, ref _service.SearchTextReference(), 28))
            {
                _service.RecalculateEntryInformation();
            }

            ImGui.PopItemWidth();
            // END -> Search

            ImGui.Separator();

            const ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit |
                                          ImGuiTableFlags.Borders |
                                          ImGuiTableFlags.ScrollY |
                                          ImGuiTableFlags.Hideable;

            if (ImGui.BeginTable("ConsoleViewTable", _service.IsCollapsedMode ? 4 : 3, flags))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                if (_service.IsCollapsedMode)
                {
                    ImGui.TableSetupColumn("Count");
                }

                ImGui.TableSetupColumn("Level");
                ImGui.TableSetupColumn(_service.IsCollapsedMode ? "Last Time" : "Time");
                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                var currentColumnWidth = ImGui.GetColumnWidth(_service.IsCollapsedMode ? 3 : 2);
                var currentWindowHeight = ImGui.GetWindowHeight();
                var currentScrollY = ImGui.GetScrollY();

                if ((int)_service.TextWrapWidth != (int)currentColumnWidth)
                {
                    _service.SetTextWrapWidth((int)currentColumnWidth);
                }

                if (_service.TryGetFirstVisibleEntryIndex(currentScrollY, out var currentIndex))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    var entries = _service.GetEntries();

                    _topConsoleDummyVector.Y = entries[currentIndex].Offset;
                    ImGui.Dummy(_topConsoleDummyVector);

                    for (; currentIndex < entries.Count; currentIndex++)
                    {
                        var entry = entries[currentIndex];

                        if (entry.Offset > currentWindowHeight + currentScrollY)
                        {
                            break;
                        }

                        ImGui.PushStyleColor(ImGuiCol.Text, GetEntryColor(entry.Log.level));
                        ImGui.TableNextRow();

                        if (_service.IsCollapsedMode)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text(entry.CountText);
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text(entry.LevelString);
                        ImGui.TableNextColumn();
                        ImGui.Text(_service.IsCollapsedMode ? entry.LastTimeString : entry.FirstTimeString);
                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(entry.Log.text);
                        ImGui.PopStyleColor();
                    }

                    if (currentIndex < entries.Count - 1)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        _bottomConsoleDummyVector.Y = entries[^1].Offset +
                                                      entries[^1].Height -
                                                      entries[currentIndex].Offset;
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