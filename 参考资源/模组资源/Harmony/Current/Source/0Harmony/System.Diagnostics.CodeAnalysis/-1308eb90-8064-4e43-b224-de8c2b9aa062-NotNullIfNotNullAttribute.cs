namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003ENotNullIfNotNullAttribute : Attribute
{
	public string ParameterName { get; }

	public _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003ENotNullIfNotNullAttribute(string parameterName)
	{
		ParameterName = parameterName;
	}
}
