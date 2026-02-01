using System;

namespace Scriban.Syntax;

public static class ScriptBinaryOperatorExtensions
{
	public static string ToText(this ScriptBinaryOperator op)
	{
		return op switch
		{
			ScriptBinaryOperator.Add => "+", 
			ScriptBinaryOperator.Substract => "-", 
			ScriptBinaryOperator.Divide => "/", 
			ScriptBinaryOperator.DivideRound => "//", 
			ScriptBinaryOperator.Multiply => "*", 
			ScriptBinaryOperator.Modulus => "%", 
			ScriptBinaryOperator.RangeInclude => "..", 
			ScriptBinaryOperator.RangeExclude => "..<", 
			ScriptBinaryOperator.CompareEqual => "==", 
			ScriptBinaryOperator.CompareNotEqual => "!=", 
			ScriptBinaryOperator.CompareLessOrEqual => "<=", 
			ScriptBinaryOperator.CompareGreaterOrEqual => ">=", 
			ScriptBinaryOperator.CompareLess => "<", 
			ScriptBinaryOperator.CompareGreater => ">", 
			ScriptBinaryOperator.And => "&&", 
			ScriptBinaryOperator.Or => "||", 
			ScriptBinaryOperator.EmptyCoalescing => "??", 
			ScriptBinaryOperator.ShiftLeft => "<<", 
			ScriptBinaryOperator.ShiftRight => ">>", 
			ScriptBinaryOperator.LiquidContains => "| string.contains ", 
			ScriptBinaryOperator.LiquidStartsWith => "| string.starts_with ", 
			ScriptBinaryOperator.LiquidEndsWith => "| string.ends_with ", 
			ScriptBinaryOperator.LiquidHasKey => "| object.has_key ", 
			ScriptBinaryOperator.LiquidHasValue => "| object.has_value ", 
			_ => throw new ArgumentOutOfRangeException("op"), 
		};
	}
}
