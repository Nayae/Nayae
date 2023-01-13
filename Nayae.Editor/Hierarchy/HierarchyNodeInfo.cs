namespace Nayae.Editor.Hierarchy;

public class HierarchyNodeInfo
{
    public int Index { get; set; }
    public bool IsExpanded { get; set; } = true;
    public bool IsSelected { get; set; }
    public int Level { get; set; }
    public float Offset { get; set; }
    public float Height { get; set; }
}