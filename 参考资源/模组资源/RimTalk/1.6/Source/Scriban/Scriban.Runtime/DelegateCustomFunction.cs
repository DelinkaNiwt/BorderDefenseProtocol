using System;
using Scriban.Syntax;

namespace Scriban.Runtime;

public class DelegateCustomFunction : IScriptCustomFunction
{
	private readonly Func<TemplateContext, ScriptNode, ScriptArray, object> _customFunction;

	public DelegateCustomFunction(Func<TemplateContext, ScriptNode, ScriptArray, object> customFunction)
	{
		_customFunction = customFunction ?? throw new ArgumentNullException("customFunction");
	}

	public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
	{
		return _customFunction(context, callerContext, arguments);
	}
}
