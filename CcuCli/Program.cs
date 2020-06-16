using CcuCore;
using CommandLine;
using System;
using System.Threading.Tasks;

namespace CcuCli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async opt => { await AnalyzerFactory.Create(opt.Hierarchy).Analyze(opt.SolutionPath); });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
        }
    }
}
