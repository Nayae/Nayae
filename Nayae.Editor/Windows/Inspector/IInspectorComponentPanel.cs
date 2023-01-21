using Nayae.Engine.Core;

namespace Nayae.Editor.Windows.Inspector;

public interface IInspectorComponentPanel
{
    public int Order { get; }
    public Type ComponentType { get; }
    public string Name { get; }

    void SetActiveObject(GameObject obj);
    void Render();
}