using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace CcuCore
{
    public class StandardAnalyzer : BaseAnalyzer
    {
        public StandardAnalyzer(bool verbose) : base(verbose) { }

        public async override Task Analyze(string solutionPath)
        {
            await this.Run(solutionPath);
        }

        private async Task Run(string solutionPath)
        {
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => ConsoleOut(e.Diagnostic.Message);
                ConsoleOut($"Loading solution '{solutionPath}'");

                var solution = await this.OpenSolutionAsync(workspace, solutionPath);
                ConsoleOut($"Finished loading solution '{solutionPath}'");

                var prjs = new List<Project>();
                var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                foreach (var prj in solution.Projects)
                {

                    var project = new Project(prj.Name);

                    var compilation = await prj.GetCompilationAsync();
                    var syntaxTrees = compilation.SyntaxTrees;
                    foreach (var tree in syntaxTrees)
                    {
                        var root = tree.GetCompilationUnitRoot();
                        var model = compilation.GetSemanticModel(tree);

                        foreach (var namespaceNode in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
                        {

                            var @namespace = new Namespace(namespaceNode.Name.ToString());

                            foreach (var classNode in namespaceNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
                            {
                                var @class = new Class(classNode.Identifier.Text);

                                foreach (var node in classNode.DescendantNodes())
                                {
                                    var typeInfo = model.GetTypeInfo(node);
                                    if (typeInfo.Type != null)
                                    {
                                        @class.Types.Add(typeInfo.Type.ToDisplayString(symbolDisplayFormat));
                                    }
                                }

                                if (@class.Types.Count > 0)
                                {
                                    @namespace.Classes.Add(@class);
                                }
                            }

                            if (@namespace.Classes.Count > 0)
                            {
                                project.Namespaces.Add(@namespace);
                            }
                        }
                    }

                    if (project.Namespaces.Count > 0)
                    {
                        prjs.Add(project);
                    }
                }

                if (prjs.Count > 0)
                {
                    var serialized = JsonSerializer.Serialize(
                        prjs,
                        new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true, });
                    Console.WriteLine(serialized);
                }
            }
        }

        private class Project
        {
            public string ProjectName { get; }
            public IList<Namespace> Namespaces { get; }

            public Project(string name)
            {
                this.ProjectName = name;
                this.Namespaces = new List<Namespace>();
            }
        }

        private class Namespace
        {
            public string Name { get; }
            public IList<Class> Classes { get; }

            public Namespace(string name)
            {
                this.Name = name;
                this.Classes = new List<Class>();
            }
        }

        private class Class
        {
            public string Name { get; }
            public ISet<string> Types { get; }

            public Class(string name)
            {
                this.Name = name;
                this.Types = new HashSet<string>();
            }
        }
    }
}
