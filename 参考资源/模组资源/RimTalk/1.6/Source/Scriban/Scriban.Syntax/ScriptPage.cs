namespace Scriban.Syntax;

public class ScriptPage : ScriptNode
{
	public ScriptBlockStatement FrontMatter { get; set; }

	public ScriptBlockStatement Body { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		context.FlowState = ScriptFlowState.None;
		try
		{
			return context.Evaluate(Body);
		}
		finally
		{
			context.FlowState = ScriptFlowState.None;
		}
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Body);
	}
}
