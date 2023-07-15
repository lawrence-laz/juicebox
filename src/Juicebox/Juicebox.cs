using System.Collections.Immutable;
using System.Diagnostics;
using static SDL2.SDL;

namespace JuiceboxEngine;

public class Camera : IComponent
{
    public Vector2 Position => Entity.Position;
    public Vector2 Size => Entity.Transform.Scale;

    public Entity Entity { get; init; }

    public Camera(Entity entity)
    {
        Entity = entity;
    }

    public Matrix33 GetWorldToCameraMatrix() => Entity.Transform.GetWorldToLocalMatrix();
}

public static class TransformEntityExtensions
{
    public static Entity WithParent(this Entity entity, Transform parent)
    {
        entity.Transform.Parent = parent;
        return entity;
    }
    public static Entity WithParent(this Entity entity, Entity parent)
    {
        entity.Transform.Parent = parent.Transform;
        return entity;
    }
    public static Entity WithPosition(this Entity entity, Vector2 position)
    {
        entity.Transform.Position = position;
        return entity;
    }
    public static Entity WithPosition(this Entity entity, float x, float y)
    {
        entity.Transform.Position = new Vector2(x, y);
        return entity;
    }
    public static Entity WithLocalPosition(this Entity entity, Vector2 position)
    {
        entity.Transform.LocalPosition = position;
        return entity;
    }
    // public static Entity WithScale(this Entity entity, Vector2 scale)
    // {
    //     entity.Transform.Scale = scale;
    //     return entity;
    // }
    // public static Entity WithScale(this Entity entity, float scale)
    // {
    //     entity.Transform.Scale = Vector2.One * scale;
    //     return entity;
    // }
    public static Entity WithLocalScale(this Entity entity, Vector2 scale)
    {
        entity.Transform.LocalScale = scale;
        return entity;
    }
    public static Entity WithLocalScale(this Entity entity, float scale)
    {
        entity.Transform.LocalScale = Vector2.One * scale;
        return entity;
    }
    public static Entity WithRotation(this Entity entity, float degrees)
    {
        entity.Transform.RotationDegrees = degrees;
        return entity;
    }
}

public class Transform
{
    public Entity Entity { get; init; }
    public Vector2 LocalPosition { get; set; }
    public Vector2 Center { get; set; }
    public Vector2 LocalScale { get; set; } = Vector2.One;
    public double LocalRotation { get; set; }
    public Transform? Parent { get; set; }
    public List<Transform> Children { get; } = new();
    public Vector2 Scale { get; set; } = Vector2.One;

    public Transform(Entity entity)
    {
        Entity = entity;
    }

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

public struct Matrix22
{
    public float _11, _12, _21, _22;

    public Matrix22(float m11, float m12, float m21, float m22)
    {
        _11 = m11; _12 = m12; _21 = m21; _22 = m22;
    }

    public static Vector2 operator *(Matrix22 matrix, Vector2 vector) => new(
        (vector.X * matrix._11) + (vector.Y * matrix._12),
        (vector.X * matrix._21) + (vector.Y * matrix._22));
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

public struct Rectangle
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }

    public static Rectangle One = new(Vector2.Zero, Vector2.One);

    public Vector2 TopLeft => Position;
    public Vector2 TopMiddle => Position + (Vector2.Right * (Size.X / 2));
    public Vector2 TopRight => Position + (Vector2.Right * Size.X);
    public Vector2 BottomLeft => Position + (Vector2.Down * Size.Y);
    public Vector2 BottomMiddle => Position + (Vector2.Down * Size.Y) + (Vector2.Right * (Size.X / 2));
    public Vector2 BottomRight => Position + Size;
    public Vector2 Center => Position + (Size / 2);
    public float Left => Position.X;
    public float Right => Position.X + Size.X;
    public float Bottom => Position.Y + Size.Y;
    public float Top => Position.Y;

    public Rectangle(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public bool Contains(Vector2 point) => point.X >= TopLeft.X && point.X <= TopRight.X && point.Y >= TopLeft.Y && point.Y <= BottomLeft.Y;
    public Rectangle Normalized(Rectangle other) => new(Position.Normalized(other.Size), Size.Normalized(other.Size));
    public Vector2[] AsPoints() => new[] { TopLeft, TopRight, BottomRight, BottomLeft, TopLeft };

    public static Rectangle operator +(Rectangle a, Vector2 b) => new(a.Position + b, a.Size);
    public static Rectangle operator -(Rectangle a, Vector2 b) => new(a.Position - b, a.Size);
    public static Rectangle operator *(Matrix33 m, Rectangle r) => new(m * r.Position, r.Size);

    public override string ToString() => $"({Position.X};{Position.Y};{Size.X};{Size.Y})";
}


public class Polygon
{
    public List<Vector2> Vertices { get; } = new();
    public List<Vector2> Uvs { get; set; } = new();

    public Polygon(IEnumerable<Vector2> vertices, IEnumerable<Vector2> uvs)
    {
        Vertices = vertices.ToList();
        Uvs = uvs.ToList();
    }

    public static Polygon operator *(Matrix33 m, Polygon p) => new(p.Vertices.Select(vertex => m * vertex), p.Uvs);
    public static explicit operator Polygon(Rectangle r) => new(r.AsPoints(), Enumerable.Empty<Vector2>());
}


public class Entity
{
    private readonly List<IComponent> _components = new();

    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; } = new();
    public Vector2 Position
    {
        get => Transform.Position;
        set => Transform.Position = value;
    }
    public double RotationRadians
    {
        get => Transform.RotationRadians;
        set => Transform.RotationRadians = value;
    }
    public double RotationDegrees
    {
        get => Transform.RotationDegrees;
        set => Transform.RotationDegrees = value;
    }
    public Transform Transform { get; }
    public bool IsActive { get; set; } = true;

