namespace Scriban.Syntax;

[ScriptSyntax("empty expression", "<expression>.empty?")]
public class ScriptIsEmptyExpression : ScriptExpression, IScriptVariablePath
{
	public ScriptExpression Target { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		return context.GetValue(this);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Target);
		context.Write(".empty?");
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public object GetValue(TemplateContext context)
	{
		object targetObject = GetTargetObject(context, isSet: false);
		return context.IsEmpty(Span, targetObject);
	}

	public void SetValue(TemplateContext context, object valueToSet)
	{
		throw new ScriptRuntimeException(Span, "The `.empty?` property cannot be set");
	}

	public string GetFirstPath()
	{
		return (Target as IScriptVariablePath)?.GetFirstPath();
	}

	private object GetTargetObject(TemplateContext context, bool isSet)
	{
		object value = context.GetValue(Target);
		if (value == null && (isSet || !context.EnableRelaxedMemberAccess))
		{
			throw new ScriptRuntimeException(Span, $"Object `{Target}` is null. Cannot access property `empty?`");
		}
		return value;
	}

	public override string ToString()
	{
		return $"{Target}.empty?";
	}
}
