using Netstrata.Utilities.Parsers;

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

            var rootClass = CSharpClassParser.ParseClasses(input);

            string tsCode = TypeScriptConverter.ConvertClasses(rootClass);

            Console.WriteLine(tsCode);
        }
    }


    

    
}
