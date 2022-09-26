using Microsoft.Extensions.DependencyInjection;

namespace JuiceboxEngine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJuicebox(this IServiceCollection services)
    {
        services.AddSingleton<SdlFacade>();
        services.AddSingleton<JuiceboxInstance>();
        services.AddSingleton<MainLoop>();
        services.AddSingleton<Events>();
        services.AddSingleton<Collisions>();
        services.AddSingleton<Gizmos>();
        services.AddSingleton<Input>();
        services.AddSingleton<Physics>();
        services.AddSingleton<Time>();

        services
            .AddSingleton<SpriteRendererSystem>();

        services
            .AddSingleton<EntityFactory>()
            .AddSingleton<SpriteFactory>()
            .AddSingleton<TextFactory>()
            .AddSingleton<FontFactory>();

        services
            .AddSingleton<EntityRepository>()
            .AddSingleton<SpriteRepository>()
            .AddSingleton<FontRepository>();

        return services;
    }
}


