using System.Collections;
using System.Threading.Channels;

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
    private readonly Time _time;
    public CollisionResolver _resolver = new();

    public Collisions(Time time)
    {
        _time = time;
    }

    public void Update()
    {
        var colliders = Juicebox.FindComponents<ICollider>();
        var hasCollisions = false;
        _resolver.Chaos = 0;
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
                    (RectangleCollider a, CircleCollider b) => CollisionDetector.AreColliding(a.Rectangle, b.Circle, out collisionData),
                    (CircleCollider a, RectangleCollider b) => CollisionDetector.AreColliding(b.Rectangle, a.Circle, out collisionData),
                    _ => false
                };

                if (hasCollision && float.IsNaN(collisionData.Center.X))
                {
                    Console.WriteLine("oh shii");
                }

                if (hasCollision)
                {
                    hasCollisions = true;
                    Action? resolver = (first, second) switch
                    {
                        (CircleCollider a, CircleCollider b) => () => _resolver.ResolveCollision(a, b, collisionData),
                        (RectangleCollider a, RectangleCollider b) => () => _resolver.ResolveCollision(a, b, collisionData),
                        (RectangleCollider a, CircleCollider b) => () => _resolver.ResolveCollision(a, b, collisionData),
                        (CircleCollider a, RectangleCollider b) => () => _resolver.ResolveCollision(b, a, collisionData),
                        _ => null
                    };
                    resolver?.Invoke();
                }

            });
            _resolver.Chaos += _time.Delta;
        }
        while (hasCollisions);

        foreach (var (body, directions) in _resolver._bounceOffDirections)
        {
            body.Velocity = Vector2.Average(directions).Normalized * body.Velocity.Length;
            if (float.IsNaN(body.Velocity.X))
            {
                body.Velocity = Vector2.Zero;
                Console.WriteLine("NaN speeds");
            }

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
    public float Chaos;
    public DictionaryList<Body, Vector2> _bounceOffDirections = new();

    private Vector2 ChaosVector => Vector2.Random() * Chaos * 0.01f;
    private float ChaosMultiplyer => Mathf.RandomFloat() * Chaos + 1;

    public void ResolveCollision(CircleCollider a, CircleCollider b, CollisionData collisionData)
    {
        var collisionCenter = collisionData.Center;
        var resolveOffset = a.Radius - a.Circle.Center.DistanceTo(collisionCenter);
        var aBody = a.Entity.GetComponent<Body>();
        var bBody = b.Entity.GetComponent<Body>();

        if (aBody is null && bBody is not null)
        {
            b.Entity.Position += collisionCenter.DirectionTo(b.Circle.Center) * resolveOffset * 2 * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, a.Circle.Center.DirectionTo(collisionCenter)).Normalized);
        }
        else if (bBody is null && aBody is not null)
        {
            a.Entity.Position += collisionCenter.DirectionTo(a.Circle.Center) * resolveOffset * 2 * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, b.Circle.Center.DirectionTo(collisionCenter)).Normalized);
        }
        else if (aBody is not null && bBody is not null)
        {
            a.Entity.Position += collisionCenter.DirectionTo(a.Circle.Center) * resolveOffset * ChaosMultiplyer + ChaosVector;
            b.Entity.Position += collisionCenter.DirectionTo(b.Circle.Center) * resolveOffset * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, b.Circle.Center.DirectionTo(collisionCenter)).Normalized);
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, a.Circle.Center.DirectionTo(collisionCenter)).Normalized);
        }
        else
        {
            Console.WriteLine("??????????");
        }
    }

    public void ResolveCollision(RectangleCollider rectangle, CircleCollider circle, CollisionData collision)
    {
        var rectangleBody = rectangle.Entity.GetComponent<Body>();
        var circleBody = circle.Entity.GetComponent<Body>();
        // Juicebox.Pause();

        // Would be easier to assume the other is stationary and only process `a`, then just iterate over all `a` that have body
        if (rectangleBody is null && circleBody is not null)
        {
            circle.Entity.Position += collision.Delta * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(circleBody, Vector2.Reflect(circleBody.Velocity, collision.Normal).Normalized);
        }
        else if (circleBody is null && rectangleBody is not null)
        {
            rectangle.Entity.Position -= collision.Delta * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(rectangleBody, Vector2.Reflect(rectangleBody.Velocity, collision.Normal).Normalized);
        }
        else if (rectangleBody is not null && circleBody is not null)
        {
            rectangle.Entity.Position -= collision.Delta * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(rectangleBody, Vector2.Reflect(rectangleBody.Velocity, -collision.Normal).Normalized);
            circle.Entity.Position += collision.Delta * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(circleBody, Vector2.Reflect(circleBody.Velocity, collision.Normal).Normalized);
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
            b.Entity.Position -= collision.Delta * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, -collision.Normal).Normalized);
        }
        else if (bBody is null && aBody is not null)
        {
            a.Entity.Position += collision.Delta * ChaosMultiplyer + ChaosVector;
            _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, collision.Normal).Normalized);
        }
        else if (aBody is not null && bBody is not null)
        {
            a.Entity.Position += collision.Delta * ChaosMultiplyer + ChaosVector;
            if (!aBody.Velocity.IsZero())
            {
                _bounceOffDirections.Add(aBody, Vector2.Reflect(aBody.Velocity, collision.Normal).Normalized);
            }

            b.Entity.Position -= collision.Delta * ChaosMultiplyer + ChaosVector;
            if (!bBody.Velocity.IsZero())
            {
                _bounceOffDirections.Add(bBody, Vector2.Reflect(bBody.Velocity, -collision.Normal).Normalized);
            }
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
        if (float.IsNaN(collisionData.Center.X))
        {
            Console.WriteLine("Well this sucks");
        }
        return true;
    }

    public static bool AreColliding(Rectangle rectangle, Circle circle, out CollisionData collisionData)
    {
        var closestX = circle.Center.X < rectangle.Left
            ? rectangle.Left
            : circle.Center.X > rectangle.Right
            ? rectangle.Right
            : circle.Center.X;

        var closestY = circle.Center.Y < rectangle.Top
            ? rectangle.Top
            : circle.Center.Y > rectangle.Bottom
            ? rectangle.Bottom
            : circle.Center.Y;

        var closestPoint = new Vector2(closestX, closestY);

        if (!circle.Contains(closestPoint))
        {
            collisionData = default;
            return false;
        }

        var closestEdgeX = circle.Center.X.DistanceTo(rectangle.Left) < circle.Center.X.DistanceTo(rectangle.Right)
            ? rectangle.Left
            : rectangle.Right;
        var closestEdgeY = circle.Center.Y.DistanceTo(rectangle.Top) < circle.Center.Y.DistanceTo(rectangle.Bottom)
            ? rectangle.Top
            : rectangle.Bottom;

        if (closestX != closestEdgeX && closestY != closestEdgeY)
        {
            if (closestX.DistanceTo(closestEdgeX) < closestY.DistanceTo(closestEdgeY))
            {
                closestX = closestEdgeX;
            }
            else
            {
                closestY = closestEdgeY;
            }
        }

        var closestEdgePoint = new Vector2(closestX, closestY);

        var delta = (closestEdgePoint.DirectionTo(circle.Center) * circle.Radius) - (circle.Center - closestEdgePoint);

        var deltaDirection = rectangle.Contains(circle.Center)
            ? -1
            : 1;

        delta *= deltaDirection;

        collisionData = new(
            Center: circle.Center - delta,
            Normal: delta.Normalized
        )
        {
            Delta = delta
        };

        return true;
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

        return true;
    }
}
