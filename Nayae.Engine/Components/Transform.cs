using System.Numerics;

namespace Nayae.Engine.Components;

public class Transform : IComponent
{
    public Vector3 Position = Vector3.Zero;
    public Vector3 Scale = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
}