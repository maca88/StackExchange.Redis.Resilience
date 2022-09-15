using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace StackExchange.Redis.Resilience.SourceGenerator
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();
            var workspace = CreateWorkspace("net5.0");
            var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../StackExchange.Redis.Resilience/", "StackExchange.Redis.Resilience.csproj"));
            var project = await workspace.OpenProjectAsync(projectPath, null, CancellationToken.None);
            CheckForErrors(workspace);

            project = project.RemoveDocuments(project.Documents.Where(o => string.Join("/", o.Folders) == "Generated").Select(o => o.Id).ToImmutableArray());
            var compilation = await project.GetCompilationAsync(CancellationToken.None);
            var redisAssembly = compilation!.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .First(o => o.Name == "StackExchange.Redis");

            project = GenerateResilientClass(project, compilation, redisAssembly, "StackExchange.Redis.ISubscriber", false);
            project = GenerateResilientClass(project, compilation, redisAssembly, "StackExchange.Redis.IServer", false);
            project = GenerateResilientClass(project, compilation, redisAssembly, "StackExchange.Redis.IDatabase", false);
            project = GenerateResilientClass(project, compilation, redisAssembly, "StackExchange.Redis.IConnectionMultiplexer", true, "_connectionMultiplexer");

            await ApplyChangesAsync(workspace, project.Solution);

            return 0;
        }

        private static Project GenerateResilientClass(
            Project project,
            Compilation compilation,
            IAssemblySymbol redisAssembly,

            string interfaceFullName,
            bool onlyBody,
            string instancePath = null)
        {
            instancePath ??= "_instance!.Value";
            var interfaceSymbol = redisAssembly.GetTypeByMetadataName(interfaceFullName);
            var name = interfaceSymbol!.Name.TrimStart('I');
            var template = onlyBody
                ?
@$"using System;
using System.Threading.Tasks;

#nullable enable

namespace StackExchange.Redis.Resilience
{{
    public partial class Resilient{name}
    {{
[BODY]
    }}
}}
"
                :
@$"using System;
using System.Threading.Tasks;

#nullable enable

namespace StackExchange.Redis.Resilience
{{
    internal partial class Resilient{name}
    {{
        private readonly ResilientConnectionMultiplexer _resilientConnectionMultiplexer;
        private readonly Func<{interfaceFullName}> _instanceProvider;
        private readonly object _resetLock = new object();
        private AtomicLazy<{interfaceFullName}>? _instance;
        private long _lastReconnectTicks;

        public Resilient{name}(ResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<{interfaceFullName}> instanceProvider)
        {{
            _resilientConnectionMultiplexer = resilientConnectionMultiplexer;
            _instanceProvider = instanceProvider;
            _lastReconnectTicks = resilientConnectionMultiplexer.LastReconnectTicks;
            ResetInstance();
        }}

        /// <inheritdoc />
        public IConnectionMultiplexer Multiplexer => _resilientConnectionMultiplexer;
[BODY]

        private void ResetInstance()
        {{
            _instance = new AtomicLazy<{interfaceFullName}>(_instanceProvider);
        }}

        private void CheckAndReset()
        {{
            ResilientConnectionMultiplexer.CheckAndReset(
                _resilientConnectionMultiplexer.LastReconnectTicks,
                ref _lastReconnectTicks,
                _resetLock,
                ResetInstance);
        }}

        private T ExecuteAction<T>(Func<T> action)
        {{
            return ResilientConnectionMultiplexer.ExecuteAction(_resilientConnectionMultiplexer, () =>
            {{
                CheckAndReset();
                return action();
            }});
        }}

        private Task<T> ExecuteActionAsync<T>(Func<Task<T>> action)
        {{
            return ResilientConnectionMultiplexer.ExecuteActionAsync(_resilientConnectionMultiplexer, () =>
            {{
                CheckAndReset();
                return action();
            }});
        }}

        private Task ExecuteActionAsync(Func<Task> action)
        {{
            return ResilientConnectionMultiplexer.ExecuteActionAsync(_resilientConnectionMultiplexer, () =>
            {{
                CheckAndReset();
                return action();
            }});
        }}

        private void ExecuteAction(Action action)
        {{
            ResilientConnectionMultiplexer.ExecuteAction(_resilientConnectionMultiplexer, () =>
            {{
                CheckAndReset();
                action();
            }});
        }}
    }}
}}
";
            var resilientType = compilation.Assembly.GetTypeByMetadataName($"StackExchange.Redis.Resilience.Resilient{name}");
            var bodyBuilder = new StringBuilder();
            foreach (var member in interfaceSymbol.GetMembers().Concat(interfaceSymbol.AllInterfaces.SelectMany(o => o.GetMembers())))
            {
                if (resilientType!.FindImplementationForInterfaceMember(member) != null)
                {
                    continue;
                }

                if (member is IMethodSymbol methodSymbol && methodSymbol.MethodKind != MethodKind.PropertyGet && methodSymbol.MethodKind != MethodKind.PropertySet)
                {
                    PrependAttributes(member, bodyBuilder);
                    bodyBuilder.Append(GenerateMethod(methodSymbol, instancePath));
                }
                else if (member is IPropertySymbol propertySymbol && member.Name != "Multiplexer")
                {
                    PrependAttributes(member, bodyBuilder);
                    bodyBuilder.Append(GenerateProperty(propertySymbol, instancePath));
                }
            }

            var documentName = $"Resilient{name}.g.cs";
            return project.AddDocument(
                documentName,
                template.Replace("[BODY]", bodyBuilder.ToString()),
                new[] { "Generated" },
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.FilePath), $"Generated/{documentName}"))).Project;
        }

        private static void PrependAttributes(ISymbol member, StringBuilder bodyBuilder)
        {
            var obsoleteAttribute = member.GetAttributes().FirstOrDefault(o => o.AttributeClass?.Name == "ObsoleteAttribute");
            bodyBuilder.Append(Environment.NewLine);
            bodyBuilder.Append(Environment.NewLine);
            bodyBuilder.Append("        /// <inheritdoc />");
            bodyBuilder.Append(Environment.NewLine);
            if (obsoleteAttribute != null)
            {
                bodyBuilder.Append($"        [{obsoleteAttribute}]");
                bodyBuilder.Append(Environment.NewLine);
            }

            bodyBuilder.Append($"        ");
        }

        private static string GenerateProperty(IPropertySymbol propertySymbol, string instancePath)
        {
            var definition = $@"public {propertySymbol.Type.ToDisplayString()} {propertySymbol.Name}";
            if (propertySymbol.SetMethod != null)
            {
                return $@"{definition}
        {{
            get => {instancePath}.{propertySymbol.Name};
            set => {instancePath}.{propertySymbol.Name} = value;
        }}";
            }

            return $@"{definition} => {instancePath}.{propertySymbol.Name};";
        }

        private static string GenerateMethod(IMethodSymbol methodSymbol, string instancePath)
        {
            var returnType = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", methodSymbol.Parameters.Select(o =>
            {
                var type = o.Type.ToDisplayString();
                if (!type.EndsWith("?") && o.HasExplicitDefaultValue && !o.Type.IsValueType)
                {
                    type += "?";
                }

                var p = $"{type} {o.Name}";
                if (o.HasExplicitDefaultValue)
                {
                    var value = o.ExplicitDefaultValue;
                    if (o.Type.TypeKind == TypeKind.Enum)
                    {
                        var field = o.Type.GetMembers().OfType<IFieldSymbol>().First(f => f.ConstantValue!.Equals(o.ExplicitDefaultValue));
                        value = $"{o.Type.ToDisplayString()}.{field.Name}";
                    }
                    else if (o.ExplicitDefaultValue is bool boolean)
                    {
                        value = boolean ? "true" : "false";
                    }
                    else if (o.ExplicitDefaultValue is double doubleValue)
                    {
                        value = double.PositiveInfinity.Equals(doubleValue) ? "double.PositiveInfinity"
                            : double.NegativeInfinity.Equals(doubleValue) ? "double.NegativeInfinity"
                            : doubleValue.ToString(CultureInfo.InvariantCulture);
                    }

                    p += $" = {value ?? "default"}";
                }

                return p;
            }));
            var actionName = methodSymbol.Name.EndsWith("Async") && !methodSymbol.ReturnType.Name.StartsWith("IAsyncEnumerable") ? "return ExecuteActionAsync"
                : !methodSymbol.ReturnsVoid ? "return ExecuteAction"
                : "ExecuteAction";
            var arguments = string.Join(", ", methodSymbol.Parameters.Select(o => o.Name));
            var typeArguments = methodSymbol.IsGenericMethod
                ? $"<{string.Join(", ", methodSymbol.TypeArguments.Select(o => o.Name))}>"
                : null;

            return $@"public {returnType} {methodSymbol.Name}{typeArguments}({parameters})
        {{
            {actionName}(() => {instancePath}.{methodSymbol.Name}{typeArguments}({arguments}));
        }}";
        }

        private static MSBuildWorkspace CreateWorkspace(string targetFramework)
        {
            var props = new Dictionary<string, string>
            {
                ["CheckForSystemRuntimeDependency"] = "true" // needed in order that project references are loaded
            };
            if (!string.IsNullOrEmpty(targetFramework))
            {
                props["TargetFramework"] = targetFramework;
            }

            return MSBuildWorkspace.Create(props);
        }

        private static void CheckForErrors(MSBuildWorkspace workspace)
        {
            // Throw if any failure
            var failures = workspace.Diagnostics
                .Where(o => o.Kind == WorkspaceDiagnosticKind.Failure)
                .Select(o => o.Message)
                .ToList();
            if (failures.Any())
            {
                var message =
                    $"One or more errors occurred while opening project:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}{Environment.NewLine}" +
                    "Hint: For suppressing irrelevant errors use SuppressDiagnosticFailures option.";
                throw new InvalidOperationException(message);
            }
        }

        private static async Task ApplyChangesAsync(Workspace workspace, Solution solution,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var changes = solution.GetChanges(workspace.CurrentSolution);
            var newSolution = workspace.CurrentSolution;

            // Apply changes manually as the AddDocument and RemoveDocument methods do not play well with the new csproj format
            // Problems with AddDocument and RemoveDocument methods:
            // - When an imported document is removed in a new csproj, TryApplyChanges will throw because the file was imported by a glob
            // - When a document is added in a new csproj, the file will be explicitly added in the csproj even if there is a glob that could import it
            foreach (var projectChanges in changes.GetProjectChanges())
            {
                var xml = new XmlDocument();
                xml.Load(projectChanges.NewProject.FilePath);
                var isNewCsproj = xml.DocumentElement?.GetAttribute("Sdk") == "Microsoft.NET.Sdk";

                var addedDocuments = projectChanges
                    .GetAddedDocuments()
                    .Select(o => projectChanges.NewProject.GetDocument(o))
                    .ToDictionary(o => o.FilePath);
                var removedDocuments = projectChanges
                    .GetRemovedDocuments()
                    .Select(o => projectChanges.OldProject.GetDocument(o))
                    .ToDictionary(o => o.FilePath);

                // Add new documents or replace the document text if it was already there
                foreach (var addedDocumentPair in addedDocuments)
                {
                    var addedDocument = addedDocumentPair.Value;
                    if (removedDocuments.ContainsKey(addedDocumentPair.Key))
                    {
                        var removedDocument = removedDocuments[addedDocumentPair.Key];
                        newSolution = newSolution.GetDocument(removedDocument.Id)
                            .WithText(await addedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false))
                            .Project.Solution;
                        continue;
                    }

                    var sourceText = await addedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    // For new csproj format we don't want to explicitly add the document as they are imported by default
                    if (isNewCsproj)
                    {
                        var dirPath = Path.GetDirectoryName(addedDocument.FilePath);
                        Directory.CreateDirectory(dirPath); // Create all directories if not exist
                        using (var writer = new StreamWriter(addedDocument.FilePath, false, Encoding.UTF8))
                        {
                            sourceText.Write(writer, cancellationToken);
                        }
                    }
                    else
                    {
                        var newProject = newSolution.GetProject(projectChanges.ProjectId);
                        newSolution = newProject.AddDocument(
                                addedDocument.Name,
                                sourceText,
                                addedDocument.Folders,
                                addedDocument.FilePath)
                            .Project.Solution;
                    }
                }

                // Remove documents that are not generated anymore
                foreach (var removedDocumentPair in removedDocuments.Where(o => !addedDocuments.ContainsKey(o.Key)))
                {
                    var removedDocument = removedDocumentPair.Value;
                    // For new csproj format we cannot remove a document as they are imported by globs (RemoveDocument throws an exception for new csproj format)
                    if (!isNewCsproj)
                    {
                        newSolution = newSolution.RemoveDocument(removedDocument.Id);
                    }

                    File.Delete(removedDocument.FilePath);
                }

                // Update changed documents
                foreach (var documentId in projectChanges.GetChangedDocuments())
                {
                    var newDocument = projectChanges.NewProject.GetDocument(documentId);
                    newSolution = newSolution.GetDocument(documentId)
                        .WithText(await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false))
                        .Project.Solution;
                }
            }

            workspace.TryApplyChanges(newSolution);
        }
    }
}
