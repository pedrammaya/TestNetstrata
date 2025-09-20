namespace Netstrata.Models
{
    public class CSharpProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Normalized type (e.g., "string", "number", "Address", "Address[]")
        public bool IsNullable { get; set; }
    }
}
