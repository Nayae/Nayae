using Nayae.Engine.Components;

namespace Nayae.Engine.Core;

public class GameObject
{
    public string Name { get; set; }

    public GameObject Parent { get; set; }
    public LinkedListNode<GameObject> Self { get; set; }
    public LinkedList<GameObject> Children { get; }

    private readonly Dictionary<Type, IComponent> _components;

    private GameObject(string name)
    {
        Name = name;
        Children = new LinkedList<GameObject>();

        _components = new Dictionary<Type, IComponent>
        {
            { typeof(Transform), new Transform() }
        };
    }

    public T GetComponent<T>() where T : class, IComponent
    {
        return (T)_components[typeof(T)];
    }

    public IEnumerable<Type> GetComponentTypes()
    {
        return _components.Keys;
    }

    public static GameObject Create(string name)
    {
        return GameObjectRegistry.AddToRegistry(new GameObject(name));
    }

    public static GameObject Create(string name, GameObject parent)
    {
        return GameObjectRegistry.AddToRegistry(new GameObject(name), parent);
    }

    public override string ToString()
    {
        return Name;
    }
}