    public SpriteRenderer? SpriteRenderer { get; set; }
    public IList<IComponent> Components => _components;

    public Entity(string name)
    {
        Transform = new(this);
        Name = name;
    }

    public Entity WithComponent(IComponent component)
    {
        _components.Add(component);
        return this;
    }

    public T? GetComponent<T>() => _components.OfType<T>().FirstOrDefault();
    public T? GetComponentInParent<T>()
    {
        return GetComponent<T>() is T component
            ? component
            : Transform.Parent is Transform parent
            ? parent.Entity.GetComponentInParent<T>()
            : default;
    }

    public void Destroy()
    {
        IsActive = false;
        Juicebox.Destroy(this);
        foreach (var component in _components.OfType<IDisposable>())
        {
            component.Dispose();
        }
    }
}

public static class TagEntityExtensions
{
    public static Entity WithTags(this Entity entity, params string[] tags)
    {
        entity.Tags.Clear();
        entity.Tags.AddRange(tags);
        return entity;
    }
}

public static class BodyEntityExtensions
{
    public static Entity WithBody(this Entity entity) => entity.WithComponent(Juicebox.Physics.NewBody(entity));
    public static Entity WithBody(this Entity entity, out Body body)
    {
        body = Juicebox.Physics.NewBody(entity);
        return entity.WithComponent(body);
    }
}

public static class TextEntityExtensions
{
    public static Entity WithText(this Entity entity, string text, string font) => entity.WithComponent(Juicebox.Instance.NewText(entity, text, font));
    public static Entity WithText(this Entity entity, string value, string font, out Text text)
    {
        text = Juicebox.Instance.NewText(entity, value, font);
        return entity;
    }
}

public static class EventEntityExtensions
{
    public record struct OnPressBuilder(Entity Entity);
    public static OnPressBuilder OnPress(this Entity entity) => new(entity);
    public delegate void OnPressHandler(Entity Entity);
    public static Entity Do(this OnPressBuilder builder, Action<OnPressEntity> handler)
    {
        Juicebox.Instance._events._entityHandlers.AddHandler(builder.Entity, handler);
        return builder.Entity;
    }

    public record struct OnHitBuilder(Entity Entity, Func<Entity, bool>? OtherFilter, Func<Entity, CollisionData, bool>? OtherAndHitFilter);
    public static OnHitBuilder OnHit(this Entity entity) => new(entity, null, null);
    public static OnHitBuilder OnHit(this Entity entity, Func<Entity, bool> otherFilter) => new(entity, otherFilter, null);
    public static OnHitBuilder OnHit(this Entity entity, Func<Entity, CollisionData, bool> otherAndHitFilter) => new(entity, null, otherAndHitFilter);
    public record OnHitEvent(Entity Self, Entity Other, CollisionData Hit);
    public delegate void OnHitHandler(Entity other, Entity self, CollisionData hit);
    public static Entity Do(this OnHitBuilder builder, OnHitHandler handler)
    {
        Juicebox.Instance._events._entityHandlers.AddHandler<OnHitEvent>(builder.Entity, onHitEvent =>
        {
            if ((builder.OtherFilter?.Invoke(onHitEvent.Other) ?? true)
                && (builder.OtherAndHitFilter?.Invoke(onHitEvent.Other, onHitEvent.Hit) ?? true))
            {
                handler.Invoke(onHitEvent.Other, builder.Entity, onHitEvent.Hit);
            }
        });
        return builder.Entity;
    }

    public record struct OnEachFrameBuilder(Entity Entity);
    public static OnEachFrameBuilder OnEachFrame(this Entity entity) => new(entity);
    public delegate void OnEachFrameHandler(Entity entity);
    public static Entity Do(this OnEachFrameBuilder builder, OnEachFrameHandler handler)
    {
        Juicebox.Instance._events.OnEachFrameEventHandlers.Add(new JuiceboxEventHanlder(builder.Entity, null, handler));
        return builder.Entity;
    }
}

public class SpriteRepository
{
    private readonly Dictionary<string, Sprite> _sprites = new();

    public SpriteRepository()
    {
    }

    public void Add(Sprite sprite)
    {
        if (_sprites.ContainsKey(sprite.Path))
        {
            throw new InvalidOperationException($"Sprite '{sprite.Path}' already exists.");
        }
        _sprites[sprite.Path] = sprite;
    }

    public Sprite? GetByPath(string path)
    {
        return _sprites.GetValueOrDefault(path);
    }

    public List<Sprite> GetAll()
    {
        return _sprites.Values.ToList();
    }
}

public class SpriteRendererSystem
{
    private readonly EntityRepository _entityRepository;

    public SpriteRendererSystem(EntityRepository entityRepository)
    {
        _entityRepository = entityRepository;
    }

    public void Start()
    {
        Juicebox.Instance._events._handlers.AddHandler<OnPress>(OnPress);
    }

    private void OnPress(OnPress e)
    {
        foreach (var entity in _entityRepository.GetAll())
        {
            if (entity.SpriteRenderer is not null && entity.GetTargetRectangle().Contains(e.MousePosition))
            {
                Juicebox.Instance.Send(entity, new OnPressEntity(entity, e.MousePosition));
            }
        }
    }
}

public class Sprite
{
    public string Path;
    public Vector2 Center;
    public Rectangle FullRectangle;

