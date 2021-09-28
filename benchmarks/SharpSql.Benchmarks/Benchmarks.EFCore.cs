using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using SharpSql.Northwind;
using System;
using System.ComponentModel;
using System.Linq;

namespace SharpSql.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [Description(nameof(EFCoreBenchmarks))]
    public class EFCoreBenchmarks : BaseBenchmark
    {
        private EFCoreContext Context;

        private static readonly Func<EFCoreContext, int, Order> compiledQuery =
            EF.CompileQuery((EFCoreContext ctx, int id) => ctx.Orders.First(p => p.OrderId == id));

        [GlobalSetup]
        public void Init()
        {
            BaseSetup();
            Context = new EFCoreContext();
        }

        //[Benchmark(Description = nameof(First))]
        //public Order First()
        //{
        //    Step();
        //    return Context.Orders.First(p => p.OrderId == i);
        //}

        //[Benchmark(Description = nameof(Compiled))]
        //public Order Compiled()
        //{
        //    Step();
        //    return compiledQuery(Context, i);
        //}

        //[Benchmark(Description = nameof(SqlQuery))]
        //public Order SqlQuery()
        //{
        //    Step();
        //    return Context.Orders.FromSqlRaw("select * from Orders where OrderID = {0}", i).First();
        //}

        //[Benchmark(Description = nameof(NoTracking))]
        //public Order NoTracking()
        //{
        //    Step();
        //    return Context.Orders.AsNoTracking().First(p => p.OrderId == i);
        //}
    }
}