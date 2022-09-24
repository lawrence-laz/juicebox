namespace JuiceboxEngine;

public static class Mathf
{
    public const float Pi = 3.14159265f;

    public static int Min(int a, int b) => a <= b ? a : b;

    public static float Min(float a, float b) => a <= b ? a : b;

    public static float Max(float a, float b) => a >= b ? a : b;
    public static long Max(long a, long b) => a >= b ? a : b;
    public static int Max(int a, int b) => a >= b ? a : b;

    public static int Swap(int current, int a, int b) => current == a ? b : a;

    public static float Swap(float current, float a, float b) => current == a ? b : a;

    public static float Ceil(float current) => (float)Math.Ceiling(current);
    public static float Round(float current) => (float)Math.Round(current, MidpointRounding.AwayFromZero);

    public static float Floor(float current) => (float)Math.Floor(current);

    public static byte FromFractionToByteValue(this float fraction, byte from, byte to) => (byte)(from + ((to - from) * fraction));

    public static bool IsNegative(this float value) => value < 0;
    public static bool IsPositive(this float value) => value > 0;

    public static float Sign(this float value) => value >= 0 ? 1 : -1;

    /// <summary>
    /// [ 0 ]--------[ from (0.0) ]========[ value (result) ]--------[ to (1.0) ]
    /// 
    /// Returns part marked with '='.
    /// </summary>
    public static float InverseLerp(this float value, float from, float to)
        => (value.Clamped(from, to) - from) / (to - from);

    public static float InverseLerp(this int value, int from, int to)
        => (float)(value.Clamped(from, to) - from) / (to - from);

    public static Vector2 Normalized(this Vector2 a, Vector2 b)
        => new(a.X.InverseLerp(0, b.X), a.Y.InverseLerp(0, b.Y));

    /// <summary>
    /// [ 0 ]--------[ from (0.0) ]--------[ value (x) ]========[ to (1.0) ]
    /// 
    /// Returns part marked with '=', i.e. 1.0 - x.
    /// </summary>
    public static float InverseLerpComplement(this float value, float from, float to)
        => 1.0f - InverseLerp(value, from, to);


    public static float Lerp(this float value, float from, float to) => from + ((to - from) * value);

    public static float Remap(this float value, float inputFrom, float inputTo, float outputFrom, float outputTo)
    {
        return value.InverseLerp(inputFrom, inputTo).Lerp(outputFrom, outputTo);
    }

    public static float FromFraction(this float fraction, float from, float to)
        => from + ((to - from) * fraction);

    public static float DegreesToRadians(this float degrees) => degrees / 180f * Pi;

    // public static float DistanceTo(this Transform from, Transform to) => from.Position.Distance(to.Position);

    public static float Clamped01(this float value) => value.Clamped(0f, 1f);

    public static float Clamped(this float value, float a, float b)
    {
        float min, max;
        if (a > b)
        {
            min = b;
            max = a;
        }
        else
        {
            min = a;
            max = b;
        }

        if (value > max)
            return max;

        if (value < min)
            return min;

        return value;
    }

    public static int Clamped(this int value, int a, int b)
    {
        int min, max;
        if (a > b)
        {
            min = b;
            max = a;
        }
        else
        {
            min = a;
            max = b;
        }

        if (value > max)
            return max;

        if (value < min)
            return min;

        return value;
    }

    public static float DistanceTo(this float a, float b) => Abs(a - b);

    public static float DirectionTo(this float from, float to) => (to - from).Sign();

    public static float MoveTowards(this float original, float target, float delta) =>
        original.DistanceTo(target) < delta ? target : original + (delta * original.DirectionTo(target));

    public static int ClampedToPositive(this int a) => a < 0 ? 0 : a;

    public static float ClampedToPositive(this float a) => a < 0 ? 0 : a;

    public static bool IsPositive(this int value) => value > 0;

    public static bool IsNegative(this int value) => value < 0;

    public static int Abs(this int number) => number > 0 ? number : -number;

    public static float Sqrt(this float number) => (float)System.Math.Sqrt(number);

    public static float GetFractionPart(this float number) => number.Sign() * (number.Abs() - (int)number.Abs());

    public static float Pow(this float number, int exponent)
    {
        for (var i = 0; i < exponent; i++)
        {
            number *= number;
        }

        return number;
    }

    public static float Pow(this float number, float exponent) => (float)System.Math.Pow(number, exponent);

    public static float Abs(this float number) => number > 0 ? number : -number;

    public static float Sin(float a) => (float)System.Math.Sin(a);
    public static float Cos(float a) => (float)System.Math.Cos(a);
}