    public Sprite(string path, Vector2 center, Rectangle fullRectangle)
    {
        Path = path;
        Center = center;
        FullRectangle = fullRectangle;
    }
}

public class SpriteFactory
{
    private readonly SpriteRepository _spriteRepository;
    private readonly SdlFacade _sdl;

    public SpriteFactory(SpriteRepository spriteRepository, SdlFacade sdl)
    {
        _spriteRepository = spriteRepository;
        _sdl = sdl;
    }

    public Sprite Create(string path)
    {
        var sprite = new Sprite(path, Vector2.Zero, default);
        _sdl.LoadSprite(sprite);
        _spriteRepository.Add(sprite);
        return sprite;
    }
}

public interface IRenderInOrder
{
    public int Order { get; set; }
}

public class SpriteRenderer : IComponent, IRenderInOrder
{
    public Entity Entity { get; init; }
    public Sprite Sprite;
    public Rectangle SourceRectangle { get; set; }
    public int Order { get; set; }

    public SpriteRenderer(Entity entity, Sprite sprite)
    {
        Entity = entity;
        Sprite = sprite;
        SourceRectangle = sprite.FullRectangle;
    }
}

public class AnimationFrame
{
    public Rectangle SourceRectangle;
    public float Duration;

    public AnimationFrame(Rectangle sourceRectangle, float duration)
    {
        SourceRectangle = sourceRectangle;
        Duration = duration;
    }
}

public class Animation
{
    public string SpriteSheetPath { get; set; } = string.Empty;
    public AnimationFrame[] Frames = Array.Empty<AnimationFrame>();

    public Animation(string spriteSheetPath, AnimationFrame[] frames)
    {
        SpriteSheetPath = spriteSheetPath;
        Frames = frames;
    }
}

public class Animator : IComponent
{
    public int _frameIndex;
    public float _frameDuration;

    public Animation Animation { get; set; }
    public Entity Entity { get; init; }
    public AnimationFrame Frame => Animation.Frames[_frameIndex];

    public Animator(Animation animation, Entity entity)
    {
        Animation = animation;
        Entity = entity;
    }

    public void OnUpdate()
    {
        _frameDuration += Juicebox.Time.Delta;
        if (_frameDuration >= Frame.Duration)
        {
            _frameDuration = 0;
            _frameIndex = (_frameIndex + 1) % Animation.Frames.Length;
            // Entity.Sprite.FullRectangle = Frame.SourceRectangle;
            // Entity.Sprite.FullRectangle.Position = Vector2.Zero;
            Entity.SpriteRenderer.SourceRectangle = Frame.SourceRectangle;
        }
    }
}

public interface IComponent
{
    public Entity Entity { get; init; }
}

public class FontFactory
{
    private readonly SdlFacade _sdl;
    private readonly FontRepository _repository;

    public FontFactory(SdlFacade sdl, FontRepository repository)
    {
        _sdl = sdl;
        _repository = repository;
    }

    public Font Create(string path)
    {
        var font = new Font(path);
        _repository.Add(font);
        _sdl.LoadFont(font);
        return font;
    }
}

public class FontRepository
{
    public Dictionary<string, Font> _fonts = new();

    public void Add(Font font)
    {
        if (_fonts.ContainsKey(font.Path))
        {
            throw new InvalidOperationException($"Font '{font.Path}' already exists.");
        }

        _fonts[font.Path] = font;
    }

    public Font? GetByPath(string path)
    {
        return _fonts.GetValueOrDefault(path);
    }
}

public class TextFactory
{
    private readonly FontRepository _fontRepository;
    private readonly FontFactory _fontFactory;
    private readonly SdlFacade _sdl;

    public TextFactory(
        FontRepository fontRepository,
        FontFactory fontFactory,
        SdlFacade sdl)
    {
        _fontRepository = fontRepository;
        _fontFactory = fontFactory;
        _sdl = sdl;
    }

    public Text Create(Entity entity, string fontPath)
    {
        if (_fontRepository.GetByPath(fontPath) is not Font font)
        {
            font = _fontFactory.Create(fontPath);
        }
        var text = new Text(entity, font);
        entity.Components.Add(text);
        _sdl.LoadText(text);
        return text;
    }
}

public class Text : IComponent, IRenderInOrder
{
    private string _value = string.Empty;

    public string Value
    {
        get => _value;
        set
        {
            var oldValue = _value;
            _value = value;
            if (oldValue != value)
            {
                OnValueChanged?.Invoke(this, oldValue);
            }
        }
    }

    /// <summary>
    /// Rendering order among other renderables like sprites, texts, etc.
    /// Higher number goes on top of others.
    /// </summary>
    public int Order { get; set; }
    public Font Font { get; set; }
    public Entity Entity { get; init; }
    public Vector2 Center { get => Entity.Transform.Center; set => Entity.Transform.Center = value; }
    public Rectangle Rectangle;

    public event Action<Text, string> OnValueChanged;

    public Text(Entity entity, Font font)
    {
        Entity = entity;
        Font = font;
    }
}

public static class SpriteEntityExtensions
{
    public static Entity WithSprite(this Entity entity, string path, Action<Sprite>? configureSprite = null)
    {
        entity.SpriteRenderer = Juicebox.GetSprite(entity, path, configureSprite);
        return entity;
    }

    public static Rectangle GetTargetRectangle(this Entity entity)
    {
        var position = Juicebox.Camera.Entity.Transform.InverseTranslationMatrix * entity.Position;

        if (entity.SpriteRenderer is not null)
        {
            return entity.SpriteRenderer.Sprite.FullRectangle - entity.SpriteRenderer.Sprite.Center + position;
        }
        else if (entity.GetComponent<Text>() is Text text)
        {
            return text.Rectangle - text.Center + position;
        }
        else
        {
            throw new InvalidOperationException($"Cannot {nameof(GetTargetRectangle)} on entity without a sprite or text.");
        }
    }
}

