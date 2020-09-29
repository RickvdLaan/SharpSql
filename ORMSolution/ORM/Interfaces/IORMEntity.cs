namespace ORM.Interfaces
{
    public interface IORMEntity
    {
        public string ExecutedQuery { get; }

        public bool DisableChangeTracking { get; }

        void Save();

        void Delete();
    }
}
