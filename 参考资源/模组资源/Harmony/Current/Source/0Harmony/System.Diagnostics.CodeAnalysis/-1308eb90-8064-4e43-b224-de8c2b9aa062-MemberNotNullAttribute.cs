namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EMemberNotNullAttribute : Attribute
{
	public string[] Members { get; }

	public _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EMemberNotNullAttribute(string member)
	{
		Members = new string[1] { member };
	}

	public _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EMemberNotNullAttribute(params string[] members)
	{
		Members = members;
	}
}
