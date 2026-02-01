using Scriban.Helpers;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("member expression", "<expression>.<variable_name>")]
public class ScriptMemberExpression : ScriptExpression, IScriptVariablePath
{
	public ScriptExpression Target { get; set; }

	public ScriptVariable Member { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		return context.GetValue(this);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Target);
		context.Write(".");
		context.Write(Member);
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public object GetValue(TemplateContext context)
	{
		object targetObject = GetTargetObject(context, isSet: false);
		if (targetObject == null)
		{
			return null;
		}
		IObjectAccessor memberAccessor = context.GetMemberAccessor(targetObject);
		string name = Member.Name;
		if (!memberAccessor.TryGetValue(context, Span, targetObject, name, out var value))
		{
			context.TryGetMember?.Invoke(context, Span, targetObject, name, out value);
		}
		return value;
	}

	public void SetValue(TemplateContext context, object valueToSet)
	{
		object targetObject = GetTargetObject(context, isSet: true);
		if (!context.GetMemberAccessor(targetObject).TrySetValue(member: Member.Name, context: context, span: Span, target: targetObject, value: valueToSet))
		{
			throw new ScriptRuntimeException(Member.Span, $"Cannot set a value for the readonly member: {this}");
		}
	}

	public string GetFirstPath()
	{
		return (Target as IScriptVariablePath)?.GetFirstPath();
	}

	private object GetTargetObject(TemplateContext context, bool isSet)
	{
		object obj = context.GetValue(Target);
		if (obj == null)
		{
			if (isSet || !context.EnableRelaxedMemberAccess)
			{
				throw new ScriptRuntimeException(Span, $"Object `{Target}` is null. Cannot access member: {this}");
			}
		}
		else if (obj is string || obj.GetType().IsPrimitiveOrDecimal())
		{
			if (isSet || !context.EnableRelaxedMemberAccess)
			{
				throw new ScriptRuntimeException(Span, $"Cannot get or set a member on the primitive `{obj}/{obj.GetType()}` when accessing member: {this}");
			}
			if (context.EnableRelaxedMemberAccess)
			{
				obj = null;
			}
		}
		return obj;
	}

	public override string ToString()
	{
		return $"{Target}.{Member}";
	}
}
