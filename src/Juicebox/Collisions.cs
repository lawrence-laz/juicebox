using System.Collections;
using System.Runtime.Intrinsics.Arm;

namespace JuiceboxEngine;

public interface ICollider : IComponent
{

}

public static class CircleColliderEntityExtensions
{
    public static Entity WithCircleCollider(this Entity entity)
    {
        var collider = new CircleCollider(entity.Transform.Center, entity.GetTargetRectangle().Size.X / 2, entity);
        return entity.WithComponent(collider);
    }
    public static Entity WithCircleCollider(this Entity entity, Action<CircleCollider> configureCollider)
    {
        var collider = new CircleCollider(entity.Transform.Center, entity.GetTargetRectangle().Size.X / 2, entity);
        configureCollider.Invoke(collider);
        return entity.WithComponent(collider);
    }
}

public static class RectangleColliderEntityExtensions
{
    public static Entity WithRectangleCollider(this Entity entity)
    {
        var collider = new RectangleCollider(Vector2.Zero, entity.GetTargetRectangle().Size, entity);
        return entity.WithComponent(collider);
    }
}

public class RectangleCollider : ICollider
{
    public Vector2 LocalPosition;
    public Vector2 Size;

    public Entity Entity { get; init; }
    public Rectangle Rectangle => new Rectangle(Entity.Position - Entity.Transform.Center, Size);

    public RectangleCollider(Vector2 localPosition, Vector2 size, Entity entity)
    {
        LocalPosition = localPosition;
        Size = size;
        Entity = entity;
    }
}

public class CircleCollider : ICollider
{
    public Vector2 LocalCenter;
    public float Radius;

    public Entity Entity { get; init; }
    public Circle Circle => Entity.Transform.GetLocalToWorldMatrix() * new Circle(LocalCenter, Radius);

    public CircleCollider(Vector2 localCenter, float radius, Entity entity)
    {
        LocalCenter = localCenter;
        Radius = radius;
        Entity = entity;
    }
}

public record struct CollisionData(Vector2 Center, Vector2 Normal)
{
    public Vector2 Delta = Vector2.Zero;
};

public class Collisions
{
    public CollisionResolver _resolver = new();

    public void OnUpdate()
    {
        var colliders = Juicebox.FindComponents<ICollider>();
        var hasCollisions = false;
        do
        {
            hasCollisions = false;
            colliders.ForeachPermutePairs((first, second) =>
            {
                if (first.Entity.GetComponent<Body>() is null && second.Entity.GetComponent<Body>() is null)
                {
                    return; // Colliders without bodies do not collide.
                }

                var collisionData = new CollisionData();
                var hasCollision = (first, second) switch
                {
                    (CircleCollider a, CircleCollider b) => CollisionDetector.AreColliding(a.Circle, b.Circle, out collisionData),
                    (RectangleCollider a, RectangleCollider b) => CollisionDetector.AreColliding(a.Rectangle, b.Rectangle, out collisionData),
                    _ => false
                };
                if (hasCollision)
                {
                    hasCollisions = true;
                    Action? resolver = (first, second) switch
                    {
                        (CircleCollider a, CircleCollider b) => () => _resolver.ResolveCollision(a, b, collisionData),
                        (RectangleCollider a, RectangleCollider b) => () => _resolver.ResolveCollision(a, b, collisionData),
                        _ => null
                    };
                    resolver?.Invoke();
                }
            });
        }
        while (hasCollisions);

        foreach (var (body, directions) in _resolver._bounceOffDirections)
        {
            body.Velocity = Vector2.Average(directions).Normalized * body.Velocity.Length;
        }
        _resolver._bounceOffDirections.Clear();
    }

}

