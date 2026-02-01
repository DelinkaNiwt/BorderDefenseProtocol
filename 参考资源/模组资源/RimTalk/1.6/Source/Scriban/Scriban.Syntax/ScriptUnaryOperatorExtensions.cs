using System;

namespace Scriban.Syntax;

public static class ScriptUnaryOperatorExtensions
{
	public static string ToText(this ScriptUnaryOperator op)
	{
		return op switch
		{
			ScriptUnaryOperator.Not => "!", 
			ScriptUnaryOperator.Negate => "-", 
			ScriptUnaryOperator.Plus => "+", 
			ScriptUnaryOperator.FunctionAlias => "@", 
			ScriptUnaryOperator.FunctionParametersExpand => "^", 
			_ => throw new ArgumentOutOfRangeException("op"), 
		};
	}
}
