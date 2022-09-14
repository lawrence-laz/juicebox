```csharp
var ball = Juicebox.NewEntity("ball")
    .WithSprite("resources/sprites/ball.png")
    .OnEachFrame().Do(ball => ball.Position += ball.Movement.Speed * ball.Movement.Direction)
    .OnHit(other => other.Name == "ground").Do(() => Juicebox.Restart())
    .OnHit(other => other.Tags.Contains("bouncy")).Do((bouncy, ball, hit) => ball.Movement.Direction.BounceOff(bouncy.Position));

var paddle = Juicebox.NewEntity("paddle")
    .WithTag("bouncy")
    .WithSprite("resources/sprites/paddle.png")
    .OnEachFrame().Do(paddle => paddle.position += paddle.movement.speed * Input.Horizontal)
    .OnHit(other => other.Name == "ball").Do(() => Juicebox.PlaySound("resources/sounds/paddle-hit.wav"));

var leftWall = Juicebox.NewEntity("wall")
    .WithTag("bouncy")
    .WithSprite("resources/sprites/wall.png")
    .OnHit(other => other.Name == "ball").Do(() => Juicebox.PlaySound("resources/sounds/wall-hit.wav"));

var rightWall = leftWall.CopyEntity("wall");

var ceiling = leftWall.CopyEntity("ceiling");

var tile = Juicebox.NewEntity("tile")
    .WithTag("bouncy")
    .WithSprite("resources/sprites/tile.png")
    .OnHit(other => other.Name == "ball").Do((ball, hit, tile) => {
        Juicebox.PlaySound("resources/sounds/tile-hit.wav");
        tile.Destroy();
    });

for (var x = 0; x < 14; ++x) 
{
    for (var y = 0; y < 8; ++y)
    {
        tile.CopyEntity()
            .WithPosition(x, y);
    }
}

Juicebox.Start();
```

- Changes to resources (sprites, sounds, fonts) are immediatelly reflected in game
- All entities are also prototypes and can be cloned
- There should be a way to specify multiple handlers with filter parameter.
    - For example: .OnHit()

