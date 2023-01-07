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

    private GameObject(string name, GameObject parent)
    {
        Name = name;
        Parent = parent;
    }

    public static GameObject Create(string name)
    {
        return GameObjectRegistry.Upsert(new GameObject(name));
    }

    public static GameObject Create(string name, GameObject parent)
    {
        return GameObjectRegistry.Upsert(new GameObject(name), parent);
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

    public static GameObject Upsert(GameObject obj)
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

    public static GameObject Upsert(GameObject obj, GameObject parent)
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

    public static LinkedList<GameObject> GetEditorGameObjectList()
    {
        return _root;
    }
}