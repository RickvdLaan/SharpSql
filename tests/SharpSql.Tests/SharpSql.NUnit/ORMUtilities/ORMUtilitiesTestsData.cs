using NUnit.Framework;
using System.Collections;

namespace SharpSql.NUnit
{
    [TestFixture]
    public partial class SharpSqlUtilityTests
    {
        public static IEnumerable ConvertTo_Data
        {
            get
            {
                // Change tracking enabled.
                yield return new TestCaseData("Imaani", "qwerty", false, 0);
                yield return new TestCaseData("Clarence", "password", false, 1);
                yield return new TestCaseData("Beverley", "abc123", false, 2);
                yield return new TestCaseData("Adyan", "123456", false, 3);
                yield return new TestCaseData("Chloe", "dragon", false, 4);
                // Change tracking disabled.
                yield return new TestCaseData("Imaani", "qwerty", true, 0);
                yield return new TestCaseData("Clarence", "password", true, 1);
                yield return new TestCaseData("Beverley", "abc123", true, 2);
                yield return new TestCaseData("Adyan", "123456", true, 3);
                yield return new TestCaseData("Chloe", "dragon", true, 4);
            }
        }
    }
}
