using System.Drawing;
using System.Numerics;
using Nayae.Engine;
using Nayae.Engine.Camera;
using Nayae.Engine.Graphics;
using Nayae.Engine.Graphics.Resources;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader = Nayae.Engine.Graphics.Resources.Shader;

namespace Nayae.Editor;

internal static class Program
{
    private static GL _gl;

    private static IWindow _window;
    private static IInputContext _input;

    private static Shader _shader;
    private static Mesh _mesh;

    private static ICamera _camera;

    private static void Main()
    {
        _window = Window.Create(WindowOptions.Default);

        _window.Load += OnWindowLoad;
        _window.Render += OnWindowRender;
        _window.FramebufferResize += OnWindowResize;
        _window.Closing += OnWindowClosing;

        _window.Run();
    }

    private static void OnWindowLoad()
    {
        _window.Center();

        GraphicsFactory.Initialize(_gl = _window.CreateOpenGL());
        Input.Initialize(_input = _window.CreateInput());

        _shader = GraphicsFactory.CreateShader(
            new ShaderDescriptor(
                new ShaderStage(ShaderType.VertexShader, File.ReadAllText("./Resources/Shaders/basic.vert.glsl")),
                new ShaderStage(ShaderType.FragmentShader, File.ReadAllText("./Resources/Shaders/basic.frag.glsl"))
            )
        );

        _mesh = GraphicsFactory.CreateMesh(
            new MeshDescriptor(
                new[]
                {
                    // new Vector3D<float>(0.0f, 10.0f, 0.0f),
                    // new Vector3D<float>(-10.0f, 0.0f, 0.0f),
                    // new Vector3D<float>(10.0f, 0.0f, 0.0f)
                    new Vector3D<float>(-10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(10.0f, -10.0f, -10.0f),
                    new Vector3D<float>(10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(10.0f, -10.0f, 10.0f),
                    new Vector3D<float>(10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, -10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(-10.0f, 10.0f, 10.0f),
                    new Vector3D<float>(10.0f, -10.0f, 10.0f)
                }
            )
        );

        _camera = new PerspectiveCamera(
            new Vector3(0, 0, 0),
            new Vector2(_window.Size.X, _window.Size.Y),
            45.0f,
            0.01f,
            100.0f
        );
    }

    private static void OnWindowRender(double delta)
    {
        Input.Update();

        _camera.Update((float)delta);

        _gl.ClearColor(Color.CornflowerBlue);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);

        _shader.Bind();
        {
            _shader.SetMatrix4x4("uModel", Matrix4x4.CreateTranslation(0, 0, 50.0f));
            _shader.SetMatrix4x4(
                "uView",
                Matrix4x4.CreateLookAt(
                    _camera.Position,
                    _camera.Position + _camera.Forward,
                    -Vector3.UnitY
                )
            );
            _shader.SetMatrix4x4("uProjection", _camera.Projection);

            _mesh.Render();
        }
        _shader.Unbind();
    }

    private static void OnWindowResize(Vector2D<int> size)
    {
        _gl.Viewport(size);
    }

    private static void OnWindowClosing()
    {
        _gl.Dispose();
        _window.Dispose();
    }

    public static float AsRadians(this float value)
    {
        return Scalar.DegreesToRadians(value);
    }
}