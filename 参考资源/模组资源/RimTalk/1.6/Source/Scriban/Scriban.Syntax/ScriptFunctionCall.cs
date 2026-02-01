using System;
using System.Collections;
using System.Collections.Generic;
using Scriban.Helpers;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("function call expression", "<target_expression> <arguemnt[0]> ... <arguement[n]>")]
public class ScriptFunctionCall : ScriptExpression
{
	public ScriptExpression Target { get; set; }

	public List<ScriptExpression> Arguments { get; private set; }

	public ScriptFunctionCall()
	{
		Arguments = new List<ScriptExpression>();
	}

	public override object Evaluate(TemplateContext context)
	{
		object obj = context.Evaluate(Target, aliasReturnedFunction: true);
		if (obj == null)
		{
			if (context.EnableRelaxedMemberAccess)
			{
				return null;
			}
			throw new ScriptRuntimeException(Target.Span, $"The function `{Target}` was not found");
		}
		return Call(context, this, obj, context.AllowPipeArguments, Arguments);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Target);
		foreach (ScriptExpression argument in Arguments)
		{
			context.ExpectSpace();
			context.Write(argument);
		}
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public override string ToString()
	{
		string arg = StringHelper.Join(" ", Arguments);
		return $"{Target} {arg}";
	}

	public static bool IsFunction(object target)
	{
		if (!(target is ScriptFunction))
		{
			return target is IScriptCustomFunction;
		}
		return true;
	}

	public static object Call(TemplateContext context, ScriptNode callerContext, object functionObject, bool processPipeArguments, List<ScriptExpression> arguments = null)
	{
		if (callerContext == null)
		{
			throw new ArgumentNullException("callerContext");
		}
		if (functionObject == null)
		{
			throw new ScriptRuntimeException(callerContext.Span, $"The target function `{callerContext}` is null");
		}
		ScriptFunction scriptFunction = functionObject as ScriptFunction;
		IScriptCustomFunction scriptCustomFunction = functionObject as IScriptCustomFunction;
		if (scriptFunction == null && scriptCustomFunction == null)
		{
			throw new ScriptRuntimeException(callerContext.Span, $"Invalid target function `{callerContext}`( as `{functionObject?.GetType()}`)");
		}
		ScriptBlockStatement scriptBlockStatement = null;
		if (context.BlockDelegates.Count > 0)
		{
			scriptBlockStatement = context.BlockDelegates.Pop();
		}
		ScriptArray scriptArray;
		if (processPipeArguments && context.PipeArguments != null && context.PipeArguments.Count > 0)
		{
			ScriptPipeArguments pipeArguments = context.PipeArguments;
			scriptArray = new ScriptArray(pipeArguments.Count);
			for (int i = 0; i < pipeArguments.Count; i++)
			{
				scriptArray.Add(pipeArguments[i]);
			}
			pipeArguments.Clear();
		}
		else
		{
			scriptArray = new ScriptArray(arguments?.Count ?? 0);
		}
		if (arguments != null)
		{
			foreach (ScriptExpression argument in arguments)
			{
				object obj;
				if (argument is ScriptNamedArgument scriptNamedArgument)
				{
					if (scriptCustomFunction == null)
					{
						if (scriptArray.CanWrite(scriptNamedArgument.Name))
						{
							scriptArray.SetValue(context, callerContext.Span, scriptNamedArgument.Name, context.Evaluate(scriptNamedArgument), readOnly: false);
							continue;
						}
						obj = context.Evaluate(scriptNamedArgument);
					}
					else
					{
						obj = argument;
					}
				}
				else
				{
					obj = context.Evaluate(argument);
				}
				if (argument is ScriptUnaryExpression { Operator: ScriptUnaryOperator.FunctionParametersExpand } && obj is IEnumerable enumerable)
				{
					foreach (object item in enumerable)
					{
						scriptArray.Add(item);
					}
				}
				else
				{
					scriptArray.Add(obj);
				}
			}
		}
		object result = null;
		context.EnterFunction(callerContext);
		try
		{
			if (scriptCustomFunction != null)
			{
				result = scriptCustomFunction.Invoke(context, callerContext, scriptArray, scriptBlockStatement);
			}
			else
			{
				context.SetValue(ScriptVariable.Arguments, scriptArray, asReadOnly: true);
				if (scriptBlockStatement != null)
				{
					context.SetValue(ScriptVariable.BlockDelegate, scriptBlockStatement, asReadOnly: true);
				}
				result = context.Evaluate(scriptFunction.Body);
			}
		}
		finally
		{
			context.ExitFunction();
		}
		context.FlowState = ScriptFlowState.None;
		return result;
	}
}
