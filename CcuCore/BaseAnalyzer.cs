using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CcuCore
{
    public abstract class BaseAnalyzer : IAnalyzer
    {

        private readonly bool _verbose;

        public BaseAnalyzer(bool verbose)
        {
            this._verbose = verbose;
            this.Prepare();
        }

        public abstract Task Analyze(string solutionPath);

        protected virtual void Prepare()
        {
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                ? visualStudioInstances[0]
                : SelectVisualStudioInstance(visualStudioInstances);
            ConsoleOut($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");
            MSBuildLocator.RegisterInstance(instance);
        }

        protected Task<Solution> OpenSolutionAsync(MSBuildWorkspace workspace, string solutionPath)
        {
            var reporter = this._verbose ? new ConsoleProgressReporter() : null;
            return workspace.OpenSolutionAsync(solutionPath, reporter);
        }

        private VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            ConsoleOut("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                ConsoleOut($"Instance {i + 1}");
                ConsoleOut($"    Name: {visualStudioInstances[i].Name}");
                ConsoleOut($"    Version: {visualStudioInstances[i].Version}");
                ConsoleOut($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    if (!_verbose) Console.Clear();
                    return visualStudioInstances[instanceNumber - 1];
                }
                ConsoleOut("Input not accepted, try again.");
            }
        }

        protected void ConsoleOut(string content)
        {
            if (!this._verbose) return;
            Console.WriteLine(content);
        }

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
