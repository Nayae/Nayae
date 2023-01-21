using System.Numerics;
using Nayae.Engine.Utility;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Nayae.Engine.Camera;

public class PerspectiveCamera : ICamera
{
    public Matrix4x4 Projection { get; private set; } = Matrix4x4.Identity;
    public Vector3 Forward { get; private set; } = Vector3.UnitZ;
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    private Vector3 _direction = Vector3.UnitZ;
    private float _targetYaw;
    private float _targetPitch;
    private float _currentYaw;
    private float _currentPitch;

    private Vector2 _size;
    private float _fieldOfView;
    private float _nearPlane;
    private float _farPlane;

    private Vector3 _position;

    public PerspectiveCamera(Vector3 position, Vector2 size, float fieldOfView, float nearPlane, float farPlane)
    {
        _position = position;
        _size = size;
        _fieldOfView = fieldOfView;
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        UpdateProjection();
    }

    public void SetProjectionSize(float width, float height)
    {
        _size = new Vector2(width, height);
        UpdateProjection();
    }

    public void SetPlanes(float nearPlane, float farPlane)
    {
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        UpdateProjection();
    }

    private void UpdateProjection()
    {
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            Scalar.DegreesToRadians(_fieldOfView),
            _size.X / _size.Y,
            _nearPlane,
            _farPlane
        );
    }

    public void Update(float delta)
    {
        UpdateLook();
        UpdateMovement(delta);
    }

    public void UpdateLook()
    {
        if (Input.IsMouseDown(MouseButton.Right))
        {
            _targetYaw += Input.GetMouseDelta().X;
            _targetPitch += Input.GetMouseDelta().Y;
        }

        _currentYaw = MathU.Lerp(_currentYaw, _targetYaw, 0.5f);
        _currentPitch = MathU.Lerp(_currentPitch, _targetPitch, 0.5f);

        _direction.X = MathF.Sin(Scalar.DegreesToRadians(_currentYaw)) *
                       MathF.Cos(Scalar.DegreesToRadians(_currentPitch));
        _direction.Y = MathF.Sin(Scalar.DegreesToRadians(_currentPitch));
        _direction.Z = MathF.Cos(Scalar.DegreesToRadians(_currentYaw)) *
                       MathF.Cos(Scalar.DegreesToRadians(_currentPitch));

        Forward = Vector3.Normalize(_direction);
    }

    private void UpdateMovement(float delta)
    {
        var movementDirection = Vector3.Zero;
        var hasMovement = false;

        if (Input.IsKeyDown(Key.W))
        {
            hasMovement = true;
            movementDirection += Forward;
        }

        if (Input.IsKeyDown(Key.S))
        {
            hasMovement = true;
            movementDirection -= Forward;
        }

        if (Input.IsKeyDown(Key.D))
        {
            hasMovement = true;
            movementDirection += Vector3.Normalize(Vector3.Cross(Forward, -Vector3.UnitY));
        }

        if (Input.IsKeyDown(Key.A))
        {
            hasMovement = true;
            movementDirection -= Vector3.Normalize(Vector3.Cross(Forward, -Vector3.UnitY));
        }

        if (hasMovement)
        {
            _position += Vector3.Normalize(movementDirection) * 10.0f * delta;
        }
    }
}