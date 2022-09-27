
using static SDL2.SDL;

namespace JuiceboxEngine;

public class MainLoop
{
    private readonly SdlFacade _sdl;
    private readonly Input _input;
    private readonly Gizmos _gizmos;
    private readonly Events _events;
    private readonly Collisions _collisions;
    private readonly EntityRepository _entityRepository;
    private readonly Time _time;

    public MainLoop(
        SdlFacade sdl,
        Input input,
        Gizmos gizmos,
        Events events,
        Collisions collisions,
        EntityRepository entityRepository,
        Time time)
    {
        _sdl = sdl;
        _input = input;
        _gizmos = gizmos;
        _events = events;
        _collisions = collisions;
        _entityRepository = entityRepository;
        _time = time;
    }

    public void Start()
    {
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

            // Origin of the coordinates
            Juicebox.DrawLine(Vector2.Left * 10, Vector2.Right * 10, Color.White);
            Juicebox.DrawLine(Vector2.Up * 10, Vector2.Down * 10, Color.White);

            _collisions.Update();
            Juicebox.Physics.Update(Juicebox.Time.Delta);

            foreach (var handler in _events.OnEachFrameEventHandlers)
            {
                ((EventEntityExtensions.OnEachFrameHandler)handler.Handler)(handler.Entity);
            }

            _sdl.BeforeUpdate();

            foreach (var entity in _entityRepository.GetAll())
            {
                // Animator
                if (entity.GetComponent<Animator>() is Animator animator)
                {
                    animator.OnUpdate();
                }

                // Sprite
                if (entity.SpriteRenderer is SpriteRenderer sprite)
                {
                    _sdl.RenderSprite(sprite);
                }

                // Text
                else if (entity.GetComponent<Text>() is Text text)
                {
                    _sdl.RenderText(text);
                }

                if (entity.GetComponent<CircleCollider>() is CircleCollider collider)
                {
                    Juicebox.Draw(collider);
                }
                else if (entity.GetComponent<RectangleCollider>() is RectangleCollider rectangleCollider)
                {
                }
            }

            _gizmos.Update(Juicebox.Time.Delta);
            _input.AfterUpdate();
            _sdl.AfterUpdate();
            _time.AfterUpdate();

            SDL_Delay(1000 / 60);
        }
    }
}


