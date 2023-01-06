using Silk.NET.OpenGL;

namespace Nayae.Engine.Graphics.Resources;

public record ShaderStage(
    ShaderType Type,
    string Source
);

public record ShaderDescriptor(
    params ShaderStage[] Stages
) : IGraphicsResourceDescriptor;

public class Shader : GraphicsResource<ShaderDescriptor>
{
    private Dictionary<ShaderType, uint> _shaders;

    public override void Initialize()
    {
        _shaders = new Dictionary<ShaderType, uint>();

        if (Descriptor.Stages.Length == 0)
        {
            throw new Exception("Shader should at least have one stage");
        }

        ID = OpenGL.CreateProgram();

        foreach (var stage in Descriptor.Stages)
        {
            var shader = _shaders[stage.Type] = OpenGL.CreateShader(stage.Type);
            OpenGL.ShaderSource(shader, stage.Source);
            OpenGL.CompileShader(shader);

            OpenGL.GetShader(shader, GLEnum.CompileStatus, out var shaderSuccess);
            if (shaderSuccess != 1)
            {
                throw new Exception(OpenGL.GetShaderInfoLog(shader));
            }

            OpenGL.AttachShader(ID, shader);
        }

        OpenGL.LinkProgram(ID);
        OpenGL.GetProgram(ID, ProgramPropertyARB.LinkStatus, out var programSuccess);
        if (programSuccess != 1)
        {
            throw new Exception(OpenGL.GetProgramInfoLog(ID));
        }
    }

    public void Bind()
    {
        OpenGL.UseProgram(ID);
    }

    public void Unbind()
    {
        OpenGL.UseProgram(0);
    }
}