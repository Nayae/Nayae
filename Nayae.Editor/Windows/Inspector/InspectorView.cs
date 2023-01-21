using System.Numerics;
using ImGuiNET;

namespace Nayae.Editor.Windows.Inspector;

public class InspectorView
{
    private readonly InspectorService _service;

    public InspectorView(InspectorService service)
    {
        _service = service;
    }

    public void Render()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));

        if (ImGui.Begin("Inspector"))
        {
            if (_service.IsGameObjectSelected() && ImGui.BeginTable("Inspector", 1, ImGuiTableFlags.RowBg))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                foreach (var panel in _service.GetActivePanels())
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    var isPanelShown = ImGui.TreeNodeEx(
                        panel.Name,
                        ImGuiTreeNodeFlags.SpanFullWidth |
                        ImGuiTreeNodeFlags.DefaultOpen |
                        ImGuiTreeNodeFlags.FramePadding
                    );

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    if (isPanelShown)
                    {
                        panel.Render();
                        ImGui.TreePop();
                    }
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }

        ImGui.PopStyleVar();
    }
}