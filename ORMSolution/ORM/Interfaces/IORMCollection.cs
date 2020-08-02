namespace ORM.Interfaces
{
    public interface IORMCollection
    {
        public string ExecutedQuery { get; }

        public bool DisableChangeTracking { get; set; }

        void Fetch();

        void Fetch(long maxNumberOfItemsToReturn);
    }
}
