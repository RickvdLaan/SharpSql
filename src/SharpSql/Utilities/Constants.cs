namespace SharpSql;

internal abstract class Constants
{
    internal const char Semicolon = ';';

    internal const string QueryParam = "@PARAM";

    internal const string Join = "JOIN";
    internal const string Left = "LEFT";
    internal const string Right = "RIGHT";

    internal const string Full = "FULL";
    internal const string Inner = "INNER";
    internal const string Outer = "OUTER";

    internal const string OrderByAsc = "ASC";
    internal const string OrderByDesc = "DESC";

    internal const string IsManyToMany = "IsManyToMany";
    internal const string ManyToMany = "ManyToMany";
}