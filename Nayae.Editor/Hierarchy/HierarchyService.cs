using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Core;
using Silk.NET.Maths;

namespace Nayae.Editor.Hierarchy;

public class HierarchyService
{
    private readonly GameObjectRegistry _registry;
    private readonly Dictionary<GameObject, HierarchyNodeInfo> _hierarchyNodeInfos;
    private readonly List<LinkedListNode<GameObject>> _treeRootNodes;
    private readonly SortedDictionary<int, GameObject> _activeSelectionObjects;

    private GameObject _lastSelectedObject;
    private int _nextHierarchyNodeIndex;
    private bool _shouldRecalculateHierarchyInformation;

    public HierarchyService(GameObjectRegistry registry)
    {
        _registry = registry;

        _hierarchyNodeInfos = new Dictionary<GameObject, HierarchyNodeInfo>();
        _treeRootNodes = new List<LinkedListNode<GameObject>>();
        _activeSelectionObjects = new SortedDictionary<int, GameObject>();

        EngineEvents.GameObjectCreated += obj =>
        {
            _hierarchyNodeInfos[obj] = new HierarchyNodeInfo();
            _shouldRecalculateHierarchyInformation = true;
        };

        EngineEvents.GameObjectDeleted += obj =>
        {
            _hierarchyNodeInfos.Remove(obj);
            _shouldRecalculateHierarchyInformation = true;
        };
    }

    public void Update()
    {
        if (_shouldRecalculateHierarchyInformation)
        {
            _shouldRecalculateHierarchyInformation = false;
            RecalculateHierarchyNodeInformation();
        }
    }

    public void MoveAboveTarget(GameObject target)
    {
        foreach (var source in _activeSelectionObjects.Values.Reverse())
        {
            if (!IsSourceParentOfTarget(source, target))
            {
                if (source == target)
                {
                    continue;
                }

                GetHierarchyNodeInfo(source).Level = GetHierarchyNodeInfo(target).Level;
                source.Parent = target.Parent;

                UpdateChildrenLevels(source.Children, GetHierarchyNodeInfo(target).Level + 1);

                source.Self.List!.Remove(source.Self);
                target.Self.List!.AddBefore(target.Self, source.Self);
            }
        }

        _shouldRecalculateHierarchyInformation = true;
    }

    public void MoveBelowTarget(GameObject target)
    {
        foreach (var source in _activeSelectionObjects.Values.Reverse())
        {
            if (!IsSourceParentOfTarget(source, target))
            {
                if (source == target)
                {
                    continue;
                }

                GetHierarchyNodeInfo(source).Level = GetHierarchyNodeInfo(target).Level;
                source.Parent = target.Parent;
                UpdateChildrenLevels(source.Children, GetHierarchyNodeInfo(target).Level + 1);

                source.Self.List!.Remove(source.Self);
                target.Self.List!.AddAfter(target.Self, source.Self);
            }
        }

        _shouldRecalculateHierarchyInformation = true;
    }

    public void MoveAsFirstChild(GameObject parent)
    {
        foreach (var source in _activeSelectionObjects.Values.Reverse())
        {
            if (source == parent)
            {
                continue;
            }

            if (!IsSourceParentOfTarget(source, parent))
            {
                GetHierarchyNodeInfo(source).Level = GetHierarchyNodeInfo(parent).Level + 1;
                source.Parent = parent;
                UpdateChildrenLevels(source.Children, GetHierarchyNodeInfo(parent).Level + 2);

                source.Self.List!.Remove(source.Self);
                parent.Children.AddFirst(source.Self);
            }
        }

        GetHierarchyNodeInfo(parent).IsExpanded = true;

        _shouldRecalculateHierarchyInformation = true;
    }

