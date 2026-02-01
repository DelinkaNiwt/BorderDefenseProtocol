using System;
using System.Collections.Generic;
using Scriban.Helpers;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Scriban;

public class TemplateRewriterContext
{
	private readonly IScriptOutput _output;

	private bool _isInCode;

	private bool _expectSpace;

	private bool _expectEnd;

	private bool _expectEndOfStatement;

	private bool _previousHasSpace;

	private ScriptTriviaType _nextLStrip;

	private ScriptTriviaType _nextRStrip;

	private bool _hasEndOfStatement;

	private FastStack<bool> _isWhileLoop;

	private ScriptRawStatement _previousRawStatement;

	public readonly TemplateRewriterOptions Options;

	public bool PreviousHasSpace => _previousHasSpace;

	public bool IsInWhileLoop
	{
		get
		{
			if (_isWhileLoop.Count > 0)
			{
				return _isWhileLoop.Peek();
			}
			return false;
		}
	}

	public TemplateRewriterContext(IScriptOutput output, TemplateRewriterOptions options = default(TemplateRewriterOptions))
	{
		_isWhileLoop = new FastStack<bool>(4);
		Options = options;
		if (options.Mode != ScriptMode.Default)
		{
			throw new ArgumentException($"The rendering mode `{options.Mode}` is not supported. Only `ScriptMode.Default` is currently supported");
		}
		_output = output;
	}

	public TemplateRewriterContext Write(ScriptNode node)
	{
		if (node != null)
		{
			bool flag = false;
			if (node is ScriptLoopStatementBase)
			{
				_isWhileLoop.Push(node is ScriptWhileStatement);
				flag = true;
			}
			try
			{
				WriteBegin(node);
				node.Write(this);
				WriteEnd(node);
			}
			finally
			{
				if (flag)
				{
					_isWhileLoop.Pop();
				}
				if (!IsBlockOrPage(node))
				{
					_previousRawStatement = node as ScriptRawStatement;
				}
			}
		}
		return this;
	}

	public TemplateRewriterContext Write(string text)
	{
		_previousHasSpace = text.Length > 0 && char.IsWhiteSpace(text[text.Length - 1]);
		_output.Write(text);
		return this;
	}

	public TemplateRewriterContext ExpectEos()
	{
		if (!_hasEndOfStatement)
		{
			_expectEndOfStatement = true;
		}
		return this;
	}

	public TemplateRewriterContext ExpectSpace()
	{
		_expectSpace = true;
		return this;
	}

	public TemplateRewriterContext ExpectEnd()
	{
		_expectEnd = true;
		ExpectEos();
		return this;
	}

	public TemplateRewriterContext WriteListWithCommas<T>(IList<T> list) where T : ScriptNode
	{
		if (list == null)
		{
			return this;
		}
		for (int i = 0; i < list.Count; i++)
		{
			T node = list[i];
			Write(node);
			if (i + 1 < list.Count && !node.HasTrivia(ScriptTriviaType.Comma, before: false))
			{
				Write(",");
			}
		}
		return this;
	}

	public TemplateRewriterContext WriteEnterCode(int escape = 0)
	{
		Write("{");
		for (int i = 0; i < escape; i++)
		{
			Write("%");
		}
		Write("{");
		if (_nextLStrip != ScriptTriviaType.Empty)
		{
			Write((_nextLStrip == ScriptTriviaType.Whitespace) ? "~" : "-");
			_nextLStrip = ScriptTriviaType.Empty;
		}
		_expectEndOfStatement = false;
		_expectEnd = false;
		_expectSpace = false;
		_hasEndOfStatement = false;
		_isInCode = true;
		return this;
	}

	public TemplateRewriterContext WriteExitCode(int escape = 0)
	{
		if (_nextRStrip != ScriptTriviaType.Empty)
		{
			Write((_nextRStrip == ScriptTriviaType.Whitespace) ? "~" : "-");
			_nextRStrip = ScriptTriviaType.Empty;
		}
		Write("}");
		for (int i = 0; i < escape; i++)
		{
			Write("%");
		}
		Write("}");
		_expectEndOfStatement = false;
		_expectEnd = false;
		_expectSpace = false;
		_hasEndOfStatement = false;
		_isInCode = false;
		return this;
	}

