namespace SharpSql.Interfaces
{
    public interface IORMCollection<EntityType> where EntityType : ORMEntity
    {
        public string ExecutedQuery { get; }

        public bool DisableChangeTracking { get; }

        IORMCollection<EntityType> Fetch();

        IORMCollection<EntityType> Fetch(long maxNumberOfItemsToReturn);

        void SaveChanges();

        void Add(EntityType entity);

        void Remove(EntityType entity);
    }
}
