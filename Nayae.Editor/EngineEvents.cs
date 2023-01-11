using Nayae.Engine.Core;

namespace Nayae.Editor;

public static class EditorEvents
{
    public static event Action<GameObject> GameObjectSelected;

    public static void NotifyGameObjectSelected(GameObject obj)
    {
        GameObjectSelected?.Invoke(obj);
    }

    public static event Action<GameObject> GameObjectDeselected;

    public static void NotifyGameObjectDeselected(GameObject obj)
    {
        GameObjectDeselected?.Invoke(obj);
    }
}