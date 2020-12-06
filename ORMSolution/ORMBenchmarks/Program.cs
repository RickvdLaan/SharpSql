using BenchmarkDotNet.Running;
using System;

namespace ORMBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ORMEntityBenchmark>();

            Console.ReadLine();
        }
    }
}
