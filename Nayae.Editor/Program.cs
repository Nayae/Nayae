using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Nayae.Engine;
using Nayae.Engine.Graphics;
using Nayae.Engine.Graphics.Resources;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using Framebuffer = Nayae.Engine.Graphics.Resources.Framebuffer;
using Shader = Nayae.Engine.Graphics.Resources.Shader;
using Texture = Nayae.Engine.Graphics.Resources.Texture;

namespace Nayae.Editor;

internal static class Program
{
    private static GL _gl;
    private static IInputContext _input;

    private static IWindow _window;

    private static ImGuiController _controller;

    private static Shader _shader;
    private static Texture _texture;
    private static Framebuffer _framebuffer;
    private static Mesh _mesh;
    private static Vector2D<uint> _sceneWindowSize;

    private static void Main()
    {
        GlfwWindowing.RegisterPlatform();
        GlfwInput.RegisterPlatform();

        Log.Entry += ConsoleView.OnLoggerEntry;

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

        _controller = new ImGuiController(_gl, _window, _input, () =>
        {
            var io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        });

        _shader = GraphicsFactory.CreateShader(
            new ShaderDescriptor(
                new ShaderStage(ShaderType.VertexShader, File.ReadAllText("./Resources/Shaders/basic.vert.glsl")),
                new ShaderStage(ShaderType.FragmentShader, File.ReadAllText("./Resources/Shaders/basic.frag.glsl"))
            )
        );

        _texture = GraphicsFactory.CreateTexture(
            new TextureDescriptor(
                TextureTarget.Texture2D,
                new TextureSpecification(
                    Width: 800,
                    Height: 600,
                    InternalFormat.Rgb,
                    PixelFormat.Rgb
                ),
                new[]
                {
                    new TextureParameter(TextureParameterName.TextureMinFilter, GLEnum.Linear),
                    new TextureParameter(TextureParameterName.TextureMagFilter, GLEnum.Linear)
                }
            )
        );

        _framebuffer = GraphicsFactory.CreateFramebuffer(
            new FramebufferDescriptor(
                _texture
            )
        );

        _mesh = GraphicsFactory.CreateMesh(
            new MeshDescriptor(
                new[]
                {
                    new Vector3D<float>(-0.5f, -0.5f, 0.0f),
                    new Vector3D<float>(0.5f, -0.5f, 0.0f),
                    new Vector3D<float>(0.0f, 0.5f, 0.0f)
                }
            )
        );

        Log.Info("Hello, World!");
    }

    private static void OnWindowRender(double delta)
    {
        Input.Update();
        _controller.Update((float)delta);

        _gl.ClearColor(Color.CornflowerBlue);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);

        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        ImGui.ShowDemoWindow();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f));
        ImGui.Begin("Scene");
        {
            var sceneWindowSize = new Vector2D<uint>(
                (uint)ImGui.GetContentRegionAvail().X, (uint)ImGui.GetContentRegionAvail().Y
            );

            if (_sceneWindowSize != sceneWindowSize)
            {
                _texture.Resize(sceneWindowSize.X, sceneWindowSize.Y);
                _sceneWindowSize = sceneWindowSize;
            }

            _framebuffer.Bind();
            {
                _gl.ClearColor(Color.CornflowerBlue);
                _gl.Clear(ClearBufferMask.ColorBufferBit);
                _gl.Viewport(0, 0, sceneWindowSize.X, sceneWindowSize.Y);

                _shader.Bind();
                {
                    _mesh.Render();
                }
                _shader.Unbind();
            }
            _framebuffer.Unbind();

            _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
            ImGui.Image(
                new nint(_texture.ID),
                new Vector2(_sceneWindowSize.X, _sceneWindowSize.Y),
                new Vector2(0, 1),
                new Vector2(1, 0)
            );
        }

        ImGui.End();
        ImGui.PopStyleVar();

        ConsoleView.Render();
        HierarchyView.Render();

        _controller.Render();
    }

    private static void OnWindowResize(Vector2D<int> size)
    {
        _gl.Viewport(size);
    }

    private static void OnWindowClosing()
    {
        _controller.Dispose();
        _gl.Dispose();
        _window.Dispose();
    }
}