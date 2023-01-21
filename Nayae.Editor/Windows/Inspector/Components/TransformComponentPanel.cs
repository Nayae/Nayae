using ImGuiNET;
using Nayae.Engine.Components;
using Nayae.Engine.Core;

namespace Nayae.Editor.Windows.Inspector.Components;

public class TransformComponentPanel : IInspectorComponentPanel
{
    public string Name => "Transform";
    public int Order => 0;
    public Type ComponentType => typeof(Transform);

    private Transform _transform;

    public void Render()
    {
        ImGui.DragFloat3("Position", ref _transform.Position, 1.0f);
        ImGui.DragFloat3("Scale", ref _transform.Scale, 1.0f);
        ImGui.DragFloat3("Rotation", ref _transform.Rotation, 1.0f);
    }

    public void SetActiveObject(GameObject obj)
    {
        _transform = obj.GetComponent<Transform>();
    }
}