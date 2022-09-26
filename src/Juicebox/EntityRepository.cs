using System.Collections.Immutable;

namespace JuiceboxEngine;

public class EntityFactory
{
    private readonly EntityRepository _repository;

    public EntityFactory(EntityRepository repository)
    {
        _repository = repository;
    }

    public Entity Create(string name)
    {
        var entity = new Entity(name);
        _repository.Add(entity);
        return entity;
    }
}

public class EntityRepository
{
    private readonly Dictionary<string, Entity> _entities = new();

    public void Add(Entity entity)
    {
        if (_entities.ContainsKey(entity.Name))
        {
            throw new Exception($"Entity with name '{entity.Name}' already exists.");
        }
        _entities[entity.Name] = entity;
    }

    public Entity? GetByName(string name) => _entities.GetValueOrDefault(name);

    public List<Entity> GetAll() => _entities.Values.ToList();

    public IEnumerable<Entity> GetByTag(string tag) =>
        _entities.Values.Where(entity => entity.Tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase)).ToList();

    public IList<T> GetComponents<T>() => _entities.Values.SelectMany(entity => entity.Components.OfType<T>()).ToList();

    public void Remove(Entity entity)
    {
        _entities.Remove(entity.Name);
    }
}

