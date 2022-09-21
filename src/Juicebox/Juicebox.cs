using System.Diagnostics;
using static SDL2.SDL;

namespace JuiceboxEngine;

public class Camera
{
    public Vector2 Size { get; set; } = Vector2.Zero;
}

public struct Rectangle
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }

    public Vector2 TopLeft => Position;
    public Vector2 TopMiddle => Position + (Vector2.Right * (Size.X / 2));
    public Vector2 TopRight => Position + (Vector2.Right * Size.X);
    public Vector2 BottomLeft => Position + (Vector2.Down * Size.Y);
    public Vector2 BottomMiddle => Position + (Vector2.Down * Size.Y) + (Vector2.Right * (Size.X / 2));
    public Vector2 BottomRight => Position + Size;
    public Vector2 Center => Position + (Size / 2);

    public Rectangle(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public bool Contains(Vector2 point) => point.X >= TopLeft.X && point.X <= TopRight.X && point.Y >= TopLeft.Y && point.Y <= BottomLeft.Y;

    public static Rectangle operator +(Rectangle a, Vector2 b) => new(a.Position + b, a.Size);
    public static Rectangle operator -(Rectangle a, Vector2 b) => new(a.Position - b, a.Size);

    public override string ToString() => $"({Position.X};{Position.Y};{Size.X};{Size.Y})";
}

public struct Vector2
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
    public static Vector2 operator /(Vector2 a, int b) => new(a.X / b, a.Y / b);
    public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
    public static Vector2 operator *(float b, Vector2 a) => new(a.X * b, a.Y * b);
}

public class Entity
{
    private readonly List<IComponent> _components = new();

    public string Name { get; set; } = string.Empty;
    public Vector2 Position { get; set; } = new(0, 0);
    public double Rotation { get; set; }

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

public static class EntitySpriteExtensions
{
    public static Rectangle GetTargetRectangle(this Entity entity) =>
        entity.Sprite is null
            ? throw new InvalidOperationException($"Cannot {nameof(GetTargetRectangle)} on entity without a sprite.")
            : entity.Sprite.Rectangle - entity.Sprite.Center + entity.Position;
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
    public record struct OnPressBuilder(Entity Entity);
    public static OnPressBuilder OnPress(this Entity entity) => new(entity);
    public delegate void OnPressHandler(Entity Entity);
    public static Entity Do(this OnPressBuilder builder, Action<OnPressEntity> handler)
    {
        Juicebox._instance._events._entityHandlers.AddHandler(builder.Entity, handler);
        return builder.Entity;
    }

    public record struct OnEachFrameBuilder(Entity Entity);
    public static OnEachFrameBuilder OnEachFrame(this Entity entity) => new(entity);
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

public class Sprites
{
    public readonly Dictionary<string, Sprite> _sprites = new();
    public readonly Dictionary<Sprite, Action<Sprite>> _spriteConfigurations = new();

    public void OnStart()
    {
        Juicebox._instance._events._handlers.AddHandler<OnPress>(OnPress);
    }

    private void OnPress(OnPress e)
    {
        foreach (var entity in Juicebox._instance._entities.Values)
        {
            if (entity.Sprite is not null && entity.GetTargetRectangle().Contains(e.MousePosition))
            {
                Juicebox._instance.Send(entity, new OnPressEntity(entity, e.MousePosition));
            }
        }
    }
}

public class Sprite
{
    public string Path { get; }
    public Vector2 Center { get; set; } = Vector2.Zero;
    public Rectangle Rectangle;

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

    public static Entity WithSprite(this Entity entity, string path, Action<Sprite> configureSprite)
    {
        if (configureSprite is null)
        {
            throw new ArgumentNullException(nameof(configureSprite));
        }

        entity.WithSprite(path);
        Juicebox._instance._sprites._spriteConfigurations[entity.Sprite] = configureSprite;
        return entity;
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

public class JuiceboxInput
{
    public Vector2 Joystick = Vector2.Zero;
    public Vector2 Pointer = Vector2.Zero;
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
            (Pointer.X, Pointer.Y) = @event switch
            {
                { type: SDL_EventType.SDL_MOUSEMOTION } => (@event.motion.x, @event.motion.y),
                _ => (Pointer.X, Pointer.Y),
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

public class JuiceboxEvents
{
    public List<JuiceboxEventHanlder> OnEachFrameEventHandlers { get; } = new();
    public List<JuiceboxEventHanlder> OnHitEventHandlers { get; } = new();
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

    public float Delta => _delta;
    public float Current => _stopwatch.ElapsedMilliseconds / 1000f;

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

    public readonly List<Text> _texts = new();
    internal readonly JuiceboxInput _input = new();
    public readonly JuiceboxEvents _events = new();
    public readonly Time _time = new();
    public readonly Physics _physics = new();
    public readonly Gizmos _gizmos = new();
    public readonly Camera _camera = new();
    public readonly Sprites _sprites = new();

    public Entity NewEntity(string name) => _entities[name] = new(name);
    public Sprite GetSprite(string path) => _sprites._sprites.TryGetValue(path, out var sprite) ? sprite : (_sprites._sprites[path] = new(path));
    public Text NewText(string text, string font)
    {
        var textComponent = new Text { Value = text, Font = font };
        _texts.Add(textComponent);

        return textComponent;
    }

    public void Send<T>(T @event) => _events.Send(@event);
    public void Send<T>(Entity entity, T @event) => _events.Send(entity, @event);
}

public static class Juicebox
{
    public static readonly JuiceboxInstance _instance = new();

    public static JuiceboxInput Input => _instance._input;
    public static Time Time => _instance._time;
    public static Physics Physics => _instance._physics;
    public static Gizmos Gizmos => _instance._gizmos;
    public static Camera Camera => _instance._camera;

    public static Entity NewEntity(string name) => _instance.NewEntity(name);
    public static Sprite GetSprite(string path) => _instance.GetSprite(path);
    public static void DrawCircle(Vector2 center, float radius, Color color) => Gizmos.Circles[new Circle(center, radius)] = new Gizmos.DrawData(0.0001f, color);
    public static void DrawLine(Vector2 start, Vector2 end, Color color) => Gizmos.Lines[new Line(start, end)] = new Gizmos.DrawData(0.0001f, color);
    public static void DrawRectangle(Rectangle rectangle, Color color)
    {
        Gizmos.Lines[new Line(rectangle.TopLeft, rectangle.TopRight)] = new Gizmos.DrawData(0.0001f, color);
        Gizmos.Lines[new Line(rectangle.TopRight, rectangle.BottomRight)] = new Gizmos.DrawData(0.0001f, color);
        Gizmos.Lines[new Line(rectangle.BottomRight, rectangle.BottomLeft)] = new Gizmos.DrawData(0.0001f, color);
        Gizmos.Lines[new Line(rectangle.BottomLeft, rectangle.TopLeft)] = new Gizmos.DrawData(0.0001f, color);
    }
    public static void Send<T>(T @event) => _instance.Send(@event);
    public static void Send<T>(Entity entity, T @event) => _instance.Send(entity, @event);
}