public enum MouseButton
{
    None = 0,
    Left, Right, Middle
}

public enum KeyboardButton
{
    None = 0,
    Escape, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Backquote, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key0, Minus, Equals,
    Tab, Q, W, E, R, T, Y, U, I, O, P, LeftBracket, RightBracket, Backslash,
    Capslock, A, S, D, F, G, H, J, K, L, Colon, Quote, Enter,
    LeftShift, Z, X, C, V, B, N, M, Comma, Period, Slash, RightShift,
    LeftCtrl, LeftAlt, Space, RightAlt, RightCtrl,
    Up, Left, Down, Right
}

public record OnPress(Vector2 MousePosition);
public record OnPressEntity(Entity Entity, Vector2 MousePosition);

public class Input
{
    public Vector2 Joystick = Vector2.Zero;
    public Vector2 PointerScreen = Vector2.Zero;
    public Vector2 PointerWorld => Juicebox.Camera.Entity.Transform.TranslationMatrix * PointerScreen;
    public HashSet<MouseButton> _isMouseButtonPressed = new();
    public HashSet<MouseButton> _isMouseButtonDown = new();
    public HashSet<MouseButton> _isMouseButtonUp = new();
    public HashSet<KeyboardButton> _isKeyboardButtonPressed = new();
    public HashSet<KeyboardButton> _isKeyboardButtonDown = new();
    public HashSet<KeyboardButton> _isKeyboardButtonUp = new();

    public static MouseButton GetMouseButton(uint button) => button switch
    {
        SDL_BUTTON_LEFT => MouseButton.Left,
        SDL_BUTTON_MIDDLE => MouseButton.Middle,
        SDL_BUTTON_RIGHT => MouseButton.Right,
        _ => MouseButton.None,
    };

    public static KeyboardButton GetKeyboardButton(SDL_Keycode keycode) => keycode switch
    {
        SDL_Keycode.SDLK_ESCAPE => KeyboardButton.Escape,
        SDL_Keycode.SDLK_F1 => KeyboardButton.F1,
        SDL_Keycode.SDLK_F2 => KeyboardButton.F2,
        SDL_Keycode.SDLK_F3 => KeyboardButton.F3,
        SDL_Keycode.SDLK_F4 => KeyboardButton.F4,
        SDL_Keycode.SDLK_F5 => KeyboardButton.F5,
        SDL_Keycode.SDLK_F6 => KeyboardButton.F6,
        SDL_Keycode.SDLK_F7 => KeyboardButton.F7,
        SDL_Keycode.SDLK_F8 => KeyboardButton.F8,
        SDL_Keycode.SDLK_F9 => KeyboardButton.F9,
        SDL_Keycode.SDLK_F10 => KeyboardButton.F10,
        SDL_Keycode.SDLK_F11 => KeyboardButton.F11,
        SDL_Keycode.SDLK_F12 => KeyboardButton.F12,
        SDL_Keycode.SDLK_BACKQUOTE => KeyboardButton.Backquote,
        SDL_Keycode.SDLK_0 => KeyboardButton.Key0,
        SDL_Keycode.SDLK_1 => KeyboardButton.Key1,
        SDL_Keycode.SDLK_2 => KeyboardButton.Key2,
        SDL_Keycode.SDLK_3 => KeyboardButton.Key3,
        SDL_Keycode.SDLK_4 => KeyboardButton.Key4,
        SDL_Keycode.SDLK_5 => KeyboardButton.Key5,
        SDL_Keycode.SDLK_6 => KeyboardButton.Key6,
        SDL_Keycode.SDLK_7 => KeyboardButton.Key7,
        SDL_Keycode.SDLK_8 => KeyboardButton.Key8,
        SDL_Keycode.SDLK_9 => KeyboardButton.Key9,
        SDL_Keycode.SDLK_MINUS => KeyboardButton.Minus,
        SDL_Keycode.SDLK_EQUALS => KeyboardButton.Equals,
        SDL_Keycode.SDLK_TAB => KeyboardButton.Tab,
        SDL_Keycode.SDLK_q => KeyboardButton.Q,
        SDL_Keycode.SDLK_w => KeyboardButton.W,
        SDL_Keycode.SDLK_e => KeyboardButton.E,
        SDL_Keycode.SDLK_r => KeyboardButton.R,
        SDL_Keycode.SDLK_t => KeyboardButton.T,
        SDL_Keycode.SDLK_y => KeyboardButton.Y,
        SDL_Keycode.SDLK_u => KeyboardButton.U,
        SDL_Keycode.SDLK_i => KeyboardButton.I,
        SDL_Keycode.SDLK_o => KeyboardButton.O,
        SDL_Keycode.SDLK_p => KeyboardButton.P,
        SDL_Keycode.SDLK_LEFTBRACKET => KeyboardButton.LeftBracket,
        SDL_Keycode.SDLK_RIGHTBRACKET => KeyboardButton.RightBracket,
        SDL_Keycode.SDLK_BACKSLASH => KeyboardButton.Backslash,
        SDL_Keycode.SDLK_CAPSLOCK => KeyboardButton.Capslock,
        SDL_Keycode.SDLK_a => KeyboardButton.A,
        SDL_Keycode.SDLK_s => KeyboardButton.S,
        SDL_Keycode.SDLK_d => KeyboardButton.D,
        SDL_Keycode.SDLK_f => KeyboardButton.F,
        SDL_Keycode.SDLK_h => KeyboardButton.H,
        SDL_Keycode.SDLK_j => KeyboardButton.J,
        SDL_Keycode.SDLK_k => KeyboardButton.K,
        SDL_Keycode.SDLK_l => KeyboardButton.L,
        SDL_Keycode.SDLK_COLON => KeyboardButton.Colon,
        SDL_Keycode.SDLK_QUOTE => KeyboardButton.Quote,
        SDL_Keycode.SDLK_RETURN => KeyboardButton.Enter,
        SDL_Keycode.SDLK_LSHIFT => KeyboardButton.LeftShift,
        SDL_Keycode.SDLK_z => KeyboardButton.Z,
        SDL_Keycode.SDLK_x => KeyboardButton.X,
        SDL_Keycode.SDLK_c => KeyboardButton.C,
        SDL_Keycode.SDLK_v => KeyboardButton.V,
        SDL_Keycode.SDLK_b => KeyboardButton.B,
        SDL_Keycode.SDLK_n => KeyboardButton.N,
        SDL_Keycode.SDLK_m => KeyboardButton.M,
        SDL_Keycode.SDLK_COMMA => KeyboardButton.Comma,
        SDL_Keycode.SDLK_PERIOD => KeyboardButton.Period,
        SDL_Keycode.SDLK_SLASH => KeyboardButton.Slash,
        SDL_Keycode.SDLK_RSHIFT => KeyboardButton.RightShift,
        SDL_Keycode.SDLK_LCTRL => KeyboardButton.LeftCtrl,
        SDL_Keycode.SDLK_LALT => KeyboardButton.LeftAlt,
        SDL_Keycode.SDLK_SPACE => KeyboardButton.Space,
        SDL_Keycode.SDLK_RALT => KeyboardButton.RightAlt,
        SDL_Keycode.SDLK_RCTRL => KeyboardButton.RightCtrl,
        SDL_Keycode.SDLK_UP => KeyboardButton.Up,
        SDL_Keycode.SDLK_LEFT => KeyboardButton.Left,
        SDL_Keycode.SDLK_DOWN => KeyboardButton.Down,
        SDL_Keycode.SDLK_RIGHT => KeyboardButton.Right,
        _ => KeyboardButton.None,
    };

