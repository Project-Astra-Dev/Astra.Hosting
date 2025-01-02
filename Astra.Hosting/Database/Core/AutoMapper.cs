using Astra.Hosting.Database.Interfaces;

namespace Astra.Hosting.Database.Core;

public static class AutoMapper
{
    public static IAutoMapperCreation<TEntity, TModel> CreateMap<TEntity, TModel>()
        where TEntity : class, IDbObject
        where TModel : class
    {
        return default!;
    }
}