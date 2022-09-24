using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;
using static SDL2.SDL_gfx;
using JuiceboxEngine;
using SDL2;

SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "1");

if (SDL_Init(SDL_INIT_EVERYTHING) != 0)
{
    Console.WriteLine("Failed to initialize SDL: " + SDL_GetError());
}

if (TTF_Init() != 0)
{
    Console.WriteLine($"Error initializing TTF: {SDL_GetError()}");
}

var window = SDL_CreateWindow("Breakout by Juicebox", 0, 0, 512, 256, SDL_WindowFlags.SDL_WINDOW_OPENGL);
var renderer = SDL_CreateRenderer(window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

Juicebox.Gizmos.RenderCircle = (circle, color) =>
{
    var targetPosition = circle.Center;
    SDL_gfx.aacircleRGBA(renderer, (short)targetPosition.X, (short)targetPosition.Y, (short)circle.Radius, color.R, color.G, color.B, color.A);
};

Juicebox.Gizmos.RenderLine = (line, color) =>
{
    var start = line.Start;
    var end = line.End;
    SDL_gfx.aalineRGBA(renderer, (short)start.X, (short)start.Y, (short)end.X, (short)end.Y, color.R, color.G, color.B, color.A);
};

var surface = IMG_Load("./resources/01-Breakout-Tiles.png");
var texture = SDL_CreateTextureFromSurface(renderer, surface);
SDL_FreeSurface(surface);

// Set up sprite loading
var sprites = new Dictionary<Sprite, (IntPtr Texture, SDL_Rect TargetRect)>();
Juicebox._instance.OnLoadSprite = sprite =>
{
    var spriteSurface = IMG_Load(sprite.Path);
    var spriteTexture = SDL_CreateTextureFromSurface(renderer, spriteSurface);
    SDL_QueryTexture(spriteTexture, out var spriteFormat, out var spriteAccess, out var spriteWidth, out var spriteHeight);
    sprite.Rectangle.Size = new(spriteWidth, spriteHeight);
    var spriteTargetRect = new SDL_Rect { x = 0, y = 0, w = spriteWidth, h = spriteHeight };
    sprites[sprite] = (spriteTexture, spriteTargetRect);
    SDL_FreeSurface(spriteSurface);
};

// ----------------------------------------------------------------------------
// GAME
// ----------------------------------------------------------------------------
var ball = Juicebox.NewEntity("ball")
    .WithSprite("./resources/ball.png", sprite => sprite.Center = sprite.Rectangle.Center)
    .OnEachFrame().Do(entity =>
    {
        if (Juicebox.Input.IsDown(MouseButton.Left) || Juicebox.Input.IsDown(KeyboardButton.Space) || Juicebox.Input.IsUp(KeyboardButton.Space))
        {
            entity.Position = Juicebox.Input.PointerWorld;
        }
    })
    // .OnEachFrame().Do(entity => entity.Position = Juicebox.Input.Pointer)
    // .OnEachFrame().Do(entity => entity.Position += speed * Juicebox.Input.Joystick * Juicebox.Time.Delta)
    .OnEachFrame().Do(entity => Juicebox.DrawCircle(entity.Position, 50, Color.Green))
    .OnEachFrame().Do(entity => Juicebox.DrawLine(Vector2.Zero, entity.Position, Color.Blue))
    .OnPress().Do(entity => Console.WriteLine("Stop pressing me"))
    .OnEachFrame().Do(entity => entity.RotationDegrees += Juicebox.Input.IsPressed(KeyboardButton.Q) ? -2 : Juicebox.Input.IsPressed(KeyboardButton.E) ? 2 : 0)
    // .WithBody()
    ;

Juicebox.Camera.Entity
    .OnEachFrame().Do(camera => camera.Position += Juicebox.Input.Joystick * 10);
// .OnEachFrame().Do(ball => ball.Position += ball.Movement.Speed * ball.Movement.Direction)
// .OnHit(other => other.Name == "ground").Do(() => Juicebox.Restart())
// .OnHit(other => other.Tags.Contains("bouncy")).Do((bouncy, ball, hit) => ball.Movement.Direction.BounceOff(bouncy.Position));
//
var star = Juicebox.NewEntity("star")
    .WithSprite("./resources/star.png", sprite => sprite.Center = Vector2.Zero)
    .WithParent(ball.Transform)
    .OnEachFrame().Do(entity => entity.RotationDegrees += Juicebox.Input.IsPressed(KeyboardButton.A) ? -2 : Juicebox.Input.IsPressed(KeyboardButton.D) ? 2 : 0)
    ;
star.Transform.LocalPosition += Vector2.Right * ball.Sprite!.Rectangle.Size.X / 2;
// star.RotationDegrees = 00;

var heart = Juicebox.NewEntity("heart")
    .WithSprite("./resources/heart.png")
    .WithParent(star)
    ;
heart.Transform.LocalPosition += Vector2.Right * star.Sprite!.Rectangle.Size.X / 2;

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
    .WithTags("title-screen", "gui")
    .WithText("Breakout", font: "./resources/airstrike.ttf");
// ----------------------------------------------------------------------------

// Load texts
var texts = new Dictionary<Text, (IntPtr Texture, SDL_Rect TargetRect)>();
var fonts = new Dictionary<string, IntPtr>();
foreach (var text in Juicebox._instance._texts)
{
    if (!fonts.TryGetValue(text.Font, out var font))
    {
        font = TTF_OpenFont("./resources/airstrike.ttf", 24);
        fonts[text.Font] = font;
    }
    var fontColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
    var textSurface = TTF_RenderText_Solid(font, text.Value, fontColor);
    var textTexture = SDL_CreateTextureFromSurface(renderer, textSurface);
    SDL_FreeSurface(textSurface);
    SDL_QueryTexture(textTexture, out var _, out var _, out var textWidth, out var textHeight);
    text.Rectangle.Size = new(textWidth, textHeight);
    var textRect = new SDL_Rect() { x = 0, y = 0, w = textWidth, h = textHeight };
    texts[text] = (textTexture, textRect);
}

SDL_QueryTexture(texture, out var format, out var access, out var width, out var height);
var destination = new SDL_Rect
{
    x = 0,
    y = 0,
    w = width,
    h = height,
};

var quad = new Quad();

Juicebox._instance._sprites.OnStart();
Juicebox.Time.Start();
while (true)
{
    Juicebox.Time.OnUpdate();
    while (SDL_PollEvent(out SDL_Event e) != 0)
    {
        if (e.key.keysym.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
        {
            return;
        }

        Juicebox.Input.AcceptEvent(e);
    }

    Juicebox.DrawLine(Vector2.Left * 10, Vector2.Right * 10, Color.White);
    Juicebox.DrawLine(Vector2.Up * 10, Vector2.Down * 10, Color.White);

    SDL_SetRenderDrawColor(renderer, 0x00, 0x00, 0x00, 0xFF);
    SDL_RenderClear(renderer);

    Juicebox.Physics.OnUpdate(Juicebox.Time.Delta);

    foreach (var handler in Juicebox._instance._events.OnEachFrameEventHandlers)
    {
        ((JuiceboxEngine.EventEntityExtensions.OnEachFrameHandler)handler.Handler)(handler.Entity);
    }

    foreach (var entity in Juicebox._instance._entities.Values)
    {
        // Sprite
        if (entity.Sprite is not null)
        {
            var (spriteTexture, targetRect) = sprites[entity.Sprite];
            var targetRectangle = entity.GetTargetRectangle().ToSdlRect();
            var rotationMatrix = entity.Transform.RotationMatrix;
            System.Console.WriteLine(entity.Transform.GetLocalToWorldMatrix());
            // System.Console.WriteLine(rotationMatrix);
            var polygon =
                Juicebox.Camera.GetWorldToCameraMatrix()
                *
                // Matrix33.GetTranslationMatrix(-entity.Transform.Center) // SDL textures rect is on top left, but maybe rect can have center position
                // *
                entity.Transform.GetLocalToWorldMatrix()
                * (Polygon)new Rectangle(Vector2.Zero, entity.GetTargetRectangle().Size);
            Juicebox.DrawPolygon(
                polygon,
                Color.Purple,
                Space.Screen);
            var center = entity.Sprite.Center.ToSdlPoint();
            // Juicebox.DrawCircle(Juicebox.Camera.Entity.Transform.TranslationMatrix * entity.GetTargetRectangle().Position, 2, Color.Red);
            polygon.ToQuad(ref quad);
            if (SDL_RenderGeometry(renderer, spriteTexture, quad.Vertices, quad.Vertices.Length, null, 0) != 0)
            {
                Console.WriteLine(SDL_GetError());
            }
        }

        // Text
        if (entity.GetComponent<Text>() is Text text)
        {
            var (textTexture, rect) = texts[text];
            var targetRectangle = entity.GetTargetRectangle().ToSdlRect();
            // Juicebox.DrawPolygon(Juicebox.Camera.Entity.Transform.TranslationMatrix * (Polygon)entity.GetTargetRectangle(), Color.Purple);
            SDL_RenderCopy(renderer, textTexture, IntPtr.Zero, ref targetRectangle);
        }
    }

    Juicebox.Gizmos.Update(Juicebox.Time.Delta);
    Juicebox.Input.AfterUpdate();

    SDL_RenderPresent(renderer);
    SDL_Delay(1000 / 60);
}

Console.ReadKey(true);

SDL_DestroyTexture(texture);
SDL_DestroyRenderer(renderer);
SDL_DestroyWindow(window);
SDL_Quit();

public struct Quad
{
    public SDL_Vertex[] Vertices = new SDL_Vertex[6];
    public const int DefaultSize = 250;

    public void SetTopLeft(Vector2 position)
    {
        Vertices[0].position.x = position.X;
        Vertices[0].position.y = position.Y;
    }
    public void SetTopRight(Vector2 position)
    {
        Vertices[1].position.x = position.X;
        Vertices[1].position.y = position.Y;
        Vertices[3].position.x = position.X;
        Vertices[3].position.y = position.Y;
    }
    public void SetBottomLeft(Vector2 position)
    {
        Vertices[2].position.x = position.X;
        Vertices[2].position.y = position.Y;
        Vertices[5].position.x = position.X;
        Vertices[5].position.y = position.Y;
    }
    public void SetBottomRight(Vector2 position)
    {
        Vertices[4].position.x = position.X;
        Vertices[4].position.y = position.Y;
    }

    public Quad()
    {
        // Top left
        Vertices[0].position.x = 0;
        Vertices[0].position.y = 0;
        Vertices[0].color.r = 255;
        Vertices[0].color.g = 255;
        Vertices[0].color.b = 255;
        Vertices[0].color.a = 255;
        Vertices[0].tex_coord.x = 0;
        Vertices[0].tex_coord.x = 0;

        // Top right
        Vertices[1].position.x = DefaultSize;
        Vertices[1].position.y = 0;
        Vertices[1].color.r = 255;
        Vertices[1].color.g = 255;
        Vertices[1].color.b = 255;
        Vertices[1].color.a = 255;
        Vertices[1].tex_coord.x = 1;
        Vertices[1].tex_coord.y = 0;

        // Bottom left 
        Vertices[2].position.x = 0;
        Vertices[2].position.y = DefaultSize;
        Vertices[2].color.r = 255;
        Vertices[2].color.g = 255;
        Vertices[2].color.b = 255;
        Vertices[2].color.a = 255;
        Vertices[2].tex_coord.x = 0;
        Vertices[2].tex_coord.y = 1;

        // Top right
        Vertices[3].position.x = DefaultSize;
        Vertices[3].position.y = 0;
        Vertices[3].color.r = 255;
        Vertices[3].color.g = 255;
        Vertices[3].color.b = 255;
        Vertices[3].color.a = 255;
        Vertices[3].tex_coord.x = 1;
        Vertices[3].tex_coord.y = 0;

        // Bottom right
        Vertices[4].position.x = DefaultSize;
        Vertices[4].position.y = DefaultSize;
        Vertices[4].color.r = 255;
        Vertices[4].color.g = 255;
        Vertices[4].color.b = 255;
        Vertices[4].color.a = 255;
        Vertices[4].tex_coord.x = 1;
        Vertices[4].tex_coord.y = 1;

        // Bottom left
        Vertices[5].position.x = 0;
        Vertices[5].position.y = DefaultSize;
        Vertices[5].color.r = 255;
        Vertices[5].color.g = 255;
        Vertices[5].color.b = 255;
        Vertices[5].color.a = 255;
        Vertices[5].tex_coord.x = 0;
        Vertices[5].tex_coord.y = 1;
    }

}

public static class SdlExtensions
{
    public static void ToQuad(this Polygon polygon, ref Quad quad)
    {
        quad.SetTopLeft(polygon.Vertices[0]);
        quad.SetTopRight(polygon.Vertices[1]);
        quad.SetBottomRight(polygon.Vertices[2]);
        quad.SetBottomLeft(polygon.Vertices[3]);
    }

    public static SDL_Rect ToSdlRect(this Rectangle rectangle) => new()
    {
        x = (int)rectangle.Position.X,
        y = (int)rectangle.Position.Y,
        w = (int)rectangle.Size.X,
        h = (int)rectangle.Size.Y
    };

    public static SDL_Point ToSdlPoint(this Vector2 vector) => new()
    {
        x = (int)vector.X,
        y = (int)vector.Y
    };
}
