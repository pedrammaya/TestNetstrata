namespace Netstrata.Models
{
    public class CSharpClass
    {
        public string Name { get; set; } = string.Empty;
        public List<CSharpProperty> Properties { get; } = new();
        public List<CSharpClass> NestedClasses { get; } = new();
    }
}
