namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EMaybeNullWhenAttribute : Attribute
{
	public bool ReturnValue { get; }

	public _003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003EMaybeNullWhenAttribute(bool returnValue)
	{
		ReturnValue = returnValue;
	}
}
