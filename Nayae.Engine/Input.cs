using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Input;

namespace Nayae.Engine;

public static class Input
{
    [Flags]
    private enum InputState : byte
    {
        Pressed = 2,
        Released = 4,
        Up = 8,
        Down = 16
    }

    private static IMouse _mouse;
    private static MouseButton[] _mouseButtons;
    private static Dictionary<MouseButton, InputState> _mouseButtonStates;

    private static IKeyboard _keyboard;
    private static Key[] _keyboardKeys;
    private static Dictionary<Key, InputState> _keyboardKeyStates;

    private static Vector2 _mousePosition = Vector2.Zero;
    private static Vector2 _mouseDelta = Vector2.Zero;

    public static void Update()
    {
        UpdateKeyboard();
        UpdateMouse();
    }

    private static void UpdateKeyboard()
    {
        foreach (var key in _keyboardKeys)
        {
            var isPressed = _keyboard.IsKeyPressed(key);
            ref var inputState = ref CollectionsMarshal.GetValueRefOrNullRef(_keyboardKeyStates, key);

            if (isPressed)
            {
                if ((inputState & InputState.Pressed) != 0)
                {
                    inputState ^= InputState.Pressed;
                }

                if ((inputState & InputState.Up) != 0)
                {
                    inputState ^= InputState.Pressed;
                    inputState ^= InputState.Up;
                }

                inputState |= InputState.Down;
            }
            else
            {
                if ((inputState & InputState.Released) != 0)
                {
                    inputState ^= InputState.Released;
                }

                if ((inputState & InputState.Down) != 0)
                {
                    inputState ^= InputState.Released;
                    inputState ^= InputState.Down;
                }

                inputState |= InputState.Up;
            }
        }
    }

    private static void UpdateMouse()
    {
        _mouseDelta = _mouse.Position - _mousePosition;
        _mousePosition = _mouse.Position;

        foreach (var button in _mouseButtons)
        {
            var isPressed = _mouse.IsButtonPressed(button);
            ref var inputState = ref CollectionsMarshal.GetValueRefOrNullRef(_mouseButtonStates, button);

            if (isPressed)
            {
                if ((inputState & InputState.Pressed) != 0)
                {
                    inputState ^= InputState.Pressed;
                }

                if ((inputState & InputState.Up) != 0)
                {
                    inputState ^= InputState.Pressed;
                    inputState ^= InputState.Up;
                }

                inputState |= InputState.Down;
            }
            else
            {
                if ((inputState & InputState.Released) != 0)
                {
                    inputState ^= InputState.Released;
                }

                if ((inputState & InputState.Down) != 0)
                {
                    inputState ^= InputState.Released;
                    inputState ^= InputState.Down;
                }

                inputState |= InputState.Up;
            }
        }
    }

    public static bool IsMousePressed(MouseButton button)
    {
        return (_mouseButtonStates[button] & InputState.Pressed) != 0;
    }

    public static bool IsMouseReleased(MouseButton button)
    {
        return (_mouseButtonStates[button] & InputState.Released) != 0;
    }

    public static bool IsMouseDown(MouseButton button)
    {
        return (_mouseButtonStates[button] & InputState.Down) != 0;
    }

    public static bool IsMouseUp(MouseButton button)
    {
        return (_mouseButtonStates[button] & InputState.Up) != 0;
    }

    public static bool IsKeyPressed(Key key)
    {
        return (_keyboardKeyStates[key] & InputState.Pressed) != 0;
    }

    public static bool IsKeyReleased(Key key)
    {
        return (_keyboardKeyStates[key] & InputState.Released) != 0;
    }

    public static bool IsKeyDown(Key key)
    {
        return (_keyboardKeyStates[key] & InputState.Down) != 0;
    }

    public static bool IsKeyUp(Key key)
    {
        return (_keyboardKeyStates[key] & InputState.Up) != 0;
    }

    public static Vector2 GetMousePosition()
    {
        return _mousePosition;
    }

    public static Vector2 GetMouseDelta()
    {
        return _mouseDelta;
    }

    public static void Initialize(IInputContext context)
    {
        _mouse = context.Mice[0];
        _mouseButtons = Enum.GetValues<MouseButton>();

        _mouseButtonStates = new Dictionary<MouseButton, InputState>();
        foreach (var button in _mouseButtons)
        {
            _mouseButtonStates[button] = InputState.Up;
        }

        _keyboard = context.Keyboards[0];
        _keyboardKeys = Enum.GetValues<Key>().Where(k => k != Key.Unknown).ToArray();

        _keyboardKeyStates = new Dictionary<Key, InputState>();
        foreach (var key in _keyboardKeys)
        {
            _keyboardKeyStates[key] = InputState.Up;
        }
    }
}