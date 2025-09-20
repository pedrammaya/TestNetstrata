using System.Text.RegularExpressions;

namespace Netstrata.Utilities
{
    public static class RegexPatterns
    {
        // Matches "class ClassName"
        public static readonly Regex ClassRegex = new(@"class\s+(\w+)", RegexOptions.Compiled);

        // Matches "public TYPE Name { get; set; }"
        // Accepts generics and nullable marker: List<Address>, long?, Nullable<int>
        public static readonly Regex PropertyRegex =
            new(@"public\s+([\w\?\<\>\[\]\s]+)\s+(\w+)\s*{\s*get;\s*set;\s*}", RegexOptions.Compiled);
    }
}
