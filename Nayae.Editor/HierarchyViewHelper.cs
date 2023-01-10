using System.Runtime.CompilerServices;

namespace Nayae.Editor;

public enum HierarchyNodeType
{
    Child,
    Parent,
    Sibling,
    None
}

public static class HierarchyViewHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryGetPreviousVisualTreeObject(
        GameObject current,
        out GameObject previous,
        out HierarchyNodeType type
    )
    {
        previous = null;
        type = HierarchyNodeType.None;

        if (current.Node.Previous == null)
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
        if (current.Node.Previous.Value.Children.Count == 0)
        {
            previous = current.Node.Previous.Value;
            type = HierarchyNodeType.Sibling;
            return true;
        }

        // Search for last node in each sub-tree till no children of not expanded 
        var node = current.Node.Previous;
        while (node != null)
        {
            if (node.Value.Children.Last == null || !node.Value.IsExpanded)
            {
                break;
            }

            node = node.Value.Children.Last;
        }

        previous = node.Value;
        type = HierarchyNodeType.Child;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryGetNextVisualTreeObject(GameObject current, out GameObject next, out HierarchyNodeType type)
    {
        next = null;

        // If node has children and is expanded, next object is always first child
        if (current.Children.First != null && current.IsExpanded)
        {
            next = current.Children.First.Value;
            type = HierarchyNodeType.Child;
            return true;
        }

        // If node has next node
        if (current.Node.Next != null)
        {
            next = current.Node.Next.Value;
            type = HierarchyNodeType.Sibling;
            return true;
        }

        // Node is absolute leaf of a sub-tree, find root of that sub-tree
        var node = current.Node;
        while (node != null)
        {
            if (node.Value.Parent == null)
            {
                break;
            }

            node = node.Value.Parent.Node;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TryGetParentMatchingLevel(GameObject current, int level, out GameObject parent)
    {
        var node = current.Node;
        while (node != null)
        {
            if (node.Value.Parent == null || node.Value.Level == level)
            {
                break;
            }

            node = node.Value.Parent.Node;
        }

        if (node == null)
        {
            parent = null;
            return false;
        }

        parent = node.Value;
        return true;
    }
}