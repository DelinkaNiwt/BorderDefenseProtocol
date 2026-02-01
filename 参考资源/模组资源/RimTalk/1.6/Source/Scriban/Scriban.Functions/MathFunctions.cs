using System;
using System.Globalization;
using System.Reflection;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Scriban.Functions;

public class MathFunctions : ScriptObject
{
	public static object Abs(TemplateContext context, SourceSpan span, object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is int)
		{
			return Math.Abs((int)value);
		}
		if (value is float)
		{
			return Math.Abs((float)value);
		}
		if (value is double)
		{
			return Math.Abs((double)value);
		}
		if (value is sbyte)
		{
			return Math.Abs((sbyte)value);
		}
		if (value is short)
		{
			return Math.Abs((short)value);
		}
		if (value is long)
		{
			return Math.Abs((long)value);
		}
		if (value is decimal)
		{
			return Math.Abs((decimal)value);
		}
		if (value.GetType().GetTypeInfo().IsPrimitive)
		{
			return value;
		}
		throw new ScriptRuntimeException(span, $"The value `{value}` is not a number");
	}

	public static double Ceil(double value)
	{
		return Math.Ceiling(value);
	}

	public static object DividedBy(TemplateContext context, SourceSpan span, double value, object divisor)
	{
		object obj = ScriptBinaryExpression.Evaluate(context, span, ScriptBinaryOperator.Divide, value, divisor);
		if (divisor is int)
		{
			if (obj is double)
			{
				return (int)Math.Floor((double)obj);
			}
			if (obj is float)
			{
				return (int)Math.Floor((float)obj);
			}
		}
		return obj;
	}

	public static double Floor(double value)
	{
		return Math.Floor(value);
	}

	public static string Format(TemplateContext context, SourceSpan span, object value, string format, string culture = null)
	{
		if (value == null)
		{
			return string.Empty;
		}
		format = format ?? string.Empty;
		IFormattable formattable = value as IFormattable;
		if (!IsNumber(value) || formattable == null)
		{
			throw new ScriptRuntimeException(span, $"Unexpected `{value}`. Must be a formattable number");
		}
		return formattable.ToString(format, (culture != null) ? new CultureInfo(culture) : context.CurrentCulture);
	}

	public static bool IsNumber(object value)
	{
		if (!(value is sbyte) && !(value is byte) && !(value is short) && !(value is ushort) && !(value is int) && !(value is uint) && !(value is long) && !(value is ulong) && !(value is float) && !(value is double))
		{
			return value is decimal;
		}
		return true;
	}

	public static object Minus(TemplateContext context, SourceSpan span, object value, object with)
	{
		return ScriptBinaryExpression.Evaluate(context, span, ScriptBinaryOperator.Substract, value, with);
	}

	public static object Modulo(TemplateContext context, SourceSpan span, object value, object with)
	{
		return ScriptBinaryExpression.Evaluate(context, span, ScriptBinaryOperator.Modulus, value, with);
	}

	public static object Plus(TemplateContext context, SourceSpan span, object value, object with)
	{
		return ScriptBinaryExpression.Evaluate(context, span, ScriptBinaryOperator.Add, value, with);
	}

	public static double Round(double value, int precision = 0)
	{
		return Math.Round(value, precision);
	}

	public static object Times(TemplateContext context, SourceSpan span, object value, object with)
	{
		return ScriptBinaryExpression.Evaluate(context, span, ScriptBinaryOperator.Multiply, value, with);
	}
}
