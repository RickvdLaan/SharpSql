using BenchmarkDotNet.Running;
using System;

namespace ORMBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting test...");

            BenchmarkRunner.Run<ORMEntityBenchmark>();
            BenchmarkRunner.Run<ORMEntityMappingBenchmark>();

            Console.WriteLine("Test finished...");

            Console.ReadLine();
        }
    }
}
