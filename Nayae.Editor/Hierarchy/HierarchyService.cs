using Nayae.Engine.Core;
using Silk.NET.Maths;

namespace Nayae.Editor.Hierarchy;

public class HierarchyService
{
    private readonly GameObjectRegistry _registry;

    private readonly Dictionary<GameObject, HierarchyNodeInfo> _hierarchyNodeInfos;
    private readonly List<LinkedListNode<GameObject>> _treeRootNodes;

    private bool _shouldRecalculateHierarchyOffsets;

    public HierarchyService(GameObjectRegistry registry)
    {
        _hierarchyNodeInfos = new Dictionary<GameObject, HierarchyNodeInfo>();
        _treeRootNodes = new List<LinkedListNode<GameObject>>();

        _registry = registry;
        {
            _registry.GameObjectAdded += obj =>
            {
                _hierarchyNodeInfos[obj] = new HierarchyNodeInfo();
                _shouldRecalculateHierarchyOffsets = true;
            };

            _registry.GameObjectRemoved += obj =>
            {
                _hierarchyNodeInfos.Remove(obj);
                _shouldRecalculateHierarchyOffsets = true;
            };
        }
    }

    public void Update()
    {
        if (_shouldRecalculateHierarchyOffsets)
        {
            _shouldRecalculateHierarchyOffsets = false;

            _treeRootNodes.Clear();
            RecalculateHierarchyOffsets();
        }
    }

    public void MoveSourceAboveTarget(GameObject source, GameObject target)
    {
        if (!IsSourceParentOfTarget(source, target))
        {
            if (source == target)
            {
                return;
            }

            GetHierarchyNodeInfo(source).Level = GetHierarchyNodeInfo(target).Level;
            source.Parent = target.Parent;

            UpdateChildrenLevels(source.Children, GetHierarchyNodeInfo(target).Level + 1);

            source.Self.List!.Remove(source.Self);
            target.Self.List!.AddBefore(target.Self, source.Self);
        }
    }

    public void MoveSourceBelowTarget(GameObject source, GameObject target)
    {
        if (!IsSourceParentOfTarget(source, target))
        {
            if (source == target)
            {
                return;
            }

            GetHierarchyNodeInfo(source).Level = GetHierarchyNodeInfo(target).Level;
            source.Parent = target.Parent;
            UpdateChildrenLevels(source.Children, GetHierarchyNodeInfo(target).Level + 1);

            source.Self.List!.Remove(source.Self);
            target.Self.List!.AddAfter(target.Self, source.Self);
        }
    }

    public void MoveSourceToTargetAsFirstChild(GameObject source, GameObject target)
    {
        if (source == target)
        {
            return;
        }

        if (!IsSourceParentOfTarget(source, target))
        {
            GetHierarchyNodeInfo(source).Level = GetHierarchyNodeInfo(target).Level + 1;
            source.Parent = target;
            UpdateChildrenLevels(source.Children, GetHierarchyNodeInfo(target).Level + 2);

            source.Self.List!.Remove(source.Self);
            target.Children.AddFirst(source.Self);
        }
    }

    public void MoveSourceToTop(GameObject source)
    {
        GetHierarchyNodeInfo(source).Level = 0;
        source.Parent = null;

        UpdateChildrenLevels(source.Children, 1);

        source.Self.List!.Remove(source.Self);
        _registry.GetGameObjects().AddFirst(source.Self);
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

    public bool TryGetPreviousVisualTreeObject(GameObject current, out GameObject previous, out HierarchyNodeType type)
    {
        previous = null;
        type = HierarchyNodeType.None;

        if (current.Self.Previous == null)
        {
            // Root of tree does not have a parent or previous node
            if (current.Parent == null)
            {
                return false;
            }

            // Top of sub-tree should return parent object
            previous = current.Parent;
            type = HierarchyNodeType.Parent;
            return true;
        }

        // If regular node without children, simply return previous tree node object
        if (current.Self.Previous.Value.Children.Count == 0)
        {
            previous = current.Self.Previous.Value;
            type = HierarchyNodeType.Sibling;
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
        type = HierarchyNodeType.Child;
        return true;
    }

    public bool TryGetNextVisualTreeObject(GameObject current, out GameObject next, out HierarchyNodeType type)
    {
        next = null;

        // If node has children and is expanded, next object is always first child
        if (current.Children.First != null && GetHierarchyNodeInfo(current).IsExpanded)
        {
            next = current.Children.First.Value;
            type = HierarchyNodeType.Child;
            return true;
        }

        // If node has next node
        if (current.Self.Next != null)
        {
            next = current.Self.Next.Value;
            type = HierarchyNodeType.Sibling;
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
            type = HierarchyNodeType.None;
            return false;
        }

        // Next node is the next root node of the current sub-tree
        next = node.Next.Value;
        type = HierarchyNodeType.Parent;
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

    public float RecalculateHierarchyOffsets(LinkedList<GameObject> objects = null, bool isTreeRoot = true)
    {
        objects ??= GetHierarchyObjects();

        var node = objects.First;
        var offset = 0.0f;

        while (node != null)
        {
            var info = GetHierarchyNodeInfo(node);

            info.Offset = offset;
            info.Height = HierarchyView.TreeNodeHeight;

            offset += HierarchyView.TreeNodeHeight;

            if (GetHierarchyNodeInfo(node).IsExpanded && node.Value.Children.Count > 0)
            {
                var height = RecalculateHierarchyOffsets(node.Value.Children, false);

                info.Height += height;
                offset += height;
            }

            if (isTreeRoot)
            {
                _treeRootNodes.Add(node);
            }

            node = node.Next;
        }

        return offset;
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