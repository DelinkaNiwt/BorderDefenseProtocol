using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("binary expression", "<expression> operator <expression>")]
public class ScriptBinaryExpression : ScriptExpression
{
	public ScriptExpression Left { get; set; }

	public ScriptBinaryOperator Operator { get; set; }

	public ScriptExpression Right { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		object obj = context.Evaluate(Left);
		switch (Operator)
		{
		case ScriptBinaryOperator.And:
		{
			bool flag = context.ToBool(Left.Span, obj);
			if (!flag)
			{
				return false;
			}
			object value2 = context.Evaluate(Right);
			bool flag2 = context.ToBool(Right.Span, value2);
			return flag && flag2;
		}
		case ScriptBinaryOperator.Or:
		{
			if (context.ToBool(Left.Span, obj))
			{
				return true;
			}
			object value = context.Evaluate(Right);
			return context.ToBool(Right.Span, value);
		}
		default:
		{
			object rightValue = context.Evaluate(Right);
			return Evaluate(context, Span, Operator, obj, rightValue);
		}
		}
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Left);
		if (Operator == ScriptBinaryOperator.Substract && !context.PreviousHasSpace)
		{
			context.Write(" ");
		}
		context.Write(Operator.ToText());
		if (Operator == ScriptBinaryOperator.Substract)
		{
			context.ExpectSpace();
		}
		context.Write(Right);
	}

	public override string ToString()
	{
		return $"{Left} {Operator.ToText()} {Right}";
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public static object Evaluate(TemplateContext context, SourceSpan span, ScriptBinaryOperator op, object leftValue, object rightValue)
	{
		switch (op)
		{
		case ScriptBinaryOperator.EmptyCoalescing:
			return leftValue ?? rightValue;
		case ScriptBinaryOperator.ShiftLeft:
			if (leftValue is IList values)
			{
				return new ScriptArray(values) { rightValue };
			}
			break;
		case ScriptBinaryOperator.ShiftRight:
			if (rightValue is IList values2)
			{
				ScriptArray scriptArray = new ScriptArray(values2);
				scriptArray.Insert(0, leftValue);
				return scriptArray;
			}
			break;
		case ScriptBinaryOperator.LiquidHasKey:
			if (leftValue is IDictionary<string, object> value2)
			{
				return ObjectFunctions.HasKey(value2, context.ToString(span, rightValue));
			}
			break;
		case ScriptBinaryOperator.LiquidHasValue:
			if (leftValue is IDictionary<string, object> value)
			{
				return ObjectFunctions.HasValue(value, context.ToString(span, rightValue));
			}
			break;
		case ScriptBinaryOperator.Add:
		case ScriptBinaryOperator.Substract:
		case ScriptBinaryOperator.Divide:
		case ScriptBinaryOperator.DivideRound:
		case ScriptBinaryOperator.Multiply:
		case ScriptBinaryOperator.Modulus:
		case ScriptBinaryOperator.RangeInclude:
		case ScriptBinaryOperator.RangeExclude:
		case ScriptBinaryOperator.CompareEqual:
		case ScriptBinaryOperator.CompareNotEqual:
		case ScriptBinaryOperator.CompareLessOrEqual:
		case ScriptBinaryOperator.CompareGreaterOrEqual:
		case ScriptBinaryOperator.CompareLess:
		case ScriptBinaryOperator.CompareGreater:
		case ScriptBinaryOperator.LiquidContains:
		case ScriptBinaryOperator.LiquidStartsWith:
		case ScriptBinaryOperator.LiquidEndsWith:
			if (leftValue is string || rightValue is string)
			{
				return CalculateToString(context, span, op, leftValue, rightValue);
			}
			if (leftValue == EmptyScriptObject.Default || rightValue == EmptyScriptObject.Default)
			{
				return CalculateEmpty(context, span, op, leftValue, rightValue);
			}
			return CalculateOthers(context, span, op, leftValue, rightValue);
		}
		throw new ScriptRuntimeException(span, $"Operator `{op.ToText()}` is not implemented for `{leftValue}` and `{rightValue}`");
	}

	private static object CalculateEmpty(TemplateContext context, SourceSpan span, ScriptBinaryOperator op, object leftValue, object rightValue)
	{
		bool flag = leftValue == EmptyScriptObject.Default;
		bool flag2 = rightValue == EmptyScriptObject.Default;
		if (flag && flag2)
		{
			switch (op)
			{
			case ScriptBinaryOperator.CompareEqual:
			case ScriptBinaryOperator.CompareLessOrEqual:
			case ScriptBinaryOperator.CompareGreaterOrEqual:
				return true;
			case ScriptBinaryOperator.CompareNotEqual:
			case ScriptBinaryOperator.CompareLess:
			case ScriptBinaryOperator.CompareGreater:
			case ScriptBinaryOperator.LiquidContains:
			case ScriptBinaryOperator.LiquidStartsWith:
			case ScriptBinaryOperator.LiquidEndsWith:
				return false;
			default:
				return EmptyScriptObject.Default;
			}
		}
		object against = (flag ? rightValue : leftValue);
		object obj = context.IsEmpty(span, against);
		switch (op)
		{
		case ScriptBinaryOperator.CompareEqual:
			return obj;
		case ScriptBinaryOperator.CompareNotEqual:
			if (!(obj is bool))
			{
				return obj;
			}
			return !(bool)obj;
		case ScriptBinaryOperator.CompareLess:
		case ScriptBinaryOperator.CompareGreater:
			return false;
		case ScriptBinaryOperator.CompareLessOrEqual:
		case ScriptBinaryOperator.CompareGreaterOrEqual:
			return obj;
		case ScriptBinaryOperator.Add:
		case ScriptBinaryOperator.Substract:
		case ScriptBinaryOperator.Divide:
		case ScriptBinaryOperator.DivideRound:
		case ScriptBinaryOperator.Multiply:
		case ScriptBinaryOperator.Modulus:
		case ScriptBinaryOperator.RangeInclude:
		case ScriptBinaryOperator.RangeExclude:
			return EmptyScriptObject.Default;
		case ScriptBinaryOperator.LiquidContains:
		case ScriptBinaryOperator.LiquidStartsWith:
		case ScriptBinaryOperator.LiquidEndsWith:
			return false;
		default:
			throw new ScriptRuntimeException(span, string.Format("Operator `{0}` is not implemented for `{1}` / `{2}`", op.ToText(), flag ? "empty" : leftValue, flag2 ? "empty" : rightValue));
		}
	}

	private static object CalculateToString(TemplateContext context, SourceSpan span, ScriptBinaryOperator op, object left, object right)
	{
		switch (op)
		{
		case ScriptBinaryOperator.Add:
			return context.ToString(span, left) + context.ToString(span, right);
		case ScriptBinaryOperator.Multiply:
			if (right is int)
			{
				object obj = left;
				left = right;
				right = obj;
			}
			if (left is int)
			{
				string value = context.ToString(span, right);
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < (int)left; i++)
				{
					stringBuilder.Append(value);
				}
				return stringBuilder.ToString();
			}
			throw new ScriptRuntimeException(span, "Operator `" + op.ToText() + "` is not supported for the expression. Only working on string x int or int x string");
		case ScriptBinaryOperator.CompareEqual:
			return context.ToString(span, left) == context.ToString(span, right);
		case ScriptBinaryOperator.CompareNotEqual:
			return context.ToString(span, left) != context.ToString(span, right);
		case ScriptBinaryOperator.CompareGreater:
			return context.ToString(span, left).CompareTo(context.ToString(span, right)) > 0;
		case ScriptBinaryOperator.CompareLess:
			return context.ToString(span, left).CompareTo(context.ToString(span, right)) < 0;
		case ScriptBinaryOperator.CompareGreaterOrEqual:
			return context.ToString(span, left).CompareTo(context.ToString(span, right)) >= 0;
		case ScriptBinaryOperator.CompareLessOrEqual:
			return context.ToString(span, left).CompareTo(context.ToString(span, right)) <= 0;
		case ScriptBinaryOperator.LiquidContains:
			return context.ToString(span, left).Contains(context.ToString(span, right));
		case ScriptBinaryOperator.LiquidStartsWith:
			return context.ToString(span, left).StartsWith(context.ToString(span, right));
		case ScriptBinaryOperator.LiquidEndsWith:
			return context.ToString(span, left).EndsWith(context.ToString(span, right));
		default:
			throw new ScriptRuntimeException(span, "Operator `" + op.ToText() + "` is not supported on string objects");
		}
	}

	private static IEnumerable<int> RangeInclude(int left, int right)
	{
		if (left < right)
		{
			for (int i = left; i <= right; i++)
			{
				yield return i;
			}
			yield break;
		}
		for (int i = left; i >= right; i--)
		{
			yield return i;
		}
	}

	private static IEnumerable<int> RangeExclude(int left, int right)
	{
		if (left < right)
		{
			for (int i = left; i < right; i++)
			{
				yield return i;
			}
			yield break;
		}
		for (int i = left; i > right; i--)
		{
			yield return i;
		}
	}

	private static IEnumerable<long> RangeInclude(long left, long right)
	{
		if (left < right)
		{
			for (long i = left; i <= right; i++)
			{
				yield return i;
			}
			yield break;
		}
		for (long i = left; i >= right; i--)
		{
			yield return i;
		}
	}

	private static IEnumerable<long> RangeExclude(long left, long right)
	{
		if (left < right)
		{
			for (long i = left; i < right; i++)
			{
				yield return i;
			}
			yield break;
		}
		for (long i = left; i > right; i--)
		{
			yield return i;
		}
	}

	private static object CalculateOthers(TemplateContext context, SourceSpan span, ScriptBinaryOperator op, object leftValue, object rightValue)
	{
		if (leftValue == null && rightValue == null)
		{
			switch (op)
			{
			case ScriptBinaryOperator.CompareEqual:
				return true;
			case ScriptBinaryOperator.CompareNotEqual:
				return false;
			case ScriptBinaryOperator.CompareLessOrEqual:
			case ScriptBinaryOperator.CompareGreaterOrEqual:
			case ScriptBinaryOperator.CompareLess:
			case ScriptBinaryOperator.CompareGreater:
				return false;
			case ScriptBinaryOperator.Add:
			case ScriptBinaryOperator.Substract:
			case ScriptBinaryOperator.Divide:
			case ScriptBinaryOperator.DivideRound:
			case ScriptBinaryOperator.Multiply:
			case ScriptBinaryOperator.Modulus:
			case ScriptBinaryOperator.RangeInclude:
			case ScriptBinaryOperator.RangeExclude:
				return null;
			case ScriptBinaryOperator.LiquidContains:
			case ScriptBinaryOperator.LiquidStartsWith:
			case ScriptBinaryOperator.LiquidEndsWith:
				return false;
			default:
				return null;
			}
		}
		if (leftValue == null || rightValue == null)
		{
			switch (op)
			{
			case ScriptBinaryOperator.CompareNotEqual:
				return true;
			case ScriptBinaryOperator.CompareEqual:
			case ScriptBinaryOperator.CompareLessOrEqual:
			case ScriptBinaryOperator.CompareGreaterOrEqual:
			case ScriptBinaryOperator.CompareLess:
			case ScriptBinaryOperator.CompareGreater:
			case ScriptBinaryOperator.LiquidContains:
			case ScriptBinaryOperator.LiquidStartsWith:
			case ScriptBinaryOperator.LiquidEndsWith:
				return false;
			case ScriptBinaryOperator.Add:
			case ScriptBinaryOperator.Substract:
			case ScriptBinaryOperator.Divide:
			case ScriptBinaryOperator.DivideRound:
			case ScriptBinaryOperator.Multiply:
			case ScriptBinaryOperator.Modulus:
			case ScriptBinaryOperator.RangeInclude:
			case ScriptBinaryOperator.RangeExclude:
				return null;
			default:
				return null;
			}
		}
		Type type = leftValue.GetType();
		Type type2 = rightValue.GetType();
		if (type == typeof(decimal))
		{
			decimal right = (decimal)context.ToObject(span, rightValue, typeof(decimal));
			return CalculateDecimal(op, span, (decimal)leftValue, right);
		}
		if (type2 == typeof(decimal))
		{
			decimal left = (decimal)context.ToObject(span, leftValue, typeof(decimal));
			return CalculateDecimal(op, span, left, (decimal)rightValue);
		}
		if (type == typeof(double))
		{
			double right2 = (double)context.ToObject(span, rightValue, typeof(double));
			return CalculateDouble(op, span, (double)leftValue, right2);
		}
		if (type2 == typeof(double))
		{
			double left2 = (double)context.ToObject(span, leftValue, typeof(double));
			return CalculateDouble(op, span, left2, (double)rightValue);
		}
		if (type == typeof(float))
		{
			float right3 = (float)context.ToObject(span, rightValue, typeof(float));
			return CalculateFloat(op, span, (float)leftValue, right3);
		}
		if (type2 == typeof(float))
		{
			float left3 = (float)context.ToObject(span, leftValue, typeof(float));
			return CalculateFloat(op, span, left3, (float)rightValue);
		}
		if (type == typeof(long))
		{
			long right4 = (long)context.ToObject(span, rightValue, typeof(long));
			return CalculateLong(op, span, (long)leftValue, right4);
		}
		if (type2 == typeof(long))
		{
			long left4 = (long)context.ToObject(span, leftValue, typeof(long));
			return CalculateLong(op, span, left4, (long)rightValue);
		}
		if (type == typeof(int) || (type != null && type.GetTypeInfo().IsEnum))
		{
			int right5 = (int)context.ToObject(span, rightValue, typeof(int));
			return CalculateInt(op, span, (int)leftValue, right5);
		}
		if (type2 == typeof(int) || (type2 != null && type2.GetTypeInfo().IsEnum))
		{
			int left5 = (int)context.ToObject(span, leftValue, typeof(int));
			return CalculateInt(op, span, left5, (int)rightValue);
		}
		if (type == typeof(bool))
		{
			bool right6 = (bool)context.ToObject(span, rightValue, typeof(bool));
			return CalculateBool(op, span, (bool)leftValue, right6);
		}
		if (type2 == typeof(bool))
		{
			bool left6 = (bool)context.ToObject(span, leftValue, typeof(bool));
			return CalculateBool(op, span, left6, (bool)rightValue);
		}
		if (type == typeof(DateTime) && type2 == typeof(DateTime))
		{
			return CalculateDateTime(op, span, (DateTime)leftValue, (DateTime)rightValue);
		}
		if (type == typeof(DateTime) && type2 == typeof(TimeSpan))
		{
			return CalculateDateTime(op, span, (DateTime)leftValue, (TimeSpan)rightValue);
		}
		if (op == ScriptBinaryOperator.CompareEqual)
		{
			return leftValue.Equals(rightValue);
		}
		throw new ScriptRuntimeException(span, $"Unsupported types `{leftValue}/{type}` {op.ToText()} `{rightValue}/{type2}` for binary operation");
	}

	private static object CalculateInt(ScriptBinaryOperator op, SourceSpan span, int left, int right)
	{
		return op switch
		{
			ScriptBinaryOperator.Add => left + right, 
			ScriptBinaryOperator.Substract => left - right, 
			ScriptBinaryOperator.Multiply => left * right, 
			ScriptBinaryOperator.Divide => (float)left / (float)right, 
			ScriptBinaryOperator.DivideRound => left / right, 
			ScriptBinaryOperator.Modulus => left % right, 
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			ScriptBinaryOperator.CompareGreater => left > right, 
			ScriptBinaryOperator.CompareLess => left < right, 
			ScriptBinaryOperator.CompareGreaterOrEqual => left >= right, 
			ScriptBinaryOperator.CompareLessOrEqual => left <= right, 
			ScriptBinaryOperator.RangeInclude => RangeInclude(left, right), 
			ScriptBinaryOperator.RangeExclude => RangeExclude(left, right), 
			_ => throw new ScriptRuntimeException(span, "The operator `" + op.ToText() + "` is not implemented for int<->int"), 
		};
	}

	private static object CalculateLong(ScriptBinaryOperator op, SourceSpan span, long left, long right)
	{
		return op switch
		{
			ScriptBinaryOperator.Add => left + right, 
			ScriptBinaryOperator.Substract => left - right, 
			ScriptBinaryOperator.Multiply => left * right, 
			ScriptBinaryOperator.Divide => (double)left / (double)right, 
			ScriptBinaryOperator.DivideRound => left / right, 
			ScriptBinaryOperator.Modulus => left % right, 
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			ScriptBinaryOperator.CompareGreater => left > right, 
			ScriptBinaryOperator.CompareLess => left < right, 
			ScriptBinaryOperator.CompareGreaterOrEqual => left >= right, 
			ScriptBinaryOperator.CompareLessOrEqual => left <= right, 
			ScriptBinaryOperator.RangeInclude => RangeInclude(left, right), 
			ScriptBinaryOperator.RangeExclude => RangeExclude(left, right), 
			_ => throw new ScriptRuntimeException(span, "The operator `" + op.ToText() + "` is not implemented for long<->long"), 
		};
	}

	private static object CalculateDouble(ScriptBinaryOperator op, SourceSpan span, double left, double right)
	{
		return op switch
		{
			ScriptBinaryOperator.Add => left + right, 
			ScriptBinaryOperator.Substract => left - right, 
			ScriptBinaryOperator.Multiply => left * right, 
			ScriptBinaryOperator.Divide => left / right, 
			ScriptBinaryOperator.DivideRound => Math.Round(left / right), 
			ScriptBinaryOperator.Modulus => left % right, 
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			ScriptBinaryOperator.CompareGreater => left > right, 
			ScriptBinaryOperator.CompareLess => left < right, 
			ScriptBinaryOperator.CompareGreaterOrEqual => left >= right, 
			ScriptBinaryOperator.CompareLessOrEqual => left <= right, 
			_ => throw new ScriptRuntimeException(span, "The operator `" + op.ToText() + "` is not implemented for double<->double"), 
		};
	}

	private static object CalculateDecimal(ScriptBinaryOperator op, SourceSpan span, decimal left, decimal right)
	{
		return op switch
		{
			ScriptBinaryOperator.Add => left + right, 
			ScriptBinaryOperator.Substract => left - right, 
			ScriptBinaryOperator.Multiply => left * right, 
			ScriptBinaryOperator.Divide => left / right, 
			ScriptBinaryOperator.DivideRound => Math.Round(left / right), 
			ScriptBinaryOperator.Modulus => left % right, 
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			ScriptBinaryOperator.CompareGreater => left > right, 
			ScriptBinaryOperator.CompareLess => left < right, 
			ScriptBinaryOperator.CompareGreaterOrEqual => left >= right, 
			ScriptBinaryOperator.CompareLessOrEqual => left <= right, 
			_ => throw new ScriptRuntimeException(span, "The operator `" + op.ToText() + "` is not implemented for decimal<->decimal"), 
		};
	}

	private static object CalculateFloat(ScriptBinaryOperator op, SourceSpan span, float left, float right)
	{
		return op switch
		{
			ScriptBinaryOperator.Add => left + right, 
			ScriptBinaryOperator.Substract => left - right, 
			ScriptBinaryOperator.Multiply => left * right, 
			ScriptBinaryOperator.Divide => left / right, 
			ScriptBinaryOperator.DivideRound => (double)(int)(left / right), 
			ScriptBinaryOperator.Modulus => left % right, 
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			ScriptBinaryOperator.CompareGreater => left > right, 
			ScriptBinaryOperator.CompareLess => left < right, 
			ScriptBinaryOperator.CompareGreaterOrEqual => left >= right, 
			ScriptBinaryOperator.CompareLessOrEqual => left <= right, 
			_ => throw new ScriptRuntimeException(span, "The operator `" + op.ToText() + "` is not implemented for float<->float"), 
		};
	}

	private static object CalculateDateTime(ScriptBinaryOperator op, SourceSpan span, DateTime left, DateTime right)
	{
		return op switch
		{
			ScriptBinaryOperator.Substract => left - right, 
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			ScriptBinaryOperator.CompareLess => left < right, 
			ScriptBinaryOperator.CompareLessOrEqual => left <= right, 
			ScriptBinaryOperator.CompareGreater => left > right, 
			ScriptBinaryOperator.CompareGreaterOrEqual => left >= right, 
			_ => throw new ScriptRuntimeException(span, $"The operator `{op}` is not supported for DateTime"), 
		};
	}

	private static object CalculateDateTime(ScriptBinaryOperator op, SourceSpan span, DateTime left, TimeSpan right)
	{
		if (op == ScriptBinaryOperator.Add)
		{
			return left + right;
		}
		throw new ScriptRuntimeException(span, $"The operator `{op}` is not supported for between <DateTime> and <TimeSpan>");
	}

	private static object CalculateBool(ScriptBinaryOperator op, SourceSpan span, bool left, bool right)
	{
		return op switch
		{
			ScriptBinaryOperator.CompareEqual => left == right, 
			ScriptBinaryOperator.CompareNotEqual => left != right, 
			_ => throw new ScriptRuntimeException(span, "The operator `" + op.ToText() + "` is not valid for bool<->bool"), 
		};
	}
}
