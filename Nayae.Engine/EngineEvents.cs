using Nayae.Engine.Core;

namespace Nayae.Engine;

public static class EngineEvents
{
    public static event Action<GameObject> GameObjectCreated;

    public static void NotifyGameObjectCreated(GameObject obj)
    {
        GameObjectCreated?.Invoke(obj);
    }

    public static event Action<GameObject> GameObjectDeleted;

    public static void NotifyGameObjectDeleted(GameObject obj)
    {
        GameObjectDeleted?.Invoke(obj);
    }
}