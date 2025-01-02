using Astra.Hosting.Database.Interfaces;

namespace Astra.Hosting.Database.Core;

public interface IAutoMapperInstance
{
    IAutoMapperCreation<TEntity, TModel> CreateMap<TEntity, TModel>()
        where TEntity : class, IDbObject
        where TModel : class;
    
    TModel Map<TEntity, TModel>(TEntity entity)
        where TEntity : class, IDbObject
        where TModel : class;
}

public sealed class AutoMapperInstance : IAutoMapperInstance
{
    private readonly Dictionary<Type, IAutoMapperCreation> _allMappings = new Dictionary<Type, IAutoMapperCreation>();
    
    public IAutoMapperCreation<TEntity, TModel> CreateMap<TEntity, TModel>()
        where TEntity : class, IDbObject
        where TModel : class
    {
        var mapperCreation = new StandardAutoMapperCreation<TEntity, TModel>();
        if (!_allMappings.TryAdd(typeof(TEntity), mapperCreation))
            throw new InvalidOperationException($"Type {typeof(TEntity).Name} is already registered");
        return mapperCreation;
    }

    public TModel Map<TEntity, TModel>(TEntity entity)
        where TEntity : class, IDbObject
        where TModel : class
    {
        if (!_allMappings.TryGetValue(typeof(TEntity), out var mapperCreation))
            throw new InvalidOperationException($"Type {typeof(TEntity).Name} is not registered");
        return (TModel)mapperCreation.GetAutoMapperNonGeneric().Map(typeof(TEntity), entity);
    }

    public IEnumerable<TModel> MapCollection<TEntity, TModel>(IEnumerable<TEntity> entities)
        where TEntity : class, IDbObject
        where TModel : class
    {
        if (!_allMappings.TryGetValue(typeof(TEntity), out var mapperCreation))
            throw new InvalidOperationException($"Type {typeof(TEntity).Name} is not registered");
        return (IEnumerable<TModel>)mapperCreation.GetAutoMapperNonGeneric().MapCollection(typeof(TEntity), entities);
    }

    public TEntity MapBack<TEntity, TModel>(TModel model)
        where TEntity : class, IDbObject
        where TModel : class
    {
        if (!_allMappings.TryGetValue(typeof(TEntity), out var mapperCreation))
            throw new InvalidOperationException($"Type {typeof(TEntity).Name} is not registered");
        return (TEntity)mapperCreation.GetAutoMapperNonGeneric().MapBack(typeof(TModel), model);
    }
}