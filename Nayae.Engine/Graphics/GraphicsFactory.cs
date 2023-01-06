using Nayae.Engine.Graphics.Resources;
using Silk.NET.OpenGL;
using Framebuffer = Nayae.Engine.Graphics.Resources.Framebuffer;
using Shader = Nayae.Engine.Graphics.Resources.Shader;
using Texture = Nayae.Engine.Graphics.Resources.Texture;

namespace Nayae.Engine.Graphics;

public static class GraphicsFactory
{
    private static GL _gl;

    public static void Initialize(GL gl)
    {
        _gl = gl;
    }

    public static Shader CreateShader(ShaderDescriptor descriptor)
    {
        // Logger.CVerbose(EngineConstants.InternalLoggerGroup, "Creating shader:", descriptor);
        return CreateResource<Shader, ShaderDescriptor>(descriptor);
    }

    public static Texture CreateTexture(TextureDescriptor descriptor)
    {
        // Logger.CVerbose(EngineConstants.InternalLoggerGroup, "Creating texture:", descriptor);
        return CreateResource<Texture, TextureDescriptor>(descriptor);
    }

    public static Framebuffer CreateFramebuffer(FramebufferDescriptor descriptor)
    {
        // Logger.CVerbose(EngineConstants.InternalLoggerGroup, "Creating framebuffer:", descriptor);
        return CreateResource<Framebuffer, FramebufferDescriptor>(descriptor);
    }

    public static Mesh CreateMesh(MeshDescriptor descriptor)
    {
        // Logger.CVerbose(EngineConstants.InternalLoggerGroup, "Creating mesh:", descriptor);
        return CreateResource<Mesh, MeshDescriptor>(descriptor);
    }

    private static T CreateResource<T, K>(K descriptor)
        where T : GraphicsResource<K>, new()
        where K : IGraphicsResourceDescriptor
    {
        var resource = new T
        {
            OpenGL = _gl,
            Descriptor = descriptor
        };

        resource.Initialize();

        return resource;
    }
}