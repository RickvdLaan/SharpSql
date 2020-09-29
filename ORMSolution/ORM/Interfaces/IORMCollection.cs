namespace ORM.Interfaces
{
    public interface IORMCollection
    {
        public string ExecutedQuery { get; }

        public bool DisableChangeTracking { get; }

        void Fetch();

        void Fetch(long maxNumberOfItemsToReturn);

        void SaveChanges();

        void Add(ORMEntity entity);

        void Remove(ORMEntity entity);
    }
}
