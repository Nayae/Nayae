using Silk.NET.OpenGL;

namespace Nayae.Engine.Graphics.Resources;

public record FramebufferDescriptor(
    Texture Texture
) : IGraphicsResourceDescriptor;

public class Framebuffer : GraphicsResource<FramebufferDescriptor>
{
    public override void Initialize()
    {
        ID = OpenGL.GenFramebuffer();
        Bind();
        {
            OpenGL.FramebufferTexture2D(
                target: FramebufferTarget.Framebuffer,
                attachment: FramebufferAttachment.ColorAttachment0,
                textarget: Descriptor.Texture.Descriptor.TextureTarget,
                texture: Descriptor.Texture.ID,
                level: 0
            );
        }
        Unbind();
    }

    public void Bind()
    {
        OpenGL.BindFramebuffer(FramebufferTarget.Framebuffer, ID);
    }

    public void Unbind()
    {
        OpenGL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}