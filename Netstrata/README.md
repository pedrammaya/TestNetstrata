


This is the Task:

Please write a C# console app to convert a C# object definition to the Typescript counterpart, using good coding practices.
An example of the C# object definition as a string input to your code:

“public class PersonDto
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
	}
}”


The definition of the same object in Typescript as the string output of your code: 

“export interface PersonDto { 
	name: string; 
	age: number; 
	gender: string; 
	driverLicenceNumber?: number; 
	addresses: Address[]; 
} 
export interface Address 
{ 
	streetNumber: number; 
	streetName: string; 
	suburb: string; postCode: number; 
}”

Assumptions:
1. A class property can only be a string, an int, a long, a nullable of these types, a list of these types, or a nested class consisting of these data types.
2. All class members are public.
3. Only one level of class nesting is allowed. (Pderam: Lets do more thna one level as well)
4. The definition of the nested class is always at the end of the definition of the parent class.
5. No empty lines are presented in the class definition.
