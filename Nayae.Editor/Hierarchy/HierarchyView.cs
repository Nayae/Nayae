using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Nayae.Engine.Core;
using Nayae.Engine.Extensions;

namespace Nayae.Editor.Hierarchy;

public class HierarchyView
{
    public const float TreeNodePaddingY = 2.0f;
    public const float TreeNodeHeight = 17.0f;

    public const float IndentSize = 11.0f;
    public const float LineStartOffset = 22.0f;

    private readonly HierarchyService _service;
    private readonly Dictionary<GameObject, Vector2> _treeNodeBulletPosition;
    private readonly HashSet<GameObject> _checkSelectionNextFrame;

    private bool _isDragging;
    private bool _checkDragAction;

    public HierarchyView(HierarchyService service)
    {
        _service = service;

        _treeNodeBulletPosition = new Dictionary<GameObject, Vector2>();
        _checkSelectionNextFrame = new HashSet<GameObject>();
    }

    public void Render()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
        if (ImGui.Begin("Hierarchy"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5.0f, TreeNodePaddingY));
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, IndentSize);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.0f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.0f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.0f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.0f));

            if (ImGui.BeginTable("Hierarchy", 1, ImGuiTableFlags.RowBg))
            {
                if (_isDragging)
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                        !ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
                       )
                    {
                        _isDragging = false;
                    }
                }

                var drawList = ImGui.GetWindowDrawList();
                var indentSpacing = ImGui.GetStyle().IndentSpacing;

                var currentScrollY = ImGui.GetScrollY();
                var currentWindowHeight = ImGui.GetWindowHeight();

                if (_service.GetFirstVisibleEntry(currentScrollY, out var currentNode))
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

                    if (currentNode != null)
                    {
                        var objects = _service.GetHierarchyObjects();
                        if (objects.Last != null)
                        {
                            ImGui.Dummy(
                                new Vector2(
                                    0,
                                    _service.GetHierarchyNodeInfo(objects.Last).Offset + TreeNodeHeight -
                                    _service.GetHierarchyNodeInfo(currentNode).Offset
                                )
                            );
                        }
                    }
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(4);

            ImGui.End();
        }

        ImGui.PopStyleVar();
    }

    private Vector2 RenderTree(ImDrawListPtr drawList, GameObject current, float indentSpacing)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var currentNodeInfo = _service.GetHierarchyNodeInfo(current);

        if (currentNodeInfo.IsSelected)
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, Color.FromArgb(25, Color.White).ToImGui());
        }

        var flags = ImGuiTreeNodeFlags.SpanFullWidth |
                    ImGuiTreeNodeFlags.OpenOnArrow |
                    ImGuiTreeNodeFlags.FramePadding;

        if (current.Children.Count == 0)
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        ImGui.SetNextItemOpen(currentNodeInfo.IsExpanded);
        var isNodeOpen = ImGui.TreeNodeEx(current.Name, flags);

        var cursor = ImGui.GetCursorScreenPos();
        var nodeMin = ImGui.GetItemRectMin();
        var nodeMax = ImGui.GetItemRectMax();

        var isNodeToggledOpen = ImGui.IsItemToggledOpen();
        if (isNodeToggledOpen)
        {
            currentNodeInfo.IsExpanded = isNodeOpen;
            _service.RecalculateHierarchyNodeInformation();
        }

        if (ImGui.IsItemHovered())
        {
            var dragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4.0f);
            switch (dragging)
            {
                case true when !_isDragging:
                    _isDragging = true;
                    break;
                case false when _isDragging:
                    _checkDragAction = true;
                    _isDragging = false;
                    break;
            }
        }

        if (_checkSelectionNextFrame.Contains(current))
        {
            if (ImGui.IsItemHovered())
            {
                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _service.SetHierarchyNodeSelection(current);
                    _checkSelectionNextFrame.Remove(current);
                }
            }
            else
            {
                _checkSelectionNextFrame.Remove(current);
            }
        }

        if (!isNodeToggledOpen && ImGui.IsItemClicked())
        {
            if (!_service.IsNodeSelected(current))
            {
                if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                {
                    _service.ToggleHierarchyNodeSelection(current);
                }
                else if (ImGui.IsKeyDown(ImGuiKey.ModShift))
                {
                    _service.AddHierarchyNodeSelectionFromLastToTarget(current);
                }
                else
                {
                    _service.SetHierarchyNodeSelection(current);
                }
            }
            else
            {
                _checkSelectionNextFrame.Add(current);
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
                            HierarchyRelativeNodeType.Child => Math.Max(
                                0,
                                Math.Min(
                                    (int)Math.Floor(relativeMouseX / IndentSize),
                                    _service.GetHierarchyNodeInfo(previous).Level
                                )
                            ),
                            HierarchyRelativeNodeType.Parent => _service.GetHierarchyNodeInfo(previous).Level + 1,
                            HierarchyRelativeNodeType.Sibling => _service.GetHierarchyNodeInfo(previous).Level,
                            HierarchyRelativeNodeType.None => _service.GetHierarchyNodeInfo(current).Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyRelativeNodeType.Child:
                                    if (levelSelection == _service.GetHierarchyNodeInfo(previous).Level)
                                    {
                                        _service.MoveBelowTarget(previous);
                                    }

                                    break;
                                case HierarchyRelativeNodeType.Sibling:
                                case HierarchyRelativeNodeType.Parent:
                                    _service.MoveAboveTarget(current);
                                    break;
                                case HierarchyRelativeNodeType.None:
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
                            _service.MoveToTop();
                        }
                    }

                    if (type == HierarchyRelativeNodeType.Child)
                    {
                        if (levelSelection != _service.GetHierarchyNodeInfo(previous).Level)
                        {
                            if (_service.TryGetParentMatchingLevel(previous, levelSelection, out var parent))
                            {
                                if (_checkDragAction)
                                {
                                    _service.MoveBelowTarget(parent);
                                }

                                DrawParentBullet(drawList, parent);
                            }
                        }
                        else
                        {
                            if (_checkDragAction)
                            {
                                _service.MoveBelowTarget(previous);
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
                        type != HierarchyRelativeNodeType.Parent
                    )
                    {
                        levelSelection = type switch
                        {
                            HierarchyRelativeNodeType.Child => _service.GetHierarchyNodeInfo(next).Level,
                            HierarchyRelativeNodeType.Sibling => _service.GetHierarchyNodeInfo(next).Level,
                            HierarchyRelativeNodeType.None => _service.GetHierarchyNodeInfo(current).Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyRelativeNodeType.Child:
                                    _service.MoveAsFirstChild(current);
                                    break;
                                case HierarchyRelativeNodeType.Sibling:
                                    _service.MoveBelowTarget(current);
                                    break;
                                case HierarchyRelativeNodeType.None:
                                case HierarchyRelativeNodeType.Parent:
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
                            levelSelection != currentNodeInfo.Level &&
                            _service.TryGetParentMatchingLevel(current, levelSelection, out var parent)
                        )
                        {
                            if (_checkDragAction)
                            {
                                _service.MoveBelowTarget(parent);
                            }

                            DrawParentBullet(drawList, parent);
                        }
                        else
                        {
                            if (_checkDragAction)
                            {
                                _service.MoveBelowTarget(current);
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
                        _service.MoveAsFirstChild(current);
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