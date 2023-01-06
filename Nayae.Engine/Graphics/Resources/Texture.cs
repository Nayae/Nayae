using Silk.NET.OpenGL;

namespace Nayae.Engine.Graphics.Resources;

public record TextureParameter(
    TextureParameterName TextureParameterName,
    GLEnum Value
);

public record TextureSpecification(
    uint Width,
    uint Height,
    InternalFormat InternalFormat,
    PixelFormat PixelFormat
)
{
    public int Level { get; init; } = 0;
    public int Border { get; init; } = 0;
    public PixelType PixelType { get; init; } = PixelType.UnsignedByte;
}

public record TextureDescriptor(
    TextureTarget TextureTarget,
    TextureSpecification TextureSpecification,
    TextureParameter[] TextureParameters
) : IGraphicsResourceDescriptor
{
    public byte[] Pixels { get; init; } = null;
}

public class Texture : GraphicsResource<TextureDescriptor>
{
    public override unsafe void Initialize()
    {
        ID = OpenGL.GenTexture();
        Bind();
        {
            if (Descriptor.Pixels == null)
            {
                OpenGL.TexImage2D(
                    target: Descriptor.TextureTarget,
                    level: Descriptor.TextureSpecification.Level,
                    internalformat: Descriptor.TextureSpecification.InternalFormat,
                    width: Descriptor.TextureSpecification.Width,
                    height: Descriptor.TextureSpecification.Height,
                    border: Descriptor.TextureSpecification.Border,
                    format: Descriptor.TextureSpecification.PixelFormat,
                    type: Descriptor.TextureSpecification.PixelType,
                    pixels: (void*)0
                );
            }
            else
            {
                fixed (byte* ptrPixels = Descriptor.Pixels)
                {
                    OpenGL.TexImage2D(
                        target: Descriptor.TextureTarget,
                        level: 0,
                        internalformat: Descriptor.TextureSpecification.InternalFormat,
                        width: Descriptor.TextureSpecification.Width,
                        height: Descriptor.TextureSpecification.Height,
                        border: 0,
                        format: Descriptor.TextureSpecification.PixelFormat,
                        type: PixelType.UnsignedByte,
                        pixels: ptrPixels
                    );
                }
            }

            if (Descriptor.TextureParameters != null)
            {
                foreach (var parameter in Descriptor.TextureParameters)
                {
                    OpenGL.TexParameterI(Descriptor.TextureTarget, parameter.TextureParameterName,
                        (int)parameter.Value);
                }
            }
        }
        Unbind();
    }

    public unsafe void Resize(uint width, uint height)
    {
        Bind();
        {
            OpenGL.TexImage2D(
                target: Descriptor.TextureTarget,
                level: Descriptor.TextureSpecification.Level,
                internalformat: Descriptor.TextureSpecification.InternalFormat,
                width: width,
                height: height,
                border: Descriptor.TextureSpecification.Border,
                format: Descriptor.TextureSpecification.PixelFormat,
                type: Descriptor.TextureSpecification.PixelType,
                pixels: (void*)0
            );
        }

        Unbind();
    }

    public void Bind()
    {
        OpenGL.BindTexture(Descriptor.TextureTarget, ID);
    }

    public void Unbind()
    {
        OpenGL.BindTexture(Descriptor.TextureTarget, 0);
    }
}