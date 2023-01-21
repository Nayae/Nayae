using System.Runtime.CompilerServices;

namespace Nayae.Engine.Utility;

public static class MathU
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Lerp(float a, float b, float t)
    {
        if (t <= 0.5f)
        {
            return a + (b - a) * t;
        }

        return b - (b - a) * (1.0f - t);
    }
}