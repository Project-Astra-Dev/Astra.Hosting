// Copyright (c) nexusverypro (github.com/nexusverypro) $2024.
//     All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Astra.Hosting.Database.Interfaces;
using LiteDB;

namespace Astra.Hosting.Database.Core;

public sealed class StandardAutoMapperCreation<TEntity, TModel> : IAutoMapperCreation<TEntity, TModel>
    where TEntity : class, IDbObject
    where TModel : class
{
    private Action<TEntity, TModel> _beforeMapAction;
    private Action<TEntity, TModel> _afterMapAction;
    private List<MemberInfo> _allExclusions = new();
    private List<IAutoMapperCreationMemberConfiguration<TEntity, TModel>> _allMemberConfigurations = new();

    public IAutoMapperCreation<TEntity, TModel> Exclude<TMember>(Expression<Func<TEntity, TMember>> sourceMember)
    {
        MemberExpression memberExpr = null;
        if (sourceMember.Body is MemberExpression)
            memberExpr = (MemberExpression)sourceMember.Body;
        else if (sourceMember.Body is UnaryExpression)
            memberExpr = (MemberExpression)((UnaryExpression)sourceMember.Body).Operand;

        if (memberExpr == null)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(sourceMember));
        }
        if (_allExclusions.Contains(memberExpr.Member)) 
            throw new ArgumentException("Member expression is already excluded", nameof(sourceMember));

        _allExclusions.Add(memberExpr.Member);
        return this;
    }

    public IAutoMapperCreation<TEntity, TModel> ForMember<TMember>(
        Expression<Func<TModel, TMember>> destinationMember,
        Action<IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember>> memberOptions)
    {
        MemberExpression memberExpr = null;
        if (destinationMember.Body is MemberExpression)
            memberExpr = (MemberExpression)destinationMember.Body;
        else if (destinationMember.Body is UnaryExpression)
            memberExpr = (MemberExpression)((UnaryExpression)destinationMember.Body).Operand;

        if (memberExpr == null)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(destinationMember));
        }
        if (_allMemberConfigurations.Any(x => x.Member == memberExpr.Member))
            throw new ArgumentException("Member configuration is already defined", nameof(destinationMember));
        
        var memberConfiguration = new StandardAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember>(
            memberExpr.Member.DeclaringType?.GetTypeInfo() ?? throw new InvalidOperationException(),
            memberExpr.Member);
        
        memberOptions.Invoke(memberConfiguration);
        _allMemberConfigurations.Add(memberConfiguration);
        return this;
    }

    public IAutoMapperCreation<TEntity, TModel> BeforeMap(Action<TEntity, TModel> action)
    {
        _beforeMapAction = action;
        return this;
    }

    public IAutoMapperCreation<TEntity, TModel> AfterMap(Action<TEntity, TModel> action)
    {
        _afterMapAction = action;
        return this;
    }

    public IAutoMapper GetAutoMapperNonGeneric() => AutoMapper;
    public IAutoMapper<TEntity, TModel> GetAutoMapper() => AutoMapper;
    
    public IAutoMapper<TEntity, TModel> AutoMapper => null;
}