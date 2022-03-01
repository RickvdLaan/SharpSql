using NUnit.Framework;
using System.Collections;

namespace SharpSql.NUnit;

[TestFixture]
public partial class SharpSqlUtilityTests
{
    public static IEnumerable ConvertTo_Data
    {
        get
        {
            // Disable changetracking is set to false:
            yield return new TestCaseData("Imaani", "qwerty", false, 0, null);
            yield return new TestCaseData("Clarence", "password", false, 1, null);
            yield return new TestCaseData("Beverley", "abc123", false, 2, null);
            yield return new TestCaseData("Adyan", "123456", false, 3, null);
            yield return new TestCaseData("Chloe", "dragon", false, 4, null);
            // Disable changetracking is set to true without any changes:
            yield return new TestCaseData("Imaani", "qwerty", true, 0, null);
            yield return new TestCaseData("Clarence", "password", true, 1, null);
            yield return new TestCaseData("Beverley", "abc123", true, 2, null);
            yield return new TestCaseData("Adyan", "123456", true, 3, null);
            yield return new TestCaseData("Chloe", "dragon", true, 4, null);
            // Disable changetracking is set to true with changes:
            yield return new TestCaseData("Imaani", "qwerty", true, 0, "Qwerty");
            yield return new TestCaseData("Clarence", "password", true, 1, "Password");
            yield return new TestCaseData("Beverley", "abc123", true, 2, "Abc123");
            yield return new TestCaseData("Adyan", "123456", true, 3, "1234567");
            yield return new TestCaseData("Chloe", "dragon", true, 4, "Dragon");
        }
    }
}