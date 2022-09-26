using System.Text.Json;

namespace JuiceboxEngine.Aseprite;

public record struct AsepriteRectangle(int X, int Y, int W, int H);
public record AsepriteFrame(AsepriteRectangle Frame, int Duration);
public record struct AsepriteSize(int W, int H);
public record AsepriteMetadata(string Image, AsepriteSize Size);
public record AsepriteAnimation(AsepriteFrame[] Frames, AsepriteMetadata Meta);

public static class AsepriteEntityExtensions
{
    public static Entity WithAnimation(this Entity entity, string path)
    {
        var json = File.ReadAllText(path);
        var asepriteAnimation = JsonSerializer.Deserialize<AsepriteAnimation>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var animation = new Animation(
            Path.Combine(Path.GetDirectoryName(path), asepriteAnimation.Meta.Image),
            asepriteAnimation.Frames.Select(frame =>
                new AnimationFrame(
                    new Rectangle(new(frame.Frame.X, frame.Frame.Y), new(frame.Frame.W, frame.Frame.H)),
                    frame.Duration / 1000f)
                ).ToArray()
        );

        entity.SpriteRenderer = Juicebox.GetSprite(entity, animation.SpriteSheetPath, null);
        var animator = new Animator(animation, entity);

        return entity.WithComponent(animator);
    }
}

