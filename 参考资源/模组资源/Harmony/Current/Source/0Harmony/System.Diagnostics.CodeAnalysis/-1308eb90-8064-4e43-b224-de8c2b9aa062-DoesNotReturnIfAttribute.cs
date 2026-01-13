namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EDoesNotReturnIfAttribute : Attribute
{
	public bool ParameterValue { get; }

	public _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EDoesNotReturnIfAttribute(bool parameterValue)
	{
		ParameterValue = parameterValue;
	}
}
