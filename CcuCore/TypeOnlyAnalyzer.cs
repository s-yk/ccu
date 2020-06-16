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
        public async override Task Analyze(string solutionPath)
        {
            await this.Run(solutionPath);
        }

        private async Task Run(string solutionPath)
        {
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
                Console.WriteLine($"Loading solution '{solutionPath}'");

                var solution = await this.OpenSolutionAsync(workspace, solutionPath);
                Console.WriteLine($"Finished loading solution '{solutionPath}'");

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

                if (types.Count > 0)
                {
                    var serialized = JsonSerializer.Serialize(
                        types,
                        new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true, });
                    Console.WriteLine(serialized);
                }
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
        }
    }
}
