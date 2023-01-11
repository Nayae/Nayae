namespace Nayae.Engine.Core;

public class GameObject
{
    public string Name { get; set; }

    public GameObject Parent { get; set; }
    public LinkedListNode<GameObject> Self { get; set; }
    public LinkedList<GameObject> Children { get; set; }

    private GameObject(string name)
    {
        Name = name;
        Children = new LinkedList<GameObject>();
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