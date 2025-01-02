// Copyright (c) nexusverypro (github.com/nexusverypro) $2024.
//     All rights reserved.

using System.Linq.Expressions;
using System.Reflection;
using Astra.Hosting.Database.Interfaces;

namespace Astra.Hosting.Database.Core;

public sealed class StandardAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> : IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember>
    where TEntity : class, IDbObject
    where TModel : class
{
    private readonly TypeInfo _destinationTypeInfo;
    private readonly MemberInfo _destinationMemberInfo;
    private bool _ignored;

    private readonly List<Expression<Func<TEntity, TMember, bool>>> _allConditions;
    
    private bool _hasForcedValue;
    private TMember? _forcedValue;

    private Expression<Func<TEntity, TMember>>? _sourceMemberExpression;
    private Type? _conversionType;
    private Func<TEntity, TMember>? _customResolver;
    private Expression? _customMapExpression;
    private string? _sourceTableName;

    public StandardAutoMapperCreationMemberConfiguration(TypeInfo typeInfo, MemberInfo memberInfo)
    {
        _destinationTypeInfo = typeInfo;
        _destinationMemberInfo = memberInfo;
        _allConditions = new List<Expression<Func<TEntity, TMember, bool>>>();
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> MapFromTable(string tableName)
    {
        _sourceTableName = tableName;
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> MapFromMember(Expression<Func<TEntity, TMember>> sourceMember)
    {
        if (_sourceMemberExpression != null || _customResolver != null || _hasForcedValue)
            return this;

        _sourceMemberExpression = sourceMember;
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> ConvertUsing(Type conversionType)
    {
        if (_conversionType != null)
            return this;
        _conversionType = conversionType;
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> ConvertUsing<TConversion>()
    {
        return ConvertUsing(typeof(TConversion));
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> UsingCondition(Expression<Func<TEntity, TMember, bool>> condition)
    {
        _allConditions.Add(condition);
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> Ignore()
    {
        _ignored = true;
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> MapFrom<TSourceMember>(Expression<Func<TEntity, TSourceMember>> sourceMember)
    {
        if (_customMapExpression != null || _sourceMemberExpression != null || _customResolver != null || _hasForcedValue)
            return this;

        _customMapExpression = sourceMember;
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> UseValue(TMember value)
    {
        if (_hasForcedValue) 
            return this;
            
        _hasForcedValue = true;
        _forcedValue = value;
        return this;
    }

    public IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> ResolveUsing(Func<TEntity, TMember> resolver)
    {
        if (_customResolver != null || _sourceMemberExpression != null || _customMapExpression != null || _hasForcedValue)
            return this;

        _customResolver = resolver;
        return this;
    }

    public TypeInfo Type => _destinationTypeInfo;
    public MemberInfo Member => _destinationMemberInfo;
    public bool Ignored => _ignored;
    public bool HasForcedValue => _hasForcedValue;
    public TMember? ForcedValue => _forcedValue;
    public Expression<Func<TEntity, TMember>>? SourceMemberExpression => _sourceMemberExpression;
    public Type? ConversionType => _conversionType;
    public Func<TEntity, TMember>? CustomResolver => _customResolver;
    public Expression? CustomMapExpression => _customMapExpression;
    public string? SourceTableName => _sourceTableName;
    public IReadOnlyList<Expression<Func<TEntity, TMember, bool>>> Conditions => _allConditions.AsReadOnly();
}