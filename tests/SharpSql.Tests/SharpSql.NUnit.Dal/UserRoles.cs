using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [ORMTable(typeof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles))]
    public class UserRoles : ORMCollection<UserRole>
    {
        public UserRoles() { }
    }
}
