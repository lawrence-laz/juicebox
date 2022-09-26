
using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;
using static SDL2.SDL_gfx;
using JuiceboxEngine;
using SDL2;

namespace JuiceboxEngine;

public class SdlFacade
{
    private IntPtr _window;
    private IntPtr _renderer;
    private readonly Dictionary<Sprite, IntPtr> _spriteTextures = new();
    private readonly Dictionary<Font, IntPtr> _fonts = new();
    private readonly Dictionary<Text, IntPtr> _textTextures = new();
    private Quad _renderingQuad = new();

    public void Start()
    {
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

        _window = SDL_CreateWindow("Breakout by Juicebox", 0, 0, 512, 256, SDL_WindowFlags.SDL_WINDOW_OPENGL);
        _renderer = SDL_CreateRenderer(_window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

        Juicebox.Gizmos.RenderCircle = (circle, color) =>
        {
            var targetPosition = circle.Center;
            SDL_gfx.aacircleRGBA(_renderer, (short)targetPosition.X, (short)targetPosition.Y, (short)circle.Radius, color.R, color.G, color.B, color.A);
        };

        Juicebox.Gizmos.RenderLine = (line, color) =>
        {
            var start = line.Start;
            var end = line.End;
            SDL_gfx.aalineRGBA(_renderer, (short)start.X, (short)start.Y, (short)end.X, (short)end.Y, color.R, color.G, color.B, color.A);
        };

        Juicebox.Instance.OnLoadSprite = LoadSprite;
    }

    public void BeforeUpdate()
    {
        SDL_SetRenderDrawColor(_renderer, 0x00, 0x00, 0x00, 0xFF);
        SDL_RenderClear(_renderer);
    }

    public void AfterUpdate()
    {
        SDL_RenderPresent(_renderer);
    }

    public void LoadSprite(Sprite sprite)
    {
        var surface = IMG_Load(sprite.Path);
        var texture = SDL_CreateTextureFromSurface(_renderer, surface);
        if (texture == IntPtr.Zero)
        {
            throw new Exception($"Failed to load texture for sprite '{sprite.Path}' ({SDL_GetError()})");
        }
        SDL_FreeSurface(surface);
        SDL_QueryTexture(texture, out var spriteFormat, out var spriteAccess, out var spriteWidth, out var spriteHeight);
        sprite.FullRectangle.Size = new(spriteWidth, spriteHeight);
        _spriteTextures[sprite] = texture;
    }

    public void LoadFont(Font font)
    {
        var ttf = TTF_OpenFont(font.Path, 24);
        _fonts[font] = ttf;
    }

    public void LoadText(Text text)
    {
        if (!_fonts.TryGetValue(text.Font, out var font))
        {
            LoadFont(text.Font);
        }
        var fontColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
        var surface = TTF_RenderText_Solid(font, text.Value, fontColor);
        var texture = SDL_CreateTextureFromSurface(_renderer, surface);
        SDL_FreeSurface(surface);
        SDL_QueryTexture(texture, out var _, out var _, out var textWidth, out var textHeight);
        text.Rectangle.Size = new(textWidth, textHeight);
        var textRect = new SDL_Rect() { x = 0, y = 0, w = textWidth, h = textHeight };
        _textTextures[text] = texture;
    }

    public void RenderText(Text text)
    {
        var texture = _textTextures[text];
        var polygon =
            Juicebox.Camera.GetWorldToCameraMatrix()
            * text.Entity.Transform.GetLocalToWorldMatrix()
            * (Polygon)new Rectangle(Vector2.Zero, text.Entity.GetTargetRectangle().Size);
        polygon.Uvs = Rectangle.One.AsPoints().ToList();
        Juicebox.DrawPolygon(polygon, Color.Purple, Space.Screen);
        polygon.ToQuad(ref _renderingQuad);
        if (SDL_RenderGeometry(_renderer, texture, _renderingQuad.Vertices, _renderingQuad.Vertices.Length, null, 0) != 0)
        {
            Console.WriteLine(SDL_GetError());
        }
    }

    internal void Stop()
    {
        SDL_DestroyRenderer(_renderer);
        SDL_DestroyWindow(_window);
        SDL_Quit();
    }

    internal void RenderSprite(SpriteRenderer spriteRenderer)
    {
        var spriteTexture = _spriteTextures[spriteRenderer.Sprite];
        var polygon =
            Juicebox.Camera.GetWorldToCameraMatrix()
            * spriteRenderer.Entity.Transform.GetLocalToWorldMatrix()
            * (Polygon)new Rectangle(Vector2.Zero, spriteRenderer.SourceRectangle.Size);
        polygon.Uvs = spriteRenderer.SourceRectangle.Normalized(spriteRenderer.Sprite.FullRectangle).AsPoints().ToList();
        Juicebox.DrawPolygon(polygon, Color.Purple, Space.Screen);
        polygon.ToQuad(ref _renderingQuad);
        if (SDL_RenderGeometry(_renderer, spriteTexture, _renderingQuad.Vertices, _renderingQuad.Vertices.Length, null, 0) != 0)
        {
            Console.WriteLine(SDL_GetError());
        }
    }
}

public static class SdlExtensions
{
    public static void ToQuad(this Polygon polygon, ref Quad quad)
    {
        quad.SetTopLeft(polygon.Vertices[0], polygon.Uvs[0]);
        quad.SetTopRight(polygon.Vertices[1], polygon.Uvs[1]);
        quad.SetBottomRight(polygon.Vertices[2], polygon.Uvs[2]);
        quad.SetBottomLeft(polygon.Vertices[3], polygon.Uvs[3]);
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

