using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [SharpSqlTable(typeof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles))]
    public class UserRoles : SharpSqlCollection<UserRole>
    {
        public UserRoles() { }
    }
}
