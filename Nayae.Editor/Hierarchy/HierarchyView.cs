using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Core;
using Nayae.Engine.Extensions;

namespace Nayae.Editor.Hierarchy;

public class HierarchyView
{
    public const float TreeNodePaddingY = 4.0f;
    public const float TreeNodeHeight = 21.0f;

    public const float IndentSize = 11.0f;
    public const float LineStartOffset = 22.0f;

    private readonly HierarchyService _service;
    private readonly Dictionary<GameObject, Vector2> _treeNodeBulletPosition;

    private bool _isDragging;
    private bool _checkDragAction;

    private GameObject _draggingObject;

    public HierarchyView(HierarchyService service)
    {
        _treeNodeBulletPosition = new Dictionary<GameObject, Vector2>();
        _service = service;

        for (var i = 0; i < 1000000; i++)
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

        _service.RecalculateHierarchyOffsets();
    }

    public void Render()
    {
        var watch = Stopwatch.StartNew();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
        if (ImGui.Begin("Hierarchy"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5.0f, TreeNodePaddingY));
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, IndentSize);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.0f));

            if (ImGui.BeginTable("Hierarchy", 1))
            {
                if (_isDragging)
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                        !ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
                       )
                    {
                        _isDragging = false;
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4.0f));
                    ImGui.BeginTooltip();
                    ImGui.Text(_draggingObject.Name);
                    ImGui.EndTooltip();
                    ImGui.PopStyleVar();
                }

                var drawList = ImGui.GetWindowDrawList();
                var indentSpacing = ImGui.GetStyle().IndentSpacing;

                var currentScrollY = ImGui.GetScrollY();
                var currentWindowHeight = ImGui.GetWindowHeight();

                var objects = _service.GetHierarchyObjects();
                if (_service.GetFirstVisibleEntry(objects, currentScrollY, out var currentNode))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Dummy(new Vector2(0, _service.GetHierarchyNodeInfo(currentNode).Offset));

                    while (currentNode != null)
                    {
                        if (_service.GetHierarchyNodeInfo(currentNode).Offset > currentWindowHeight + currentScrollY)
                        {
                            break;
                        }

                        RenderTree(drawList, currentNode.Value, indentSpacing);

                        currentNode = currentNode.Next;
                    }

                    if (currentNode != null && objects.Last != null)
                    {
                        ImGui.Dummy(
                            new Vector2(
                                0,
                                _service.GetHierarchyNodeInfo(objects.Last).Offset +
                                _service.GetHierarchyNodeInfo(objects.Last).Height -
                                _service.GetHierarchyNodeInfo(currentNode).Offset
                            )
                        );
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

    private Vector2 RenderTree(ImDrawListPtr drawList, GameObject current, float indentSpacing)
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

        ImGui.SetNextItemOpen(_service.GetHierarchyNodeInfo(current).IsExpanded, ImGuiCond.FirstUseEver);
        var isNodeOpen = ImGui.TreeNodeEx(current.Name, flags);

        var cursor = ImGui.GetCursorScreenPos();
        var nodeMin = ImGui.GetItemRectMin();
        var nodeMax = ImGui.GetItemRectMax();

        if (ImGui.IsItemToggledOpen())
        {
            _service.GetHierarchyNodeInfo(current).IsExpanded = isNodeOpen;
            _service.RecalculateHierarchyOffsets();
        }

        if (ImGui.IsItemHovered())
        {
            var dragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4.0f);
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
                    if (_service.TryGetPreviousVisualTreeObject(current, out var previous, out var type))
                    {
                        levelSelection = type switch
                        {
                            HierarchyNodeType.Child => Math.Max(
                                0,
                                Math.Min(
                                    (int)Math.Floor(relativeMouseX / IndentSize),
                                    _service.GetHierarchyNodeInfo(previous).Level
                                )
                            ),
                            HierarchyNodeType.Parent => _service.GetHierarchyNodeInfo(previous).Level + 1,
                            HierarchyNodeType.Sibling => _service.GetHierarchyNodeInfo(previous).Level,
                            HierarchyNodeType.None => _service.GetHierarchyNodeInfo(current).Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyNodeType.Child:
                                    if (levelSelection == _service.GetHierarchyNodeInfo(previous).Level)
                                    {
                                        _service.MoveSourceBelowTarget(_draggingObject, previous);
                                    }

                                    break;
                                case HierarchyNodeType.Sibling:
                                case HierarchyNodeType.Parent:
                                    _service.MoveSourceAboveTarget(_draggingObject, current);
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
                            _service.MoveSourceToTop(_draggingObject);
                        }
                    }

                    if (type == HierarchyNodeType.Child)
                    {
                        if (levelSelection != _service.GetHierarchyNodeInfo(previous).Level)
                        {
                            if (_service.TryGetParentMatchingLevel(previous, levelSelection, out var parent))
                            {
                                if (_checkDragAction)
                                {
                                    _service.MoveSourceBelowTarget(_draggingObject, parent);
                                }

                                DrawParentBullet(drawList, parent);
                            }
                        }
                        else
                        {
                            if (_checkDragAction)
                            {
                                _service.MoveSourceBelowTarget(_draggingObject, previous);
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
                        _service.TryGetNextVisualTreeObject(current, out var next, out var type) &&
                        type != HierarchyNodeType.Parent
                    )
                    {
                        levelSelection = type switch
                        {
                            HierarchyNodeType.Child => _service.GetHierarchyNodeInfo(next).Level,
                            HierarchyNodeType.Sibling => _service.GetHierarchyNodeInfo(next).Level,
                            HierarchyNodeType.None => _service.GetHierarchyNodeInfo(current).Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyNodeType.Child:
                                    _service.MoveSourceToTargetAsFirstChild(_draggingObject, current);
                                    break;
                                case HierarchyNodeType.Sibling:
                                    _service.MoveSourceBelowTarget(_draggingObject, current);
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
                                _service.GetHierarchyNodeInfo(current).Level
                            )
                        );

                        if (
                            levelSelection != _service.GetHierarchyNodeInfo(current).Level &&
                            _service.TryGetParentMatchingLevel(current, levelSelection, out var parent)
                        )
                        {
                            if (_checkDragAction)
                            {
                                _service.MoveSourceBelowTarget(_draggingObject, parent);
                            }

                            DrawParentBullet(drawList, parent);
                        }
                        else
                        {
                            if (_checkDragAction)
                            {
                                _service.MoveSourceBelowTarget(_draggingObject, current);
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
                        _service.MoveSourceToTargetAsFirstChild(_draggingObject, current);
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

                lastChildY = rect.Y - indentSpacing - TreeNodePaddingY;

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
    private void DrawParentBullet(ImDrawListPtr drawList, GameObject parent)
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
    private void DrawObjectLine(ImDrawListPtr drawList, float lineStartX, float lineEndX, float lineY)
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