    public void AfterUpdate()
    {
        _isKeyboardButtonDown.Clear();
        _isKeyboardButtonUp.Clear();
        _isMouseButtonDown.Clear();
        _isMouseButtonUp.Clear();
    }

    public bool IsPressed(KeyboardButton button) => _isKeyboardButtonPressed.Contains(button);
    public bool IsDown(KeyboardButton button) => _isKeyboardButtonDown.Contains(button);
    public bool IsUp(KeyboardButton button) => _isKeyboardButtonUp.Contains(button);

    public bool IsPressed(MouseButton button) => _isMouseButtonPressed.Contains(button);
    public bool IsDown(MouseButton button) => _isMouseButtonDown.Contains(button);
    public bool IsUp(MouseButton button) => _isMouseButtonUp.Contains(button);

    public void AcceptEvent(SDL_Event @event)
    {
        if (@event.type is SDL_EventType.SDL_MOUSEMOTION)
        {
            (PointerScreen.X, PointerScreen.Y) = @event switch
            {
                { type: SDL_EventType.SDL_MOUSEMOTION } => (@event.motion.x, @event.motion.y),
                _ => (PointerScreen.X, PointerScreen.Y),
            };
        }
        else if (@event.type is SDL_EventType.SDL_MOUSEBUTTONDOWN)
        {
            var button = GetMouseButton(@event.button.button);
            _isMouseButtonDown.Add(button);
            _isMouseButtonPressed.Add(button);

            Juicebox.Send(new OnPress(new(@event.button.x, @event.button.y)));
        }
        else if (@event.type is SDL_EventType.SDL_MOUSEBUTTONUP)
        {
            var button = GetMouseButton(@event.button.button);
            _isMouseButtonUp.Add(button);
            _isMouseButtonPressed.Remove(button);
        }
        else if (@event.type is SDL_EventType.SDL_KEYDOWN or SDL_EventType.SDL_KEYUP)
        {
            if (@event.key.repeat == 1)
            {
                return;
            }

            if (@event.type is SDL_EventType.SDL_KEYDOWN)
            {
                var button = GetKeyboardButton(@event.key.keysym.sym);
                _isKeyboardButtonDown.Add(button);
                _isKeyboardButtonPressed.Add(button);
            }
            else if (@event.type is SDL_EventType.SDL_KEYUP)
            {

                var button = GetKeyboardButton(@event.key.keysym.sym);
                _isKeyboardButtonUp.Add(button);
                _isKeyboardButtonPressed.Remove(button);
            }

            (Joystick.X, Joystick.Y) = @event switch
            {
                { type: SDL_EventType.SDL_KEYDOWN, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_LEFT } => (Joystick.X - 1, Joystick.Y),
                { type: SDL_EventType.SDL_KEYDOWN, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_RIGHT } => (Joystick.X + 1, Joystick.Y),
                { type: SDL_EventType.SDL_KEYDOWN, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_UP } => (Joystick.X, Joystick.Y - 1),
                { type: SDL_EventType.SDL_KEYDOWN, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_DOWN } => (Joystick.X, Joystick.Y + 1),

                { type: SDL_EventType.SDL_KEYUP, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_LEFT } => (Joystick.X + 1, Joystick.Y),
                { type: SDL_EventType.SDL_KEYUP, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_RIGHT } => (Joystick.X - 1, Joystick.Y),
                { type: SDL_EventType.SDL_KEYUP, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_UP } => (Joystick.X, Joystick.Y + 1),
                { type: SDL_EventType.SDL_KEYUP, key.keysym.scancode: SDL_Scancode.SDL_SCANCODE_DOWN } => (Joystick.X, Joystick.Y - 1),
                _ => (Joystick.X, Joystick.Y),
            };
        }
    }
}

