using static SDL2.SDL;

namespace JuiceboxEngine;

public class Vector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, int b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(int b, Vector2 a) => new(a.X * b, a.Y * b);
}

public class Entity
{
    public string Name { get; set; } = string.Empty;
    public Vector2 Position { get; set; } = new(0, 0);

    public Sprite? Sprite { get; set; }

    public Entity(string name)
    {
        Name = name;
    }
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

public class JuiceboxInstance
{
    public readonly Dictionary<string, Entity> _entities = new();
    public readonly Dictionary<string, Sprite> _sprites = new();
    internal readonly JuiceboxInput _input = new();
    public readonly JuiceboxEvents _events = new();

    public Entity NewEntity(string name) => _entities[name] = new(name);
    public Sprite GetSprite(string path) => _sprites.TryGetValue(path, out var sprite) ? sprite : (_sprites[path] = new(path));
}

public static class Juicebox
{
    public static readonly JuiceboxInstance _instance = new();

    public static JuiceboxInput Input => _instance._input;

    public static Entity NewEntity(string name) => _instance.NewEntity(name);
    public static Sprite GetSprite(string path) => _instance.GetSprite(path);
}