	private void WriteBegin(ScriptNode node)
	{
		ScriptRawStatement scriptRawStatement = node as ScriptRawStatement;
		if (!IsBlockOrPage(node))
		{
			if (_isInCode)
			{
				if (scriptRawStatement != null)
				{
					_nextRStrip = GetWhitespaceModeFromTrivia(scriptRawStatement, before: true);
					WriteExitCode();
				}
			}
			else if (scriptRawStatement == null)
			{
				if (_previousRawStatement != null)
				{
					_nextLStrip = GetWhitespaceModeFromTrivia(_previousRawStatement, before: false);
				}
				WriteEnterCode();
			}
		}
		WriteTrivias(node, before: true);
		HandleEos(node);
		if (node.CanHaveLeadingTrivia())
		{
			if (_expectSpace && !_previousHasSpace)
			{
				Write(" ");
			}
			_expectSpace = false;
		}
	}

	private void WriteEnd(ScriptNode node)
	{
		if (_expectEnd)
		{
			HandleEos(node);
			bool num = node.HasTrivia(ScriptTriviaType.End, before: false);
			if (_previousRawStatement != null)
			{
				_nextLStrip = GetWhitespaceModeFromTrivia(_previousRawStatement, before: false);
			}
			if (!_isInCode)
			{
				WriteEnterCode();
			}
			if (num)
			{
				WriteTrivias(node, before: false);
			}
			else
			{
				Write(_isInCode ? "end" : " end ");
			}
			if (!_isInCode)
			{
				WriteExitCode();
			}
			else
			{
				_expectEndOfStatement = true;
			}
			_expectEnd = false;
		}
		else
		{
			WriteTrivias(node, before: false);
		}
		if (node is ScriptPage && _isInCode)
		{
			WriteExitCode();
		}
	}

	private void HandleEos(ScriptNode node)
	{
		if (node is ScriptStatement && !IsBlockOrPage(node) && _isInCode && _expectEndOfStatement)
		{
			if (!_hasEndOfStatement && !(node is ScriptRawStatement))
			{
				Write("; ");
			}
			_expectEndOfStatement = false;
			_hasEndOfStatement = false;
		}
	}

	private static bool IsBlockOrPage(ScriptNode node)
	{
		if (!(node is ScriptBlockStatement))
		{
			return node is ScriptPage;
		}
		return true;
	}

	private void WriteTrivias(ScriptNode node, bool before)
	{
		if (node.Trivias == null)
		{
			return;
		}
		foreach (ScriptTrivia item in before ? node.Trivias.Before : node.Trivias.After)
		{
			item.Write(this);
			if (item.Type == ScriptTriviaType.End)
			{
				_hasEndOfStatement = false;
			}
			else if (item.Type == ScriptTriviaType.NewLine || item.Type == ScriptTriviaType.SemiColon)
			{
				_hasEndOfStatement = true;
				if (_expectSpace)
				{
					_expectSpace = false;
				}
			}
		}
	}

	private ScriptTriviaType GetWhitespaceModeFromTrivia(ScriptNode node, bool before)
	{
		if (node.Trivias == null)
		{
			return ScriptTriviaType.Empty;
		}
		if (before)
		{
			List<ScriptTrivia> before2 = node.Trivias.Before;
			for (int num = before2.Count - 1; num >= 0; num--)
			{
				ScriptTriviaType type = before2[num].Type;
				if (type == ScriptTriviaType.WhitespaceFull || type == ScriptTriviaType.Whitespace)
				{
					return type;
				}
			}
		}
		else
		{
			List<ScriptTrivia> after = node.Trivias.After;
			for (int i = 0; i < after.Count; i++)
			{
				ScriptTriviaType type2 = after[i].Type;
				if (type2 == ScriptTriviaType.WhitespaceFull || type2 == ScriptTriviaType.Whitespace)
				{
					return type2;
				}
			}
		}
		return ScriptTriviaType.Empty;
	}
}