public delegate bool OnHitCondition(Entity other);

public record JuiceboxEventHanlder(Entity Entity, object? Condition, object Handler);

public class Events
{
    public List<JuiceboxEventHanlder> OnEachFrameEventHandlers { get; } = new();
    public readonly EventHandlers _handlers = new();
    public readonly EventHandlers<Entity> _entityHandlers = new();

    internal void Send<T>(T @event) => _handlers.SendEvent(@event);
    internal void Send<T>(Entity entity, T @event) => _entityHandlers.SendEvent(entity, @event);
}

public class EventHandlers
{
    private readonly Dictionary<Type, List<object>> _handlers = new();

    public IEnumerable<Action<TEvent>> GetHandlers<TEvent>()
    {
        return _handlers.TryGetValue(typeof(TEvent), out var handlers)
            ? handlers.OfType<Action<TEvent>>()
            : Enumerable.Empty<Action<TEvent>>();
    }

    public void AddHandler<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers) || handlers is null)
        {
            handlers = new List<object>();
            _handlers[typeof(TEvent)] = handlers;
        }

        handlers.Add(handler);
    }

    public void SendEvent<TEvent>(TEvent @event)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers) && handlers is not null)
        {
            foreach (var handler in handlers.OfType<Action<TEvent>>())
            {
                handler?.Invoke(@event);
            }
        }
    }
}
public class EventHandlers<TKey>
{
    private readonly Dictionary<(TKey, Type), List<object>> _handlers = new();

    public IEnumerable<Action<TEvent>> GetHandlers<TEvent>(TKey key)
    {
        return _handlers.TryGetValue((key, typeof(TEvent)), out var handlers)
            ? handlers.OfType<Action<TEvent>>()
            : Enumerable.Empty<Action<TEvent>>();
    }

    public void AddHandler<TEvent>(TKey key, Action<TEvent> handler) where TEvent : class
    {
        if (!_handlers.TryGetValue((key, typeof(TEvent)), out var handlers) || handlers is null)
        {
            handlers = new List<object>();
            _handlers[(key, typeof(TEvent))] = handlers;
        }

        handlers.Add(handler);
    }

    public void SendEvent<TEvent>(TKey key, TEvent @event)
    {
        if (_handlers.TryGetValue((key, typeof(TEvent)), out var handlers) && handlers is not null)
        {
            foreach (var handler in handlers.OfType<Action<TEvent>>())
            {
                handler?.Invoke(@event);
            }
        }
    }
}

public class Time
{
    public Stopwatch _stopwatch;
    public float _lastUpdate;
    public float _delta;
    private float _current = 0;

    public float Delta => _delta;
    public float Current => _current;

    public void Start()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void OnUpdate()
    {
        var current = _stopwatch.ElapsedMilliseconds / 1000f;
        _delta = current - _lastUpdate;
        _lastUpdate = current;
    }

    public void AfterUpdate()
    {
        _current = _stopwatch.ElapsedMilliseconds / 1000f;
    }
}

public class Body : IComponent
{
    private float _drag;

    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public Entity Entity { get; init; }
    public bool IsResting { get; set; } = false;
    public float Bounciness { get; set; } = 1f;
    public float Drag
    {
        get => _drag;
        set
        {
            if (value is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException();
            }
            _drag = value;
        }
    }

    public Body(Entity entity)
    {
        Entity = entity;
    }
}

public class Physics
{
    public List<(Entity Entity, Body Body)> Bodies = new();

    public Vector2 Gravity = Vector2.Down * 500;

    public Body NewBody(Entity entity) => Bodies.AddAndReturnSelf((Entity: entity, Body: new(entity))).Body;

    public void Update(float delta)
    {
        foreach (var (Entity, Body) in Bodies)
        {
            if (Body.IsResting)
            {
                continue;
            }

            if (Body.Velocity.Length > 0.001f && Body.Drag != 0)
            {
                Body.Velocity -= -Body.Velocity.Normalized * Body.Drag * delta;
            }
            Body.Velocity += Gravity * delta;
            Entity.Position += Body.Velocity * delta;
        }
    }
}

public static class IntExtensions
{
    public static byte GetByte(this int number, int index) => (byte)((number >> (8 * index)) & 0xFF);
}

[DebuggerDisplay("#{R:X2}{G:X2}{B:X2}{A:X2}")]
public record Color(byte R, byte G, byte B, byte A = 0xFF)
{
    public static readonly Color White = new(0xFFFFFF);
    public static readonly Color Black = new(0x000000);
    public static readonly Color Gray = new(0x1f2937);
    public static readonly Color Green = new(0x22c55e);
    public static readonly Color Red = new(0xef4444);
    public static readonly Color Blue = new(0x3b82f6);
    public static readonly Color Purple = new(0xa855f7);
    public static readonly Color Yellow = new(0xfacc15);

    public Color(int color) : this(color.GetByte(2), color.GetByte(1), color.GetByte(0), 0xFF) { }
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}

public record Circle(Vector2 Center, float Radius)
{
    public static Circle operator *(Matrix33 m, Circle c) => new(m * c.Center, c.Radius);

