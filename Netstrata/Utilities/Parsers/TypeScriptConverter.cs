using Netstrata.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Netstrata.Utilities.Parsers
{
    public static class TypeScriptConverter
    {
        private static readonly Dictionary<string, string> _typeMap = new()
        {
            { "string", "string" },
            { "int", "number" },
            { "long", "number" },
        };

        public static string ConvertClasses(CSharpClass rootClass)
        {
            var sb = new StringBuilder();
            var emitted = new HashSet<string>(StringComparer.Ordinal);

            if (rootClass != null)
                AppendClass(sb, rootClass, emitted);

            return sb.ToString();
        }

        private static void AppendClass(StringBuilder sb, CSharpClass cls, HashSet<string> emitted)
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

        private static string MapType(string csharpType)
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
