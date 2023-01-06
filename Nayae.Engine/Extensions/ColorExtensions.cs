using System.Drawing;
using System.Numerics;

namespace Nayae.Engine.Extensions;

public static class ColorExtensions
{
    public static Vector4 ToVector(this Color color)
    {
        return new Vector4(color.R / (float)byte.MaxValue, color.G / (float)byte.MaxValue,
            color.B / (float)byte.MaxValue, color.A / (float)byte.MaxValue);
    }

    public static uint ToImGui(this Color color)
    {
        return (uint)(color.A << 24 | color.B << 16 | color.G << 8 | color.R);
    }
}