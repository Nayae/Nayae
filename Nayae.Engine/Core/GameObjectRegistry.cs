namespace Nayae.Engine.Core;

public class GameObjectRegistry
{
    public event Action<GameObject> GameObjectAdded;
    public event Action<GameObject> GameObjectRemoved;

    private static GameObjectRegistry _instance;
    private readonly LinkedList<GameObject> _root;

    public GameObjectRegistry()
    {
        _instance = this;
        _root = new LinkedList<GameObject>();
    }

    public static GameObject AddToRegistry(GameObject obj) => _instance.Add(obj);

    public GameObject Add(GameObject obj)
    {
        if (obj.Self == null)
        {
            obj.Self = _root.AddLast(obj);
        }
        else
        {
            obj.Self.List!.Remove(obj.Self);
            _root.AddLast(obj.Self);
        }

        GameObjectAdded?.Invoke(obj);

        return obj;
    }

    public static GameObject AddToRegistry(GameObject obj, GameObject parent) => _instance.Add(obj, parent);

    public GameObject Add(GameObject obj, GameObject parent)
    {
        if (obj.Self == null)
        {
            obj.Parent = parent;
            obj.Self = parent.Children.AddLast(obj);
        }
        else
        {
            obj.Self.List!.Remove(obj.Self);
            parent.Children.AddLast(obj.Self);
        }

        GameObjectAdded?.Invoke(obj);

        return obj;
    }

    public void Remove(GameObject obj)
    {
        obj.Self.List!.Remove(obj.Self);
        GameObjectRemoved?.Invoke(obj);
    }

    public LinkedList<GameObject> GetGameObjects()
    {
        return _root;
    }
}