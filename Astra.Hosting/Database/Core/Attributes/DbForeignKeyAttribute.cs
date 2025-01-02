namespace Astra.Hosting.Database.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DbForeignKeyAttribute : Attribute
{
    public DbForeignKeyAttribute(string tableName, string columnName)
    {
        TableName = tableName;
        ColumnName = columnName;
    }
    
    public string TableName { get; }
    public string ColumnName { get; }
}