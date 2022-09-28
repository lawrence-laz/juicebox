using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;
using static SDL2.SDL_gfx;
using JuiceboxEngine;
using SDL2;
using JuiceboxEngine.Aseprite;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddJuicebox()
    .BuildServiceProvider();

var juiceBox = services.GetRequiredService<JuiceboxInstance>();
juiceBox.Start();

// Juicebox.PlayMusic("./resources/music.wav");

var createBall = () =>
{
    return Juicebox.NewEntity($"ball {Guid.NewGuid()}")
        .WithLocalScale(0.2f)
        .WithSprite("./resources/ball.png", sprite =>
        {
            // Console.WriteLine("Configuring!");
            sprite.Center = sprite.FullRectangle.Center;
            // Console.WriteLine($"Sprite.center={sprite.Center}, sprite.FullRectangle.center={sprite.FullRectangle.Center}");
        })
        .WithCircleCollider()
        .WithBody()
        // .OnHit(other => other.Name == "bottom-bar").Do((bar, ball, hit) => Console.WriteLine($"{ball.Name} hit {bar.Name}"))
        // .OnHit(other => other.Tags.Contains("first-ball")).Do((firstBall, ball, hit) => Console.WriteLine($"{ball.Name} hit {firstBall.Name}"))
        .OnHit().Do((other, ball, hit) => Juicebox.PlaySound("./resources/metal-hit.wav"))
        ;
};

var timer = Juicebox.NewEntity("timer")
    .WithPosition(new(0, 0))
    .WithText("0 fwefwefwefwef", font: "./resources/airstrike.ttf", out var timerText)
    .OnEachFrame().Do(entity => timerText.Value = TimeSpan.FromSeconds(Juicebox.Time.Current).ToString())
    ;

timerText.Order = 100;

var ball = Juicebox.NewEntity("ball")
    .WithTags("first-ball")
    .WithSprite("./resources/ball.png", sprite => sprite.Center = sprite.FullRectangle.Center)
    .WithCircleCollider()
    // .WithRectangleCollider()
    .OnEachFrame().Do(entity =>
    {
        if (Juicebox.Input.IsDown(MouseButton.Left) || Juicebox.Input.IsDown(KeyboardButton.Space) || Juicebox.Input.IsUp(KeyboardButton.Space))
        {
            // entity.Position = Juicebox.Input.PointerWorld;
            var newBall = createBall();
            newBall.Transform.Position = Juicebox.Input.PointerWorld;
        }
    })
    // .OnEachFrame().Do(entity => entity.Position = Juicebox.Input.Pointer)
    // .OnEachFrame().Do(entity => entity.Position += speed * Juicebox.Input.Joystick * Juicebox.Time.Delta)
    // .OnEachFrame().Do(entity => Juicebox.DrawCircle(entity.Position, 50, Color.Green))
    .OnEachFrame().Do(entity => Juicebox.DrawLine(Vector2.Zero, entity.Position, Color.Blue))
    .OnPress().Do(entity => Console.WriteLine("Stop pressing me"))
    .OnEachFrame().Do(_ =>
    {
        if (Juicebox.Input.IsDown(MouseButton.Left))
        {
            var entities = Juicebox.PointCast(Juicebox.Input.PointerWorld);
            foreach (var entity in entities)
            {
                Console.WriteLine($"Pressed '{entity.Name}'");
            }
        }
    })
    .OnEachFrame().Do(entity => entity.RotationDegrees += Juicebox.Input.IsPressed(KeyboardButton.Q) ? -2 : Juicebox.Input.IsPressed(KeyboardButton.E) ? 2 : 0)
    .WithBody()
    ;

var bar = Juicebox.NewEntity("bottom-bar")
    .WithLocalScale(0.8f)
    .WithSprite("./resources/blue-tile.png")
    .WithRectangleCollider();
bar.Transform.Position = new(0, 200);

// var bar2 = Juicebox.NewEntity("bottom-bar2")
//     .WithSprite("./resources/blue-tile.png")
//     .WithCircleCollider(collider => collider.Radius = 100);
// bar2.Transform.Position = new(-50, 300);

var powerUp = Juicebox.NewEntity("powerup")
    .WithPosition(500, 0)
    .WithAnimation("./resources/power-up-100.json");

Juicebox.Camera.Entity
    .OnEachFrame().Do(camera => camera.Position += Juicebox.Input.Joystick * 10);
// .OnEachFrame().Do(ball => ball.Position += ball.Movement.Speed * ball.Movement.Direction)
// .OnHit(other => other.Name == "ground").Do(() => Juicebox.Restart())
// .OnHit(other => other.Tags.Contains("bouncy")).Do((bouncy, ball, hit) => ball.Movement.Direction.BounceOff(bouncy.Position));
//

// var star = Juicebox.NewEntity("star")
//     .WithSprite("./resources/star.png", sprite => sprite.Center = Vector2.Zero)
//     .WithParent(ball.Transform)
//     .OnEachFrame().Do(entity => entity.RotationDegrees += Juicebox.Input.IsPressed(KeyboardButton.A) ? -2 : Juicebox.Input.IsPressed(KeyboardButton.D) ? 2 : 0)
//     ;
// star.Transform.LocalPosition += Vector2.Right * ball.Sprite!.FullRectangle.Size.X / 2;
// // star.RotationDegrees = 00;

// var heart = Juicebox.NewEntity("heart")
//     .WithSprite("./resources/heart.png")
//     .WithParent(star)
//     ;
// heart.Transform.LocalPosition += Vector2.Right * star.Sprite!.FullRectangle.Size.X / 2;
//
// heart.Transform.LocalPosition += Vector2.Right * 50;

var destroyer = Juicebox.NewEntity("destroyer")
.OnEachFrame().Do(entity =>
{
    if (Juicebox.Input.IsDown(KeyboardButton.X))
    {
        // Juicebox.FindEntityByName("title").Destroy();
        // Juicebox.FindComponent<Text>().Entity.Destroy();
        Juicebox.FindEntityByTag("gui").Destroy();
    }
});

var title = Juicebox.NewEntity("title")
    .WithParent(ball)
    .WithTags("title-screen", "gui")
    .WithText("Breakout", font: "./resources/airstrike.ttf");

Juicebox.BeginMainLoopThisIsBadNameFixMePlease();


