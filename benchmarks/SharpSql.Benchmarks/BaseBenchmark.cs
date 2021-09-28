namespace SharpSql.Benchmarks
{
    public abstract class BaseBenchmark
    {
        protected int i;

        protected void BaseSetup()
        {
            i = 0;
        }

        protected void Step()
        {
            i++;
            if (i > 829) i = 1;
        }
    }
}
