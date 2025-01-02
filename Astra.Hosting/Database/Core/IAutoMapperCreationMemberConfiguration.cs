// Copyright (c) nexusverypro (github.com/nexusverypro) $2024.
//     All rights reserved.

using System.Linq.Expressions;
using System.Reflection;
using Astra.Hosting.Database.Interfaces;

namespace Astra.Hosting.Database.Core;

public interface IAutoMapperCreationMemberConfiguration
{
    TypeInfo Type { get; }
    MemberInfo Member { get; }
    bool Ignored { get; }
    Type? ConversionType { get; }
}

public interface IAutoMapperCreationMemberConfiguration<TEntity, TModel> : IAutoMapperCreationMemberConfiguration
    where TEntity : class, IDbObject
    where TModel : class
{
}

public interface IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember>
    : IAutoMapperCreationMemberConfiguration<TEntity, TModel>, IAutoMapperCreationMemberConfiguration
    where TEntity : class, IDbObject
    where TModel : class
{
    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> MapFromMember(
        Expression<Func<TEntity, TMember>> sourceMember);

    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> ConvertUsing(Type conversionType);
    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> ConvertUsing<TConversion>();

    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> UsingCondition(
        Expression<Func<TEntity, TMember, bool>> condition);

    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> Ignore();

    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> MapFrom<TSourceMember>(
        Expression<Func<TEntity, TSourceMember>> sourceMember);

    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> UseValue(TMember value);
    IAutoMapperCreationMemberConfiguration<TEntity, TModel, TMember> ResolveUsing(Func<TEntity, TMember> resolver);

    bool HasForcedValue { get; }
    TMember? ForcedValue { get; }
    Expression<Func<TEntity, TMember>>? SourceMemberExpression { get; }
    Func<TEntity, TMember>? CustomResolver { get; }
    Expression? CustomMapExpression { get; }
    string? SourceTableName { get; }
    IReadOnlyList<Expression<Func<TEntity, TMember, bool>>> Conditions { get; }
}