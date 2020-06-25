namespace ORM
{
    public class ORMEntityField
    {
        public string Name { get; private set; }

        public ORMEntityField(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public static ORMSortClause operator &(ORMEntityField field, ORMSortType sortType)
        {
            return new ORMSortClause(field, sortType);
        }

        public ORMSortClause Ascending()
        {
            return new ORMSortClause(this, ORMSortType.Ascending);
        }

        public ORMSortClause Descending()
        {
            return new ORMSortClause(this, ORMSortType.Descending);
        }
    }
}