public class DictionaryList<TKey, TValue> : IEnumerable<(TKey Key, IEnumerable<TValue> Values)>
where TKey : notnull
{
    public Dictionary<TKey, List<TValue>> _dictionary = new();

    public IEnumerable<TValue> Get(TKey key) => _dictionary.TryGetValue(key, out var values) ? values : Enumerable.Empty<TValue>();
    public void Add(TKey key, TValue value)
    {
        if (!_dictionary.TryGetValue(key, out var list))
        {
            list = new List<TValue>();
            _dictionary[key] = list;
        }
        list.Add(value);
    }
    public void Clear() => _dictionary.Clear();

    public IEnumerator<(TKey Key, IEnumerable<TValue> Values)> GetEnumerator() => _dictionary.Select(pair => (pair.Key, pair.Value.AsEnumerable())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CollisionResolver
{
    public DictionaryList<Body, Vector2> _bounceOffDirections = new();

    public void ResolveCollision(CircleCollider a, CircleCollider b, CollisionData collisionData)
    {
        var collisionCenter = collisionData.Center;
        var resolveOffset = a.Radius - a.Circle.Center.DistanceTo(collisionCenter);
        var aBody = a.Entity.GetComponent<Body>();
        var bBody = b.Entity.GetComponent<Body>();
        System.Console.WriteLine($"Resolve offset {resolveOffset}");
        // Juicebox.Pause();

        if (aBody is null && bBody is not null)
        {
            b.Entity.Position += collisionCenter.DirectionTo(b.Circle.Center) * resolveOffset * 2;
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, a.Circle.Center.DirectionTo(collisionCenter)).Normalized);
        }
        else if (bBody is null && aBody is not null)
        {
            a.Entity.Position += collisionCenter.DirectionTo(a.Circle.Center) * resolveOffset * 2;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, b.Circle.Center.DirectionTo(collisionCenter)).Normalized);
        }
        else if (aBody is not null && bBody is not null)
        {
            a.Entity.Position += collisionCenter.DirectionTo(a.Circle.Center) * resolveOffset;
            b.Entity.Position += collisionCenter.DirectionTo(b.Circle.Center) * resolveOffset;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, b.Circle.Center.DirectionTo(collisionCenter)).Normalized);
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, a.Circle.Center.DirectionTo(collisionCenter)).Normalized);
        }
        else
        {
            Console.WriteLine("??????????");
        }
    }

    public void ResolveCollision(RectangleCollider a, RectangleCollider b, CollisionData collision)
    {
        var aBody = a.Entity.GetComponent<Body>();
        var bBody = b.Entity.GetComponent<Body>();
        // Juicebox.Pause();

        // Would be easier to assume the other is stationary and only process `a`, then just iterate over all `a` that have body
        if (aBody is null && bBody is not null)
        {
            b.Entity.Position -= collision.Delta;
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, -collision.Normal).Normalized);
        }
        else if (bBody is null && aBody is not null)
        {
            a.Entity.Position += collision.Delta;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, collision.Normal).Normalized);
        }
        else if (aBody is not null && bBody is not null)
        {
            a.Entity.Position += collision.Delta;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, collision.Normal).Normalized);
            b.Entity.Position -= collision.Delta;
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, -collision.Normal).Normalized);
        }
        else
        {
            Console.WriteLine("??????????");
        }
    }
}

public static class CollisionDetector
{
    public static bool AreColliding(Circle a, Circle b, out CollisionData collisionData)
    {
        var overlapLength = a.Radius + b.Radius - a.Center.DistanceTo(b.Center);
        if (overlapLength <= 0.001)
        {
            collisionData = default;
            return false;
        }

        collisionData = new(a.Center + (a.Center.DirectionTo(b.Center) * (a.Radius - (overlapLength / 2))), Vector2.Zero);
        return true;
    }

    public static bool AreColliding(Rectangle a, Circle b, out CollisionData collisionData)
    {
        collisionData = default;
        return false;
    }

    public static bool AreColliding(Rectangle a, Rectangle b, out CollisionData collisionData)
    {
        collisionData = default;
        var dx = a.Center.X - b.Center.X;
        var px = (a.Size.X / 2) + (b.Size.X / 2) - dx.Abs();
        if (px <= 0)
        {
            return false;
        }

        var dy = a.Center.Y - b.Center.Y;
        var py = (a.Size.Y / 2) + (b.Size.Y / 2) - dy.Abs();
        if (py <= 0)
        {
            return false;
        }

        if (px < py)
        {
            var sx = dx.Sign();
            collisionData = new(
                Center: new(b.Center.X + (b.Size.X / 2 * sx), a.Center.Y),
                Normal: new(sx, 0)
            );
            collisionData.Delta.X = px * sx;
        }
        else
        {
            var sy = dy.Sign();
            collisionData = new(
                Center: new(a.Center.X, b.Center.Y + (b.Size.Y / 2 * sy)),
                Normal: new(0, sy)
            );
            collisionData.Delta.Y = py * sy;
        }

        // if (a.Bottom <= b.Top || a.Top >= b.Bottom || a.Right <= b.Left || a.Left >= b.Right)
        // {
        //     collisionCenter = Vector2.Zero;
        //     return false;
        // }
        // var collisionCenterX = a.Left.DistanceTo(b.Right) < a.Right.DistanceTo(b.Left);

        return true;
    }
}
