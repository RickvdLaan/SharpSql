using SharpSql.Attributes;

namespace SharpSql.NUnit;

[SharpSqlTable(typeof(Users), typeof(User))]
public class Users : SharpSqlCollection<User>
{
    public Users() { }
}