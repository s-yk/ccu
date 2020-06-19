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
    public class TypeOnlyAnalyzer : BaseAnalyzer
    {
        public TypeOnlyAnalyzer() : base(false) { }

        public TypeOnlyAnalyzer(bool verbose) : base(verbose) { }

        public async override Task Analyze(string solutionPath)
        {
            var types = await this.Run(solutionPath);

            if (types != null && types.Count > 0)
            {
                var serialized = JsonSerializer.Serialize(
                    types,
                    new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true, });

                Console.WriteLine(serialized);
            }
        }

        public async Task<List<Dictionary<string, string>>> AnalyzeToDict(string solutionPath)
        {
            var types = await this.Run(solutionPath);
            if (types == null && types.Count == 0) return new List<Dictionary<string, string>>();
            return types.Select(t => t.ToDictionary()).ToList();
        }

        private async Task<HashSet<TypeUsageInfo>> Run(string solutionPath)
        {
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => ConsoleOut(e.Diagnostic.Message);
                ConsoleOut($"Loading solution '{solutionPath}'");

                var solution = await this.OpenSolutionAsync(workspace, solutionPath);
                ConsoleOut($"Finished loading solution '{solutionPath}'");

                var types = new HashSet<TypeUsageInfo>();
                var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                foreach (var prj in solution.Projects)
                {
                    var compilation = await prj.GetCompilationAsync();
                    var syntaxTrees = compilation.SyntaxTrees;
                    foreach (var tree in syntaxTrees)
                    {
                        var root = tree.GetCompilationUnitRoot();
                        var model = compilation.GetSemanticModel(tree);

                        foreach (var namespaceNode in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
                        {
                            foreach (var classNode in namespaceNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
                            {
                                foreach (var node in classNode.DescendantNodes())
                                {
                                    var typeInfo = model.GetTypeInfo(node);
                                    if (typeInfo.Type != null)
                                    {
                                        types.Add(new TypeUsageInfo(prj.Name, namespaceNode.Name.ToString(), classNode.Identifier.Text, typeInfo.Type.ToDisplayString(symbolDisplayFormat)));
                                    }
                                }
                            }
                        }
                    }
                }

                return types;
            }
        }

        private class TypeUsageInfo
        {
            public string Project { get; }

            public string Namespace { get; }

            public string Class { get; }

            public string Type { get; }

            public TypeUsageInfo(string project, string @namespace, string @class, string type)
            {
                this.Project = project;
                this.Namespace = @namespace;
                this.Class = @class;
                this.Type = type;
            }

            public override bool Equals(object obj)
            {
                var o = obj as TypeUsageInfo;
                return o != null && this.Project == o.Project && this.Namespace == o.Namespace && this.Class == o.Class && this.Type == o.Type;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.Project, this.Namespace, this.Class, this.Type);
            }

            public Dictionary<string, string> ToDictionary()
            {
                return new Dictionary<string, string>
                {
                    { nameof(Project), this.Project },
                    { nameof(Namespace), this.Namespace },
                    { nameof(Class), this.Class },
                    { nameof(Type), this.Type },
                };
            }
        }
    }
}
