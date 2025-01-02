// Copyright (c) nexusverypro (github.com/nexusverypro) $2024.
//     All rights reserved.

using System;
using System.Linq.Expressions;
using Astra.Hosting.Database.Interfaces;

namespace Astra.Hosting.Database.Core;

public interface IAutoMapperCreation
{
    IAutoMapper GetAutoMapperNonGeneric();
}

public interface IAutoMapperCreation<TEntity, TModel> : IAutoMapperCreation
    where TEntity : class, IDbObject
    where TModel : class
{
    IAutoMapperCreation<TEntity, TModel> Exclude<TMember>(Expression<Func<TEntity, TMember>> sourceMember);
    IAutoMapperCreation<TEntity, TModel> ForMember<TMember>(
        Expression<Func<TModel, TMember>> destinationMember,
        Action<IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember>> memberOptions);
            
    IAutoMapperCreation<TEntity, TModel> BeforeMap(Action<TEntity, TModel> action);
    IAutoMapperCreation<TEntity, TModel> AfterMap(Action<TEntity, TModel> action);
    
    IAutoMapper<TEntity, TModel> GetAutoMapper();
}