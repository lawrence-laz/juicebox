using JuiceboxEngine;

namespace JuiceboxEngine.Wip;

public struct Matrix33
{
    public float _11, _12, _13, _21, _22, _23, _31, _32, _33;

    public static Matrix33 Identity => new(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1
    );

    public Matrix33(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
    {
        _11 = m11; _12 = m12; _13 = m13; _21 = m21; _22 = m22; _23 = m23; _31 = m31; _32 = m32; _33 = m33;
    }

    public static Vector2 operator *(Matrix33 m, Vector2 v) => new(
        (m._11 * v.X) + (m._12 * v.Y) + m._13,
        (m._21 * v.X) + (m._22 * v.Y) + m._23
    );

    public static Matrix33 operator *(Matrix33 a, Matrix33 b) => new(
        (a._11 * b._11) + (a._12 * b._21) + (a._13 * b._31),
        (a._11 * b._12) + (a._12 * b._22) + (a._13 * b._32),
        (a._11 * b._13) + (a._12 * b._23) + (a._13 * b._33),

        (a._21 * b._11) + (a._22 * b._21) + (a._23 * b._31),
        (a._21 * b._12) + (a._22 * b._22) + (a._23 * b._32),
        (a._21 * b._13) + (a._22 * b._23) + (a._23 * b._33),

        (a._31 * b._11) + (a._32 * b._21) + (a._33 * b._31),
        (a._31 * b._12) + (a._32 * b._22) + (a._33 * b._32),
        (a._31 * b._13) + (a._32 * b._23) + (a._33 * b._33)
    );

    public override string ToString() =>
        $"({_11}, {_12}, {_13}{Environment.NewLine}"
        + $"{_21}, {_22}, {_23}{Environment.NewLine}"
        + $"{_31}, {_32}, {_33})";
}


public static class Matrix33Extensions
{
    public static Matrix33 ToTranslationMatrix(this Vector2 position) => new(
    1, 0, position.X,
    0, 1, position.Y,
    0, 0, 1
    );

    public static Matrix33 ToRotationMatrix(this double radians) => new(
    (float)Math.Cos(radians), (float)-Math.Sin(radians), 0,
    (float)Math.Sin(radians), (float)Math.Cos(radians), 0,
    0, 0, 1
    );

    public static Matrix33 ToScaleMatrix(this Vector2 size) => new(
    size.X, 0, 0,
    0, size.Y, 0,
    0, 0, 1
    );
}
