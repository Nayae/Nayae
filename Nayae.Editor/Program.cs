using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Nayae.Editor.Windows.Console;
using Nayae.Editor.Windows.Hierarchy;
using Nayae.Editor.Windows.Inspector;
using Nayae.Engine;
using Nayae.Engine.Core;
using Nayae.Engine.Graphics;
using Nayae.Engine.Graphics.Resources;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
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

    private static Shader _clickShader;
    private static Texture _clickTexture;
    private static Framebuffer _clickFramebuffer;

    private static ConsoleService _consoleService;
    private static ConsoleView _consoleView;

    private static GameObjectRegistry _gameObjectRegistry;

    private static HierarchyService _hierarchyService;
    private static HierarchyView _hierarchyView;

    private static InspectorService _inspectorService;
    private static InspectorView _inspectorView;

    private static void Main()
    {
        _consoleService = new ConsoleService();
        Log.Entry += _consoleService.OnLoggerEntry;

        _consoleView = new ConsoleView(_consoleService);

        _gameObjectRegistry = new GameObjectRegistry();

        _hierarchyService = new HierarchyService(_gameObjectRegistry);
        _hierarchyView = new HierarchyView(_hierarchyService);

        _inspectorService = new InspectorService();
        _inspectorView = new InspectorView(_inspectorService);

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

        _clickShader = GraphicsFactory.CreateShader(
            new ShaderDescriptor(
                new ShaderStage(ShaderType.VertexShader, File.ReadAllText("./Resources/Shaders/click.vert.glsl")),
                new ShaderStage(ShaderType.FragmentShader, File.ReadAllText("./Resources/Shaders/click.frag.glsl"))
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

        _clickTexture = GraphicsFactory.CreateTexture(
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

        _clickFramebuffer = GraphicsFactory.CreateFramebuffer(
            new FramebufferDescriptor(
                _clickTexture
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

        for (var i = 0; i < 3; i++)
        {
            var child1 = GameObject.Create($"Child {i}");
            {
                GameObject.Create($"Child {i}.1", child1);
                GameObject.Create($"Child {i}.2", child1);
                var child13 = GameObject.Create($"Child {i}.3", child1);
                {
                    GameObject.Create($"Child {i}.3.1", child13);
                    GameObject.Create($"Child {i}.3.2", child13);
                    GameObject.Create($"Child {i}.3.3", child13);
                }
            }
        }
    }

    private static void OnWindowRender(double delta)
    {
        Input.Update();
        _controller.Update((float)delta);

        _consoleService.Update();
        _hierarchyService.Update();
        _inspectorService.Update();

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
                _clickTexture.Resize(sceneWindowSize.X, sceneWindowSize.Y);
                _sceneWindowSize = sceneWindowSize;
            }

            var id = 0u;
            if (ImGui.IsWindowHovered())
            {
                id = _clickTexture.ID;
                var pos = ImGui.GetMousePos() - ImGui.GetWindowPos();
                pos.Y = ImGui.GetWindowContentRegionMax().Y - pos.Y;

                _clickFramebuffer.Bind();
                {
                    _gl.ClearColor(Color.CornflowerBlue);
                    _gl.Clear(ClearBufferMask.ColorBufferBit);
                    _gl.Viewport(0, 0, sceneWindowSize.X, sceneWindowSize.Y);

                    _clickShader.Bind();
                    {
                        _clickShader.SetColor(Color.Red);
                        _clickShader.SetVector2(pos);
                        _mesh.Render();
                    }
                    _clickShader.Unbind();
                }

                var bytes = new byte[3];
                _gl.ReadPixels(
                    (int)pos.X, (int)pos.Y,
                    1, 1,
                    _clickTexture.Descriptor.TextureSpecification.PixelFormat,
                    _clickTexture.Descriptor.TextureSpecification.PixelType,
                    bytes.AsSpan()
                );

                _clickFramebuffer.Unbind();

                Log.Info(Color.FromArgb(255, bytes[0], bytes[1], bytes[2]));
            }
            // else
            // {
            //     id = _texture.ID;
            //     _framebuffer.Bind();
            //     {
            //         _gl.ClearColor(Color.CornflowerBlue);
            //         _gl.Clear(ClearBufferMask.ColorBufferBit);
            //         _gl.Viewport(0, 0, sceneWindowSize.X, sceneWindowSize.Y);
            //
            //         _shader.Bind();
            //         {
            //             _mesh.Render();
            //         }
            //         _shader.Unbind();
            //     }
            //     _framebuffer.Unbind();
            // }

            _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
            ImGui.Image(
                new nint(id),
                new Vector2(_sceneWindowSize.X, _sceneWindowSize.Y),
                new Vector2(0, 1),
                new Vector2(1, 0)
            );
        }

        ImGui.End();
        ImGui.PopStyleVar();

        _consoleView.Render();
        _hierarchyView.Render();
        _inspectorView.Render();

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