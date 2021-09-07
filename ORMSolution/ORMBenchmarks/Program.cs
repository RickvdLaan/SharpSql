using BenchmarkDotNet.Running;
using System;

namespace ORMBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<ORMEntityBenchmark>();
            //BenchmarkRunner.Run<ORMCollectionBenchmark>();
            BenchmarkRunner.Run<NorthwindCollectionBenchmark>();

            Console.ReadLine();
        }
    }
}
