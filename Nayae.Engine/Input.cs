using Silk.NET.Input;

namespace Nayae.Engine;

public static class Input
{
    private enum MouseButtonState
    {
        Pressed,
        Released,
        Up,
        Down
    }

    private static IMouse _mouse;
    private static MouseButton[] _mouseButtons;
    private static Dictionary<MouseButton, MouseButtonState> _mouseButtonStates;

    public static void Update()
    {
        foreach (var button in _mouseButtons)
        {
            var isPressed = _mouse.IsButtonPressed(button);
            var currentState = _mouseButtonStates[button];

            if (isPressed)
            {
                _mouseButtonStates[button] = currentState switch
                {
                    MouseButtonState.Up => MouseButtonState.Pressed,
                    MouseButtonState.Pressed => MouseButtonState.Down,
                    _ => _mouseButtonStates[button]
                };
            }
            else
            {
                _mouseButtonStates[button] = currentState switch
                {
                    MouseButtonState.Down => MouseButtonState.Released,
                    MouseButtonState.Released => MouseButtonState.Up,
                    _ => _mouseButtonStates[button]
                };
            }
        }
    }

    public static bool IsMousePressed(MouseButton button)
    {
        return _mouseButtonStates[button] == MouseButtonState.Pressed;
    }

    public static bool IsMouseReleased(MouseButton button)
    {
        return _mouseButtonStates[button] == MouseButtonState.Released;
    }

    public static void Initialize(IInputContext context)
    {
        _mouse = context.Mice[0];
        _mouseButtons = Enum.GetValues<MouseButton>();

        _mouseButtonStates = new Dictionary<MouseButton, MouseButtonState>();
        foreach (var button in _mouseButtons)
        {
            _mouseButtonStates[button] = MouseButtonState.Up;
        }
    }
}