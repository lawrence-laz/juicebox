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
        var collider = new RectangleCollider(entity.Transform.Center, entity.GetTargetRectangle().Size, entity);
        return entity.WithComponent(collider);
    }
}

public class RectangleCollider : ICollider
{
    public Vector2 LocalPosition;
    public Vector2 Size;

    public Entity Entity { get; init; }
    public Rectangle Rectangle => Entity.Transform.GetLocalToWorldMatrix() * new Rectangle(LocalPosition, Size);

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

public record struct CollisionData(ICollider First, ICollider Second, Vector2 Center);

public class Collisions
{
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

                var collisionCenter = Vector2.Zero;
                var hasCollision = (first, second) switch
                {
                    (CircleCollider a, CircleCollider b) => CollisionDetector.AreColliding(a.Circle, b.Circle, out collisionCenter),
                    _ => false
                };
                if (hasCollision)
                {
                    hasCollisions = true;
                    Action? resolver = (first, second) switch
                    {
                        (CircleCollider a, CircleCollider b) => () => CollisionResolver.ResolveCollision(a, b, collisionCenter),
                        _ => null
                    };
                    resolver?.Invoke();
                }
            });
        }
        while (hasCollisions);
    }

}

public class CollisionResolver
{
    public static void ResolveCollision(CircleCollider a, CircleCollider b, Vector2 collisionCenter)
    {
        var resolveOffset = a.Radius - a.Circle.Center.DistanceTo(collisionCenter);
        var aBody = a.Entity.GetComponent<Body>();
        var bBody = b.Entity.GetComponent<Body>();
        System.Console.WriteLine($"Resolve offset {resolveOffset}");
        // Juicebox.Pause();

        if (aBody is null && bBody is not null)
        {
            b.Entity.Position += collisionCenter.DirectionTo(b.Circle.Center) * resolveOffset * 2;
            bBody.Velocity = Vector2.Reflect(bBody.Velocity, a.Circle.Center.DirectionTo(collisionCenter));
        }
        else if (bBody is null && aBody is not null)
        {
            a.Entity.Position += collisionCenter.DirectionTo(a.Circle.Center) * resolveOffset * 2;
            aBody.Velocity = Vector2.Reflect(aBody.Velocity, b.Circle.Center.DirectionTo(collisionCenter));
        }
        else if (aBody is not null && bBody is not null)
        {
            a.Entity.Position += collisionCenter.DirectionTo(a.Circle.Center) * resolveOffset;
            b.Entity.Position += collisionCenter.DirectionTo(b.Circle.Center) * resolveOffset;
            bBody.Velocity = Vector2.Reflect(bBody.Velocity, a.Circle.Center.DirectionTo(collisionCenter));
            aBody.Velocity = Vector2.Reflect(aBody.Velocity, b.Circle.Center.DirectionTo(collisionCenter));
        }
        else
        {
            System.Console.WriteLine("??????????");
        }
    }
}

public static class CollisionDetector
{
    public static bool AreColliding(Circle a, Circle b, out Vector2 collisionCenter)
    {
        var overlapLength = a.Radius + b.Radius - a.Center.DistanceTo(b.Center);
        if (overlapLength <= 0)
        {
            collisionCenter = default;
            return false;
        }

        collisionCenter = a.Center + (a.Center.DirectionTo(b.Center) * (a.Radius - (overlapLength / 2)));
        return true;
    }
}
