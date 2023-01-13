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
        Log.TimeStart();
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
                            var lastInfo = _service.GetHierarchyNodeInfo(objects.Last);

                            ImGui.Dummy(
                                new Vector2(
                                    0,
                                    lastInfo.Offset + lastInfo.Height -
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
        Log.TimeEnd();
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

        const ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.SpanFullWidth |
                                         ImGuiTreeNodeFlags.OpenOnArrow |
                                         ImGuiTreeNodeFlags.FramePadding;

        ImGui.SetNextItemOpen(currentNodeInfo.IsExpanded);
        var isNodeOpen = ImGui.TreeNodeEx(
            current.Name,
            current.Children.Count > 0
                ? flags
                : flags | ImGuiTreeNodeFlags.Leaf
        );

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
                            HierarchyNodeRelativeTargetType.Child => Math.Max(
                                0,
                                Math.Min(
                                    (int)Math.Floor(relativeMouseX / IndentSize),
                                    _service.GetHierarchyNodeInfo(previous).Level
                                )
                            ),
                            HierarchyNodeRelativeTargetType.Parent => _service.GetHierarchyNodeInfo(previous).Level + 1,
                            HierarchyNodeRelativeTargetType.Sibling => _service.GetHierarchyNodeInfo(previous).Level,
                            HierarchyNodeRelativeTargetType.None => _service.GetHierarchyNodeInfo(current).Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyNodeRelativeTargetType.Child:
                                    if (levelSelection == _service.GetHierarchyNodeInfo(previous).Level)
                                    {
                                        _service.MoveBelowTarget(previous);
                                    }

                                    break;
                                case HierarchyNodeRelativeTargetType.Sibling:
                                case HierarchyNodeRelativeTargetType.Parent:
                                    _service.MoveAboveTarget(current);
                                    break;
                                case HierarchyNodeRelativeTargetType.None:
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

                    if (type == HierarchyNodeRelativeTargetType.Child)
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
                        type != HierarchyNodeRelativeTargetType.Parent
                    )
                    {
                        levelSelection = type switch
                        {
                            HierarchyNodeRelativeTargetType.Child => _service.GetHierarchyNodeInfo(next).Level,
                            HierarchyNodeRelativeTargetType.Sibling => _service.GetHierarchyNodeInfo(next).Level,
                            HierarchyNodeRelativeTargetType.None => _service.GetHierarchyNodeInfo(current).Level,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (_checkDragAction)
                        {
                            switch (type)
                            {
                                case HierarchyNodeRelativeTargetType.Child:
                                    _service.MoveAsFirstChild(current);
                                    break;
                                case HierarchyNodeRelativeTargetType.Sibling:
                                    _service.MoveBelowTarget(current);
                                    break;
                                case HierarchyNodeRelativeTargetType.None:
                                case HierarchyNodeRelativeTargetType.Parent:
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