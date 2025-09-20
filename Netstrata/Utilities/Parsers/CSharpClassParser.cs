using Netstrata.Models;
using System.Text.RegularExpressions;

namespace Netstrata.Utilities.Parsers
{
    public static class CSharpClassParser
    {
        /// <summary>
        /// Parse the first/top-level class found in the input and return its CSharpClass representation.
        /// </summary>
        public static CSharpClass ParseClasses(string input)
        {
            var firstMatch = RegexPatterns.ClassRegex.Match(input);
            if (!firstMatch.Success) return null;

            var (cls, _) = ParseClassAt(firstMatch.Index, input);
            return cls;
        }

        /// <summary>
        /// Parse a class located at the given index of the input (index should point at the 'class' keyword).
        /// Returns the parsed CSharpClass and the index of the closing brace for that class.
        /// </summary>
        private static (CSharpClass cls, int endIndex) ParseClassAt(int classKeywordIndex, string source)
        {
            var match = FindClassMatch(classKeywordIndex, source);
            string className = match.Groups[1].Value;
            var (braceOpen, braceClose, classBody) = ExtractClassBody(source, match.Index, className);

            var result = new CSharpClass { Name = className };

            var nestedRanges = ParseNestedClasses(classBody, braceOpen + 1, source);

            result.Properties.AddRange(ParseProperties(classBody, braceOpen + 1, nestedRanges));
            foreach (var r in nestedRanges)
                result.NestedClasses.Add(r.parsed);

            return (result, braceClose);
        }

        private static Match FindClassMatch(int classKeywordIndex, string source)
        {
            var match = RegexPatterns.ClassRegex.Match(source, classKeywordIndex);
            if (!match.Success || match.Index != classKeywordIndex)
                throw new ArgumentException("No class found at the provided index.", nameof(classKeywordIndex));
            return match;
        }

        private static (int braceOpen, int braceClose, string classBody) ExtractClassBody(string source, int classMatchIndex, string className)
        {
            int braceOpen = source.IndexOf('{', classMatchIndex);
            if (braceOpen == -1) throw new FormatException($"Could not find opening brace for class {className}.");
            int braceClose = FindMatchingBrace(source, braceOpen);
            if (braceClose == -1) throw new FormatException($"Could not find closing brace for class {className}.");
            int classBodyStart = braceOpen + 1;
            int classBodyLength = braceClose - classBodyStart;
            string classBody = classBodyLength > 0 ? source.Substring(classBodyStart, classBodyLength) : string.Empty;
            return (braceOpen, braceClose, classBody);
        }

        private static List<(int start, int end, CSharpClass parsed)> ParseNestedClasses(string classBody, int classBodyStart, string source)
        {
            var nestedRanges = new List<(int start, int end, CSharpClass parsed)>();
            foreach (Match nestedMatch in RegexPatterns.ClassRegex.Matches(classBody))
            {
                int nestedGlobalIndex = classBodyStart + nestedMatch.Index;
                if (nestedRanges.Any(r => nestedGlobalIndex >= r.start && nestedGlobalIndex <= r.end))
                    continue;
                var (nestedParsed, nestedEndIndex) = ParseClassAt(nestedGlobalIndex, source);
                nestedRanges.Add((nestedGlobalIndex, nestedEndIndex, nestedParsed));
            }
            return nestedRanges;
        }

        private static List<CSharpProperty> ParseProperties(string classBody, int classBodyStart, List<(int start, int end, CSharpClass parsed)> nestedRanges)
        {
            var properties = new List<CSharpProperty>();
            foreach (Match propMatch in RegexPatterns.PropertyRegex.Matches(classBody))
            {
                int propGlobalIndex = classBodyStart + propMatch.Index;
                if (nestedRanges.Any(r => propGlobalIndex >= r.start && propGlobalIndex <= r.end))
                    continue;

                string rawType = propMatch.Groups[1].Value.Trim();
                string propName = propMatch.Groups[2].Value.Trim();

                bool isNullable = false;
                string normalizedType = rawType;

                if (normalizedType.EndsWith("?"))
                {
                    isNullable = true;
                    normalizedType = normalizedType[..^1].Trim();
                }
                var nullableMatch = Regex.Match(normalizedType, @"^Nullable<\s*(\w+)\s*>$");
                if (nullableMatch.Success)
                {
                    isNullable = true;
                    normalizedType = nullableMatch.Groups[1].Value;
                }
                properties.Add(new CSharpProperty
                {
                    Name = propName,
                    Type = normalizedType,
                    IsNullable = isNullable
                });
            }
            return properties;
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
}
