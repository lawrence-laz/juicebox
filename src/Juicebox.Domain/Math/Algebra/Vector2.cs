namespace JuiceboxEngine;

public struct Vector2
{
    public static Vector2 Right => new(1, 0);
    public static Vector2 Left => new(-1, 0);
    public static Vector2 Up => new(0, -1);
    public static Vector2 Down => new(0, 1);
    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);

    public float X { get; set; }
    public float Y { get; set; }

    public double Length => Math.Sqrt((X * X) + (Y * Y));
    public Vector2 Normalized => this / Length;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public double DistanceTo(Vector2 other) => (other - this).Length;
    public Vector2 DirectionTo(Vector2 other) => (other - this).Normalized;
    public bool IsZero() => X.Abs() < 0.0001f && Y.Abs() < 0.0001f;
    public Vector2 MultiplyElementWise(Vector2 other) => new(X * other.X, Y * other.Y);

    public override string ToString() => $"({X};{Y})";

    public static float Dot(Vector2 a, Vector2 b) => (a.X * b.X) + (a.Y * b.Y);
    public static Vector2 Reflect(Vector2 vector, Vector2 normal) => vector - (2 * Dot(vector, normal) * normal);
    public static Vector2 Average(IEnumerable<Vector2> vectors)
    {
        var average = Zero;
        foreach (var vector in vectors)
        {
            average += vector;
        }
        return average / vectors.Count();
    }
    public static Vector2 Random()
    {
        return new Vector2(Mathf.RandomFloat(), Mathf.RandomFloat()).Normalized;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
    public static Vector2 operator *(Vector2 a, int b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(int b, Vector2 a) => new(a.X * b, a.Y * b);
    public static Vector2 operator /(Vector2 a, int b) => new(a.X / b, a.Y / b);
    public static Vector2 operator /(Vector2 a, double b) => new(a.X / (float)b, a.Y / (float)b);
    public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(float b, Vector2 a) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(Vector2 a, double b) => new(a.X * (float)b, a.Y * (float)b);
    public static Vector2 operator *(double b, Vector2 a) => new(a.X * (float)b, a.Y * (float)b);
}

