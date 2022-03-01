namespace SharpSql.Benchmarks;

public abstract class BaseBenchmark
{
    protected int i;

    protected void BaseSetup()
    {
        i = 10247;
    }

    protected void Step()
    {
        i++;
        if (i > 11077) i = 10248;
    }
}