using System.Diagnostics;
using System.Numerics;
using static SDL2.SDL;

namespace JuiceboxEngine;

public class Vector2
{
    public static Vector2 Right => new(1, 0);
    public static Vector2 Left => new(-1, 0);
    public static Vector2 Up => new(0, -1);
    public static Vector2 Down => new(0, 1);
    public static Vector2 Zero => new(0, 0);

    public float X { get; set; }
    public float Y { get; set; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X};{Y})";

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, int b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(int b, Vector2 a) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(float b, Vector2 a) => new(a.X * b, a.Y * b);
}

public class Entity
{
    private readonly List<IComponent> _components = new();

    public string Name { get; set; } = string.Empty;
    public Vector2 Position { get; set; } = new(0, 0);

    public Sprite? Sprite { get; set; }
    public IEnumerable<IComponent> Components => _components;

    public Entity(string name)
    {
        Name = name;
    }

    public Entity WithComponent(IComponent component)
    {
        _components.Add(component);
        return this;
    }

    public T? GetComponent<T>() => _components.OfType<T>().FirstOrDefault();
}

public static class BodyEntityExtensions
{
    public static Entity WithBody(this Entity entity) => entity.WithComponent(Juicebox.Physics.NewBody(entity));
}

public static class TextEntityExtensions
{
    public static Entity WithText(this Entity entity, string text, string font) => entity.WithComponent(Juicebox._instance.NewText(text, font));
}

public static class EventEntityExtensions
{
    public static OnEachFrameBuilder OnEachFrame(this Entity entity) => new(entity);
    public record struct OnEachFrameBuilder(Entity Entity);
    public delegate void OnEachFrameHandler(Entity entity);
    public static Entity Do(this OnEachFrameBuilder builder, OnEachFrameHandler handler)
    {
        Juicebox._instance._events.OnEachFrameEventHandlers.Add(new JuiceboxEventHanlder(builder.Entity, null, handler));
        return builder.Entity;
    }

    public static OnHitOtherConditionBuilder OnHit(this Entity entity) => new(entity, null);
    public delegate bool OnHitOtherCondition(Entity other);
    public record struct OnHitOtherConditionBuilder(Entity Entity, OnHitOtherCondition? Condition);
    public static OnHitOtherConditionBuilder OnHit(this Entity entity, OnHitOtherCondition condition) => new(entity, condition);
    public delegate void OnHitHandler();
    public static Entity Do(this OnHitOtherConditionBuilder builder, OnHitHandler handler)
    {
        Juicebox._instance._events.OnHitEventHandlers.Add(new JuiceboxEventHanlder(builder.Entity, builder.Condition, handler));
        return builder.Entity;
    }
    public delegate void OnHitThisAndOtherHandler(Entity @this, Entity other);
    public static Entity Do(this OnHitOtherConditionBuilder builder, OnHitThisAndOtherHandler handler)
    {
        Juicebox._instance._events.OnHitEventHandlers.Add(new JuiceboxEventHanlder(builder.Entity, builder.Condition, handler));
        return builder.Entity;
    }
}

public class Sprite
{
    public string Path { get; }

    public Sprite(string path)
    {
        Path = path;
    }
}

public interface IComponent
{
}

public class Text : IComponent
{
    public string Value { get; set; } = string.Empty;
    public string Font { get; set; } = string.Empty;
}

public static class SpriteEntityExtensions
{
    public static Entity WithSprite(this Entity entity, string path)
    {
        entity.Sprite = Juicebox.GetSprite(path);
        return entity;
    }
}

public class JuiceboxInput
{
    public Vector2 Joystick { get; } = new(0, 0);

    public void AcceptEvent(SDL_Event @event)
    {
        if (@event.key.repeat == 1)
        {
            return;
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

public delegate bool OnHitCondition(Entity other);

public record JuiceboxEventHanlder(Entity Entity, object? Condition, object Handler);

public class JuiceboxEvents
{
    public List<JuiceboxEventHanlder> OnEachFrameEventHandlers { get; } = new();
    public List<JuiceboxEventHanlder> OnHitEventHandlers { get; } = new();
}

public class Time
{
    public Stopwatch _stopwatch;
    public float _delta;

    public float Delta => _delta;

    public void Start()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void OnUpdate()
    {
        _delta = _stopwatch.ElapsedMilliseconds / 1000f;
        _stopwatch.Restart();
    }
}

public class Body : IComponent
{
    public Vector2 Velocity { get; set; } = Vector2.Zero;
}

public class Physics
{
    public List<(Entity Entity, Body Body)> Bodies = new();

    public Vector2 Gravity = Vector2.Down * 50;

    public Body NewBody(Entity entity) => Bodies.AddAndReturnSelf((Entity: entity, Body: new())).Body;

    public void OnUpdate(float delta)
    {
        foreach (var (Entity, Body) in Bodies)
        {
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

public record Circle(Vector2 Center, float Radius);
public record Line(Vector2 Start, Vector2 End);

public class Gizmos
{
    public class DrawData
    {
        public float Duration;
        public Color Color = Color.Green;

        public DrawData(float duration, Color color)
        {
            Duration = duration;
            Color = color;
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
    public readonly Dictionary<string, Entity> _entities = new();
    public readonly Dictionary<string, Sprite> _sprites = new();
    public readonly List<Text> _texts = new();
    internal readonly JuiceboxInput _input = new();
    public readonly JuiceboxEvents _events = new();
    public readonly Time _time = new();
    public readonly Physics _physics = new();
    public readonly Gizmos _gizmos = new();

    public Entity NewEntity(string name) => _entities[name] = new(name);
    public Sprite GetSprite(string path) => _sprites.TryGetValue(path, out var sprite) ? sprite : (_sprites[path] = new(path));
    public Text NewText(string text, string font)
    {
        var textComponent = new Text { Value = text, Font = font };
        _texts.Add(textComponent);

        return textComponent;
    }
}

public static class Juicebox
{
    public static readonly JuiceboxInstance _instance = new();

    public static JuiceboxInput Input => _instance._input;
    public static Time Time => _instance._time;
    public static Physics Physics => _instance._physics;
    public static Gizmos Gizmos => _instance._gizmos;

    public static Entity NewEntity(string name) => _instance.NewEntity(name);
    public static Sprite GetSprite(string path) => _instance.GetSprite(path);
    public static void DrawCircle(Vector2 center, float radius, Color color) => Gizmos.Circles[new Circle(center, radius)] = new Gizmos.DrawData(0.0001f, color);
    public static void DrawLine(Vector2 start, Vector2 end, Color color) => Gizmos.Lines[new Line(start, end)] = new Gizmos.DrawData(0.0001f, color);
}

