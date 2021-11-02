namespace SharpSql.Interfaces
{
    public interface ISharpSqlCollection<EntityType> where EntityType : SharpSqlEntity
    {
        public string ExecutedQuery { get; }

        public bool DisableChangeTracking { get; }

        ISharpSqlCollection<EntityType> Fetch();

        ISharpSqlCollection<EntityType> Fetch(long maxNumberOfItemsToReturn);

        void SaveChanges();

        void Add(EntityType entity);

        void Remove(EntityType entity);
    }
}