    public bool Contains(Vector2 point) => Center.DistanceTo(point) < Radius;
}
public record Line(Vector2 Start, Vector2 End)
{
    public static Line operator *(Matrix33 m, Line l) => new(m * l.Start, m * l.End);
}

public enum Space
{
    None = 0,
    World,
    Screen
}

public class Gizmos
{
    public class DrawData
    {
        public float Duration;
        public Color Color = Color.Green;
        public Space Space = Space.World;

        public DrawData(float duration, Color color, Space space)
        {
            Duration = duration;
            Color = color;
            Space = space;
        }
    }

    public Dictionary<Circle, DrawData> Circles { get; } = new();
    public Dictionary<Line, DrawData> Lines { get; } = new();
    public Action<Circle, Color> RenderCircle;
    public Action<Line, Color> RenderLine;

    public void Update(float delta)
    {
        if (RenderCircle is null || RenderLine is null)
        {
            throw new InvalidOperationException("Missing rendering function");
        }

        foreach (var circle in Circles.Keys)
        {
            RenderCircle(circle, Circles[circle].Color);
            Circles[circle].Duration -= delta;
            if (Circles[circle].Duration < 0)
            {
                Circles[circle].Duration = 0;
            }
        }

        foreach (var line in Lines.Keys)
        {
            RenderLine(line, Lines[line].Color);
            Lines[line].Duration -= delta;
            if (Lines[line].Duration < 0)
            {
                Lines[line].Duration = 0;
            }
        }

        var expiredCircles = Circles.Where(pair => pair.Value.Duration <= 0).Select(pair => pair.Key).ToList();
        foreach (var key in expiredCircles)
        {
            Circles.Remove(key);
        }

        var expiredLines = Lines.Where(pair => pair.Value.Duration <= 0).Select(pair => pair.Key).ToList();
        foreach (var key in expiredLines)
        {
            Lines.Remove(key);
        }
    }
}

public static class ListExtensions
{
    public static T AddAndReturnSelf<T>(this List<T> list, T item)
    {
        list.Add(item);
        return item;
    }
}

public class JuiceboxInstance
{
    public readonly List<Text> _texts = new();
    internal readonly Input _input;
    public readonly Events _events;
    public readonly Time _time;
    public readonly Physics _physics;
    public readonly Gizmos _gizmos;
    public Camera _camera;
    public readonly Collisions _collisions;
    private readonly AudioPlayer _audioPlayer;
    private readonly SdlFacade _sdl;
    private readonly EntityFactory _entityFactory;
    private readonly EntityRepository _entityRepository;
    private readonly TextFactory _textFactory;
    private readonly SpriteRepository _spriteRepository;
    private readonly SpriteFactory _spriteFactory;
    private readonly SpriteRendererSystem _spriteRendererSystem;
    private readonly MainLoop _mainLoop;
    public Action<Sprite>? OnLoadSprite;

    public JuiceboxInstance(
        SdlFacade sdl,
        EntityFactory entityFactory,
        EntityRepository entityRepository,
        TextFactory textFactory,
        SpriteRepository spriteRepository,
        SpriteFactory spriteFactory,
        SpriteRendererSystem spriteRendererSystem,
        MainLoop mainLoop,
        Input input,
        Events events,
        Time time,
        Physics physics,
        Gizmos gizmos,
        Collisions collisions,
        AudioPlayer audioPlayer)
    {
        Juicebox.Instance = this;
        _sdl = sdl;
        _entityFactory = entityFactory;
        _entityRepository = entityRepository;
        _textFactory = textFactory;
        _spriteRepository = spriteRepository;
        _spriteFactory = spriteFactory;
        _spriteRendererSystem = spriteRendererSystem;
        _mainLoop = mainLoop;
        _input = input;
        _events = events;
        _time = time;
        _physics = physics;
        _gizmos = gizmos;
        _collisions = collisions;
        _audioPlayer = audioPlayer;
    }

    public void Start()
    {
        _camera = new(NewEntity("camera"));
        _sdl.Start();
        _spriteRendererSystem.Start();
        _time.Start();
    }

    public void BeginMainLoopThisIsBadNameFixMePlease()
    {
        _mainLoop.Start();
    }

    public void Stop()
    {
        _sdl.Stop();
    }

    public void PlaySound(Sound sound) => _audioPlayer.Play(sound);
    public void PlaySound(string soundPath) => _audioPlayer.PlaySound(soundPath);

    public SpriteRenderer GetSprite(Entity entity, string path, Action<Sprite>? configureSprite)
    {
        if (_spriteRepository.GetByPath(path) is not Sprite sprite)
        {
            sprite = _spriteFactory.Create(path);
            configureSprite?.Invoke(sprite);
        }
        var spriteRenderer = new SpriteRenderer(entity, sprite);
        entity.Components.Add(spriteRenderer);
        entity.Transform.Center = sprite.Center;
        return spriteRenderer;
    }
    public Text NewText(Entity entity, string text, string font)
    {
        var textComponent = _textFactory.Create(entity, font);
        textComponent.Value = text;
        return textComponent;
    }

    public void Send<T>(T @event) => _events.Send(@event);
    public void Send<T>(Entity entity, T @event) => _events.Send(entity, @event);

    internal Entity NewEntity(string name) => _entityFactory.Create(name);
    internal IList<T> FindComponents<T>() => _entityRepository.GetComponents<T>();
    internal IEnumerable<Entity> FindEntitiesByTag(string tag) => _entityRepository.GetByTag(tag);
    internal Entity? FindEntityByName(string name) => _entityRepository.GetByName(name);

