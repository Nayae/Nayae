namespace Nayae.Editor;

public class GameObject
{
    internal LinkedListNode<GameObject> Node { get; set; }

    public string Name { get; set; }

    public bool IsExpanded { get; set; } = true;
    public int Level { get; set; }

    public GameObject Parent { get; set; }
    public LinkedList<GameObject> Children { get; set; } = new();

    private GameObject(string name)
    {
        Name = name;
    }

    public static GameObject Create(string name)
    {
        return GameObjectRegistry.Add(new GameObject(name));
    }

    public static GameObject Create(string name, GameObject parent)
    {
        return GameObjectRegistry.AddToParent(new GameObject(name), parent);
    }

    public override string ToString()
    {
        return Name;
    }
}

public static class GameObjectRegistry
{
    private static readonly LinkedList<GameObject> _root;
    private static readonly Dictionary<GameObject, LinkedList<GameObject>> _gameObjectResidingList;

    static GameObjectRegistry()
    {
        _root = new LinkedList<GameObject>();
        _gameObjectResidingList = new Dictionary<GameObject, LinkedList<GameObject>>();
    }

    public static GameObject Add(GameObject obj)
    {
        if (_gameObjectResidingList.TryGetValue(obj, out var list))
        {
            list.Remove(obj);
        }

        obj.Level = 0;
        obj.Node = _root.AddLast(obj);

        _gameObjectResidingList[obj] = _root;

        return obj;
    }

    public static GameObject AddToParent(GameObject obj, GameObject parent)
    {
        if (_gameObjectResidingList.TryGetValue(obj, out var list))
        {
            list.Remove(obj);
        }

        obj.Parent = parent;
        obj.Level = parent.Level + 1;
        obj.Node = parent.Children.AddLast(obj);

        _gameObjectResidingList[obj] = parent.Children;

        return obj;
    }

    public static void PlaceSourceAboveTarget(GameObject source, GameObject target)
    {
        source.Level = target.Level;
        source.Parent = target.Parent;
        UpdateChildrenLevels(source.Children, target.Level + 1);

        var sourceList = _gameObjectResidingList[source];
        sourceList.Remove(source.Node);

        var targetList = _gameObjectResidingList[target];
        _gameObjectResidingList[source] = targetList;
        targetList.AddBefore(target.Node, source.Node);
    }

    public static void PlaceSourceBelowTarget(GameObject source, GameObject target)
    {
        source.Level = target.Level;
        source.Parent = target.Parent;
        UpdateChildrenLevels(source.Children, target.Level + 1);

        var sourceList = _gameObjectResidingList[source];
        sourceList.Remove(source.Node);

        var targetList = _gameObjectResidingList[target];
        _gameObjectResidingList[source] = targetList;
        targetList.AddAfter(target.Node, source.Node);
    }

    private static void UpdateChildrenLevels(LinkedList<GameObject> list, int level)
    {
        var node = list.First;
        while (node != null)
        {
            node.Value.Level = level;

            if (node.Value.Children.Count > 0)
            {
                UpdateChildrenLevels(node.Value.Children, level + 1);
            }

            node = node.Next;
        }
    }

    public static LinkedList<GameObject> GetEditorGameObjectList()
    {
        return _root;
    }
}