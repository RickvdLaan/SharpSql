using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SharpSql.Benchmarks
{
    [SimpleJob(RunStrategy.Throughput)]
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [Description(nameof(EFCoreBenchmarks))]
    public class EFCoreBenchmarks : BaseBenchmark
    {
        private EFCoreContext Context;

        private static readonly Func<EFCoreContext, int, EFCore.Order> compiledQuery =
            EF.CompileQuery((EFCoreContext ctx, int id) => ctx.Orders.First(x => x.OrderId == id));

        [GlobalSetup]
        public void Init()
        {
            BaseSetup();
            Context = new EFCoreContext();
        }

        [Benchmark(Description = nameof(GetOrderById))]
        public EFCore.Order GetOrderById()
        {
            return Context.Orders.First(x => x.OrderId == 10248);
        }

        [Benchmark(Description = nameof(GetAllOrders))]
        public List<EFCore.Order> GetAllOrders()
        {
            return Context.Orders.ToList();
        }

        [Benchmark(Description = nameof(GetAllOrdersIndividually))]
        public EFCore.Order GetAllOrdersIndividually()
        {
            Step();
            return Context.Orders.First(x => x.OrderId == i);
        }

        [Benchmark(Description = nameof(GetAllOrdersIndividuallyCompiled))]
        public EFCore.Order GetAllOrdersIndividuallyCompiled()
        {
            Step();
            return compiledQuery(Context, i);
        }

        [Benchmark(Description = nameof(SqlQueryMapped))]
        public EFCore.Order SqlQueryMapped()
        {
            Step();
            return Context.Orders.FromSqlRaw("select * from Orders where OrderID = {0}", i).First();
        }

        [Benchmark(Description = nameof(NoTracking))]
        public EFCore.Order NoTracking()
        {
            Step();
            return Context.Orders.AsNoTracking().First(x => x.OrderId == i);
        }
    }
}