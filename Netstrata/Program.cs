using System.Text;
using System.Text.RegularExpressions;

namespace CSharpToTypeScript
{
    internal class Program
    {
        private static void Main()
        {
            string input = @"
public class PersonDto
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; }
    public long? DriverLicenceNumber { get; set; }
    public List<Address> Addresses { get; set; }

    public class Address
    {
        public int StreetNumber { get; set; }
        public string StreetName { get; set; }
        public string Suburb { get; set; }
        public int PostCode { get; set; }
        public Metadata Metadata { get; set; }

        public class Metadata
        {
            public string Source { get; set; }
            public int Year { get; set; }
        }
    }
}";

            var parser = new CSharpClassParser();
            var rootClass = parser.ParseClasses(input);

            var converter = new TypeScriptConverter();
            string tsCode = converter.ConvertClasses(rootClass);

            Console.WriteLine(tsCode);
        }
    }

    #region Models
    public class CSharpClass
    {
        public string Name { get; set; } = string.Empty;
        public List<CSharpProperty> Properties { get; } = new();
        public List<CSharpClass> NestedClasses { get; } = new();
    }

    public class CSharpProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Normalized type (e.g., "string", "number", "Address", "Address[]")
        public bool IsNullable { get; set; }
    }
    #endregion

    public class CSharpClassParser
    {
        // Matches "class ClassName"
        private static readonly Regex ClassRegex = new(@"class\s+(\w+)", RegexOptions.Compiled);

        // Matches "public TYPE Name { get; set; }"
        // Accepts generics and nullable marker: List<Address>, long?, Nullable<int>
        private static readonly Regex PropertyRegex =
            new(@"public\s+([\w\?\<\>\[\]\s]+)\s+(\w+)\s*{\s*get;\s*set;\s*}", RegexOptions.Compiled);

        /// <summary>
        /// Parse the first/top-level class found in the input and return its CSharpClass representation.
        /// </summary>
        public CSharpClass ParseClasses(string input)
        {
            var firstMatch = ClassRegex.Match(input);
            if (!firstMatch.Success) return null;

            var (cls, _) = ParseClassAt(firstMatch.Index, input);
            return cls;
        }

        /// <summary>
        /// Parse a class located at the given index of the input (index should point at the 'class' keyword).
        /// Returns the parsed CSharpClass and the index of the closing brace for that class.
        /// </summary>
        private (CSharpClass cls, int endIndex) ParseClassAt(int classKeywordIndex, string source)
        {
            var match = ClassRegex.Match(source, classKeywordIndex);
            if (!match.Success || match.Index != classKeywordIndex)
                throw new ArgumentException("No class found at the provided index.", nameof(classKeywordIndex));

            string className = match.Groups[1].Value;

            int braceOpen = source.IndexOf('{', match.Index);
            if (braceOpen == -1) throw new FormatException($"Could not find opening brace for class {className}.");

            int braceClose = FindMatchingBrace(source, braceOpen);
            if (braceClose == -1) throw new FormatException($"Could not find closing brace for class {className}.");

            int classBodyStart = braceOpen + 1;
            int classBodyLength = braceClose - classBodyStart;
            string classBody = classBodyLength > 0 ? source.Substring(classBodyStart, classBodyLength) : string.Empty;

            var result = new CSharpClass { Name = className };

            // Find nested classes (direct children) - collect their global ranges and parsed CSharpClass
            var nestedRanges = new List<(int start, int end, CSharpClass parsed)>();

            foreach (Match nestedMatch in ClassRegex.Matches(classBody))
            {
                int nestedGlobalIndex = classBodyStart + nestedMatch.Index;

                // skip if nested match lies inside an already discovered nested range (ensures direct children only)
                if (nestedRanges.Any(r => nestedGlobalIndex >= r.start && nestedGlobalIndex <= r.end))
                    continue;

                // Parse nested class recursively
                var (nestedParsed, nestedEndIndex) = ParseClassAt(nestedGlobalIndex, source);

                nestedRanges.Add((nestedGlobalIndex, nestedEndIndex, nestedParsed));
            }

            // Parse properties that are NOT inside any nested class ranges
            foreach (Match propMatch in PropertyRegex.Matches(classBody))
            {
                int propGlobalIndex = classBodyStart + propMatch.Index;

                // Skip properties that belong to nested classes
                if (nestedRanges.Any(r => propGlobalIndex >= r.start && propGlobalIndex <= r.end))
                    continue;

                string rawType = propMatch.Groups[1].Value.Trim();
                string propName = propMatch.Groups[2].Value.Trim();

                bool isNullable = false;
                string normalizedType = rawType;

                // handle trailing "?" (e.g., long?)
                if (normalizedType.EndsWith("?"))
                {
                    isNullable = true;
                    normalizedType = normalizedType[..^1].Trim();
                }

                // handle Nullable<...>
                var nullableMatch = Regex.Match(normalizedType, @"^Nullable<\s*(\w+)\s*>$");
                if (nullableMatch.Success)
                {
                    isNullable = true;
                    normalizedType = nullableMatch.Groups[1].Value;
                }

                result.Properties.Add(new CSharpProperty
                {
                    Name = propName,
                    Type = normalizedType,
                    IsNullable = isNullable
                });
            }

            // Attach nested parsed classes to the result (preserve order)
            foreach (var r in nestedRanges)
                result.NestedClasses.Add(r.parsed);

            return (result, braceClose);
        }

        /// <summary>
        /// Find matching '}' for the '{' located at startIndex. Returns -1 if not found.
        /// </summary>
        private static int FindMatchingBrace(string text, int startIndex)
        {
            if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != '{')
                return -1;

            int depth = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
    }

    public class TypeScriptConverter
    {
        private readonly Dictionary<string, string> _typeMap = new()
        {
            { "string", "string" },
            { "int", "number" },
            { "long", "number" },
        };

        public string ConvertClasses(CSharpClass rootClass)
        {
            var sb = new StringBuilder();
            var emitted = new HashSet<string>(StringComparer.Ordinal);

            if (rootClass != null)
                AppendClass(sb, rootClass, emitted);

            return sb.ToString();
        }

        private void AppendClass(StringBuilder sb, CSharpClass cls, HashSet<string> emitted)
        {
            // Defensive: avoid emitting duplicate interfaces (if same name appears more than once)
            if (emitted.Contains(cls.Name)) return;
            emitted.Add(cls.Name);

            sb.AppendLine($"export interface {cls.Name} {{");

            foreach (var p in cls.Properties)
            {
                string tsType = MapType(p.Type);
                string tsName = ToCamelCase(p.Name);

                if (p.IsNullable)
                    sb.AppendLine($"    {tsName}?: {tsType};");
                else
                    sb.AppendLine($"    {tsName}: {tsType};");
            }

            sb.AppendLine("}");
            sb.AppendLine();

            // Emit nested classes after the parent (flattened output)
            foreach (var nested in cls.NestedClasses)
                AppendClass(sb, nested, emitted);
        }

        private string MapType(string csharpType)
        {
            csharpType = csharpType.Trim();

            // List<T> => T[]
            var listMatch = Regex.Match(csharpType, @"^List<\s*(\w+)\s*>$", RegexOptions.Compiled);
            if (listMatch.Success)
            {
                string inner = listMatch.Groups[1].Value;
                return MapType(inner) + "[]";
            }

            if (_typeMap.TryGetValue(csharpType, out var mapped))
                return mapped;

            // fallback: assume it's a class name
            return csharpType;
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (char.IsLower(name[0])) return name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
