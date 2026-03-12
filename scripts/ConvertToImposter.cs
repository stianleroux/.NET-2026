#!/usr/bin/env dotnet
#:property TargetFramework=net10.0
#:property PublishAot=false
#:property Nullable=enable
#:property ImplicitUsings=enable

// ┌─────────────────────────────────────────────────────────────────────────┐
// │                     ConvertToImposter.cs                                │
// │          NSubstitute → Imposter migration script                        │
// │                                                                         │
// │  Run:  dotnet ConvertToImposter.cs [path-to-test-project]               │
// │   or:  dotnet run ConvertToImposter.cs -- [path-to-test-project]        │
// │                                                                         │
// │  Defaults to: ../CloudPizza/src/CloudPizza.Tests                        │
// │                                                                         │
// │  What this script does:                                                 │
// │  ✓ using NSubstitute  → using Imposter.Abstractions                     │
// │  ✓ Substitute.For<T>()→ T.Imposter() + .Instance() split               │
// │  ✓ Arg.Any<T>()       → Arg<T>.Any()                                   │
// │  ✓ Arg.Is<T>(x)       → Arg<T>.Is(x)                                   │
// │  ✓ Arg.Is("str")      → Arg<string>.Is("str")                          │
// │  ✓ .Returns(a, b, c)  → .Returns(a).Then().Returns(b).Then().Returns(c)│
// │  ✓ .Returns(p => p[0])→ .Returns(p => p)  (single-arg callback)        │
// │  ✓ .Received(n)       → TODO comment + Imposter equivalent hint         │
// │  ✓ .DidNotReceive()   → TODO comment + Imposter equivalent hint         │
// │  ✓ Updates .csproj: removes NSubstitute, adds Imposter package          │
// │  ✓ Creates AssemblyImposterAttributes.cs with GenerateImposter attrs    │
// │                                                                         │
// │  Imposter docs: https://themidnightgospel.github.io/Imposter/latest/   │
// │  File-based apps: https://learn.microsoft.com/dotnet/core/sdk/          │
// │                   file-based-apps                                       │
// └─────────────────────────────────────────────────────────────────────────┘

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// ─────────────────────────────────────────────────────────────────────────────
// Resolve target path
// ─────────────────────────────────────────────────────────────────────────────

var cwd = Directory.GetCurrentDirectory();
var defaultTestPath = Path.GetFullPath(Path.Combine(cwd, "..", "CloudPizza", "src", "CloudPizza.Tests"));

if (!Directory.Exists(defaultTestPath))
    defaultTestPath = Path.GetFullPath(Path.Combine(cwd, "src", "CloudPizza.Tests"));

var testProjectPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : defaultTestPath;

// ─────────────────────────────────────────────────────────────────────────────
// Validation
// ─────────────────────────────────────────────────────────────────────────────

