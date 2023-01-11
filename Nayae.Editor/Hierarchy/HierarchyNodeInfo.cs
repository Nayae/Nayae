namespace Nayae.Editor.Hierarchy;

public enum HierarchyNodeType
{
    Child,
    Parent,
    Sibling,
    None
}

public class HierarchyNodeInfo
{
    public bool IsExpanded { get; set; } = true;
    public int Level { get; set; }
    public float Offset { get; set; }
    public float Height { get; set; }
}