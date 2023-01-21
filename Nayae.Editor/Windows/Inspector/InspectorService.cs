using Nayae.Editor.Windows.Inspector.Components;
using Nayae.Engine.Core;

namespace Nayae.Editor.Windows.Inspector;

public class InspectorService
{
    private readonly List<IInspectorComponentPanel> _allPanels;
    private readonly List<IInspectorComponentPanel> _activePanels;

    private GameObject _activeObject;

    public InspectorService()
    {
        _activePanels = new List<IInspectorComponentPanel>();
        _allPanels = new List<IInspectorComponentPanel>();

        CreatePanel<TransformComponentPanel>();

        EditorEvents.GameObjectSelected += obj =>
        {
            _activePanels.Clear();

            _activeObject = obj;

            foreach (var type in _activeObject.GetComponentTypes())
            {
                var panel = _allPanels.Find(p => p.ComponentType == type);
                if (panel == null)
                {
                    continue;
                }

                panel.SetActiveObject(_activeObject);
                _activePanels.Add(panel);
            }

            _activePanels.Sort((p1, p2) => p1.Order > p2.Order ? 1 : 0);
        };
    }

    public void Update()
    {
    }

    public void CreatePanel<T>() where T : IInspectorComponentPanel, new()
    {
        _allPanels.Add(new T());
    }

    public bool IsGameObjectSelected()
    {
        return _activeObject != null;
    }

    public IEnumerable<IInspectorComponentPanel> GetActivePanels()
    {
        return _activePanels;
    }
}