    internal void Destroy(Entity entity)
    {
        _entityRepository.Remove(entity);
    }

    internal void PlayMusic(string musicPath)
    {
        _audioPlayer.PlayMusic(musicPath);
    }
}

public static class Juicebox
{
    private static JuiceboxInstance? _instance;
    public static JuiceboxInstance Instance
    {
        get => _instance ?? throw new InvalidOperationException("Start Juicebox engine before using it.");
        internal set => _instance = value;
    }

    public static Input Input => Instance._input;
    public static Time Time => Instance._time;
    public static Physics Physics => Instance._physics;
    public static Gizmos Gizmos => Instance._gizmos;
    public static Camera Camera => Instance._camera;

    public static Entity NewEntity(string name) => Instance.NewEntity(name);
    public static SpriteRenderer GetSprite(Entity entity, string path, Action<Sprite> configuration) => Instance.GetSprite(entity, path, configuration);
    public static void DrawCircle(Vector2 center, float radius, Color color, Space space = Space.World) =>
        Gizmos.Circles[(space == Space.World ? Camera.GetWorldToCameraMatrix() : Matrix33.Identity) * new Circle(center, radius)]
            = new Gizmos.DrawData(0.0001f, color, space);
    public static void Draw(Circle circle, Color color, Space space = Space.World) => DrawCircle(circle.Center, circle.Radius, color, space);
    public static void Draw(CircleCollider circle, Space space = Space.World) => DrawCircle(circle.Circle.Center, circle.Circle.Radius, Color.Green, space);
    public static void DrawLine(Vector2 start, Vector2 end, Color color, Space space = Space.World) =>
        Gizmos.Lines[(space == Space.World ? Camera.GetWorldToCameraMatrix() : Matrix33.Identity) * new Line(start, end)]
            = new Gizmos.DrawData(0.0001f, color, space);
    public static void DrawRectangle(Rectangle rectangle, Color color, Space space = Space.World)
    {
        var transformation = space == Space.World ? Camera.GetWorldToCameraMatrix() : Matrix33.Identity;
        Gizmos.Lines[transformation * new Line(rectangle.TopLeft, rectangle.TopRight)] = new Gizmos.DrawData(0.0001f, color, space);
        Gizmos.Lines[transformation * new Line(rectangle.TopRight, rectangle.BottomRight)] = new Gizmos.DrawData(0.0001f, color, space);
        Gizmos.Lines[transformation * new Line(rectangle.BottomRight, rectangle.BottomLeft)] = new Gizmos.DrawData(0.0001f, color, space);
        Gizmos.Lines[transformation * new Line(rectangle.BottomLeft, rectangle.TopLeft)] = new Gizmos.DrawData(0.0001f, color, space);
    }
    public static void Draw(RectangleCollider collider) => DrawRectangle(collider.Rectangle, Color.Green);
    public static void DrawPolygon(Polygon polygon, Color color, Space space = Space.World)
    {
        polygon.Vertices.ForeachInPairs((start, end) =>
        {
            Gizmos.Lines[new Line(start, end)] = new Gizmos.DrawData(0.0001f, color, space);
        });
    }
    public static void Send<T>(T @event) => Instance.Send(@event);
    public static void Send<T>(Entity entity, T @event) => Instance.Send(entity, @event);
    public static Entity? FindEntityByName(string name) => Instance.FindEntityByName(name);
    public static IEnumerable<Entity> FindEntitiesByTag(string tag) => Instance.FindEntitiesByTag(tag);
    public static Entity? FindEntityByTag(string tag) => FindEntitiesByTag(tag).FirstOrDefault();
    public static IList<T> FindComponents<T>() => Instance.FindComponents<T>();
    public static T? FindComponent<T>() => FindComponents<T>().FirstOrDefault();
    public static void Destroy(Entity entity) => Instance.Destroy(entity);
    public static void Pause()
    {
        Console.WriteLine("The game was paused. Press any button to resume . . .");
        Time._stopwatch.Stop();
        Console.ReadKey(true);
        Time._stopwatch.Start();
    }
    public static void BeginMainLoopThisIsBadNameFixMePlease()
    {
        _instance.BeginMainLoopThisIsBadNameFixMePlease();
    }
    public static void PlaySound(Sound sound) => Instance.PlaySound(sound);
    public static void PlaySound(string soundPath) => Instance.PlaySound(soundPath);
    public static void PlayMusic(string musicPath)
    {
        _instance.PlayMusic(musicPath);
    }

    public static IEnumerable<Entity> PointCast(Vector2 point)
    {
        return CollisionCaster.PointCast(point);
    }
}

public static class EnumerableExtensions
{
    public static void ForeachPermutePairs<T>(this IEnumerable<T> enumerable, Action<T, T> handler)
    {
        for (var i = 0; i < enumerable.Count() - 1; ++i)
        {
            for (var j = i + 1; j < enumerable.Count(); ++j)
            {
                handler.Invoke(enumerable.ElementAt(i), enumerable.ElementAt(j));
            }
        }
    }

    public static void ForeachInPairs<T>(this IEnumerable<T> enumerable, Action<T, T> handler)
    {
        if (enumerable is null)
        {
            throw new ArgumentNullException(nameof(enumerable));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var size = enumerable.Count();
        if (size < 2)
        {
            throw new ArgumentException("Cannot enumerate in pairs, collection has less than 2 elements", nameof(enumerable));
        }

        for (var i = 1; i < size; i++)
        {
            var first = enumerable.ElementAt(i - 1);
            var second = enumerable.ElementAt(i);
            handler.Invoke(first, second);
        }
    }
}