if (!Directory.Exists(testProjectPath))
{
    WriteColor(ConsoleColor.Red,
        $"❌  Test project not found: {testProjectPath}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "    Usage: dotnet ConvertToImposter.cs [path-to-test-project]");
    Console.Error.WriteLine(
        "    Example: dotnet ConvertToImposter.cs ../CloudPizza/src/CloudPizza.Tests");
    return 1;
}

// ─────────────────────────────────────────────────────────────────────────────
// Banner
// ─────────────────────────────────────────────────────────────────────────────

WriteColor(ConsoleColor.Cyan, """
╔══════════════════════════════════════════════════════════╗
║         NSubstitute  →  Imposter  Converter              ║
║         .NET 10 file-based app                           ║
╚══════════════════════════════════════════════════════════╝
""");

Console.WriteLine($"📂 Target: {testProjectPath}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// State tracking
// ─────────────────────────────────────────────────────────────────────────────

var mockedTypes   = new SortedSet<string>();  // collects all T from Substitute.For<T>()
var changeLog     = new List<(string file, List<string> notes)>();
int totalFiles    = 0;
int totalChanges  = 0;

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

static void WriteColor(ConsoleColor color, string text)
{
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ResetColor();
}

// Splits "a, b, c" respecting strings, nested parens, and generic brackets.
static List<string> SplitArgs(string input)
{
    var parts   = new List<string>();
    var current = new StringBuilder();
    int depth   = 0;
    bool inStr  = false;
    char strCh  = '"';

    for (int i = 0; i < input.Length; i++)
    {
        char c = input[i];

        if (inStr)
        {
            current.Append(c);
            if (c == strCh && (i == 0 || input[i - 1] != '\\'))
                inStr = false;
        }
        else if (c is '"' or '\'')
        {
            inStr = true;
            strCh = c;
            current.Append(c);
        }
        else if (c is '(' or '[' or '{')
        {
            depth++;
            current.Append(c);
        }
        else if (c is ')' or ']' or '}')
        {
            depth--;
            current.Append(c);
        }
        else if (c == ',' && depth == 0)
        {
            parts.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }

    if (current.Length > 0)
        parts.Add(current.ToString());

    return parts;
}

// Reads the balanced content of a Returns(...) invocation starting AFTER the '('.
// Returns (args string, total chars consumed including closing paren).
static (string args, int length) ExtractReturnArgs(string text, int openParenPos)
{
    int depth = 1;
    bool inStr = false;
    char strCh = '"';
    int i = openParenPos + 1;

    for (; i < text.Length && depth > 0; i++)
    {
        char c = text[i];
        if (inStr)
        {
            if (c == strCh && (i == 0 || text[i - 1] != '\\'))
                inStr = false;
        }
        else if (c is '"' or '\'')
        {
            inStr = true;
            strCh = c;
        }
        else if (c is '(' or '[' or '{')
        {
            depth++;
        }
        else if (c is ')' or ']' or '}')
        {
            depth--;
        }
    }

    // i is now one past the closing ')'
    var args = text.Substring(openParenPos + 1, i - openParenPos - 2);
    return (args, i - openParenPos); // length includes both parens
}

// ─────────────────────────────────────────────────────────────────────────────
// CS file processing
// ─────────────────────────────────────────────────────────────────────────────

var csFiles = Directory
    .GetFiles(testProjectPath, "*.cs", SearchOption.AllDirectories)
    .Where(f =>
        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .OrderBy(f => f)
    .ToArray();

Console.WriteLine($"🔍 Scanning {csFiles.Length} C# files...");
Console.WriteLine();

foreach (var filePath in csFiles)
{
    var original = File.ReadAllText(filePath, Encoding.UTF8);
    var content  = original;
    var notes    = new List<string>();

    // ── helper: record and apply a simple string replacement ─────────────────
    void Replace(string from, string to, string description)
    {
        if (!content.Contains(from)) return;
        int count = (content.Length - content.Replace(from, "").Length) / from.Length;
        content = content.Replace(from, to);
        notes.Add($"  [{count}x] {description}");
    }

    // ── helper: record and apply a regex replacement ──────────────────────────
    void ReplaceRegex(string pattern, string replacement, string description,
                      RegexOptions opts = RegexOptions.None)
    {
        var matches = Regex.Matches(content, pattern, opts);
        if (matches.Count == 0) return;
        content = Regex.Replace(content, pattern, replacement, opts);
        notes.Add($"  [{matches.Count}x] {description}");
    }

    // ── 1. using NSubstitute → using Imposter.Abstractions ───────────────────
    Replace("using NSubstitute;",
            "using Imposter.Abstractions;",
            "using NSubstitute → using Imposter.Abstractions");

    // ── 2. NSubstitute in doc-comments ───────────────────────────────────────
    // Only inside XML doc / line-comment strings referencing NSubstitute
    ReplaceRegex(
        @"(///.*?)NSubstitute",
        "$1Imposter",
        "NSubstitute mention in doc-comment → Imposter",
        RegexOptions.Multiline);

    // ── 3. Substitute.For<T>() → T.Imposter() + instance split ──────────────
    //      Pattern: var <name> = Substitute.For<T>();
    //      Becomes: var <name> = T.Imposter();
    //               var <name>Instance = <name>.Instance(); // pass to SUT
    //
    //  Also handles: new T declarations without var.
    //  Collects T into mockedTypes for the GenerateImposter attributes file.
    content = Regex.Replace(
        content,
        @"(\bSubstitute\.For<([^>]+)>\(\))",
        m =>
        {
            var typeArg = m.Groups[2].Value.Trim();
            // Extract just the simple type name for C# 14 static method syntax.
            // For fully-qualified names like Microsoft.Extensions.Logging.ILogger
            // the generated imposter is accessed via the simple name.
            var shortName = typeArg.Contains('.') ? typeArg.Split('.').Last() : typeArg;
            mockedTypes.Add(typeArg);
            notes.Add($"  [1x] Substitute.For<{typeArg}>() → {shortName}.Imposter()");
            return $"{shortName}.Imposter()";
        });

    // After converting the creation line, inject the .Instance() split.
    // Matches:  var <name> = <Type>.Imposter();
    // Appends:  var <name>Instance = <name>.Instance(); // inject into SUT
    // Idempotent: skips injection if the Instance line already exists in the file.
    content = Regex.Replace(
        content,
        @"^(\s*)(var\s+(\w+)\s*=\s*\w+\.Imposter\(\)\s*;)",
        m =>
        {
            var indent    = m.Groups[1].Value;
            var original2 = m.Groups[2].Value;
            var varName   = m.Groups[3].Value;

            // Guard: don't inject if already present (makes the script idempotent)
            if (Regex.IsMatch(content,
                    $@"\bvar\s+{Regex.Escape(varName)}Instance\s*=\s*{Regex.Escape(varName)}\.Instance\(\)"))
                return m.Value;

            var suffix = varName.Replace("mock", "").Replace("Mock", "").TrimStart('_');
            notes.Add($"  [1x] Added {varName}Instance = {varName}.Instance() split");
            return $"{indent}{original2}\n{indent}var {varName}Instance = {varName}.Instance();" +
                   $" // ← pass {varName}Instance where {(suffix.Length > 0 ? suffix : varName)} is consumed (Act step)";
        },
        RegexOptions.Multiline);

    // ── 4. Arg.Any<T>() → Arg<T>.Any() ──────────────────────────────────────
    ReplaceRegex(
        @"Arg\.Any<([^>]+)>\(\)",
        "Arg<$1>.Any()",
        "Arg.Any<T>() → Arg<T>.Any()");

    // ── 5. Arg.Is<T>(x) → Arg<T>.Is(x) ─────────────────────────────────────
    ReplaceRegex(
        @"Arg\.Is<([^>]+)>\(",
        "Arg<$1>.Is(",
        "Arg.Is<T>(...) → Arg<T>.Is(...)");

    // ── 6. Arg.Is("string") → Arg<string>.Is("string")  (no generic given) ──
    Replace(
        @"Arg.Is(""",
        @"Arg<string>.Is(""",
        @"Arg.Is(""..."") → Arg<string>.Is(""..."")");

    // ── 7. Multi-value .Returns(a, b, c) → .Returns(a).Then().Returns(b)…  ──
    //  Use balanced-paren scanner to avoid breaking lambda / nested-call Returns.
    var returnsPattern = new Regex(@"\.Returns\(", RegexOptions.Compiled);
    var sbContent      = new StringBuilder(content.Length);
    int lastPos        = 0;
    bool multiConverted = false;

    foreach (Match rm in returnsPattern.Matches(content))
    {
        var openParen = rm.Index + rm.Length - 1;      // position of '('
        var (argsStr, consumed) = ExtractReturnArgs(content, openParen);

        // Only process if there are multiple args and none is a lambda (=>)
        if (!argsStr.Contains(',') || argsStr.Contains("=>"))
        {
            sbContent.Append(content, lastPos, rm.Index - lastPos + rm.Length);
            lastPos = rm.Index + rm.Length;
            // append original content up to but not including the (
            continue;
        }

        var parts = SplitArgs(argsStr);
        if (parts.Count < 2)
        {
            sbContent.Append(content, lastPos, rm.Index - lastPos + rm.Length);
            lastPos = rm.Index + rm.Length;
            continue;
        }

        // Emit text up to the .Returns(
        sbContent.Append(content, lastPos, rm.Index - lastPos);

        // Build chained form
        sbContent.Append(".Returns(");
        sbContent.Append(parts[0].Trim());
        sbContent.Append(')');
        for (int pi = 1; pi < parts.Count; pi++)
        {
            sbContent.Append(".Then().Returns(");
            sbContent.Append(parts[pi].Trim());
            sbContent.Append(')');
        }

        lastPos = openParen + consumed;
        multiConverted = true;
    }

    if (multiConverted || lastPos > 0)
    {
        sbContent.Append(content, lastPos, content.Length - lastPos);
        var newContent = sbContent.ToString();
        if (newContent != content)
        {
            int count = Regex.Matches(content, @"\.Returns\(([^)]+,[^)]+)\)").Count;
            notes.Add($"  [{Math.Max(1, count)}x] Multi-value .Returns(a,b,c) → chained .Then().Returns()");
            content = newContent;
        }
    }

    // ── 8. Single-arg callback: .Returns(p => …p[0]…) → .Returns(p => …p…) ─
    //  NSubstitute: p[0] is first argument via CallInfo indexer
    //  Imposter:    delegate receives the argument value directly
    content = Regex.Replace(
        content,
        @"\.Returns\((\w+)\s*=>\s*(.+?)\)",
        m =>
        {
            var param = m.Groups[1].Value;
            var body  = m.Groups[2].Value;
            var newBody = Regex.Replace(body, Regex.Escape(param) + @"\[0\]", param);
            if (newBody == body) return m.Value; // no [0] found, skip
            notes.Add($"  [1x] Callback .Returns({param} => …{param}[0]…) → .Returns({param} => …{param}…)");
            return $".Returns({param} => {newBody})";
        });

    // ── 9. .Received(n).Method(…) → TODO hint ────────────────────────────────
    //  NSubstitute: mock.Received(1).DoThing(arg);
    //  Imposter:    imposter.DoThing(arg).Called(Count.Once());  ← manual step
    //  Uses (?m)^(?!\s*//) so already-commented lines are skipped (idempotent).
    content = Regex.Replace(
        content,
        @"(?m)^(\s*)(\w+)\.Received\((\d+)\)\.(\w+)\(([^)]*)\)\s*;",
        m =>
        {
            var indent     = m.Groups[1].Value;
            var varName    = m.Groups[2].Value;
            var count      = m.Groups[3].Value;
            var method     = m.Groups[4].Value;
            var methodArgs = m.Groups[5].Value;
            var countExpr  = count == "1" ? "Count.Once()" : $"Count.Exactly({count})";

            // Idempotent: skip if already preceded by our TODO comment
            var full = m.Value;
            var todoMark = $"// TODO[Imposter]: {varName}.{method}";
            if (content.Contains(todoMark))
                return full;

            notes.Add($"  [1x] .Received({count}).{method}() → TODO: .{method}().Called({countExpr})");
            return
                $"{indent}// TODO[Imposter]: {varName}.{method}({methodArgs}).Called({countExpr});\n" +
                $"{indent}// {varName}.Received({count}).{method}({methodArgs}); // ← remove; replace with line above";
        });

    // ── 10. .DidNotReceive().Method(…) → TODO hint ───────────────────────────
    content = Regex.Replace(
        content,
        @"(?m)^(\s*)(\w+)\.DidNotReceive\(\)\.(\w+)\(([^)]*)\)\s*;",
        m =>
        {
            var indent     = m.Groups[1].Value;
            var varName    = m.Groups[2].Value;
            var method     = m.Groups[3].Value;
            var methodArgs = m.Groups[4].Value;

            // Idempotent: skip if already preceded by our TODO comment
            var todoMark = $"// TODO[Imposter]: {varName}.{method}";
            if (content.Contains(todoMark))
                return m.Value;

            notes.Add($"  [1x] .DidNotReceive().{method}() → TODO: .{method}().Called(Count.Never())");
            return
                $"{indent}// TODO[Imposter]: {varName}.{method}({methodArgs}).Called(Count.Never());\n" +
                $"{indent}// {varName}.DidNotReceive().{method}({methodArgs}); // ← remove; replace with line above";
        });

    // ── Write if changed ──────────────────────────────────────────────────────
    if (content != original)
    {
        File.WriteAllText(filePath, content, Encoding.UTF8);
        totalFiles++;
        totalChanges += notes.Count;

        var rel = Path.GetRelativePath(testProjectPath, filePath);
        changeLog.Add((rel, notes));

        WriteColor(ConsoleColor.Green, $"  ✓ {rel}  ({notes.Count} changes)");
        foreach (var n in notes)
            Console.WriteLine($"    {n}");
        Console.WriteLine();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Update .csproj: swap NSubstitute → Imposter
// ─────────────────────────────────────────────────────────────────────────────

var csprojFiles = Directory.GetFiles(testProjectPath, "*.csproj");
foreach (var csproj in csprojFiles)
{
    var xml        = XDocument.Load(csproj);
    var itemGroups = xml.Descendants("ItemGroup");
    bool modified  = false;

    foreach (var itemGroup in itemGroups)
    {
        // Remove NSubstitute
        var nsub = itemGroup.Elements("PackageReference")
            .FirstOrDefault(e =>
                string.Equals(e.Attribute("Include")?.Value,
                              "NSubstitute",
                              StringComparison.OrdinalIgnoreCase));

        if (nsub is not null)
        {
            nsub.Remove();
            modified = true;

            // Add Imposter in the same ItemGroup
            itemGroup.Add(new XElement("PackageReference",
                new XAttribute("Include", "Imposter"),
                new XAttribute("Version", "*")));

            WriteColor(ConsoleColor.Yellow,
                $"  📦 {Path.GetFileName(csproj)}: NSubstitute removed, Imposter@* added");
            break;
        }
    }

    if (modified)
    {
        xml.Save(csproj);
        totalChanges++;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Create AssemblyImposterAttributes.cs
// ─────────────────────────────────────────────────────────────────────────────

if (mockedTypes.Count > 0)
{
    var attrsPath = Path.Combine(testProjectPath, "AssemblyImposterAttributes.cs");
    bool exists   = File.Exists(attrsPath);

    // If file exists, merge existing types with new ones
    if (exists)
    {
        var existing = File.ReadAllText(attrsPath);
        foreach (var t in mockedTypes)
        {
            var typeName = t.Contains('.') ? t.Split('.').Last() : t;
            if (!existing.Contains(typeName))
            {
                // Append missing attribute before the last line
                var lastNewline = existing.LastIndexOf('\n');
                existing = existing.Insert(
                    lastNewline + 1,
                    $"[assembly: GenerateImposter(typeof({t}))]\n");
            }
        }
        File.WriteAllText(attrsPath, existing, Encoding.UTF8);
        WriteColor(ConsoleColor.Yellow,
            $"  📄 AssemblyImposterAttributes.cs updated (+{mockedTypes.Count} types)");
    }
    else
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by ConvertToImposter.cs");
        sb.AppendLine("// Instructs the Imposter source generator to create imposters for each type.");
        sb.AppendLine("// See: https://themidnightgospel.github.io/Imposter/latest/");
        sb.AppendLine("//");
        sb.AppendLine("// After a build, use the generated imposters in tests:");
        sb.AppendLine("//   C# 14:   var imposter = IMyService.Imposter();");
        sb.AppendLine("//   C#  9+:  var imposter = new IMyServiceImposter();");
        sb.AppendLine("//");
        sb.AppendLine("// ⚠  IMPORTANT: API difference vs NSubstitute");
        sb.AppendLine("//   NSubstitute: mock IS the interface (mock.Method() both sets up AND acts)");
        sb.AppendLine("//   Imposter:    imposter.Method().Returns(…) → setup / verify");
        sb.AppendLine("//                imposter.Instance().Method() → act (pass to SUT)");
        sb.AppendLine("//");
        sb.AppendLine("// So wherever the old mock was passed to a constructor or method,");
        sb.AppendLine("// use mockInstance (the .Instance() value) instead.");
        sb.AppendLine();
        sb.AppendLine("using Imposter.Abstractions;");
        sb.AppendLine();

        foreach (var t in mockedTypes)
            sb.AppendLine($"[assembly: GenerateImposter(typeof({t}))]");

        File.WriteAllText(attrsPath, sb.ToString(), Encoding.UTF8);
        WriteColor(ConsoleColor.Yellow,
            $"  📄 AssemblyImposterAttributes.cs created ({mockedTypes.Count} types)");
    }

    Console.WriteLine();
    Console.WriteLine("     Types registered:");
    foreach (var t in mockedTypes)
        Console.WriteLine($"       • {t}");

    Console.WriteLine();
    totalChanges++;
}

// ─────────────────────────────────────────────────────────────────────────────
// Summary
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine();
WriteColor(ConsoleColor.Cyan, "─────────────────────────────────────────────────────────");
WriteColor(ConsoleColor.Cyan, " Conversion Summary");
WriteColor(ConsoleColor.Cyan, "─────────────────────────────────────────────────────────");
Console.WriteLine($"  Files modified : {totalFiles}");
Console.WriteLine($"  Total changes  : {totalChanges}");
Console.WriteLine();

// Manual steps
WriteColor(ConsoleColor.Yellow, "⚠  Manual steps required:");
Console.WriteLine("""
  1. Act-step calls — where the old mock was used AS the interface, switch to
     the <varName>Instance variable that .Instance() provides.

     Before:  var result = mockService.GetValue();
     After:   var result = mockServiceInstance.GetValue();

  2. Received / DidNotReceive — lines marked TODO[Imposter] above.
     NSubstitute: mockService.Received(1).GetValue();
     Imposter:    mockService.GetValue().Called(Count.Once());   ← imposter obj
                  mockService.SomeMethod("x").Called(Count.Never());

  3. Multi-line Received() calls (spanning multiple lines) are NOT auto-
     converted. Search for .Received( and .DidNotReceive( in the project.

  4. Rebuild the project — the Imposter source generator needs a build to
     produce the imposter types before the tests will compile.
     Run: dotnet build <path-to-CloudBurger.sln>

  5. For fully-qualified type names (e.g. Microsoft.Extensions.Logging.ILogger)
     in AssemblyImposterAttributes.cs, verify the typeof() resolves. You may
     need to add a using or use the short qualified name.
""");

WriteColor(ConsoleColor.Green, "✅  Automated conversion complete!");
return 0;
