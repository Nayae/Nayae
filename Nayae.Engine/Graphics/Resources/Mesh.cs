using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nayae.Engine.Graphics.Resources;

public record MeshDescriptor(
    Vector3D<float>[] Vertices
) : IGraphicsResourceDescriptor;

public class Mesh : GraphicsResource<MeshDescriptor>
{
    private uint _vbo;

    public override unsafe void Initialize()
    {
        ID = OpenGL.GenVertexArray();
        Bind();
        {
            _vbo = OpenGL.GenBuffer();
            OpenGL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            OpenGL.BufferData(
                BufferTargetARB.ArrayBuffer,
                new ReadOnlySpan<Vector3D<float>>(Descriptor.Vertices),
                BufferUsageARB.StaticDraw
            );

            OpenGL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            OpenGL.EnableVertexAttribArray(0);
        }
        Unbind();
    }

    public void Render()
    {
        Bind();
        {
            OpenGL.DrawArrays(PrimitiveType.Triangles, 0, (uint)Descriptor.Vertices.Length);
        }
        Unbind();
    }

    public void Bind()
    {
        OpenGL.BindVertexArray(ID);
    }

    public void Unbind()
    {
        OpenGL.BindVertexArray(0);
    }
}