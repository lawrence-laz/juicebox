
namespace JuiceboxEngine.Wip;

public class Transform
{
    public Vector2 LocalPosition { get; set; }
    public Vector2 Center { get; set; }
    public Vector2 LocalScale { get; set; } = Vector2.One;
    public double LocalRotation { get; set; }
    public Transform? Parent { get; set; }
    public List<Transform> Children { get; } = new();
    public Vector2 Scale { get; set; } = Vector2.One;

    public Vector2 Position
    {
        get => (Parent?.Position ?? Vector2.Zero) + LocalPosition;
        set => LocalPosition = value - (Parent?.Position ?? Vector2.Zero);
    }

    public double RotationRadians
    {
        get => LocalRotation;
        set => LocalRotation = value;
    }

    public double RotationDegrees
    {
        get => RotationRadians * 180 / Math.PI;
        set => RotationRadians = value / 180 * Math.PI;
    }

    public Matrix33 TranslationMatrix => new(
        1, 0, Position.X,
        0, 1, Position.Y,
        0, 0, 1
    );

    public Matrix33 InverseTranslationMatrix => new(
        1, 0, -Position.X,
        0, 1, -Position.Y,
        0, 0, 1
    );

    public Matrix33 GetLocalToParentMatrix() =>
        LocalPosition.ToTranslationMatrix()
        // * Matrix33.GetTranslationMatrix(-Center)
        * (Parent?.Center ?? Vector2.Zero).ToTranslationMatrix()
        * LocalRotation.ToRotationMatrix()
        * LocalScale.ToScaleMatrix()
        * (-Center).ToTranslationMatrix()
        ;

    public Matrix33 GetParentToLocalMatrix() =>
        new Vector2(LocalScale.X == 0 ? 0 : 1 / LocalScale.X, LocalScale.Y == 0 ? 0 : 1 / LocalScale.Y).ToScaleMatrix()
        * (-LocalRotation).ToRotationMatrix()
        * Center.ToTranslationMatrix()
        * (-LocalPosition).ToTranslationMatrix();

    public Matrix33 GetLocalToWorldMatrix() =>
        (Parent?.GetLocalToWorldMatrix() ?? Matrix33.Identity) * GetLocalToParentMatrix();

    public Matrix33 GetWorldToLocalMatrix() =>
        GetParentToLocalMatrix() * (Parent?.GetWorldToLocalMatrix() ?? Matrix33.Identity);

    public Matrix33 RotationMatrix =>
        LocalPosition.ToTranslationMatrix() *
        new Matrix33(
            (float)Math.Cos(RotationRadians), (float)-Math.Sin(RotationRadians), 0,
            (float)Math.Sin(RotationRadians), (float)Math.Cos(RotationRadians), 0,
            0, 0, 1
        )
        * (-LocalPosition).ToTranslationMatrix()
        ;


    public Matrix33 InverseRotationMatrix =>
        InverseTranslationMatrix
        * new Matrix33(
            (float)Math.Cos(-RotationRadians), (float)-Math.Sin(-RotationRadians), 0,
            (float)Math.Sin(-RotationRadians), (float)Math.Cos(-RotationRadians), 0,
            0, 0, 1
        )
        * TranslationMatrix;

    // public Matrix33 Matrix => ;
}

