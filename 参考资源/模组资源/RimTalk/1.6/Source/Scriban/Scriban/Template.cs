using System;
using System.Collections.Generic;
using Scriban.Helpers;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Scriban;

public class Template
{
	public string SourceFilePath { get; }

	public ScriptPage Page { get; private set; }

	public bool HasErrors { get; private set; }

	public List<LogMessage> Messages { get; private set; }

	public ParserOptions ParserOptions { get; }

	public LexerOptions LexerOptions { get; }

	private Template(ParserOptions? parserOptions, LexerOptions? lexerOptions, string sourceFilePath)
	{
		ParserOptions = parserOptions.GetValueOrDefault();
		LexerOptions = lexerOptions.GetValueOrDefault();
		Messages = new List<LogMessage>();
		SourceFilePath = sourceFilePath;
	}

	public static Template Parse(string text, string sourceFilePath = null, ParserOptions? parserOptions = null, LexerOptions? lexerOptions = null)
	{
		Template template = new Template(parserOptions, lexerOptions, sourceFilePath);
		template.ParseInternal(text, sourceFilePath);
		return template;
	}

	public static Template ParseLiquid(string text, string sourceFilePath = null, ParserOptions? parserOptions = null, LexerOptions? lexerOptions = null)
	{
		LexerOptions valueOrDefault = lexerOptions.GetValueOrDefault();
		valueOrDefault.Mode = ScriptMode.Liquid;
		return Parse(text, sourceFilePath, parserOptions, valueOrDefault);
	}

	public static object Evaluate(string expression, TemplateContext context)
	{
		if (expression == null)
		{
			throw new ArgumentNullException("expression");
		}
		LexerOptions value = new LexerOptions
		{
			Mode = ScriptMode.ScriptOnly
		};
		LexerOptions? lexerOptions = value;
		return Parse(expression, null, null, lexerOptions).Evaluate(context);
	}

	public static object Evaluate(string expression, object model, MemberRenamerDelegate memberRenamer = null, MemberFilterDelegate memberFilter = null)
	{
		if (expression == null)
		{
			throw new ArgumentNullException("expression");
		}
		LexerOptions value = new LexerOptions
		{
			Mode = ScriptMode.ScriptOnly
		};
		LexerOptions? lexerOptions = value;
		return Parse(expression, null, null, lexerOptions).Evaluate(model, memberRenamer, memberFilter);
	}

	public object Evaluate(TemplateContext context)
	{
		bool enableOutput = context.EnableOutput;
		try
		{
			context.EnableOutput = false;
			return EvaluateAndRender(context, render: false);
		}
		finally
		{
			context.EnableOutput = enableOutput;
		}
	}

	public object Evaluate(object model = null, MemberRenamerDelegate memberRenamer = null, MemberFilterDelegate memberFilter = null)
	{
		ScriptObject scriptObject = new ScriptObject();
		if (model != null)
		{
			scriptObject.Import(model, memberFilter, memberRenamer);
		}
		TemplateContext templateContext = new TemplateContext
		{
			EnableOutput = false,
			MemberRenamer = memberRenamer,
			MemberFilter = memberFilter
		};
		templateContext.PushGlobal(scriptObject);
		object result = Evaluate(templateContext);
		templateContext.PopGlobal();
		return result;
	}

	public string Render(TemplateContext context)
	{
		EvaluateAndRender(context, render: true);
		string result = context.Output.ToString();
		if (context.Output is StringBuilderOutput stringBuilderOutput)
		{
			stringBuilderOutput.Builder.Length = 0;
		}
		return result;
	}

	public string Render(object model = null, MemberRenamerDelegate memberRenamer = null, MemberFilterDelegate memberFilter = null)
	{
		ScriptObject scriptObject = new ScriptObject();
		if (model != null)
		{
			scriptObject.Import(model, memberFilter, memberRenamer);
		}
		TemplateContext templateContext = ((LexerOptions.Mode == ScriptMode.Liquid) ? new LiquidTemplateContext() : new TemplateContext());
		templateContext.MemberRenamer = memberRenamer;
		templateContext.MemberFilter = memberFilter;
		templateContext.PushGlobal(scriptObject);
		return Render(templateContext);
	}

	public string ToText(TemplateRewriterOptions options = default(TemplateRewriterOptions))
	{
		CheckErrors();
		TextWriterOutput textWriterOutput = new TextWriterOutput();
		new TemplateRewriterContext(textWriterOutput, options).Write(Page);
		return textWriterOutput.ToString();
	}

	private object EvaluateAndRender(TemplateContext context, bool render)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		CheckErrors();
		if (SourceFilePath != null)
		{
			context.PushSourceFile(SourceFilePath);
		}
		try
		{
			object obj = context.Evaluate(Page);
			if (render && Page != null && context.EnableOutput && obj != null)
			{
				context.Write(Page.Span, obj);
			}
			return obj;
		}
		finally
		{
			if (SourceFilePath != null)
			{
				context.PopSourceFile();
			}
		}
	}

	private void CheckErrors()
	{
		if (HasErrors)
		{
			throw new InvalidOperationException("This template has errors. Check the <Template.HasError> and <Template.Messages> before evaluating a template. Messages:\n" + StringHelper.Join("\n", Messages));
		}
	}

	private void ParseInternal(string text, string sourceFilePath)
	{
		if (string.IsNullOrEmpty(text))
		{
			HasErrors = false;
			Messages = new List<LogMessage>();
			Page = new ScriptPage
			{
				Span = new SourceSpan(sourceFilePath, default(TextPosition), TextPosition.Eof)
			};
		}
		else
		{
			Parser parser = new Parser(new Lexer(text, sourceFilePath, LexerOptions), ParserOptions);
			Page = parser.Run();
			HasErrors = parser.HasErrors;
			Messages = parser.Messages;
		}
	}
}
