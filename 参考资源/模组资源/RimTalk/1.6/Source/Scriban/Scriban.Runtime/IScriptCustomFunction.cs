using Scriban.Syntax;

namespace Scriban.Runtime;

public interface IScriptCustomFunction
{
	object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement);
}
