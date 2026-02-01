namespace Scriban.Syntax;

public class ScriptNamedArgument : ScriptExpression
{
	public string Name { get; set; }

	public ScriptExpression Value { get; set; }

	public ScriptNamedArgument()
	{
	}

	public ScriptNamedArgument(string name)
	{
		Name = name;
	}

	public ScriptNamedArgument(string name, ScriptExpression value)
	{
		Name = name;
		Value = value;
	}

	public override object Evaluate(TemplateContext context)
	{
		if (Value != null)
		{
			return context.Evaluate(Value);
		}
		return true;
	}

	public override void Write(TemplateRewriterContext context)
	{
		if (Name != null)
		{
			context.Write(Name);
			if (Value != null)
			{
				context.Write(":");
				context.Write(Value);
			}
		}
	}

	public override string ToString()
	{
		return $"{Name}: {Value}";
	}
}
