using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Program
{
    private const string Schema = "sts2-base-game-facts/v1";
    private const string ExtractorVersion = "1.0.0";
    private static readonly string[] Categories = ["cards", "relics", "potions", "monsters", "powers", "rooms", "ancients"];
    private static readonly string[] PckLocalizationTables = ["ancients", "cards", "events", "monsters", "potions", "powers", "relics"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0) return Usage("missing command");
            string command = args[0].ToLowerInvariant();
            var options = ParseOptions(args.Skip(1).ToArray());
            return command switch
            {
                "extract" => RunExtract(options),
                "diff" => RunDiff(options),
                "selftest" => RunSelfTest(options),
                _ => Usage($"unknown command: {command}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[sts2-data] error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 2;
        }
    }

    private static int Usage(string error)
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine("commands: extract --source DIR --localization DIR --output DIR --release-info FILE --assembly FILE --pck FILE [--strict]");
        Console.Error.WriteLine("          diff --before DIR --after DIR --output DIR");
        Console.Error.WriteLine("          selftest --fixture DIR");
        return 2;
    }

    private static Dictionary<string, string?> ParseOptions(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"unexpected argument: {args[i]}");
            string key = args[i][2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) result[key] = args[++i];
            else result[key] = null;
        }
        return result;
    }

    private static string Required(Dictionary<string, string?> options, string key)
        => options.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : throw new ArgumentException($"missing --{key}");

    private static int RunExtract(Dictionary<string, string?> options)
    {
        string source = Required(options, "source");
        string localization = Required(options, "localization");
        string output = Required(options, "output");
        string releaseInfo = Required(options, "release-info");
        string assembly = Required(options, "assembly");
        string pck = Required(options, "pck");
        bool strict = options.ContainsKey("strict");
        string ilspyVersion = options.GetValueOrDefault("ilspy-version") ?? "unknown";
        var extractor = new ContentExtractor(source, localization, output, releaseInfo, assembly, pck, ilspyVersion, fixtureMode: false);
        ExtractionResult result = extractor.Run();
        return strict && result.ErrorCount > 0 ? 3 : 0;
    }

    private static int RunSelfTest(Dictionary<string, string?> options)
    {
        string fixture = Required(options, "fixture");
        string output = Path.Combine(Path.GetTempPath(), "sts2-data-extractor-selftest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            string dummyAssembly = Path.Combine(fixture, "fixture-sts2.dll");
            string dummyPck = Path.Combine(fixture, "fixture.pck");
            var extractor = new ContentExtractor(
                Path.Combine(fixture, "source"), Path.Combine(fixture, "localization"), output,
                Path.Combine(fixture, "release_info.json"), dummyAssembly, dummyPck, "fixture", fixtureMode: true);
            ExtractionResult result = extractor.Run();
            if (result.ErrorCount != 0) throw new InvalidOperationException($"fixture emitted {result.ErrorCount} structural errors");
            foreach (string category in Categories)
            {
                JsonArray array = JsonNode.Parse(File.ReadAllText(Path.Combine(output, category + ".json")))!.AsArray();
                if (array.Count == 0) throw new InvalidOperationException($"fixture category is empty: {category}");
            }
            JsonArray cards = JsonNode.Parse(File.ReadAllText(Path.Combine(output, "cards.json")))!.AsArray();
            JsonObject card = cards.Select(n => n!.AsObject()).Single(n => n["entry"]!.GetValue<string>() == "FIXTURE_STRIKE");
            if (card["cost"]?["display"]?.GetValue<string>() != "2(1)c") throw new InvalidOperationException("card cost upgrade was not rendered as 2(1)c");
            JsonArray vars = card["canonicalVars"]!.AsArray();
            JsonObject damage = vars.Select(n => n!.AsObject()).Single(n => n["name"]!.GetValue<string>() == "Damage");
            if (damage["display"]?.GetValue<string>() != "6(9)") throw new InvalidOperationException("damage upgrade was not rendered as 6(9)");
            JsonArray monsters = JsonNode.Parse(File.ReadAllText(Path.Combine(output, "monsters.json")))!.AsArray();
            JsonObject monster = monsters.Select(n => n!.AsObject()).Single();
            JsonObject stateMachine = monster["stateMachine"]!.AsObject();
            if (stateMachine["states"]!.AsArray().Count < 4) throw new InvalidOperationException("monster states were not recovered");
            JsonArray transitions = stateMachine["transitions"]!.AsArray();
            if (!transitions.Any(n => n?["cooldown"]?.GetValue<string>() == "2")) throw new InvalidOperationException("random branch cooldown was not distinguished");
            if (!transitions.Any(n => n?["condition"] is not null)) throw new InvalidOperationException("conditional branch was not recovered");
            Console.WriteLine($"[sts2-data] self-test passed ({result.RecordCount} records)");
            return 0;
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    private static int RunDiff(Dictionary<string, string?> options)
    {
        string before = Required(options, "before");
        string after = Required(options, "after");
        string output = Required(options, "output");
        Directory.CreateDirectory(output);
        var all = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        var markdown = new StringBuilder("# Slay the Spire 2 extracted-data diff\n\n");
        foreach (string category in Categories)
        {
            var left = LoadById(Path.Combine(before, category + ".json"));
            var right = LoadById(Path.Combine(after, category + ".json"));
            string[] added = right.Keys.Except(left.Keys).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            string[] removed = left.Keys.Except(right.Keys).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            string[] changed = left.Keys.Intersect(right.Keys).Where(k => CanonicalForDiff(left[k]) != CanonicalForDiff(right[k])).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            all[category] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["added"] = added, ["removed"] = removed, ["changed"] = changed
            };
            markdown.AppendLine($"## {TitleCase(category)}").AppendLine();
            markdown.AppendLine($"Added: {added.Length}; removed: {removed.Length}; changed: {changed.Length}.").AppendLine();
            AppendIdList(markdown, "Added", added);
            AppendIdList(markdown, "Removed", removed);
            AppendIdList(markdown, "Changed", changed);
        }
        WriteJson(Path.Combine(output, "diff.json"), new SortedDictionary<string, object?>(StringComparer.Ordinal) { ["schema"] = Schema + "/diff", ["categories"] = all });
        File.WriteAllText(Path.Combine(output, "diff.md"), markdown.ToString(), new UTF8Encoding(false));
        return 0;
    }

    private static Dictionary<string, JsonNode> LoadById(string path)
    {
        JsonArray array = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
        return array.Select(n => n!.AsObject()).ToDictionary(n => n["id"]!.GetValue<string>(), n => (JsonNode)n, StringComparer.Ordinal);
    }

    private static string CanonicalForDiff(JsonNode node)
    {
        JsonNode clone = node.DeepClone();
        if (clone is JsonObject record) record.Remove("source");
        RemoveNonSemanticPositions(clone);
        return clone.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void RemoveNonSemanticPositions(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            obj.Remove("line");
            obj.Remove("order");
            foreach (JsonNode? child in obj.Select(kv => kv.Value).ToArray()) RemoveNonSemanticPositions(child);
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array) RemoveNonSemanticPositions(child);
        }
    }

    private static void AppendIdList(StringBuilder md, string title, string[] ids)
    {
        if (ids.Length == 0) return;
        md.AppendLine($"### {title}").AppendLine();
        foreach (string id in ids) md.AppendLine($"- `{id}`");
        md.AppendLine();
    }

    private static void WriteJson(string path, object value)
        => File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions) + "\n", new UTF8Encoding(false));

    private static string TitleCase(string value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);

    private sealed class ContentExtractor
    {
        private readonly string _sourceRoot;
        private readonly string _localizationRoot;
        private readonly string _outputRoot;
        private readonly string _releaseInfo;
        private readonly string _assembly;
        private readonly string _pck;
        private readonly string _ilspyVersion;
        private readonly bool _fixtureMode;
        private readonly List<DiagnosticRecord> _diagnostics = [];
        private readonly Dictionary<string, SourceType> _types = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SortedDictionary<string, string>> _localization = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<SortedDictionary<string, object?>>> _records = new(StringComparer.Ordinal);
        private readonly HashSet<string> _registeredTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<SortedDictionary<string, object?>>> _availabilityReferences = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, string> BaseCategories = new(StringComparer.Ordinal)
        {
            ["CardModel"] = "cards", ["RelicModel"] = "relics", ["PotionModel"] = "potions",
            ["MonsterModel"] = "monsters", ["PowerModel"] = "powers",
            ["AbstractRoom"] = "rooms", ["AncientEventModel"] = "ancients"
        };

        private static readonly Dictionary<string, string> LocalizationTables = new(StringComparer.Ordinal)
        {
            ["cards"] = "cards", ["relics"] = "relics", ["potions"] = "potions",
            ["monsters"] = "monsters", ["powers"] = "powers", ["ancients"] = "ancients"
        };

        public ContentExtractor(string sourceRoot, string localizationRoot, string outputRoot, string releaseInfo, string assembly, string pck, string ilspyVersion, bool fixtureMode)
        {
            _sourceRoot = sourceRoot; _localizationRoot = localizationRoot; _outputRoot = outputRoot;
            _releaseInfo = releaseInfo; _assembly = assembly; _pck = pck; _ilspyVersion = ilspyVersion; _fixtureMode = fixtureMode;
            foreach (string category in Categories) _records[category] = [];
        }

        public ExtractionResult Run()
        {
            Directory.CreateDirectory(_outputRoot);
            LoadLocalization();
            LoadSourceTypes();
            foreach (SourceType type in _types.Values.OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                if (type.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)) continue;
                string? category = Classify(type);
                if (category is null) continue;
                try { _records[category].Add(ExtractRecord(type, category)); }
                catch (Exception ex) { AddError("record-failed", ex.Message, category, type.Entry, type.RelativePath); }
            }
            ValidateCoverage();
            WriteOutputs();
            int errors = _diagnostics.Count(d => d.Severity == "error");
            return new ExtractionResult(_records.Values.Sum(x => x.Count), errors);
        }

        private void LoadLocalization()
        {
            foreach (string table in LocalizationTables.Values.Distinct(StringComparer.Ordinal))
            {
                string path = Path.Combine(_localizationRoot, table + ".json");
                if (!File.Exists(path))
                {
                    _localization[table] = new(StringComparer.Ordinal);
                    AddWarning("localization-table-missing", $"English table is missing: {path}", null, null, path);
                    continue;
                }
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in document.RootElement.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                        values[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? "" : property.Value.GetRawText();
                }
                _localization[table] = values;
            }
        }

        private void LoadSourceTypes()
        {
            foreach (string path in Directory.EnumerateFiles(_sourceRoot, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string text = File.ReadAllText(path);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text, path: path);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (declaration.Parent is not (NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax or CompilationUnitSyntax)) continue;
                    string ns = NamespaceOf(declaration);
                    string fullName = string.IsNullOrEmpty(ns) ? declaration.Identifier.ValueText : ns + "." + declaration.Identifier.ValueText;
                    var sourceType = new SourceType(fullName, ns, declaration.Identifier.ValueText, Slugify(declaration.Identifier.ValueText),
                        NormalizePath(Path.GetRelativePath(_sourceRoot, path)), declaration, tree);
                    _types[fullName] = sourceType;
                }
                string relativePath = NormalizePath(Path.GetRelativePath(_sourceRoot, path));
                bool availabilitySource = Regex.IsMatch(relativePath, @"/(CardPools|RelicPools|PotionPools|Acts|Encounters|Characters)/", RegexOptions.IgnoreCase)
                    || relativePath.EndsWith("/ModelDb.cs", StringComparison.OrdinalIgnoreCase);
                if (availabilitySource)
                {
                    foreach (GenericNameSyntax generic in root.DescendantNodes().OfType<GenericNameSyntax>())
                    {
                        string kind = generic.Identifier.ValueText;
                        if (kind is not ("Card" or "Relic" or "Potion" or "Monster" or "Power" or "AncientEvent") || generic.TypeArgumentList.Arguments.Count != 1) continue;
                        string target = SimpleType(Compact(generic.TypeArgumentList.Arguments[0]));
                        if (!_availabilityReferences.TryGetValue(target, out List<SortedDictionary<string, object?>>? refs))
                            _availabilityReferences[target] = refs = [];
                        ClassDeclarationSyntax? owner = generic.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                        refs.Add(new(StringComparer.Ordinal)
                        {
                            ["kind"] = kind,
                            ["owner"] = owner?.Identifier.ValueText,
                            ["source"] = relativePath,
                            ["line"] = tree.GetLineSpan(generic.Span).StartLinePosition.Line + 1,
                            ["expression"] = Compact(generic.Parent is InvocationExpressionSyntax or MemberAccessExpressionSyntax ? generic.Parent : generic)
                        });
                    }
                }
                if (Path.GetFileName(path).Equals("AbstractModelSubtypes.cs", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (TypeOfExpressionSyntax typeOf in root.DescendantNodes().OfType<TypeOfExpressionSyntax>())
                    {
                        _registeredTypes.Add(Compact(typeOf.Type));
                        _registeredTypes.Add(SimpleType(Compact(typeOf.Type)));
                    }
                }
                foreach (Microsoft.CodeAnalysis.Diagnostic diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Take(20))
                    AddWarning("source-parse", diagnostic.GetMessage(), null, null, NormalizePath(Path.GetRelativePath(_sourceRoot, path)) + ":" + diagnostic.Location.GetLineSpan().StartLinePosition.Line);
            }
        }

        private string? Classify(SourceType type)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            string? current = FirstBaseName(type.Declaration);
            while (!string.IsNullOrEmpty(current) && visited.Add(current))
            {
                if (BaseCategories.TryGetValue(current, out string? category)) return category;
                SourceType? parent = FindType(current, type.Namespace);
                current = parent is null ? null : FirstBaseName(parent.Declaration);
            }
            return null;
        }

        private SourceType? FindType(string simpleOrFull, string currentNamespace)
        {
            string simple = SimpleType(simpleOrFull);
            if (_types.TryGetValue(currentNamespace + "." + simple, out SourceType? local)) return local;
            return _types.Values.FirstOrDefault(t => t.Name == simple);
        }

        private SortedDictionary<string, object?> ExtractRecord(SourceType type, string category)
        {
            string categoryId = category == "rooms" ? "ROOM" : CategoryModelId(category);
            var record = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["id"] = categoryId + "." + type.Entry,
                ["entry"] = type.Entry,
                ["class"] = type.Name,
                ["namespace"] = type.Namespace,
                ["source"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["file"] = type.RelativePath,
                    ["line"] = type.Tree.GetLineSpan(type.Declaration.Identifier.Span).StartLinePosition.Line + 1
                },
                ["baseType"] = FirstBaseName(type.Declaration),
                ["registered"] = category == "rooms" ? null : _registeredTypes.Count == 0 || _registeredTypes.Contains(type.FullName) || _registeredTypes.Contains(type.Name),
                ["classification"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["mock"] = type.Name.StartsWith("Mock", StringComparison.Ordinal) || type.RelativePath.Contains("/Mocks/", StringComparison.OrdinalIgnoreCase),
                    ["deprecated"] = type.Name.Contains("Deprecated", StringComparison.Ordinal) || type.RelativePath.Contains("/Deprecated", StringComparison.OrdinalIgnoreCase)
                },
                ["availabilityReferences"] = _availabilityReferences.TryGetValue(type.Name, out List<SortedDictionary<string, object?>>? refs)
                    ? refs.OrderBy(r => (string?)r["source"], StringComparer.Ordinal).ThenBy(r => Convert.ToInt32(r["line"], CultureInfo.InvariantCulture)).ToList()
                    : [],
                ["localization"] = ExtractLocalization(type.Entry, category),
                ["fields"] = ExtractFields(type),
                ["properties"] = ExtractProperties(type),
                ["constructors"] = ExtractConstructors(type),
                ["canonicalVars"] = ExtractCanonicalVars(type),
                ["methods"] = ExtractMethods(type)
            };
            switch (category)
            {
                case "cards": AddCardFacts(type, record); break;
                case "monsters": AddMonsterFacts(type, record); break;
            }
            return record;
        }

        private SortedDictionary<string, string> ExtractLocalization(string entry, string category)
        {
            var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (!LocalizationTables.TryGetValue(category, out string? table)) return result;
            if (!_localization.TryGetValue(table, out SortedDictionary<string, string>? values)) return result;
            string prefix = entry + ".";
            foreach ((string key, string value) in values)
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) result[key] = value;
            if (result.Count == 0) AddWarning("localization-entry-missing", $"No English localization keys matched {prefix}", category, entry, null);
            return result;
        }

        private static List<SortedDictionary<string, object?>> ExtractFields(SourceType type)
        {
            var result = new List<SortedDictionary<string, object?>>();
            foreach (FieldDeclarationSyntax field in type.Declaration.Members.OfType<FieldDeclarationSyntax>())
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                result.Add(new(StringComparer.Ordinal)
                {
                    ["name"] = variable.Identifier.ValueText,
                    ["type"] = Compact(field.Declaration.Type),
                    ["modifiers"] = string.Join(" ", field.Modifiers.Select(m => m.ValueText)),
                    ["value"] = variable.Initializer is null ? null : Compact(variable.Initializer.Value)
                });
            return result.OrderBy(x => (string)x["name"]!, StringComparer.Ordinal).ToList();
        }

        private static List<SortedDictionary<string, object?>> ExtractProperties(SourceType type)
        {
            var result = new List<SortedDictionary<string, object?>>();
            foreach (PropertyDeclarationSyntax property in type.Declaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                string? expression = property.ExpressionBody is not null ? Compact(property.ExpressionBody.Expression) : null;
                if (expression is null && property.AccessorList is not null)
                    expression = Compact(property.AccessorList);
                result.Add(new(StringComparer.Ordinal)
                {
                    ["name"] = property.Identifier.ValueText,
                    ["type"] = Compact(property.Type),
                    ["modifiers"] = string.Join(" ", property.Modifiers.Select(m => m.ValueText)),
                    ["expression"] = expression
                });
            }
            return result.OrderBy(x => (string)x["name"]!, StringComparer.Ordinal).ToList();
        }

        private static List<SortedDictionary<string, object?>> ExtractConstructors(SourceType type)
        {
            var result = new List<SortedDictionary<string, object?>>();
            foreach (ConstructorDeclarationSyntax constructor in type.Declaration.Members.OfType<ConstructorDeclarationSyntax>())
                result.Add(new(StringComparer.Ordinal)
                {
                    ["parameters"] = constructor.ParameterList.Parameters.Select(Compact).ToArray(),
                    ["initializer"] = constructor.Initializer is null ? null : Compact(constructor.Initializer),
                    ["baseArguments"] = constructor.Initializer?.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) == true
                        ? constructor.Initializer.ArgumentList.Arguments.Select(a => Compact(a.Expression)).ToArray() : [],
                    ["statements"] = constructor.Body?.Statements.Select(Compact).ToArray() ?? []
                });
            return result;
        }

        private List<SortedDictionary<string, object?>> ExtractCanonicalVars(SourceType type)
        {
            PropertyDeclarationSyntax? property = type.Declaration.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.ValueText == "CanonicalVars");
            if (property is null) return [];
            SyntaxNode scope = property.ExpressionBody?.Expression ?? (SyntaxNode?)property.AccessorList ?? property;
            MethodDeclarationSyntax? upgrade = type.Declaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == "OnUpgrade");
            var upgrades = ExtractVarUpgrades(upgrade);
            var result = new List<SortedDictionary<string, object?>>();
            foreach (ObjectCreationExpressionSyntax creation in scope.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = Compact(creation.Type);
                string simpleVarType = SimpleType(typeName);
                if (!simpleVarType.EndsWith("Var", StringComparison.Ordinal)) continue;
                string[] args = creation.ArgumentList?.Arguments.Select(a => Compact(a.Expression)).ToArray() ?? [];
                string name = DynamicVarName(typeName, args);
                string accessorName = DynamicVarAccessorName(typeName, name);
                decimal? baseValue = DynamicVarBase(typeName, args);
                decimal? upgradedValue = baseValue;
                string? upgradeExpression = null;
                if ((upgrades.TryGetValue(accessorName, out VarUpgrade? change) || upgrades.TryGetValue(name, out change)) && baseValue.HasValue)
                {
                    upgradedValue = change.Kind == "by" ? baseValue.Value + change.Value : change.Value;
                    upgradeExpression = change.Expression;
                }
                result.Add(new(StringComparer.Ordinal)
                {
                    ["name"] = name,
                    ["accessor"] = accessorName,
                    ["type"] = typeName,
                    ["arguments"] = args,
                    ["base"] = baseValue,
                    ["upgraded"] = upgradedValue,
                    ["display"] = baseValue.HasValue ? Pair(baseValue.Value, upgradedValue) : null,
                    ["upgradeExpression"] = upgradeExpression,
                    ["expression"] = Compact(creation.Parent is MemberAccessExpressionSyntax or InvocationExpressionSyntax ? creation.Parent : creation)
                });
            }
            return result;
        }

        private static Dictionary<string, VarUpgrade> ExtractVarUpgrades(MethodDeclarationSyntax? method)
        {
            var result = new Dictionary<string, VarUpgrade>(StringComparer.OrdinalIgnoreCase);
            if (method is null) return result;
            foreach (InvocationExpressionSyntax invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                string invoked = access.Name.Identifier.ValueText;
                if (invoked is not ("UpgradeValueBy" or "UpgradeValueTo")) continue;
                string receiver = Compact(access.Expression);
                Match indexed = Regex.Match(receiver, "DynamicVars\\[\\\"(?<name>[^\\\"]+)\\\"\\]");
                string name = indexed.Success ? indexed.Groups["name"].Value : receiver.Split('.').Last();
                ArgumentSyntax? first = invocation.ArgumentList.Arguments.FirstOrDefault();
                if (first is null || !TryDecimal(first.Expression, out decimal value)) continue;
                result[name] = new(invoked == "UpgradeValueBy" ? "by" : "to", value, Compact(invocation));
            }
            return result;
        }

        private static string DynamicVarName(string typeName, string[] args)
        {
            if (args.Length > 0 && TryStringLiteral(args[0], out string? explicitName)) return explicitName!;
            Match power = Regex.Match(typeName, @"PowerVar<(?<name>[^>]+)>");
            if (power.Success) return SimpleType(power.Groups["name"].Value);
            string simple = SimpleType(typeName);
            return Regex.Replace(simple, "Var$", "");
        }

        private static string DynamicVarAccessorName(string typeName, string name)
        {
            return typeName.Contains("PowerVar<", StringComparison.Ordinal) ? Regex.Replace(name, "Power$", "") : name;
        }

        private static decimal? DynamicVarBase(string typeName, string[] args)
        {
            int index = args.Length > 0 && TryStringLiteral(args[0], out _) ? 1 : 0;
            return index < args.Length && TryDecimalText(args[index], out decimal value) ? value : null;
        }

        private static List<SortedDictionary<string, object?>> ExtractMethods(SourceType type)
        {
            return type.Declaration.Members.OfType<MethodDeclarationSyntax>()
                .OrderBy(m => m.Identifier.ValueText, StringComparer.Ordinal)
                .ThenBy(m => m.ParameterList.Parameters.Count)
                .Select(m => MethodRecord(m, type.Tree)).ToList();
        }

        private static SortedDictionary<string, object?> MethodRecord(MethodDeclarationSyntax method, SyntaxTree tree)
        {
            SyntaxNode scope = method.Body ?? (SyntaxNode?)method.ExpressionBody?.Expression ?? method;
            var invocations = scope.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .Where(i => i.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault()?.Ancestors().Contains(method) != false)
                .OrderBy(i => i.SpanStart)
                .Select(i => new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["order"] = i.SpanStart,
                    ["line"] = tree.GetLineSpan(i.Span).StartLinePosition.Line + 1,
                    ["awaited"] = i.Parent is AwaitExpressionSyntax || i.Ancestors().OfType<AwaitExpressionSyntax>().Any(),
                    ["call"] = InvocationName(i),
                    ["expression"] = Compact(i)
                }).ToList();
            return new(StringComparer.Ordinal)
            {
                ["name"] = method.Identifier.ValueText,
                ["returnType"] = Compact(method.ReturnType),
                ["modifiers"] = string.Join(" ", method.Modifiers.Select(m => m.ValueText)),
                ["parameters"] = method.ParameterList.Parameters.Select(Compact).ToArray(),
                ["expressionBody"] = method.ExpressionBody is null ? null : Compact(method.ExpressionBody.Expression),
                ["statements"] = method.Body?.Statements.Select(Compact).ToArray() ?? [],
                ["conditions"] = scope.DescendantNodesAndSelf().OfType<IfStatementSyntax>().Select(i => Compact(i.Condition))
                    .Concat(scope.DescendantNodesAndSelf().OfType<ConditionalExpressionSyntax>().Select(i => Compact(i.Condition))).ToArray(),
                ["invocations"] = invocations
            };
        }

        private void AddCardFacts(SourceType type, SortedDictionary<string, object?> record)
        {
            ConstructorDeclarationSyntax? constructor = type.Declaration.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c => c.Initializer?.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) == true);
            string[] args = constructor?.Initializer?.ArgumentList.Arguments.Select(a => Compact(a.Expression)).ToArray() ?? [];
            string? baseCost = args.ElementAtOrDefault(0);
            string? upgradedCost = baseCost;
            string? costUpgrade = null;
            MethodDeclarationSyntax? onUpgrade = type.Declaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == "OnUpgrade");
            if (onUpgrade is not null)
            {
                foreach (InvocationExpressionSyntax invocation in onUpgrade.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    string name = InvocationName(invocation);
                    string? first = invocation.ArgumentList.Arguments.FirstOrDefault() is { } argument ? Compact(argument.Expression) : null;
                    if (name.EndsWith("UpgradeEnergyCostBy", StringComparison.Ordinal) && TryDecimalText(baseCost, out decimal b) && TryDecimalText(first, out decimal delta))
                    { upgradedCost = FormatNumber(b + delta); costUpgrade = Compact(invocation); }
                    else if (name.EndsWith("UpgradeEnergyCostTo", StringComparison.Ordinal) || name.EndsWith("SetEnergyCostTo", StringComparison.Ordinal))
                    { upgradedCost = first; costUpgrade = Compact(invocation); }
                }
            }
            string? costDisplay = baseCost is null ? null : baseCost + ((upgradedCost is not null && upgradedCost != baseCost) ? "(" + upgradedCost + ")" : "") + "c";
            record["cost"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["base"] = baseCost, ["upgraded"] = upgradedCost, ["display"] = costDisplay, ["upgradeExpression"] = costUpgrade
            };
            record["cardType"] = args.ElementAtOrDefault(1);
            record["rarity"] = args.ElementAtOrDefault(2);
            record["target"] = args.ElementAtOrDefault(3);

            var vars = (List<SortedDictionary<string, object?>>)record["canonicalVars"]!;
            var valueMap = vars.Where(v => v["display"] is string).ToDictionary(v => (string)v["name"]!, v => (string)v["display"]!, StringComparer.OrdinalIgnoreCase);
            var loc = (SortedDictionary<string, string>)record["localization"]!;
            string? template = loc.FirstOrDefault(kv => kv.Key.EndsWith(".description", StringComparison.OrdinalIgnoreCase)).Value;
            record["descriptionTemplate"] = template;
            record["descriptionWithUpgradePairs"] = template is null ? null : RenderDescription(template, valueMap);
        }

        private void AddMonsterFacts(SourceType type, SortedDictionary<string, object?> record)
        {
            MethodDeclarationSyntax? generator = type.Declaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == "GenerateMoveStateMachine");
            if (generator is null)
            {
                AddWarning("monster-state-machine-missing", "Monster does not declare GenerateMoveStateMachine; behavior may be inherited or test-only.", "monsters", type.Entry, type.RelativePath);
                record["stateMachine"] = null;
                return;
            }
            var constants = type.Declaration.Members.OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables)
                .Where(v => v.Initializer is not null)
                .ToDictionary(v => v.Identifier.ValueText, v => Compact(v.Initializer!.Value), StringComparer.Ordinal);
            var states = new List<SortedDictionary<string, object?>>();
            var stateByVariable = new Dictionary<string, SortedDictionary<string, object?>>(StringComparer.Ordinal);
            foreach (VariableDeclaratorSyntax variable in generator.DescendantNodes().OfType<VariableDeclaratorSyntax>().OrderBy(v => v.SpanStart))
            {
                ObjectCreationExpressionSyntax? creation = variable.Initializer?.Value.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
                if (creation is null) continue;
                string kind = SimpleType(Compact(creation.Type));
                if (kind is not ("MoveState" or "RandomBranchState" or "ConditionalBranchState")) continue;
                string[] args = creation.ArgumentList?.Arguments.Select(a => Compact(a.Expression)).ToArray() ?? [];
                var state = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["variable"] = variable.Identifier.ValueText,
                    ["kind"] = kind,
                    ["idExpression"] = args.ElementAtOrDefault(0),
                    ["id"] = ResolveExpression(args.ElementAtOrDefault(0), constants),
                    ["performMethod"] = kind == "MoveState" ? args.ElementAtOrDefault(1) : null,
                    ["intents"] = creation.ArgumentList?.Arguments.Skip(kind == "MoveState" ? 2 : 1)
                        .SelectMany(a => a.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
                        .Where(o => SimpleType(Compact(o.Type)).EndsWith("Intent", StringComparison.Ordinal))
                        .Select(o => new SortedDictionary<string, object?>(StringComparer.Ordinal)
                        {
                            ["type"] = SimpleType(Compact(o.Type)),
                            ["arguments"] = o.ArgumentList?.Arguments.Select(a => Compact(a.Expression)).ToArray() ?? [],
                            ["expression"] = Compact(o)
                        }).ToList() ?? [],
                    ["constructorArguments"] = args
                };
                states.Add(state);
                stateByVariable[variable.Identifier.ValueText] = state;
            }

            var transitions = new List<SortedDictionary<string, object?>>();
            foreach (InvocationExpressionSyntax invocation in generator.DescendantNodes().OfType<InvocationExpressionSyntax>().OrderBy(i => i.SpanStart))
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                string method = access.Name.Identifier.ValueText;
                string from = Compact(access.Expression);
                string[] args = invocation.ArgumentList.Arguments.Select(a => Compact(a.Expression)).ToArray();
                if (method == "AddBranch" && args.Length >= 2)
                {
                    var edge = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["order"] = invocation.SpanStart, ["kind"] = "random", ["from"] = from, ["to"] = args[0], ["arguments"] = args,
                        ["repeatType"] = null, ["maxRepeats"] = null, ["cooldown"] = "0", ["weight"] = "1"
                    };
                    InterpretRandomBranch(args, edge);
                    transitions.Add(edge);
                }
                else if (method == "AddState" && args.Length >= 2)
                {
                    transitions.Add(new(StringComparer.Ordinal)
                    {
                        ["order"] = invocation.SpanStart, ["kind"] = "conditional", ["from"] = from, ["to"] = args[0], ["condition"] = LambdaBody(args[1]), ["arguments"] = args
                    });
                }
            }
            foreach (AssignmentExpressionSyntax assignment in generator.DescendantNodes().OfType<AssignmentExpressionSyntax>().OrderBy(a => a.SpanStart))
            {
                if (assignment.Left is MemberAccessExpressionSyntax left && left.Name.Identifier.ValueText == "FollowUpState")
                    transitions.Add(new(StringComparer.Ordinal)
                    {
                        ["order"] = assignment.SpanStart, ["kind"] = "followUp", ["from"] = Compact(left.Expression), ["to"] = Compact(assignment.Right), ["expression"] = Compact(assignment)
                    });
            }

            string? initial = generator.Body?.Statements.OfType<ReturnStatementSyntax>().LastOrDefault()?.Expression is { } returned ? Compact(returned) : generator.ExpressionBody is null ? null : Compact(generator.ExpressionBody.Expression);
            string? initialState = InitialStateFromExpression(generator.Body?.Statements.OfType<ReturnStatementSyntax>().LastOrDefault()?.Expression ?? generator.ExpressionBody?.Expression);
            transitions = transitions.OrderBy(t => Convert.ToInt32(t["order"], CultureInfo.InvariantCulture)).ToList();
            var moveMethods = new List<SortedDictionary<string, object?>>();
            foreach (SortedDictionary<string, object?> state in states.Where(s => (string?)s["kind"] == "MoveState"))
            {
                string? perform = state["performMethod"] as string;
                if (string.IsNullOrEmpty(perform)) continue;
                string methodName = perform.Split('.').Last();
                MethodDeclarationSyntax? method = type.Declaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == methodName);
                if (method is null)
                {
                    if (!perform.Contains("=>", StringComparison.Ordinal)) AddError("monster-move-method-unresolved", $"Move perform method '{perform}' was not found.", "monsters", type.Entry, type.RelativePath);
                    continue;
                }
                var item = MethodRecord(method, type.Tree);
                item["stateVariable"] = state["variable"];
                item["stateId"] = state["id"];
                moveMethods.Add(item);
            }
            var specialHooks = type.Declaration.Members.OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(SyntaxKind.OverrideKeyword) && m.Identifier.ValueText != "GenerateMoveStateMachine")
                .Select(m => MethodRecord(m, type.Tree)).ToList();
            record["stateMachine"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["initialExpression"] = initial,
                ["initialState"] = initialState,
                ["states"] = states,
                ["transitions"] = transitions,
                ["moveMethods"] = moveMethods,
                ["specialHooks"] = specialHooks,
                ["selectionSemantics"] = "RandomBranchState filters repeat/cooldown-ineligible branches, then performs a weighted RNG choice among eligible branches. ConditionalBranchState evaluates branches in declaration order and selects the first true condition."
            };
        }

        private static void InterpretRandomBranch(string[] args, SortedDictionary<string, object?> edge)
        {
            bool IsRepeat(string text) => text.Contains("MoveRepeatType.", StringComparison.Ordinal);
            if (args.Length == 2)
            {
                if (IsRepeat(args[1])) edge["repeatType"] = args[1]; else { edge["repeatType"] = "MoveRepeatType.CanRepeatXTimes"; edge["maxRepeats"] = args[1]; }
            }
            else if (args.Length == 3)
            {
                if (IsRepeat(args[2])) { edge["cooldown"] = args[1]; edge["repeatType"] = args[2]; }
                else if (IsRepeat(args[1])) { edge["repeatType"] = args[1]; edge["weight"] = args[2]; }
                else { edge["repeatType"] = "MoveRepeatType.CanRepeatXTimes"; edge["maxRepeats"] = args[1]; edge["weight"] = args[2]; }
            }
            else if (args.Length >= 4)
            {
                edge["cooldown"] = args[1];
                if (IsRepeat(args[2])) edge["repeatType"] = args[2]; else { edge["repeatType"] = "MoveRepeatType.CanRepeatXTimes"; edge["maxRepeats"] = args[2]; }
                edge["weight"] = args[3];
            }
        }

        private void ValidateCoverage()
        {
            foreach (string category in Categories)
            {
                int discovered = _types.Values.Count(t => !t.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword) && Classify(t) == category);
                int emitted = _records[category].Count;
                if (discovered != emitted) AddError("coverage-mismatch", $"Discovered {discovered} but emitted {emitted} records.", category, null, null);
                if (emitted == 0) AddError("category-empty", "Required category emitted no records.", category, null, null);
            }
        }

        private void WriteOutputs()
        {
            foreach (string category in Categories)
            {
                List<SortedDictionary<string, object?>> records = _records[category].OrderBy(r => (string)r["id"]!, StringComparer.Ordinal).ToList();
                WriteJson(Path.Combine(_outputRoot, category + ".json"), records);
                File.WriteAllText(Path.Combine(_outputRoot, category + ".md"), RenderMarkdown(category, records), new UTF8Encoding(false));
            }
            var coverage = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach (string category in Categories)
            {
                coverage[category] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["discovered"] = _types.Values.Count(t => !t.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword) && Classify(t) == category),
                    ["emitted"] = _records[category].Count,
                    ["localized"] = _records[category].Count(r => ((SortedDictionary<string, string>)r["localization"]!).Count > 0)
                };
            }
            WriteJson(Path.Combine(_outputRoot, "coverage.json"), coverage);
            var coverageMd = new StringBuilder("# Extraction coverage\n\n| Category | Discovered | Emitted | Localized |\n|---|---:|---:|---:|\n");
            foreach (string category in Categories)
            {
                var item = (SortedDictionary<string, object?>)coverage[category]!;
                coverageMd.AppendLine($"| {category} | {item["discovered"]} | {item["emitted"]} | {item["localized"]} |");
            }
            coverageMd.AppendLine().AppendLine($"Diagnostics: {_diagnostics.Count(d => d.Severity == "error")} errors, {_diagnostics.Count(d => d.Severity == "warning")} warnings.");
            File.WriteAllText(Path.Combine(_outputRoot, "coverage.md"), coverageMd.ToString(), new UTF8Encoding(false));
            WriteJson(Path.Combine(_outputRoot, "diagnostics.json"), _diagnostics.OrderBy(d => d.Severity).ThenBy(d => d.Category).ThenBy(d => d.Id).ThenBy(d => d.Code).ToList());

            JsonNode release = JsonNode.Parse(File.ReadAllText(_releaseInfo)) ?? new JsonObject();
            string assemblyHash = _fixtureMode ? "fixture" : Sha256(_assembly);
            string pckHash = _fixtureMode ? "fixture" : Sha256(_pck);
            var manifest = new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["schema"] = Schema, ["extractorVersion"] = ExtractorVersion, ["release"] = release,
                ["inputs"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["assemblySha256"] = assemblyHash, ["pckSha256"] = pckHash, ["ilspyVersion"] = _ilspyVersion,
                    ["localizationResources"] = PckLocalizationTables.Select(x => $"res://localization/eng/{x}.json").ToArray()
                },
                ["counts"] = Categories.ToDictionary(c => c, c => _records[c].Count, StringComparer.Ordinal),
                ["diagnosticCounts"] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["errors"] = _diagnostics.Count(d => d.Severity == "error"), ["warnings"] = _diagnostics.Count(d => d.Severity == "warning")
                }
            };
            WriteJson(Path.Combine(_outputRoot, "manifest.json"), manifest);
            File.WriteAllText(Path.Combine(_outputRoot, "README.md"), RenderSnapshotReadme(release, coverage), new UTF8Encoding(false));
        }

        private static string RenderMarkdown(string category, List<SortedDictionary<string, object?>> records)
        {
            var md = new StringBuilder($"# {TitleCase(category)}\n\nGenerated static facts. Expressions are preserved when a runtime value cannot be safely evaluated.\n\n");
            foreach (SortedDictionary<string, object?> record in records)
            {
                var loc = (SortedDictionary<string, string>)record["localization"]!;
                string title = loc.FirstOrDefault(kv => kv.Key.EndsWith(".title", StringComparison.OrdinalIgnoreCase) || kv.Key.EndsWith(".name", StringComparison.OrdinalIgnoreCase)).Value ?? (string)record["class"]!;
                md.AppendLine($"## {title} (`{record["id"]}`)").AppendLine();
                if (category == "cards")
                {
                    md.AppendLine($"- Cost: `{((SortedDictionary<string, object?>)record["cost"]!)["display"]}`");
                    md.AppendLine($"- Type: `{record["cardType"]}`; rarity: `{record["rarity"]}`; target: `{record["target"]}`");
                    if (record["descriptionWithUpgradePairs"] is string description) md.AppendLine($"- Effect: {description}");
                }
                else
                {
                    string? description = loc.FirstOrDefault(kv => kv.Key.EndsWith(".description", StringComparison.OrdinalIgnoreCase)).Value;
                    if (description is not null) md.AppendLine(description);
                }
                if (category == "monsters" && record["stateMachine"] is SortedDictionary<string, object?> machine)
                {
                    md.AppendLine($"- Initial state: `{machine["initialState"]}` (from `{machine["initialExpression"]}`)");
                    var states = (List<SortedDictionary<string, object?>>)machine["states"]!;
                    md.AppendLine($"- States: {states.Count}; transitions: {((List<SortedDictionary<string, object?>>)machine["transitions"]!).Count}");
                    foreach (SortedDictionary<string, object?> state in states)
                    {
                        md.Append($"  - `{state["variable"]}`: {state["kind"]} `{state["id"]}`");
                        if (state["performMethod"] is string perform) md.Append($" → `{perform}`");
                        md.AppendLine();
                    }
                    foreach (SortedDictionary<string, object?> edge in (List<SortedDictionary<string, object?>>)machine["transitions"]!)
                    {
                        md.Append($"  - {edge["kind"]}: `{edge["from"]}` → `{edge["to"]}`");
                        if (edge.TryGetValue("condition", out object? condition) && condition is not null) md.Append($" if `{condition}`");
                        if ((string?)edge["kind"] == "random") md.Append($"; cooldown `{edge["cooldown"]}`, repeat `{edge["repeatType"]}`, weight `{edge["weight"]}`");
                        md.AppendLine();
                    }
                    foreach (SortedDictionary<string, object?> move in (List<SortedDictionary<string, object?>>)machine["moveMethods"]!)
                    {
                        md.AppendLine($"- Move `{move["stateId"]}` / `{move["name"]}`:");
                        foreach (string statement in (string[])move["statements"]!) md.AppendLine($"  1. `{statement}`");
                    }
                }
                else
                {
                    var methods = (List<SortedDictionary<string, object?>>)record["methods"]!;
                    string[] hooks = methods.Where(m => ((string)m["modifiers"]!).Contains("override", StringComparison.Ordinal)).Select(m => (string)m["name"]!).ToArray();
                    if (hooks.Length > 0) md.AppendLine($"- Hooks: {string.Join(", ", hooks.Select(h => "`" + h + "`"))}");
                }
                md.AppendLine();
            }
            return md.ToString();
        }

        private static string RenderSnapshotReadme(JsonNode release, SortedDictionary<string, object?> coverage)
        {
            string version = release["version"]?.GetValue<string>() ?? "unknown";
            var md = new StringBuilder($"# Slay the Spire 2 {version} extracted facts\n\nSchema: `{Schema}`. English localization only. Generated by static analysis; consult `diagnostics.json` before treating an unresolved expression as a final runtime value.\n\n");
            md.AppendLine("| Category | Records |").AppendLine("|---|---:|");
            foreach (string category in Categories)
                md.AppendLine($"| [{category}]({category}.md) | {((SortedDictionary<string, object?>)coverage[category]!)["emitted"]} |");
            md.AppendLine().AppendLine("Detailed machine-readable records are in the matching `.json` files. Monster JSON includes state graphs and ordered move statements.");
            return md.ToString();
        }

        private void AddWarning(string code, string message, string? category, string? id, string? source)
            => _diagnostics.Add(new("warning", code, message, category, id, source));
        private void AddError(string code, string message, string? category, string? id, string? source)
            => _diagnostics.Add(new("error", code, message, category, id, source));
    }

    private static string RenderDescription(string template, Dictionary<string, string> values)
    {
        return Regex.Replace(template, @"\{(?<name>[A-Za-z0-9_]+):diff\(\)\}", match =>
            values.TryGetValue(match.Groups["name"].Value, out string? value) ? value : match.Value);
    }

    private static string Pair(decimal baseValue, decimal? upgraded)
    {
        string left = FormatNumber(baseValue);
        return upgraded.HasValue && upgraded.Value != baseValue ? left + "(" + FormatNumber(upgraded.Value) + ")" : left;
    }

    private static string FormatNumber(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    private static bool TryDecimal(ExpressionSyntax expression, out decimal value) => TryDecimalText(Compact(expression), out value);

    private static bool TryDecimalText(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string clean = text.Trim().TrimEnd('m', 'M', 'f', 'F', 'd', 'D');
        if (clean.StartsWith("+", StringComparison.Ordinal)) clean = clean[1..];
        return decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryStringLiteral(string text, out string? value)
    {
        ExpressionSyntax expression = SyntaxFactory.ParseExpression(text);
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        { value = literal.Token.ValueText; return true; }
        value = null; return false;
    }

    private static string ResolveExpression(string? expression, Dictionary<string, string> constants)
    {
        if (expression is null) return "";
        if (constants.TryGetValue(expression, out string? resolved)) expression = resolved;
        return TryStringLiteral(expression, out string? value) ? value! : expression;
    }

    private static string LambdaBody(string expression)
    {
        int arrow = expression.IndexOf("=>", StringComparison.Ordinal);
        return arrow >= 0 ? expression[(arrow + 2)..].Trim() : expression;
    }

    private static string? InitialStateFromExpression(ExpressionSyntax? expression)
    {
        if (expression is ObjectCreationExpressionSyntax creation && creation.ArgumentList?.Arguments.LastOrDefault() is { } last)
            return Compact(last.Expression);
        if (expression is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "AsMachine")
            return Compact(member.Expression);
        return expression is null ? null : Compact(expression);
    }

    private static string InvocationName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => Compact(member.Name),
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => Compact(invocation.Expression)
        };
    }

    private static string NamespaceOf(SyntaxNode node)
    {
        return string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));
    }

    private static string? FirstBaseName(ClassDeclarationSyntax declaration)
        => declaration.BaseList?.Types.FirstOrDefault() is { } baseType ? SimpleType(Compact(baseType.Type)) : null;

    private static string SimpleType(string value)
    {
        string simple = value.Split('.').Last();
        int generic = simple.IndexOf('<');
        return generic >= 0 ? simple[..generic] : simple.TrimEnd('?');
    }

    private static string Slugify(string text)
    {
        string separated = Regex.Replace(text.Trim(), @"([A-Za-z0-9]|\G(?!^))([A-Z])", "$1_$2");
        separated = Regex.Replace(separated.ToUpperInvariant(), @"\s+", "_");
        return Regex.Replace(separated, @"[^A-Z0-9_]", "");
    }

    private static string CategoryModelId(string category) => category switch
    {
        "cards" => "CARD", "relics" => "RELIC", "potions" => "POTION", "monsters" => "MONSTER",
        "powers" => "POWER", "ancients" => "ANCIENT_EVENT", _ => category.ToUpperInvariant()
    };

    private static string Compact(SyntaxNode node) => Regex.Replace(node.ToString(), @"\s+", " ").Trim();
    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record SourceType(string FullName, string Namespace, string Name, string Entry, string RelativePath, ClassDeclarationSyntax Declaration, SyntaxTree Tree);
    private sealed record VarUpgrade(string Kind, decimal Value, string Expression);
    private sealed record DiagnosticRecord(string Severity, string Code, string Message, string? Category, string? Id, string? Source);
    private sealed record ExtractionResult(int RecordCount, int ErrorCount);
}
