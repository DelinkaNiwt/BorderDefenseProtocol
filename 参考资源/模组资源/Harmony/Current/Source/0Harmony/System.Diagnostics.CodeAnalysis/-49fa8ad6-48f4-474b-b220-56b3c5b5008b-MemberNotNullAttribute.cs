namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMemberNotNullAttribute : Attribute
{
	public string[] Members { get; }

	public _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMemberNotNullAttribute(string member)
	{
		Members = new string[1] { member };
	}

	public _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMemberNotNullAttribute(params string[] members)
	{
		Members = members;
	}
}
