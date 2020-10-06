using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;
using ORM;
using System;

namespace ORMBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting test...");

            BenchmarkRunner.Run<ORMEntityBenchmark>();

            Console.WriteLine("Test finished...");

            Console.ReadLine();
        }
    }
}
