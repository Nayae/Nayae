using System.Numerics;

namespace Nayae.Engine.Camera;

public interface ICamera
{
    public Matrix4x4 Projection { get; }

    public Vector3 Position { get; set; }
    public Vector3 Forward { get; }

    void SetProjectionSize(float width, float height);
    void SetPlanes(float nearPlane, float farPlane);

    void Update(float delta);
}