using BenchmarkDotNet.Running;
using System;

namespace SharpSql.Benchmarks;

class Program
{
    static void Main(string[] _)
    {
        BenchmarkRunner.Run<SharpSqlBenchmarks>();
        BenchmarkRunner.Run<EFCoreBenchmarks>();

        Console.ReadLine();
    }
}