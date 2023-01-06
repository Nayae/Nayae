using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Extensions;

namespace Nayae.Editor;

public class Node
{
    public required string Name { get; init; }
    public List<Node> Children { get; init; } = new();
    public bool IsExpanded { get; set; }
}

public static class HierarchyView
{
    private const float TreeNodeHeight = 4.0f;

    private static readonly Node _root;

    static HierarchyView()
    {
        _root = new Node
        {
            Name = "Root",
            Children = new List<Node>
            {
                new()
                {
                    Name = "Child 1.1"
                },
                new()
                {
                    Name = "Child 1.2"
                },
                new()
                {
                    Name = "Child 1.3",
                    Children = new List<Node>
                    {
                        new()
                        {
                            Name = "Child 1.3.1"
                        },
                        new()
                        {
                            Name = "Child 1.3.2"
                        },
                        new()
                        {
                            Name = "Child 1.3.3"
                        }
                    }
                }
            }
        };
    }

    public static void Render()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
        if (ImGui.Begin("Hierarchy"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5.0f, TreeNodeHeight));
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 11.0f);

            if (ImGui.BeginTable("Hierarchy", 1, ImGuiTableFlags.RowBg))
            {
                RenderTree(_root, ImGui.GetWindowDrawList(), ImGui.GetStyle().IndentSpacing);
                ImGui.EndTable();
            }

            ImGui.PopStyleVar(3);

            ImGui.End();
        }

        ImGui.PopStyleVar();
    }

    private static Vector2 RenderTree(Node node, ImDrawListPtr drawList, float indentSpacing)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.SpanFullWidth |
                    ImGuiTreeNodeFlags.OpenOnArrow |
                    ImGuiTreeNodeFlags.DefaultOpen |
                    ImGuiTreeNodeFlags.FramePadding;

        if (node.Children.Count == 0)
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        var isNodeOpen = ImGui.TreeNodeEx(node.Name, flags);
        var cursor = ImGui.GetCursorScreenPos();
        var nodeMax = ImGui.GetItemRectMax();

        if (ImGui.IsItemToggledOpen())
        {
            node.IsExpanded = isNodeOpen;
            Logger.Info(node.Name, "opened/closed", node.IsExpanded);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            var nodeMin = ImGui.GetItemRectMin();
            var mousePos = ImGui.GetMousePos();
            var delta = (mousePos.Y - nodeMin.Y) / (nodeMax.Y - nodeMin.Y);

            switch (delta)
            {
                case <= 0.25f:
                    Logger.Info("Insert above");
                    break;
                case >= 0.75f:
                    Logger.Info("Insert below");
                    break;
                default:
                    Logger.Info("Insert child");
                    break;
            }
        }

        if (isNodeOpen)
        {
            var lastChildY = nodeMax.Y;

            foreach (var child in node.Children)
            {
                var rect = RenderTree(child, drawList, indentSpacing);
                lastChildY = rect.Y - indentSpacing - TreeNodeHeight;

                // Draw horizontal line
                var lineWidth = child.Children.Count > 0 ? 5.0f : 10.0f;
                drawList.AddLine(
                    new Vector2(cursor.X, lastChildY),
                    new Vector2(cursor.X + lineWidth, lastChildY),
                    Color.Gray.ToImGui()
                );
            }

            if (node.Children.Count > 0)
            {
                // Draw horizontal line
                drawList.AddLine(
                    new Vector2(cursor.X, lastChildY),
                    new Vector2(cursor.X, nodeMax.Y),
                    Color.Gray.ToImGui()
                );
            }

            ImGui.TreePop();
        }

        return cursor;
    }
}