    public void MoveToTop()
    {
        foreach (var source in _activeSelectionObjects.Values.Reverse())
        {
            GetHierarchyNodeInfo(source).Level = 0;
            source.Parent = null;

            UpdateChildrenLevels(source.Children, 1);

            source.Self.List!.Remove(source.Self);
            _registry.GetGameObjects().AddFirst(source.Self);
        }

        _shouldRecalculateHierarchyInformation = true;
    }

    public void SetHierarchyNodeSelection(GameObject obj)
    {
        foreach (var selectionObj in _activeSelectionObjects.Values)
        {
            _hierarchyNodeInfos[selectionObj].IsSelected = false;
        }

        _activeSelectionObjects.Clear();

        var info = GetHierarchyNodeInfo(obj);
        info.IsSelected = true;

        _activeSelectionObjects.Add(info.Index, obj);
        _lastSelectedObject = obj;
    }

    public void ToggleHierarchyNodeSelection(GameObject obj)
    {
        var info = GetHierarchyNodeInfo(obj);
        info.IsSelected = !info.IsSelected;

        if (info.IsSelected)
        {
            _activeSelectionObjects.Remove(info.Index);
        }
        else
        {
            _activeSelectionObjects.Add(info.Index, obj);
        }

        _lastSelectedObject = obj;
    }

    public void AddHierarchyNodeSelectionFromLastToTarget(GameObject target)
    {
        if (_lastSelectedObject == null)
        {
            return;
        }

        var startInfo = GetHierarchyNodeInfo(_lastSelectedObject);
        var targetInfo = GetHierarchyNodeInfo(target);

        if (startInfo.Index == targetInfo.Index)
        {
            return;
        }

        var current = _lastSelectedObject;
        while (current != null)
        {
            var info = GetHierarchyNodeInfo(current);
            info.IsSelected = true;

            if (!_activeSelectionObjects.ContainsKey(info.Index))
            {
                _activeSelectionObjects.Add(info.Index, current);
            }

            if (current == target)
            {
                break;
            }

            if (targetInfo.Index > startInfo.Index)
            {
                if (!TryGetNextVisualTreeObject(current, out current, out _))
                {
                    break;
                }
            }
            else
            {
                if (!TryGetPreviousVisualTreeObject(current, out current, out _))
                {
                    break;
                }
            }
        }

        _lastSelectedObject = target;
    }

    private void UpdateChildrenLevels(LinkedList<GameObject> list, int level)
    {
        var node = list.First;
        while (node != null)
        {
            GetHierarchyNodeInfo(node).Level = level;

            if (node.Value.Children.Count > 0)
            {
                UpdateChildrenLevels(node.Value.Children, level + 1);
            }

            node = node.Next;
        }
    }

