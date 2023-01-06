using Silk.NET.OpenGL;

namespace Nayae.Engine.Graphics;

public abstract class GraphicsResource<T> where T : IGraphicsResourceDescriptor
{
    public GL OpenGL { protected get; init; }

    public uint ID { get; protected set; }
    public T Descriptor { get; init; }

    public abstract void Initialize();
}