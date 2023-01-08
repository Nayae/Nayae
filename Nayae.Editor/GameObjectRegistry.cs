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

    public static void MoveSourceAboveTarget(GameObject source, GameObject target)
    {
        if (!IsSourceParentOfTarget(source, target))
        {
            if (source == target)
            {
                return;
            }

            source.Level = target.Level;
            source.Parent = target.Parent;
            UpdateChildrenLevels(source.Children, target.Level + 1);

            var sourceList = _gameObjectResidingList[source];
            sourceList.Remove(source.Node);

            var targetList = _gameObjectResidingList[target];
            _gameObjectResidingList[source] = targetList;
            targetList.AddBefore(target.Node, source.Node);
        }
    }

    public static void MoveSourceBelowTarget(GameObject source, GameObject target)
    {
        if (!IsSourceParentOfTarget(source, target))
        {
            if (source == target)
            {
                return;
            }

            source.Level = target.Level;
            source.Parent = target.Parent;
            UpdateChildrenLevels(source.Children, target.Level + 1);

            var sourceList = _gameObjectResidingList[source];
            sourceList.Remove(source.Node);

            var targetList = _gameObjectResidingList[target];
            _gameObjectResidingList[source] = targetList;
            targetList.AddAfter(target.Node, source.Node);
        }
    }

    public static void MoveSourceToTargetAsFirstChild(GameObject source, GameObject target)
    {
        if (source == target)
        {
            return;
        }

        if (!IsSourceParentOfTarget(source, target))
        {
            source.Level = target.Level + 1;
            source.Parent = target;
            UpdateChildrenLevels(source.Children, target.Level + 2);

            var sourceList = _gameObjectResidingList[source];
            sourceList.Remove(source.Node);

            _gameObjectResidingList[source] = target.Children;
            target.Children.AddFirst(source.Node);
        }
    }

    public static void MoveSourceToTop(GameObject source)
    {
        source.Level = 0;
        source.Parent = null;
        UpdateChildrenLevels(source.Children, 1);

        var sourceList = _gameObjectResidingList[source];
        sourceList.Remove(source.Node);

        _gameObjectResidingList[source] = _root;
        _root.AddFirst(source.Node);
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

    private static bool IsSourceParentOfTarget(GameObject source, GameObject target)
    {
        var node = target.Node;
        while (node != null)
        {
            if (node.Value == source)
            {
                return true;
            }

            if (node.Value.Parent == null)
            {
                return false;
            }

            node = node.Value.Parent.Node;
        }

        return false;
    }

    public static LinkedList<GameObject> GetEditorGameObjectList()
    {
        return _root;
    }
}