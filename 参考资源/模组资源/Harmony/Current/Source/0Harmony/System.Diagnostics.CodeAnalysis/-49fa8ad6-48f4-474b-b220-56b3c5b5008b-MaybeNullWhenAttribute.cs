namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMaybeNullWhenAttribute : Attribute
{
	public bool ReturnValue { get; }

	public _003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMaybeNullWhenAttribute(bool returnValue)
	{
		ReturnValue = returnValue;
	}
}
