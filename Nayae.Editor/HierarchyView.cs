using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Extensions;

namespace Nayae.Editor;

public static class HierarchyView
{
    private const float TreeNodeHeight = 4.0f;
    private const float IndentSize = 11.0f;
    private const float LineStartOffset = 22.0f;

    private static readonly Dictionary<GameObject, Vector2> _treeNodeBulletPosition;

    private static bool _isDragging;
    private static bool _checkDragAction;

    private static GameObject _draggingObject;

    static HierarchyView()
    {
        _treeNodeBulletPosition = new Dictionary<GameObject, Vector2>();

        for (var i = 0; i < 3; i++)
        {
            var child1 = GameObject.Create($"Child {i}");
            {
                GameObject.Create($"Child {i}.1", child1);
                GameObject.Create($"Child {i}.2", child1);
                var child13 = GameObject.Create($"Child {i}.3", child1);
                {
                    GameObject.Create($"Child {i}.3.1", child13);
                    GameObject.Create($"Child {i}.3.2", child13);
                    GameObject.Create($"Child {i}.3.3", child13);
                }
            }
        }

        RecalculateHierarchyOffsets(GameObjectRegistry.GetEditorGameObjectList());
    }

    public static void Render()
    {
        var watch = Stopwatch.StartNew();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
        if (ImGui.Begin("Hierarchy"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5.0f, TreeNodeHeight));
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, IndentSize);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.0f));

            if (ImGui.BeginTable("Hierarchy", 1))
            {
                var drawList = ImGui.GetWindowDrawList();
                var indentSpacing = ImGui.GetStyle().IndentSpacing;

                var currentScrollY = ImGui.GetScrollY();
                var currentWindowHeight = ImGui.GetWindowHeight();

                var objects = GameObjectRegistry.GetEditorGameObjectList();
                if (GetFirstVisibleEntry(objects, currentScrollY, out var currentNode))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Dummy(new Vector2(0, currentNode.Value.Offset));

                    while (currentNode != null)
                    {
                        if (currentNode.Value.Offset > currentWindowHeight + currentScrollY)
                        {
                            break;
                        }

                        RenderTree(drawList, currentNode.Value, indentSpacing);

                        currentNode = currentNode.Next;
                    }

                    if (currentNode?.Next != null && objects.Last != null)
                    {
                        ImGui.Dummy(new Vector2(0, objects.Last.Value.Offset - currentNode.Value.Offset));
                    }
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor();

            ImGui.End();
        }

        ImGui.PopStyleVar();
        Log.Info(watch.Elapsed.TotalMilliseconds);
    }

    private static bool GetFirstVisibleEntry(
        LinkedList<GameObject> objects,
        float scrollY,
        out LinkedListNode<GameObject> objectNode
    )
    {
        objectNode = null;

        var headNode = objects.First;
        var tailNode = objects.Last;

        if (headNode == null || tailNode == null)
        {
            return false;
        }

        if (headNode == tailNode)
        {
            objectNode = headNode;
            return true;
        }

        while (headNode != tailNode)
        {
            if (headNode.Value.Offset >= scrollY)
            {
                objectNode = headNode.Previous ?? headNode;
                return true;
            }

            if (tailNode.Value.Offset <= scrollY)
            {
                objectNode = tailNode;
                return true;
            }

            headNode = headNode.Next;
            tailNode = tailNode.Previous;
        }

        objectNode = headNode;
        return true;
    }

    private static float RecalculateHierarchyOffsets(LinkedList<GameObject> objects)
    {
        var node = objects.First;
        var offset = 0.0f;

        while (node != null)
        {
            node.Value.Offset = offset;
            offset += 21;

            if (node.Value.IsExpanded && node.Value.Children.Count > 0)
            {
                offset += RecalculateHierarchyOffsets(node.Value.Children);
            }

            node = node.Next;
        }

        return offset;
    }

    private static Vector2 RenderTree(ImDrawListPtr drawList, GameObject current, float indentSpacing)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.SpanFullWidth |
                    ImGuiTreeNodeFlags.OpenOnArrow |
                    ImGuiTreeNodeFlags.FramePadding;

        if (current.Children.Count == 0)
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        ImGui.SetNextItemOpen(current.IsExpanded, ImGuiCond.FirstUseEver);
        var isNodeOpen = ImGui.TreeNodeEx(current.Name, flags);

        var cursor = ImGui.GetCursorScreenPos();
        var nodeMin = ImGui.GetItemRectMin();
        var nodeMax = ImGui.GetItemRectMax();

        if (ImGui.IsItemToggledOpen())
        {
            current.IsExpanded = isNodeOpen;
        }

        if (ImGui.IsItemHovered())
        {
            var dragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
            switch (dragging)
            {
                case true when !_isDragging:
                    _draggingObject = current;
                    _isDragging = true;
                    break;
                case false when _isDragging:
                    _checkDragAction = true;
                    _isDragging = false;
                    break;
            }
        }

        if (_isDragging && current.Children.Count > 0)
        {
            _treeNodeBulletPosition[current] = new Vector2(cursor.X + indentSpacing, nodeMax.Y);
        }

        if (_checkDragAction || (_isDragging && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)))
        {
            var mousePos = ImGui.GetMousePos();
            var delta = (mousePos.Y - nodeMin.Y) / (nodeMax.Y - nodeMin.Y);
            var relativeMouseX = mousePos.X - nodeMin.X - LineStartOffset;

            switch (delta)
            {
                case <= 0.30f:
                {
                    int levelSelection;
                    if (HierarchyViewHelper.TryGetPreviousVisualTreeObject(current, out var previous, out var type))
                    {
                        levelSelection = type switch
                        {
                            HierarchyNodeType.Child => Math.Max(
                                0,
                                Math.Min(
                                    (int)Math.Floor(relativeMouseX / IndentSize),
                                    previous.Level
                                )
                            ),
                            HierarchyNodeType.Parent => previous.Level + 1,
                            HierarchyNodeType.Sibling => previous.Level,
                            HierarchyNodeType.None => current.Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyNodeType.Child:
                                    if (levelSelection == previous.Level)
                                    {
                                        GameObjectRegistry.MoveSourceBelowTarget(_draggingObject, previous);
                                    }

                                    break;
                                case HierarchyNodeType.Sibling:
                                case HierarchyNodeType.Parent:
                                    GameObjectRegistry.MoveSourceAboveTarget(_draggingObject, current);
                                    break;
                                case HierarchyNodeType.None:
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    else
                    {
                        levelSelection = 0;

                        if (_checkDragAction)
                        {
                            GameObjectRegistry.MoveSourceToTop(_draggingObject);
                        }
                    }

                    if (type == HierarchyNodeType.Child)
                    {
                        if (levelSelection != previous.Level)
                        {
                            if (HierarchyViewHelper.TryGetParentMatchingLevel(previous, levelSelection, out var parent))
                            {
                                if (_checkDragAction)
                                {
                                    GameObjectRegistry.MoveSourceBelowTarget(_draggingObject, parent);
                                }

                                DrawParentBullet(drawList, parent);
                            }
                        }
                        else
                        {
                            if (_checkDragAction)
                            {
                                GameObjectRegistry.MoveSourceBelowTarget(_draggingObject, previous);
                            }
                        }
                    }

                    DrawObjectLine(
                        drawList,
                        lineStartX: nodeMin.X + LineStartOffset + levelSelection * IndentSize,
                        lineEndX: nodeMax.X,
                        lineY: nodeMin.Y
                    );
                    break;
                }
                case >= 0.60f:
                {
                    int levelSelection;
                    if (
                        HierarchyViewHelper.TryGetNextVisualTreeObject(current, out var next, out var type) &&
                        type != HierarchyNodeType.Parent
                    )
                    {
                        levelSelection = type switch
                        {
                            HierarchyNodeType.Child => next.Level,
                            HierarchyNodeType.Sibling => next.Level,
                            HierarchyNodeType.None => current.Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyNodeType.Child:
                                    GameObjectRegistry.MoveSourceToTargetAsFirstChild(_draggingObject, current);
                                    break;
                                case HierarchyNodeType.Sibling:
                                    GameObjectRegistry.MoveSourceBelowTarget(_draggingObject, current);
                                    break;
                                case HierarchyNodeType.None:
                                case HierarchyNodeType.Parent:
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    else
                    {
                        levelSelection = Math.Max(
                            0,
                            Math.Min(
                                (int)Math.Floor(relativeMouseX / IndentSize),
                                current.Level
                            )
                        );

                        if (
                            levelSelection != current.Level &&
                            HierarchyViewHelper.TryGetParentMatchingLevel(current, levelSelection, out var parent)
                        )
                        {
                            if (_checkDragAction)
                            {
                                GameObjectRegistry.MoveSourceBelowTarget(_draggingObject, parent);
                            }

                            DrawParentBullet(drawList, parent);
                        }
                        else
                        {
                            if (_checkDragAction)
                            {
                                GameObjectRegistry.MoveSourceBelowTarget(_draggingObject, current);
                            }
                        }
                    }

                    DrawObjectLine(
                        drawList,
                        lineStartX: nodeMin.X + LineStartOffset + levelSelection * IndentSize,
                        lineEndX: nodeMax.X,
                        lineY: nodeMax.Y
                    );
                    break;
                }
                default:
                    drawList.AddRect(
                        nodeMin,
                        nodeMax,
                        Color.White.ToImGui()
                    );

                    if (_checkDragAction)
                    {
                        GameObjectRegistry.MoveSourceToTargetAsFirstChild(_draggingObject, current);
                    }

                    break;
            }

            _checkDragAction = false;
        }

        if (isNodeOpen)
        {
            var lastChildY = nodeMax.Y;

            var currentNode = current.Children.First;
            while (currentNode != null)
            {
                var rect = RenderTree(drawList, currentNode.Value, indentSpacing);

                lastChildY = rect.Y - indentSpacing - TreeNodeHeight;

                // Draw horizontal line
                var lineWidth = currentNode.Value.Children.Count > 0 ? 5.0f : 10.0f;
                drawList.AddLine(
                    new Vector2(cursor.X, lastChildY),
                    new Vector2(cursor.X + lineWidth, lastChildY),
                    Color.Gray.ToImGui()
                );

                currentNode = currentNode.Next;
            }

            if (current.Children.Count > 0)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void DrawParentBullet(ImDrawListPtr drawList, GameObject parent)
    {
        drawList.AddCircleFilled(
            _treeNodeBulletPosition[parent],
            3.0f,
            Color.White.ToImGui()
        );

        drawList.AddCircleFilled(
            _treeNodeBulletPosition[parent],
            1.5f,
            Color.Black.ToImGui()
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void DrawObjectLine(ImDrawListPtr drawList, float lineStartX, float lineEndX, float lineY)
    {
        drawList.AddLine(
            new Vector2(lineStartX, lineY),
            new Vector2(lineEndX, lineY),
            Color.White.ToImGui()
        );
        drawList.AddCircleFilled(
            new Vector2(lineStartX, lineY),
            3.0f,
            Color.White.ToImGui()
        );
        drawList.AddCircleFilled(
            new Vector2(lineStartX, lineY),
            1.5f,
            Color.Black.ToImGui()
        );
    }
}