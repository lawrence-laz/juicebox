
using static SDL2.SDL;

namespace JuiceboxEngine;

public struct Quad
{
    public SDL_Vertex[] Vertices = new SDL_Vertex[6];
    public const int DefaultSize = 250;

    public void SetTopLeft(Vector2 position, Vector2 uv)
    {
        Vertices[0].position.x = position.X;
        Vertices[0].position.y = position.Y;

        Vertices[0].tex_coord.x = uv.X;
        Vertices[0].tex_coord.y = uv.Y;
    }
    public void SetTopRight(Vector2 position, Vector2 uv)
    {
        Vertices[1].position.x = position.X;
        Vertices[1].position.y = position.Y;
        Vertices[3].position.x = position.X;
        Vertices[3].position.y = position.Y;

        Vertices[1].tex_coord.x = uv.X;
        Vertices[1].tex_coord.y = uv.Y;
        Vertices[3].tex_coord.x = uv.X;
        Vertices[3].tex_coord.y = uv.Y;
    }
    public void SetBottomLeft(Vector2 position, Vector2 uv)
    {
        Vertices[2].position.x = position.X;
        Vertices[2].position.y = position.Y;
        Vertices[5].position.x = position.X;
        Vertices[5].position.y = position.Y;

        Vertices[2].tex_coord.x = uv.X;
        Vertices[2].tex_coord.y = uv.Y;
        Vertices[5].tex_coord.x = uv.X;
        Vertices[5].tex_coord.y = uv.Y;
    }
    public void SetBottomRight(Vector2 position, Vector2 uv)
    {
        Vertices[4].position.x = position.X;
        Vertices[4].position.y = position.Y;

        Vertices[4].tex_coord.x = uv.X;
        Vertices[4].tex_coord.y = uv.Y;
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


