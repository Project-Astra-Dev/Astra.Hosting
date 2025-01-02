using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Astra.Hosting.Database.Interfaces;

namespace Astra.Hosting.Database.Core
{
    public interface IAutoMapper<TEntity, TModel>
        where TEntity : class, IDbObject
        where TModel : class
    {
        TModel Map(TEntity entity);
        IEnumerable<TModel> MapCollection(IEnumerable<TEntity> entities);
        TEntity MapBack(TModel model);
        void MapTo(TEntity source, TModel destination);
    }
}