    private bool IsSourceParentOfTarget(GameObject source, GameObject target)
    {
        var parent = target;
        while (parent != null)
        {
            if (parent == source)
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    public bool TryGetPreviousVisualTreeObject(GameObject current, out GameObject previous,
        out HierarchyRelativeNodeType type)
    {
        previous = null;
        type = HierarchyRelativeNodeType.None;

        if (current.Self.Previous == null)
        {
            // Root of tree does not have a parent or previous node
            if (current.Parent == null)
            {
                return false;
            }

            // Top of sub-tree should return parent object
            previous = current.Parent;
            type = HierarchyRelativeNodeType.Parent;
            return true;
        }

        // If regular node without children, simply return previous tree node object
        if (current.Self.Previous.Value.Children.Count == 0)
        {
            previous = current.Self.Previous.Value;
            type = HierarchyRelativeNodeType.Sibling;
            return true;
        }

        // Search for last node in each sub-tree till no children of not expanded 
        var node = current.Self.Previous;
        while (node != null)
        {
            if (node.Value.Children.Last == null || !GetHierarchyNodeInfo(node).IsExpanded)
            {
                break;
            }

            node = node.Value.Children.Last;
        }

        previous = node.Value;
        type = HierarchyRelativeNodeType.Child;
        return true;
    }

    public bool TryGetNextVisualTreeObject(GameObject current, out GameObject next, out HierarchyRelativeNodeType type)
    {
        next = null;

        // If node has children and is expanded, next object is always first child
        if (current.Children.First != null && GetHierarchyNodeInfo(current).IsExpanded)
        {
            next = current.Children.First.Value;
            type = HierarchyRelativeNodeType.Child;
            return true;
        }

        // If node has next node
        if (current.Self.Next != null)
        {
            next = current.Self.Next.Value;
            type = HierarchyRelativeNodeType.Sibling;
            return true;
        }

        // Node is absolute leaf of a sub-tree, find root of that sub-tree
        var node = current.Self;
        while (node != null)
        {
            if (node.Value.Parent == null)
            {
                break;
            }

            node = node.Value.Parent.Self;
        }


        // If root does not have a next value, it is the last element of the tree
        if (node?.Next == null)
        {
            type = HierarchyRelativeNodeType.None;
            return false;
        }

        // Next node is the next root node of the current sub-tree
        next = node.Next.Value;
        type = HierarchyRelativeNodeType.Parent;
        return true;
    }

    public bool TryGetParentMatchingLevel(GameObject current, int level, out GameObject parent)
    {
        var node = current.Self;
        while (node != null)
        {
            if (node.Value.Parent == null || GetHierarchyNodeInfo(node).Level == level)
            {
                break;
            }

            node = node.Value.Parent.Self;
        }

        if (node == null)
        {
            parent = null;
            return false;
        }

        parent = node.Value;
        return true;
    }

    public void RecalculateHierarchyNodeInformation()
    {
        _treeRootNodes.Clear();
        _nextHierarchyNodeIndex = 0;

        RecalculateHierarchyNodeInformation(GetHierarchyObjects());

        var originalSelection = new SortedDictionary<int, GameObject>(_activeSelectionObjects);
        _activeSelectionObjects.Clear();

        foreach (var obj in originalSelection.Values)
        {
            _activeSelectionObjects.Add(GetHierarchyNodeInfo(obj).Index, obj);
        }
    }

    private void RecalculateHierarchyNodeInformation(LinkedList<GameObject> objects, int level = 0)
    {
        var node = objects.First;

        while (node != null)
        {
            var info = GetHierarchyNodeInfo(node);

            info.Index = _nextHierarchyNodeIndex++;
            info.Offset = info.Index * HierarchyView.TreeNodeHeight;
            info.Level = level;

            if (info.IsExpanded && node.Value.Children.Count > 0)
            {
                RecalculateHierarchyNodeInformation(node.Value.Children, level + 1);
            }

            if (level == 0)
            {
                _treeRootNodes.Add(node);
            }

            node = node.Next;
        }
    }

    public bool GetFirstVisibleEntry(float scrollY, out LinkedListNode<GameObject> objectNode)
    {
        int start = 0, end = _treeRootNodes.Count - 1, offset = -1;

        while (start <= end)
        {
            var mid = (start + end) / 2;
            if (_hierarchyNodeInfos[_treeRootNodes[mid].Value].Offset <= scrollY)
            {
                start = mid + 1;
            }
            else
            {
                end = mid - 1;
            }

            offset = mid;
        }

        objectNode = offset >= 0 ? _treeRootNodes[Scalar.Max(0, offset - 1)] : null;
        return offset >= 0;
    }

    public bool IsNodeSelected(GameObject obj)
    {
        return _activeSelectionObjects.ContainsKey(GetHierarchyNodeInfo(obj).Index);
    }

    public HierarchyNodeInfo GetHierarchyNodeInfo(GameObject obj)
    {
        return _hierarchyNodeInfos[obj];
    }

    public HierarchyNodeInfo GetHierarchyNodeInfo(LinkedListNode<GameObject> obj)
    {
        return _hierarchyNodeInfos[obj.Value];
    }

    public LinkedList<GameObject> GetHierarchyObjects()
    {
        return _registry.GetGameObjects();
    }
}