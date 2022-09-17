using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;
using JuiceboxEngine;

Console.WriteLine("Hello, World!");

var speed = 5;

var ball = Juicebox.NewEntity("ball")
    .WithSprite("./resources/ball.png")
    .OnEachFrame().Do(entity => entity.Position += speed * Juicebox.Input.Joystick);
// .OnEachFrame().Do(ball => ball.Position += ball.Movement.Speed * ball.Movement.Direction)
// .OnHit(other => other.Name == "ground").Do(() => Juicebox.Restart())
// .OnHit(other => other.Tags.Contains("bouncy")).Do((bouncy, ball, hit) => ball.Movement.Direction.BounceOff(bouncy.Position));

var title = Juicebox.NewEntity("title")
    .WithText("Breakout", font: "./resources/airstrike.ttf");

SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

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


var surface = IMG_Load("./resources/01-Breakout-Tiles.png");
var texture = SDL_CreateTextureFromSurface(renderer, surface);
SDL_FreeSurface(surface);

// Load sprites
var sprites = new Dictionary<Sprite, (IntPtr Texture, SDL_Rect TargetRect)>();
foreach (var sprite in Juicebox._instance._sprites.Values)
{
    var spriteSurface = IMG_Load(sprite.Path);
    var spriteTexture = SDL_CreateTextureFromSurface(renderer, spriteSurface);
    SDL_QueryTexture(spriteTexture, out var spriteFormat, out var spriteAccess, out var spriteWidth, out var spriteHeight);
    var spriteTargetRect = new SDL_Rect { x = 0, y = 0, w = spriteWidth, h = spriteHeight };
    sprites[sprite] = (spriteTexture, spriteTargetRect);
    SDL_FreeSurface(spriteSurface);
}

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

while (true)
{
    while (SDL_PollEvent(out SDL_Event e) != 0)
    {
        if (e.key.keysym.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
        {
            return;
        }

        Juicebox.Input.AcceptEvent(e);
    }

    SDL_RenderClear(renderer);
    SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref destination);

    foreach (var handler in Juicebox._instance._events.OnEachFrameEventHandlers)
    {
        ((JuiceboxEngine.EventEntityExtensions.OnEachFrameHandler)handler.Handler)(handler.Entity);
    }

    foreach (var entity in Juicebox._instance._entities.Values)
    {
        // Sprite
        if (entity.Sprite is not null)
        {

            var rect = sprites[entity.Sprite].TargetRect;
            rect.x = (int)entity.Position.X;
            rect.y = (int)entity.Position.Y;
            SDL_RenderCopy(renderer, sprites[entity.Sprite].Texture, IntPtr.Zero, ref rect);
        }

        // Text
        if (entity.GetComponent<Text>() is Text text)
        {
            var (textTexture, rect) = texts[text];
            rect.x = (int)entity.Position.X;
            rect.y = (int)entity.Position.Y;
            SDL_RenderCopy(renderer, textTexture, IntPtr.Zero, ref rect);
        }
    }

    SDL_RenderPresent(renderer);
    SDL_Delay(1000 / 60);
}

Console.ReadKey(true);

SDL_DestroyTexture(texture);
SDL_DestroyRenderer(renderer);
SDL_DestroyWindow(window);
SDL_Quit();
