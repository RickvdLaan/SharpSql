using BenchmarkDotNet.Running;
using SharpSql.Benchmarks;
using System;

namespace ORMBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SharpSqlBenchmarks>();
            //BenchmarkRunner.Run<EFCoreBenchmarks>();

            Console.ReadLine();
        }
    }
}
