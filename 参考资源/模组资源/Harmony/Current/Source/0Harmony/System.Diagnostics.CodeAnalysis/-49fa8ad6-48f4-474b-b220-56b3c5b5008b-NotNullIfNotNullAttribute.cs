namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003ENotNullIfNotNullAttribute : Attribute
{
	public string ParameterName { get; }

	public _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003ENotNullIfNotNullAttribute(string parameterName)
	{
		ParameterName = parameterName;
	}
}
