using System;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Scriban.Functions;

public sealed class IncludeFunction : IScriptCustomFunction
{
	public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
	{
		if (arguments.Count == 0)
		{
			throw new ScriptRuntimeException(callerContext.Span, "Expecting at least the name of the template to include for the <include> function");
		}
		string text = context.ToString(callerContext.Span, arguments[0]);
		if (string.IsNullOrEmpty(text))
		{
			if (context is LiquidTemplateContext)
			{
				return null;
			}
			throw new ScriptRuntimeException(callerContext.Span, "Include template name cannot be null or empty");
		}
		ITemplateLoader templateLoader = context.TemplateLoader;
		if (templateLoader == null)
		{
			throw new ScriptRuntimeException(callerContext.Span, "Unable to include <" + text + ">. No TemplateLoader registered in TemplateContext.TemplateLoader");
		}
		string path;
		try
		{
			path = templateLoader.GetPath(context, callerContext.Span, text);
		}
		catch (Exception ex) when (!(ex is ScriptRuntimeException))
		{
			throw new ScriptRuntimeException(callerContext.Span, "Unexpected exception while getting the path for the include name `" + text + "`", ex);
		}
		if (path == null)
		{
			throw new ScriptRuntimeException(callerContext.Span, "Include template path is null for `" + text);
		}
		ScriptArray scriptArray = new ScriptArray(arguments.Count - 1);
		for (int i = 1; i < arguments.Count; i++)
		{
			scriptArray[i] = arguments[i];
		}
		context.SetValue(ScriptVariable.Arguments, scriptArray, asReadOnly: true);
		if (!context.CachedTemplates.TryGetValue(path, out var value))
		{
			string text2;
			try
			{
				text2 = templateLoader.Load(context, callerContext.Span, path);
			}
			catch (Exception ex2) when (!(ex2 is ScriptRuntimeException))
			{
				throw new ScriptRuntimeException(callerContext.Span, "Unexpected exception while loading the include `" + text + "` from path `" + path + "`", ex2);
			}
			if (text2 == null)
			{
				throw new ScriptRuntimeException(callerContext.Span, "The result of including `" + text + "->" + path + "` cannot be null");
			}
			ParserOptions templateLoaderParserOptions = context.TemplateLoaderParserOptions;
			LexerOptions templateLoaderLexerOptions = context.TemplateLoaderLexerOptions;
			value = Template.Parse(text2, path, templateLoaderParserOptions, templateLoaderLexerOptions);
			if (value.HasErrors)
			{
				throw new ScriptParserRuntimeException(callerContext.Span, "Error while parsing template `" + text + "` from `" + path + "`", value.Messages);
			}
			context.CachedTemplates.Add(path, value);
		}
		context.PushOutput();
		object obj = null;
		try
		{
			context.EnterRecursive(callerContext);
			obj = value.Render(context);
			context.ExitRecursive(callerContext);
			return obj;
		}
		finally
		{
			context.PopOutput();
		}
	}
}
