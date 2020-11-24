using BenchmarkDotNet.Running;
using System;

namespace ORMBenchmarks
{
    class Program
    {
#pragma warning disable IDE0060 // Remove unused parameter
        static void Main(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            Console.WriteLine("Starting test...");

            BenchmarkRunner.Run<ORMEntityBenchmark>();

            Console.WriteLine("Test finished...");

            Console.ReadLine();
        }
    }
}
