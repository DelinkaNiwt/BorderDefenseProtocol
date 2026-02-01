using System.Collections;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("indexer expression", "<expression>[<index_expression>]")]
public class ScriptIndexerExpression : ScriptExpression, IScriptVariablePath
{
	public ScriptExpression Target { get; set; }

	public ScriptExpression Index { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		return context.GetValue(this);
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Target);
		int num;
		if (object.Equals(Target, ScriptVariable.Arguments) && Index is ScriptLiteral)
		{
			num = (((ScriptLiteral)Index).IsPositiveInteger() ? 1 : 0);
			if (num != 0)
			{
				goto IL_004e;
			}
		}
		else
		{
			num = 0;
		}
		context.Write("[");
		goto IL_004e;
		IL_004e:
		context.Write(Index);
		if (num == 0)
		{
			context.Write("]");
		}
	}

	public override string ToString()
	{
		return $"{Target}[{Index}]";
	}

	public object GetValue(TemplateContext context)
	{
		return GetOrSetValue(context, null, setter: false);
	}

	public void SetValue(TemplateContext context, object valueToSet)
	{
		GetOrSetValue(context, valueToSet, setter: true);
	}

	public string GetFirstPath()
	{
		return (Target as IScriptVariablePath)?.GetFirstPath();
	}

	private object GetOrSetValue(TemplateContext context, object valueToSet, bool setter)
	{
		object value = null;
		object value2 = context.GetValue(Target);
		if (value2 == null)
		{
			if (context.EnableRelaxedMemberAccess)
			{
				return null;
			}
			throw new ScriptRuntimeException(Target.Span, $"Object `{Target}` is null. Cannot access indexer: {this}");
		}
		object obj = context.Evaluate(Index);
		if (obj == null)
		{
			if (context.EnableRelaxedMemberAccess)
			{
				return null;
			}
			throw new ScriptRuntimeException(Index.Span, $"Cannot access target `{Target}` with a null indexer: {this}");
		}
		IListAccessor listAccessor = context.GetListAccessor(value2);
		if (value2 is IDictionary || (value2 is IScriptObject && listAccessor == null) || listAccessor == null)
		{
			IObjectAccessor memberAccessor = context.GetMemberAccessor(value2);
			string text = context.ToString(Index.Span, obj);
			if (setter)
			{
				if (!memberAccessor.TrySetValue(context, Span, value2, text, valueToSet))
				{
					throw new ScriptRuntimeException(Index.Span, $"Cannot set a value for the readonly member `{text}` in the indexer: {Target}['{text}']");
				}
			}
			else if (!memberAccessor.TryGetValue(context, Span, value2, text, out value))
			{
				context.TryGetMember?.Invoke(context, Span, value2, text, out value);
			}
		}
		else
		{
			int num = context.ToInt(Index.Span, obj);
			if (num < 0)
			{
				num = listAccessor.GetLength(context, Span, value2) + num;
			}
			if (num >= 0)
			{
				if (setter)
				{
					listAccessor.SetValue(context, Span, value2, num, valueToSet);
				}
				else
				{
					value = listAccessor.GetValue(context, Span, value2, num);
				}
			}
		}
		return value;
	}
}
