using NUnit.Framework;
using ORM;
using ORM.Attributes;
using System.Collections.Generic;
using System.Reflection;

namespace ORMNUnit
{
    [SetUpFixture, ORMUnitTest]
    internal class NUnitSetupFixture
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            var memoryEntityTables = new List<string>()
            {
                $"{nameof(ORMNUnit)}.MemoryEntityTables.USERS.xml",
                $"{nameof(ORMNUnit)}.MemoryEntityTables.ROLES.xml",
                $"{nameof(ORMNUnit)}.MemoryEntityTables.USERROLES.xml",
                $"{nameof(ORMNUnit)}.MemoryEntityTables.ORGANISATIONS.xml"
            };
            
            var memoryCollectionTables = new List<string>()
            {
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicFetchUsers.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicFetchTopUsers.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicJoinInner.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicSelectUsers.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicJoinLeft.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicOrderBy.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicWhereAnd.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicWhereEqualTo.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicWhereNotEqualTo.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicWhereLessThanOrEqual.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicWhereGreaterThanOrEqual.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.ComplexJoinA.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.ComplexJoinB.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.ComplexJoinC.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.ComplexJoinD.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.ComplexJoinE.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.ComplexWhereLike.xml",
                $"{nameof(ORMNUnit)}.MemoryCollectionTables.BasicMultiplePrimaryKeys.xml"
            };

            _ = new ORMInitialize(Assembly.GetAssembly(GetType()), memoryEntityTables, memoryCollectionTables);
        }
    }
}
