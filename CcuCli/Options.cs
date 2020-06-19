using CommandLine;

namespace CcuCli
{
    internal class Options
    {
        [Option('h', "hierarchy", Required = false)]
        public bool Hierarchy { get; set; }

        [Option('v', "verbose", Required = false)]
        public bool Verbose { get; set; }

        [Value(1, Required = true, HelpText = "Solution Path.")]
        public string SolutionPath { get; set; }
    }
}
