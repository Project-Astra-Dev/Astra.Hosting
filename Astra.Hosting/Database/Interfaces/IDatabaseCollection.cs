namespace Astra.Hosting.Database.Interfaces
{
    public interface IDatabaseCollection<T> where T : class, IDbObject
    {
        string CollectionName { get; }
    }
}