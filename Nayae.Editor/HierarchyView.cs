using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Extensions;

namespace Nayae.Editor;

public static class HierarchyView
{
    private const float TreeNodeHeight = 4.0f;
    private const float IndentSize = 11.0f;
    private const float LineStartOffset = 22.0f;

    private static readonly Dictionary<GameObject, Vector2> _other;

    static HierarchyView()
    {
        _other = new Dictionary<GameObject, Vector2>();

        var child1 = GameObject.Create("Child 1");
        {
            GameObject.Create("Child 1.1", child1);
            GameObject.Create("Child 1.2", child1);
            var child13 = GameObject.Create("Child 1.3", child1);
            {
                GameObject.Create("Child 1.3.1", child13);
                GameObject.Create("Child 1.3.2", child13);
                GameObject.Create("Child 1.3.3", child13);
            }
        }

        var child2 = GameObject.Create("Child 2");
        {
            GameObject.Create("Child 2.1", child2);
            GameObject.Create("Child 2.2", child2);
            GameObject.Create("Child 2.3", child2);
        }

        var child3 = GameObject.Create("Child 3");
        {
            GameObject.Create("Child 3.1", child3);
            GameObject.Create("Child 3.2", child3);
            var child33 = GameObject.Create("Child 3.3", child3);
            {
                GameObject.Create("Child 3.3.1", child33);
                GameObject.Create("Child 3.3.2", child33);
                GameObject.Create("Child 3.3.3", child33);
            }
        }
    }

    public static void Render()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
        if (ImGui.Begin("Hierarchy"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5.0f, TreeNodeHeight));
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, IndentSize);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0));

            if (ImGui.BeginTable("Hierarchy", 1, ImGuiTableFlags.RowBg))
            {
                foreach (var node in GameObjectRegistry.GetEditorGameObjectList())
                {
                    RenderTree(node, ImGui.GetWindowDrawList(), ImGui.GetStyle().IndentSpacing);
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor();

            ImGui.End();
        }

        ImGui.PopStyleVar();
    }

    private static Vector2 RenderTree(
        GameObject currentObject,
        ImDrawListPtr drawList,
        float indentSpacing,
        int level = 0
    )
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.SpanFullWidth |
                    ImGuiTreeNodeFlags.OpenOnArrow |
                    ImGuiTreeNodeFlags.FramePadding;

        if (currentObject.Children.Count == 0)
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        ImGui.SetNextItemOpen(currentObject.IsExpanded, ImGuiCond.FirstUseEver);
        var isNodeOpen = ImGui.TreeNodeEx(currentObject.Name, flags);

        var cursor = ImGui.GetCursorScreenPos();
        var nodeMax = ImGui.GetItemRectMax();

        if (ImGui.IsItemToggledOpen())
        {
            currentObject.IsExpanded = isNodeOpen;
        }

        if (ImGui.IsItemHovered())
        {
            var nodeMin = ImGui.GetItemRectMin();
            var mousePos = ImGui.GetMousePos();
            var delta = (mousePos.Y - nodeMin.Y) / (nodeMax.Y - nodeMin.Y);

            var relativeMouseX = mousePos.X - nodeMin.X - LineStartOffset;
            var indentSelection = 0;

            switch (delta)
            {
                case <= 0.20f:
                    // Only root objects should interact with previous tree nodes
                    if (currentObject.Parent != null)
                    {
                        indentSelection = level;
                    }
                    else
                    {
                        var lastNode = currentObject.Node.Previous;

                        // First node in the tree does not have a previous node, always indentation = 0
                        if (lastNode != null)
                        {
                            while (true)
                            {
                                // If no more tail nodes or the node was not expanded
                                if (lastNode.Value.Children.Last == null || !lastNode.Value.IsExpanded)
                                {
                                    indentSelection = Math.Max(
                                        0,
                                        Math.Min(
                                            (int)Math.Floor(relativeMouseX / IndentSize),
                                            lastNode.Value.Level
                                        )
                                    );

                                    break;
                                }

                                // Take the tail node of the children
                                lastNode = lastNode.Value.Children.Last;
                            }
                        }
                    }

                    drawList.AddLine(
                        new Vector2(nodeMin.X + LineStartOffset + indentSelection * IndentSize, nodeMin.Y),
                        new Vector2(nodeMax.X, nodeMin.Y),
                        Color.White.ToImGui()
                    );

                    drawList.AddCircleFilled(
                        new Vector2(nodeMin.X + LineStartOffset + indentSelection * IndentSize, nodeMin.Y),
                        3.0f,
                        Color.White.ToImGui()
                    );

                    drawList.AddCircleFilled(
                        new Vector2(nodeMin.X + LineStartOffset + indentSelection * IndentSize, nodeMin.Y),
                        1.5f,
                        Color.Black.ToImGui()
                    );

                    // Only draw parent indication for tree root nodes and when they are not the absolute root tree node
                    if (currentObject.Parent == null && currentObject.Node.Previous != null)
                    {
                        var nextNode = currentObject.Node.Previous;
                        while (indentSelection > currentObject.Level)
                        {
                            // If no more tail nodes or the node has the correct level
                            if (nextNode.Value.Children.Last == null || indentSelection == nextNode.Value.Level)
                            {
                                break;
                            }

                            // Take the tail node of the children
                            nextNode = nextNode.Value.Children.Last;
                        }

                        // Only if tree node has children and is expanded
                        if (nextNode.Value.Children.Count > 0 && nextNode.Value.IsExpanded)
                        {
                            drawList.AddCircleFilled(
                                _other[nextNode.Value],
                                3.0f,
                                Color.White.ToImGui()
                            );

                            drawList.AddCircleFilled(
                                _other[nextNode.Value],
                                1.5f,
                                Color.Black.ToImGui()
                            );
                        }
                    }

                    break;
                case >= 0.70f:
                    var isLastFromSubTree = true;

                    // Cannot be last tree node if the current tree node has next siblings
                    if (currentObject.Node.Next != null)
                    {
                        isLastFromSubTree = false;
                    }
                    // If root tree node
                    else if (currentObject.Parent == null)
                    {
                        // Last node if current tree node does not have any children and is not expanded
                        isLastFromSubTree = currentObject.Node.Next == null && !currentObject.IsExpanded;
                    }
                    else
                    {
                        var parentNode = currentObject.Node;
                        while (parentNode != null)
                        {
                            // If parentNode is tree root
                            if (parentNode.Value.Parent == null)
                            {
                                // Cannot be last tree node if the parent tree node has children and is expanded
                                if (currentObject.Children.Count > 0 && currentObject.IsExpanded)
                                {
                                    isLastFromSubTree = false;
                                    break;
                                }

                                // Is last node if the current tree node does not next siblings
                                isLastFromSubTree = currentObject.Node.Next == null;
                                break;
                            }

                            // Is not last node if the parent tree node has next siblings
                            if (parentNode.Next != null)
                            {
                                isLastFromSubTree = false;
                                break;
                            }

                            // Take the parent tree node of current
                            parentNode = parentNode.Value.Parent.Node;
                        }
                    }

                    if (isLastFromSubTree)
                    {
                        indentSelection = Math.Max(
                            0,
                            Math.Min(
                                (int)Math.Floor(relativeMouseX / IndentSize),
                                currentObject.Level
                            )
                        );
                    }
                    else
                    {
                        indentSelection = currentObject.Level + (
                            currentObject.Children.Count > 0 && currentObject.IsExpanded ? 1 : 0
                        );
                    }

                    drawList.AddLine(
                        new Vector2(nodeMin.X + LineStartOffset + indentSelection * IndentSize, nodeMax.Y),
                        new Vector2(nodeMax.X, nodeMax.Y),
                        Color.White.ToImGui()
                    );

                    drawList.AddCircleFilled(
                        new Vector2(nodeMin.X + LineStartOffset + indentSelection * IndentSize, nodeMax.Y),
                        3.0f,
                        Color.White.ToImGui()
                    );

                    drawList.AddCircleFilled(
                        new Vector2(nodeMin.X + LineStartOffset + indentSelection * IndentSize, nodeMax.Y),
                        1.5f,
                        Color.Black.ToImGui()
                    );

                    // Draw only if last from topmost parent, is not already the correct level and has a parent
                    if (isLastFromSubTree && indentSelection != currentObject.Level && currentObject.Parent != null)
                    {
                        var parent = currentObject.Parent;
                        while (parent.Level > indentSelection)
                        {
                            if (parent.Parent == null || indentSelection == parent.Level)
                            {
                                break;
                            }

                            parent = parent.Parent;
                        }

                        drawList.AddCircleFilled(
                            _other[parent],
                            3.0f,
                            Color.White.ToImGui()
                        );

                        drawList.AddCircleFilled(
                            _other[parent],
                            1.5f,
                            Color.Black.ToImGui()
                        );
                    }

                    break;
            }
        }

        if (currentObject.Children.Count > 0)
        {
            _other[currentObject] = new Vector2(cursor.X + indentSpacing, nodeMax.Y);
        }

        if (isNodeOpen)
        {
            var lastChildY = nodeMax.Y;

            foreach (var childObject in currentObject.Children)
            {
                var rect = RenderTree(childObject, drawList, indentSpacing, level + 1);

                lastChildY = rect.Y - indentSpacing - TreeNodeHeight;

                // Draw horizontal line
                var lineWidth = childObject.Children.Count > 0 ? 5.0f : 10.0f;
                drawList.AddLine(
                    new Vector2(cursor.X, lastChildY),
                    new Vector2(cursor.X + lineWidth, lastChildY),
                    Color.Gray.ToImGui()
                );
            }

            if (currentObject.Children.Count > 0)
            {
                // Draw vertical line
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