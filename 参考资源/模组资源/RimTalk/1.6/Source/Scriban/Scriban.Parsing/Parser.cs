using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Scriban.Functions;
using Scriban.Syntax;

namespace Scriban.Parsing;

public class Parser
{
	private enum ParseExpressionMode
	{
		Default,
		BasicExpression
	}

	private readonly Lexer _lexer;

	private readonly bool _isLiquid;

	private Lexer.Enumerator _tokenIt;

	private readonly List<Token> _tokensPreview;

	private int _tokensPreviewStart;

	private Token _previousToken;

	private Token _token;

	private bool _inCodeSection;

	private bool _isLiquidTagSection;

	private int _blockLevel;

	private bool _inFrontMatter;

	private bool _isExpressionDepthLimitReached;

	private int _expressionDepth;

	private bool _hasFatalError;

	private readonly bool _isKeepTrivia;

	private readonly List<ScriptTrivia> _trivias;

	private readonly Queue<ScriptStatement> _pendingStatements;

	public readonly ParserOptions Options;

	private static readonly Dictionary<TokenType, ScriptBinaryOperator> BinaryOperators;

	private int _allowNewLineLevel;

	private int _expressionLevel;

	public List<LogMessage> Messages { get; private set; }

	public bool HasErrors { get; private set; }

	private Stack<ScriptNode> Blocks { get; }

	private Token Current => _token;

	private Token Previous => _previousToken;

	public SourceSpan CurrentSpan => GetSpanForToken(Current);

	private ScriptMode CurrentParsingMode { get; set; }

	public Parser(Lexer lexer, ParserOptions? options = null)
	{
		_lexer = lexer ?? throw new ArgumentNullException("lexer");
		_isLiquid = _lexer.Options.Mode == ScriptMode.Liquid;
		_tokensPreview = new List<Token>(4);
		Messages = new List<LogMessage>();
		_trivias = new List<ScriptTrivia>();
		Options = options.GetValueOrDefault();
		CurrentParsingMode = lexer.Options.Mode;
		_isKeepTrivia = lexer.Options.KeepTrivia;
		_pendingStatements = new Queue<ScriptStatement>(2);
		Blocks = new Stack<ScriptNode>();
		_tokenIt = lexer.GetEnumerator();
		NextToken();
	}

	public ScriptPage Run()
	{
		Messages = new List<LogMessage>();
		HasErrors = false;
		_blockLevel = 0;
		_isExpressionDepthLimitReached = false;
		Blocks.Clear();
		ScriptPage scriptPage = Open<ScriptPage>();
		ScriptMode currentParsingMode = CurrentParsingMode;
		switch (currentParsingMode)
		{
		case ScriptMode.FrontMatterOnly:
		case ScriptMode.FrontMatterAndContent:
			if (Current.Type != TokenType.FrontMatterMarker)
			{
				LogError($"When `{CurrentParsingMode}` is enabled, expecting a `{_lexer.Options.FrontMatterMarker}` at the beginning of the text instead of `{Current.GetText(_lexer.Text)}`");
				return null;
			}
			_inFrontMatter = true;
			_inCodeSection = true;
			NextToken();
			scriptPage.FrontMatter = ParseBlockStatement(null);
			if (_inFrontMatter)
			{
				LogError("End of frontmatter `" + _lexer.Options.FrontMatterMarker + "` not found");
			}
			if (currentParsingMode == ScriptMode.FrontMatterOnly)
			{
				return scriptPage;
			}
			break;
		case ScriptMode.ScriptOnly:
			_inCodeSection = true;
			break;
		}
		scriptPage.Body = ParseBlockStatement(null);
		if (scriptPage.FrontMatter != null)
		{
			FixRawStatementAfterFrontMatter(scriptPage);
		}
		if (_lexer.HasErrors)
		{
			foreach (LogMessage error in _lexer.Errors)
			{
				Log(error);
			}
		}
		if (HasErrors)
		{
			return null;
		}
		return scriptPage;
	}

	private void PushTokenToTrivia()
	{
		if (_isKeepTrivia)
		{
			if (Current.Type == TokenType.NewLine)
			{
				_trivias.Add(new ScriptTrivia(CurrentSpan, ScriptTriviaType.NewLine, _lexer.Text));
			}
			else if (Current.Type == TokenType.SemiColon)
			{
				_trivias.Add(new ScriptTrivia(CurrentSpan, ScriptTriviaType.SemiColon, _lexer.Text));
			}
		}
	}

	private T Open<T>() where T : ScriptNode, new()
	{
		T val = new T
		{
			Span = 
			{
				FileName = _lexer.SourcePath,
				Start = Current.Start
			}
		};
		FlushTrivias(val, isBefore: true);
		return val;
	}

	private void FlushTrivias(ScriptNode element, bool isBefore)
	{
		if (_isKeepTrivia && _trivias.Count > 0 && !(element is ScriptBlockStatement))
		{
			element.AddTrivias(_trivias, isBefore);
			_trivias.Clear();
		}
	}

	private T Close<T>(T statement) where T : ScriptNode
	{
		statement.Span.End = Previous.End;
		FlushTrivias(statement, isBefore: false);
		return statement;
	}

	private string GetAsText(Token localToken)
	{
		return localToken.GetText(_lexer.Text);
	}

	private void NextToken()
	{
		_previousToken = _token;
		while (_tokensPreviewStart < _tokensPreview.Count)
		{
			_token = _tokensPreview[_tokensPreviewStart];
			_tokensPreviewStart++;
			if (_tokensPreviewStart == _tokensPreview.Count)
			{
				_tokensPreviewStart = 0;
				_tokensPreview.Clear();
			}
			if (IsHidden(_token.Type))
			{
				if (_isKeepTrivia)
				{
					PushTrivia(_token);
				}
				continue;
			}
			return;
		}
		bool flag;
		while ((flag = _tokenIt.MoveNext()) && IsHidden(_tokenIt.Current.Type))
		{
			if (_isKeepTrivia)
			{
				PushTrivia(_tokenIt.Current);
			}
		}
		_token = (flag ? _tokenIt.Current : Token.Eof);
	}

	private void PushTrivia(Token token)
	{
		ScriptTrivia item = new ScriptTrivia(type: token.Type switch
		{
			TokenType.Comment => ScriptTriviaType.Comment, 
			TokenType.CommentMulti => ScriptTriviaType.CommentMulti, 
			TokenType.Whitespace => ScriptTriviaType.Whitespace, 
			TokenType.WhitespaceFull => ScriptTriviaType.WhitespaceFull, 
			TokenType.NewLine => ScriptTriviaType.NewLine, 
			_ => throw new InvalidOperationException($"Token type `{token.Type}` not supported by trivia"), 
		}, span: GetSpanForToken(token), text: _lexer.Text);
		_trivias.Add(item);
	}

	private Token PeekToken()
	{
		for (int i = _tokensPreviewStart; i < _tokensPreview.Count; i++)
		{
			Token result = _tokensPreview[i];
			if (!IsHidden(result.Type))
			{
				return result;
			}
		}
		while (_tokenIt.MoveNext())
		{
			Token current = _tokenIt.Current;
			_tokensPreview.Add(current);
			if (!IsHidden(current.Type))
			{
				return current;
			}
		}
		return Token.Eof;
	}

	private bool IsHidden(TokenType tokenType)
	{
		switch (tokenType)
		{
		case TokenType.NewLine:
			return _allowNewLineLevel > 0;
		default:
			return false;
		case TokenType.Whitespace:
		case TokenType.WhitespaceFull:
		case TokenType.Comment:
		case TokenType.CommentMulti:
			return true;
		}
	}

	private void LogError(string text, bool isFatal = false)
	{
		LogError(Current, text, isFatal);
	}

	private void LogError(Token tokenArg, string text, bool isFatal = false)
	{
		LogError(GetSpanForToken(tokenArg), text, isFatal);
	}

	private SourceSpan GetSpanForToken(Token tokenArg)
	{
		return new SourceSpan(_lexer.SourcePath, tokenArg.Start, tokenArg.End);
	}

	private void LogError(SourceSpan span, string text, bool isFatal = false)
	{
		Log(new LogMessage(ParserMessageType.Error, span, text), isFatal);
	}

	private void LogError(ScriptNode node, string message, bool isFatal = false)
	{
		LogError(node, node.Span, message, isFatal);
	}

	private void LogError(ScriptNode node, SourceSpan span, string message, bool isFatal = false)
	{
		ScriptSyntaxAttribute scriptSyntaxAttribute = ScriptSyntaxAttribute.Get(node);
		string text = " in";
		if (message.EndsWith("after"))
		{
			text = string.Empty;
		}
		LogError(span, "Error while parsing " + scriptSyntaxAttribute.Name + ": " + message + text + ": " + scriptSyntaxAttribute.Example, isFatal);
	}

	private void Log(LogMessage logMessage, bool isFatal = false)
	{
		if (logMessage == null)
		{
			throw new ArgumentNullException("logMessage");
		}
		Messages.Add(logMessage);
		if (logMessage.Type == ParserMessageType.Error)
		{
			HasErrors = true;
			if (isFatal)
			{
				_hasFatalError = true;
			}
		}
	}

	static Parser()
	{
		BinaryOperators = new Dictionary<TokenType, ScriptBinaryOperator>();
		BinaryOperators.Add(TokenType.Multiply, ScriptBinaryOperator.Multiply);
		BinaryOperators.Add(TokenType.Divide, ScriptBinaryOperator.Divide);
		BinaryOperators.Add(TokenType.DoubleDivide, ScriptBinaryOperator.DivideRound);
		BinaryOperators.Add(TokenType.Plus, ScriptBinaryOperator.Add);
		BinaryOperators.Add(TokenType.Minus, ScriptBinaryOperator.Substract);
		BinaryOperators.Add(TokenType.Modulus, ScriptBinaryOperator.Modulus);
		BinaryOperators.Add(TokenType.ShiftLeft, ScriptBinaryOperator.ShiftLeft);
		BinaryOperators.Add(TokenType.ShiftRight, ScriptBinaryOperator.ShiftRight);
		BinaryOperators.Add(TokenType.EmptyCoalescing, ScriptBinaryOperator.EmptyCoalescing);
		BinaryOperators.Add(TokenType.And, ScriptBinaryOperator.And);
		BinaryOperators.Add(TokenType.Or, ScriptBinaryOperator.Or);
		BinaryOperators.Add(TokenType.CompareEqual, ScriptBinaryOperator.CompareEqual);
		BinaryOperators.Add(TokenType.CompareNotEqual, ScriptBinaryOperator.CompareNotEqual);
		BinaryOperators.Add(TokenType.CompareGreater, ScriptBinaryOperator.CompareGreater);
		BinaryOperators.Add(TokenType.CompareGreaterOrEqual, ScriptBinaryOperator.CompareGreaterOrEqual);
		BinaryOperators.Add(TokenType.CompareLess, ScriptBinaryOperator.CompareLess);
		BinaryOperators.Add(TokenType.CompareLessOrEqual, ScriptBinaryOperator.CompareLessOrEqual);
		BinaryOperators.Add(TokenType.DoubleDot, ScriptBinaryOperator.RangeInclude);
		BinaryOperators.Add(TokenType.DoubleDotLess, ScriptBinaryOperator.RangeExclude);
	}

	private ScriptExpression ParseExpression(ScriptNode parentNode, ScriptExpression parentExpression = null, int precedence = 0, ParseExpressionMode mode = ParseExpressionMode.Default, bool allowAssignment = true)
	{
		bool hasAnonymousFunction = false;
		return ParseExpression(parentNode, ref hasAnonymousFunction, parentExpression, precedence, mode, allowAssignment);
	}

	private ScriptExpression ParseExpression(ScriptNode parentNode, ref bool hasAnonymousFunction, ScriptExpression parentExpression = null, int precedence = 0, ParseExpressionMode mode = ParseExpressionMode.Default, bool allowAssignment = true)
	{
		int num = 0;
		_expressionLevel++;
		int expressionDepth = _expressionDepth;
		EnterExpression();
		try
		{
			ScriptFunctionCall scriptFunctionCall = null;
			while (true)
			{
				num++;
				ScriptExpression scriptExpression = null;
				switch (Current.Type)
				{
				case TokenType.IdentifierSpecial:
				case TokenType.Identifier:
					scriptExpression = ParseVariable();
					if (_isLiquid && parentNode is ScriptPipeCall && Current.Type == TokenType.Colon)
					{
						NextToken();
					}
					if (ScriptVariable.BlockDelegate.Equals(scriptExpression))
					{
						if (num != 1 || _expressionLevel > 1)
						{
							LogError("Cannot use block delegate $$ in a nested expression");
						}
						if (!(parentNode is ScriptExpressionStatement))
						{
							LogError(parentNode, "Cannot use block delegate $$ outside an expression statement");
						}
						return scriptExpression;
					}
					break;
				case TokenType.Integer:
					scriptExpression = ParseInteger();
					break;
				case TokenType.Float:
					scriptExpression = ParseFloat();
					break;
				case TokenType.String:
					scriptExpression = ParseString();
					break;
				case TokenType.ImplicitString:
					scriptExpression = ParseImplicitString();
					break;
				case TokenType.VerbatimString:
					scriptExpression = ParseVerbatimString();
					break;
				case TokenType.OpenParent:
					scriptExpression = ParseParenthesis(ref hasAnonymousFunction);
					break;
				case TokenType.OpenBrace:
					scriptExpression = ParseObjectInitializer();
					break;
				case TokenType.OpenBracket:
					scriptExpression = ParseArrayInitializer();
					break;
				case TokenType.Arroba:
				case TokenType.Caret:
				case TokenType.Not:
				case TokenType.Plus:
				case TokenType.Minus:
					scriptExpression = ParseUnaryExpression(ref hasAnonymousFunction);
					break;
				}
				if (scriptExpression == null)
				{
					break;
				}
				if (scriptExpression is ScriptAnonymousFunction)
				{
					hasAnonymousFunction = true;
				}
				while (true)
				{
					if (!hasAnonymousFunction)
					{
						if (_isLiquid && Current.Type == TokenType.Comma && scriptFunctionCall != null)
						{
							NextToken();
						}
						if (Current.Type == TokenType.Dot)
						{
							Token tokenArg = PeekToken();
							if (tokenArg.Type == TokenType.Identifier)
							{
								NextToken();
								if (GetAsText(Current) == "empty" && PeekToken().Type == TokenType.Question)
								{
									ScriptIsEmptyExpression scriptIsEmptyExpression = Open<ScriptIsEmptyExpression>();
									NextToken();
									NextToken();
									scriptIsEmptyExpression.Target = scriptExpression;
									scriptExpression = Close(scriptIsEmptyExpression);
									continue;
								}
								ScriptMemberExpression scriptMemberExpression = Open<ScriptMemberExpression>();
								scriptMemberExpression.Target = scriptExpression;
								ScriptExpression scriptExpression2 = ParseVariable();
								if (!(scriptExpression2 is ScriptVariable))
								{
									LogError("Unexpected literal member `{member}`");
									return null;
								}
								scriptMemberExpression.Member = (ScriptVariable)scriptExpression2;
								scriptExpression = Close(scriptMemberExpression);
								continue;
							}
							LogError(tokenArg, $"Invalid token `{tokenArg.Type}`. The dot operator is expected to be followed by a plain identifier");
							return null;
						}
						if (Current.Type == TokenType.OpenBracket && scriptExpression is IScriptVariablePath && !IsPreviousCharWhitespace())
						{
							NextToken();
							ScriptIndexerExpression scriptIndexerExpression = Open<ScriptIndexerExpression>();
							scriptIndexerExpression.Target = scriptExpression;
							scriptIndexerExpression.Index = ExpectAndParseExpression(scriptIndexerExpression, ref hasAnonymousFunction, scriptFunctionCall, 0, $"Expecting <index_expression> instead of `{Current.Type}`");
							if (Current.Type != TokenType.CloseBracket)
							{
								LogError($"Unexpected `{Current.Type}`. Expecting ']'");
							}
							else
							{
								NextToken();
							}
							scriptExpression = Close(scriptIndexerExpression);
							continue;
						}
						if (mode != ParseExpressionMode.BasicExpression)
						{
							if (Current.Type == TokenType.Equal)
							{
								ScriptAssignExpression scriptAssignExpression = Open<ScriptAssignExpression>();
								if (_expressionLevel > 1 || !allowAssignment)
								{
									LogError(scriptAssignExpression, "Expression is only allowed for a top level assignment");
								}
								NextToken();
								scriptAssignExpression.Target = TransformKeyword(scriptExpression);
								scriptAssignExpression.Value = ExpectAndParseExpression(scriptAssignExpression, ref hasAnonymousFunction, parentExpression);
								scriptExpression = Close(scriptAssignExpression);
								continue;
							}
							if (BinaryOperators.TryGetValue(Current.Type, out var value) || (_isLiquid && TryLiquidBinaryOperator(out value)))
							{
								int operatorPrecedence = GetOperatorPrecedence(value);
								if (operatorPrecedence > precedence)
								{
									EnterExpression();
									ScriptBinaryExpression scriptBinaryExpression = Open<ScriptBinaryExpression>();
									scriptBinaryExpression.Left = scriptExpression;
									scriptBinaryExpression.Operator = value;
									NextToken();
									scriptBinaryExpression.Right = ExpectAndParseExpression(scriptBinaryExpression, ref hasAnonymousFunction, scriptFunctionCall ?? parentExpression, operatorPrecedence, $"Expecting an <expression> to the right of the operator instead of `{Current.Type}`");
									scriptExpression = Close(scriptBinaryExpression);
									continue;
								}
							}
							else if (precedence <= 0)
							{
								if (StartAsExpression())
								{
									if (parentExpression == null)
									{
										IScriptNamedArgumentContainer scriptNamedArgumentContainer = parentNode as IScriptNamedArgumentContainer;
										if (Current.Type != TokenType.Identifier || (!(parentNode is IScriptNamedArgumentContainer) && (_isLiquid || PeekToken().Type != TokenType.Colon)))
										{
											break;
										}
										if (scriptNamedArgumentContainer == null)
										{
											if (scriptFunctionCall == null)
											{
												scriptFunctionCall = Open<ScriptFunctionCall>();
												scriptFunctionCall.Target = scriptExpression;
												scriptFunctionCall.Span.Start = scriptExpression.Span.Start;
											}
											else
											{
												scriptFunctionCall.Arguments.Add(scriptExpression);
											}
											Close(scriptExpression);
										}
										while (Current.Type == TokenType.Identifier)
										{
											ScriptNamedArgument scriptNamedArgument = Open<ScriptNamedArgument>();
											string asText = GetAsText(Current);
											scriptNamedArgument.Name = asText;
											NextToken();
											if (scriptNamedArgumentContainer != null)
											{
												scriptNamedArgumentContainer.AddParameter(Close(scriptNamedArgument));
											}
											else
											{
												scriptFunctionCall.Arguments.Add(scriptNamedArgument);
											}
											if (Current.Type == TokenType.Colon)
											{
												NextToken();
												scriptNamedArgument.Value = ExpectAndParseExpression(parentNode, null, 0, null, ParseExpressionMode.BasicExpression);
												scriptNamedArgument.Span.End = scriptNamedArgument.Value.Span.End;
											}
											if (scriptFunctionCall != null)
											{
												scriptFunctionCall.Span.End = scriptNamedArgument.Span.End;
											}
										}
										if (scriptFunctionCall != null)
										{
											scriptExpression = scriptFunctionCall;
											scriptFunctionCall = null;
										}
									}
								}
								else if (Current.Type == TokenType.Pipe)
								{
									if (scriptFunctionCall != null)
									{
										scriptFunctionCall.Arguments.Add(scriptExpression);
										scriptExpression = scriptFunctionCall;
									}
									ScriptPipeCall scriptPipeCall = Open<ScriptPipeCall>();
									scriptPipeCall.From = scriptExpression;
									NextToken();
									scriptPipeCall.To = ExpectAndParseExpression(scriptPipeCall, ref hasAnonymousFunction);
									return Close(scriptPipeCall);
								}
							}
						}
					}
					if (scriptFunctionCall != null)
					{
						scriptFunctionCall.Arguments.Add(scriptExpression);
						scriptFunctionCall.Span.End = scriptExpression.Span.End;
						return scriptFunctionCall;
					}
					return Close(scriptExpression);
				}
				if (scriptFunctionCall == null)
				{
					scriptFunctionCall = Open<ScriptFunctionCall>();
					scriptFunctionCall.Target = scriptExpression;
					if (_isLiquid && Options.LiquidFunctionsToScriban)
					{
						TransformLiquidFunctionCallToScriban(scriptFunctionCall);
					}
					scriptFunctionCall.Span.Start = scriptExpression.Span.Start;
				}
				else
				{
					scriptFunctionCall.Arguments.Add(scriptExpression);
				}
			}
			if (scriptFunctionCall != null)
			{
				LogError($"Unexpected token `{GetAsText(Current)}` while parsing function call `{scriptFunctionCall}`");
			}
			else
			{
				LogError("Unexpected token `" + GetAsText(Current) + "` while parsing expression");
			}
			return null;
		}
		finally
		{
			LeaveExpression();
			_expressionDepth = expressionDepth;
			_expressionLevel--;
		}
	}

	private ScriptExpression ParseArrayInitializer()
	{
		ScriptArrayInitializerExpression scriptArrayInitializerExpression = Open<ScriptArrayInitializerExpression>();
		_allowNewLineLevel++;
		NextToken();
		bool flag = false;
		while (Current.Type != TokenType.CloseBracket)
		{
			if (!flag)
			{
				ScriptExpression scriptExpression = ExpectAndParseExpression(scriptArrayInitializerExpression);
				if (scriptExpression == null)
				{
					break;
				}
				scriptArrayInitializerExpression.Values.Add(scriptExpression);
				if (Current.Type == TokenType.Comma)
				{
					if (_isKeepTrivia)
					{
						scriptExpression.AddTrivia(new ScriptTrivia(CurrentSpan, ScriptTriviaType.Comma, _lexer.Text), before: false);
					}
					NextToken();
					if (_isKeepTrivia && _trivias.Count > 0)
					{
						scriptExpression.AddTrivias(_trivias, before: false);
						_trivias.Clear();
					}
				}
				else
				{
					flag = true;
				}
				continue;
			}
			LogError($"Unexpected token `{Current.Type}`. Expecting a closing ] for the array initializer");
			break;
		}
		_allowNewLineLevel--;
		NextToken();
		return Close(scriptArrayInitializerExpression);
	}

	private ScriptExpression ParseObjectInitializer()
	{
		ScriptObjectInitializerExpression scriptObjectInitializerExpression = Open<ScriptObjectInitializerExpression>();
		_allowNewLineLevel++;
		NextToken();
		bool flag = false;
		while (Current.Type != TokenType.CloseBrace)
		{
			if (!flag && (Current.Type == TokenType.Identifier || Current.Type == TokenType.String))
			{
				Token current = Current;
				ScriptExpression scriptExpression = ParseExpression(scriptObjectInitializerExpression);
				ScriptVariable scriptVariable = scriptExpression as ScriptVariable;
				ScriptLiteral scriptLiteral = scriptExpression as ScriptLiteral;
				if (scriptVariable == null && scriptLiteral == null)
				{
					LogError(current, $"Unexpected member type `{scriptExpression}/{ScriptSyntaxAttribute.Get(scriptExpression).Name}` found for object initializer member name");
					break;
				}
				if (scriptLiteral != null && !(scriptLiteral.Value is string))
				{
					LogError(current, $"Invalid literal member `{scriptLiteral.Value}/{scriptLiteral.Value?.GetType()}` found for object initializer member name. Only literal string or identifier name are allowed");
					break;
				}
				if (scriptVariable != null && scriptVariable.Scope != ScriptVariableScope.Global)
				{
					LogError("Expecting a simple identifier for member names");
					break;
				}
				if (Current.Type != TokenType.Colon)
				{
					LogError($"Unexpected token `{Current.Type}` Expecting a colon : after identifier `{scriptVariable.Name}` for object initializer member name");
					break;
				}
				NextToken();
				if (!StartAsExpression())
				{
					LogError($"Unexpected token `{Current.Type}`. Expecting an expression for the value of the member instead of `{GetAsText(Current)}`");
					break;
				}
				ScriptExpression scriptExpression2 = ParseExpression(scriptObjectInitializerExpression);
				scriptObjectInitializerExpression.Members[scriptExpression] = scriptExpression2;
				if (Current.Type == TokenType.Comma)
				{
					if (_isKeepTrivia)
					{
						scriptExpression2.AddTrivia(new ScriptTrivia(CurrentSpan, ScriptTriviaType.Comma, _lexer.Text), before: false);
					}
					NextToken();
					if (_isKeepTrivia && _trivias.Count > 0)
					{
						scriptExpression2.AddTrivias(_trivias, before: false);
						_trivias.Clear();
					}
				}
				else
				{
					flag = true;
				}
				continue;
			}
			LogError($"Unexpected token `{Current.Type}` while parsing object initializer. Expecting a simple identifier for the member name instead of `{GetAsText(Current)}`");
			break;
		}
		_allowNewLineLevel--;
		NextToken();
		return Close(scriptObjectInitializerExpression);
	}

	private ScriptExpression ParseParenthesis(ref bool hasAnonymousFunction)
	{
		ScriptNestedExpression scriptNestedExpression = Open<ScriptNestedExpression>();
		NextToken();
		scriptNestedExpression.Expression = ExpectAndParseExpression(scriptNestedExpression, ref hasAnonymousFunction);
		if (Current.Type == TokenType.CloseParent)
		{
			NextToken();
		}
		else
		{
			LogError(Current, $"Invalid token `{Current.Type}`. Expecting closing ) for opening `{scriptNestedExpression.Span.Start}`");
		}
		return Close(scriptNestedExpression);
	}

	private ScriptExpression ParseUnaryExpression(ref bool hasAnonymousFunction)
	{
		ScriptUnaryExpression scriptUnaryExpression = Open<ScriptUnaryExpression>();
		switch (Current.Type)
		{
		case TokenType.Not:
			scriptUnaryExpression.Operator = ScriptUnaryOperator.Not;
			break;
		case TokenType.Minus:
			scriptUnaryExpression.Operator = ScriptUnaryOperator.Negate;
			break;
		case TokenType.Plus:
			scriptUnaryExpression.Operator = ScriptUnaryOperator.Plus;
			break;
		case TokenType.Arroba:
			scriptUnaryExpression.Operator = ScriptUnaryOperator.FunctionAlias;
			break;
		case TokenType.Caret:
			scriptUnaryExpression.Operator = ScriptUnaryOperator.FunctionParametersExpand;
			break;
		default:
			LogError($"Unexpected token `{Current.Type}` for unary expression");
			break;
		}
		int operatorPrecedence = GetOperatorPrecedence(scriptUnaryExpression.Operator);
		NextToken();
		scriptUnaryExpression.Right = ExpectAndParseExpression(scriptUnaryExpression, ref hasAnonymousFunction, null, operatorPrecedence);
		return Close(scriptUnaryExpression);
	}

	private ScriptExpression TransformKeyword(ScriptExpression leftOperand)
	{
		if (_isLiquid && leftOperand is IScriptVariablePath && IsScribanKeyword(((IScriptVariablePath)leftOperand).GetFirstPath()) && !(leftOperand is ScriptNestedExpression))
		{
			ScriptNestedExpression scriptNestedExpression = new ScriptNestedExpression
			{
				Expression = leftOperand,
				Span = leftOperand.Span
			};
			if (_isKeepTrivia && leftOperand.Trivias != null)
			{
				scriptNestedExpression.Trivias = leftOperand.Trivias;
				leftOperand.Trivias = null;
			}
			return scriptNestedExpression;
		}
		return leftOperand;
	}

	private void TransformLiquidFunctionCallToScriban(ScriptFunctionCall functionCall)
	{
		ScriptVariable scriptVariable = functionCall.Target as ScriptVariable;
		if (scriptVariable != null && LiquidBuiltinsFunctions.TryLiquidToScriban(scriptVariable.Name, out var target, out var member))
		{
			ScriptMemberExpression scriptMemberExpression = new ScriptMemberExpression
			{
				Span = scriptVariable.Span,
				Target = new ScriptVariableGlobal(target)
				{
					Span = scriptVariable.Span
				},
				Member = new ScriptVariableGlobal(member)
				{
					Span = scriptVariable.Span
				}
			};
			if (_isKeepTrivia && scriptVariable.Trivias != null)
			{
				scriptMemberExpression.Target.AddTrivias(scriptVariable.Trivias.Before, before: true);
				scriptMemberExpression.Member.AddTrivias(scriptVariable.Trivias.After, before: false);
			}
			functionCall.Target = scriptMemberExpression;
		}
	}

	private void EnterExpression()
	{
		_expressionDepth++;
		if (Options.ExpressionDepthLimit.HasValue && !_isExpressionDepthLimitReached && _expressionDepth > Options.ExpressionDepthLimit.Value)
		{
			LogError(GetSpanForToken(Previous), $"The statement depth limit `{Options.ExpressionDepthLimit.Value}` was reached when parsing this statement");
			_isExpressionDepthLimitReached = true;
		}
	}

	private ScriptExpression ExpectAndParseExpression(ScriptNode parentNode, ScriptExpression parentExpression = null, int newPrecedence = 0, string message = null, ParseExpressionMode mode = ParseExpressionMode.Default, bool allowAssignment = true)
	{
		if (StartAsExpression())
		{
			return ParseExpression(parentNode, parentExpression, newPrecedence, mode, allowAssignment);
		}
		LogError(parentNode, CurrentSpan, message ?? $"Expecting <expression> instead of `{Current.Type}`");
		return null;
	}

	private ScriptExpression ExpectAndParseExpression(ScriptNode parentNode, ref bool hasAnonymousExpression, ScriptExpression parentExpression = null, int newPrecedence = 0, string message = null, ParseExpressionMode mode = ParseExpressionMode.Default)
	{
		if (StartAsExpression())
		{
			return ParseExpression(parentNode, ref hasAnonymousExpression, parentExpression, newPrecedence, mode);
		}
		LogError(parentNode, CurrentSpan, message ?? $"Expecting <expression> instead of `{Current.Type}`");
		return null;
	}

	private ScriptExpression ExpectAndParseExpressionAndAnonymous(ScriptNode parentNode, out bool hasAnonymousFunction, ParseExpressionMode mode = ParseExpressionMode.Default)
	{
		hasAnonymousFunction = false;
		if (StartAsExpression())
		{
			return ParseExpression(parentNode, ref hasAnonymousFunction, null, 0, mode);
		}
		LogError(parentNode, CurrentSpan, $"Expecting <expression> instead of `{Current.Type}`");
		return null;
	}

	private bool StartAsExpression()
	{
		switch (Current.Type)
		{
		case TokenType.IdentifierSpecial:
		case TokenType.Identifier:
		case TokenType.Integer:
		case TokenType.Float:
		case TokenType.String:
		case TokenType.ImplicitString:
		case TokenType.VerbatimString:
		case TokenType.Arroba:
		case TokenType.Caret:
		case TokenType.Not:
		case TokenType.Plus:
		case TokenType.Minus:
		case TokenType.OpenParent:
		case TokenType.OpenBrace:
		case TokenType.OpenBracket:
			return true;
		default:
			return false;
		}
	}

	private bool TryLiquidBinaryOperator(out ScriptBinaryOperator binOp)
	{
		binOp = ScriptBinaryOperator.EmptyCoalescing;
		if (Current.Type != TokenType.Identifier)
		{
			return false;
		}
		switch (GetAsText(Current))
		{
		case "or":
			binOp = ScriptBinaryOperator.Or;
			return true;
		case "and":
			binOp = ScriptBinaryOperator.And;
			return true;
		case "contains":
			binOp = ScriptBinaryOperator.LiquidContains;
			return true;
		case "startsWith":
			binOp = ScriptBinaryOperator.LiquidStartsWith;
			return true;
		case "endsWith":
			binOp = ScriptBinaryOperator.LiquidEndsWith;
			return true;
		case "hasKey":
			binOp = ScriptBinaryOperator.LiquidHasKey;
			return true;
		case "hasValue":
			binOp = ScriptBinaryOperator.LiquidHasValue;
			return true;
		default:
			return false;
		}
	}

	private static int GetOperatorPrecedence(ScriptBinaryOperator op)
	{
		switch (op)
		{
		case ScriptBinaryOperator.EmptyCoalescing:
			return 20;
		case ScriptBinaryOperator.ShiftLeft:
		case ScriptBinaryOperator.ShiftRight:
			return 25;
		case ScriptBinaryOperator.Or:
			return 30;
		case ScriptBinaryOperator.And:
			return 40;
		case ScriptBinaryOperator.CompareEqual:
		case ScriptBinaryOperator.CompareNotEqual:
			return 50;
		case ScriptBinaryOperator.CompareLessOrEqual:
		case ScriptBinaryOperator.CompareGreaterOrEqual:
		case ScriptBinaryOperator.CompareLess:
		case ScriptBinaryOperator.CompareGreater:
			return 60;
		case ScriptBinaryOperator.LiquidContains:
		case ScriptBinaryOperator.LiquidStartsWith:
		case ScriptBinaryOperator.LiquidEndsWith:
		case ScriptBinaryOperator.LiquidHasKey:
		case ScriptBinaryOperator.LiquidHasValue:
			return 65;
		case ScriptBinaryOperator.Add:
		case ScriptBinaryOperator.Substract:
			return 70;
		case ScriptBinaryOperator.Divide:
		case ScriptBinaryOperator.DivideRound:
		case ScriptBinaryOperator.Multiply:
		case ScriptBinaryOperator.Modulus:
			return 80;
		case ScriptBinaryOperator.RangeInclude:
		case ScriptBinaryOperator.RangeExclude:
			return 90;
		default:
			return 0;
		}
	}

	private static int GetOperatorPrecedence(ScriptUnaryOperator op)
	{
		if ((uint)op <= 4u)
		{
			return 100;
		}
		return 0;
	}

	private bool IsPreviousCharWhitespace()
	{
		int num = Current.Start.Offset - 1;
		if (num >= 0)
		{
			return char.IsWhiteSpace(_lexer.Text[num]);
		}
		return false;
	}

	private void LeaveExpression()
	{
		_expressionDepth--;
	}

	private ScriptBlockStatement ParseBlockStatement(ScriptStatement parentStatement)
	{
		Blocks.Push(parentStatement);
		_blockLevel++;
		EnterExpression();
		ScriptBlockStatement scriptBlockStatement = Open<ScriptBlockStatement>();
		ScriptStatement statement;
		bool hasEnd;
		while (TryParseStatement(parentStatement, out statement, out hasEnd))
		{
			if (statement != null)
			{
				scriptBlockStatement.Statements.Add(statement);
			}
			if (hasEnd)
			{
				break;
			}
		}
		if (!hasEnd && _blockLevel > 1)
		{
			if (_isLiquid)
			{
				ScriptSyntaxAttribute scriptSyntaxAttribute = ScriptSyntaxAttribute.Get(parentStatement);
				LogError(parentStatement, parentStatement?.Span ?? CurrentSpan, "The `end" + scriptSyntaxAttribute.Name + "` was not found");
			}
			else
			{
				LogError(parentStatement, GetSpanForToken(Previous), "The <end> statement was not found");
			}
		}
		LeaveExpression();
		_blockLevel--;
		Blocks.Pop();
		return Close(scriptBlockStatement);
	}

	private bool TryParseStatement(ScriptStatement parent, out ScriptStatement statement, out bool hasEnd)
	{
		hasEnd = false;
		bool nextStatement = true;
		statement = null;
		while (true)
		{
			if (_hasFatalError)
			{
				return false;
			}
			if (_pendingStatements.Count > 0)
			{
				statement = _pendingStatements.Dequeue();
				return true;
			}
			switch (Current.Type)
			{
			case TokenType.Eof:
				nextStatement = false;
				break;
			case TokenType.Raw:
			case TokenType.Escape:
				statement = ParseRawStatement();
				if (parent is ScriptCaseStatement)
				{
					statement = null;
					continue;
				}
				break;
			case TokenType.CodeEnter:
			case TokenType.LiquidTagEnter:
				if (_inCodeSection)
				{
					LogError("Unexpected token while already in a code block");
				}
				_isLiquidTagSection = Current.Type == TokenType.LiquidTagEnter;
				_inCodeSection = true;
				if (_isKeepTrivia && (_trivias.Count > 0 || Previous.Type == TokenType.CodeEnter))
				{
					ScriptRawStatement scriptRawStatement = Open<ScriptRawStatement>();
					Close(scriptRawStatement);
					if (_trivias.Count > 0)
					{
						scriptRawStatement.Trivias.After.AddRange(scriptRawStatement.Trivias.Before);
						scriptRawStatement.Trivias.Before.Clear();
						SourceSpan span = scriptRawStatement.Trivias.After[0].Span;
						SourceSpan span2 = scriptRawStatement.Trivias.After[scriptRawStatement.Trivias.After.Count - 1].Span;
						scriptRawStatement.Span = new SourceSpan(span.FileName, span.Start, span2.End);
					}
					else
					{
						scriptRawStatement.AddTrivia(new ScriptTrivia(CurrentSpan, ScriptTriviaType.Empty, null), before: false);
					}
					statement = scriptRawStatement;
				}
				NextToken();
				if (Current.Type == TokenType.CodeExit)
				{
					ScriptNopStatement scriptNopStatement = Open<ScriptNopStatement>();
					Close(scriptNopStatement);
					if (statement == null)
					{
						statement = scriptNopStatement;
					}
					else
					{
						_pendingStatements.Enqueue(scriptNopStatement);
					}
				}
				if (statement == null)
				{
					continue;
				}
				break;
			case TokenType.FrontMatterMarker:
				if (_inFrontMatter)
				{
					_inFrontMatter = false;
					_inCodeSection = false;
					if (CurrentParsingMode != ScriptMode.FrontMatterOnly)
					{
						NextToken();
					}
					if (CurrentParsingMode == ScriptMode.FrontMatterAndContent || CurrentParsingMode == ScriptMode.FrontMatterOnly)
					{
						CurrentParsingMode = ScriptMode.Default;
						nextStatement = false;
					}
				}
				else
				{
					LogError("Unexpected frontmatter marker `" + _lexer.Options.FrontMatterMarker + "` while not inside a frontmatter");
					NextToken();
				}
				break;
			case TokenType.CodeExit:
			case TokenType.LiquidTagExit:
			{
				if (!_inCodeSection)
				{
					LogError("Unexpected code block exit '}}' while no code block enter '{{' has been found");
				}
				else if (CurrentParsingMode == ScriptMode.ScriptOnly)
				{
					LogError("Unexpected code clock exit '}}' while parsing in script only mode. '}}' is not allowed.");
				}
				_isLiquidTagSection = false;
				_inCodeSection = false;
				if (_isKeepTrivia)
				{
					_trivias.Clear();
				}
				NextToken();
				if (!_isKeepTrivia || (Current.Type != TokenType.CodeEnter && Current.Type != TokenType.Eof))
				{
					continue;
				}
				ScriptRawStatement scriptRawStatement2 = Open<ScriptRawStatement>();
				Close(scriptRawStatement2);
				if (_trivias.Count > 0)
				{
					SourceSpan span3 = scriptRawStatement2.Trivias.Before[0].Span;
					SourceSpan span4 = scriptRawStatement2.Trivias.Before[scriptRawStatement2.Trivias.Before.Count - 1].Span;
					scriptRawStatement2.Span = new SourceSpan(span3.FileName, span3.Start, span4.End);
				}
				else
				{
					scriptRawStatement2.AddTrivia(new ScriptTrivia(CurrentSpan, ScriptTriviaType.Empty, null), before: false);
				}
				statement = scriptRawStatement2;
				break;
			}
			default:
				if (_inCodeSection)
				{
					switch (Current.Type)
					{
					case TokenType.NewLine:
					case TokenType.SemiColon:
						PushTokenToTrivia();
						NextToken();
						continue;
					case TokenType.IdentifierSpecial:
					case TokenType.Identifier:
					{
						string asText = GetAsText(Current);
						if (_isLiquid)
						{
							ParseLiquidStatement(asText, parent, ref statement, ref hasEnd, ref nextStatement);
						}
						else
						{
							ParseScribanStatement(asText, parent, ref statement, ref hasEnd, ref nextStatement);
						}
						break;
					}
					default:
						if (StartAsExpression())
						{
							statement = ParseExpressionStatement();
							break;
						}
						nextStatement = false;
						LogError($"Unexpected token {Current.Type}");
						break;
					}
				}
				else
				{
					nextStatement = false;
					LogError($"Unexpected token {Current.Type} while not in a code block {{ ... }}");
				}
				break;
			}
			break;
		}
		return nextStatement;
	}

	private ScriptCaptureStatement ParseCaptureStatement()
	{
		ScriptCaptureStatement scriptCaptureStatement = Open<ScriptCaptureStatement>();
		NextToken();
		scriptCaptureStatement.Target = ExpectAndParseExpression(scriptCaptureStatement);
		ExpectEndOfStatement(scriptCaptureStatement);
		scriptCaptureStatement.Body = ParseBlockStatement(scriptCaptureStatement);
		return Close(scriptCaptureStatement);
	}

	private ScriptCaseStatement ParseCaseStatement()
	{
		ScriptCaseStatement scriptCaseStatement = Open<ScriptCaseStatement>();
		NextToken();
		scriptCaseStatement.Value = ExpectAndParseExpression(scriptCaseStatement, null, 0, null, ParseExpressionMode.Default, allowAssignment: false);
		if (ExpectEndOfStatement(scriptCaseStatement))
		{
			FlushTrivias(scriptCaseStatement.Value, isBefore: false);
			scriptCaseStatement.Body = ParseBlockStatement(scriptCaseStatement);
		}
		return Close(scriptCaseStatement);
	}

	private ScriptConditionStatement ParseElseStatement(bool isElseIf)
	{
		if (_isLiquid && isElseIf)
		{
			return ParseIfStatement(invert: false, isElseIf: true);
		}
		Token localToken = PeekToken();
		if (!_isLiquid && localToken.Type == TokenType.Identifier && GetAsText(localToken) == "if")
		{
			NextToken();
			if (_isKeepTrivia)
			{
				_trivias.Clear();
			}
			return ParseIfStatement(invert: false, isElseIf: true);
		}
		ScriptElseStatement scriptElseStatement = Open<ScriptElseStatement>();
		NextToken();
		if (ExpectEndOfStatement(scriptElseStatement))
		{
			scriptElseStatement.Body = ParseBlockStatement(scriptElseStatement);
		}
		return Close(scriptElseStatement);
	}

	private ScriptExpressionStatement ParseExpressionStatement()
	{
		ScriptExpressionStatement scriptExpressionStatement = Open<ScriptExpressionStatement>();
		scriptExpressionStatement.Expression = TransformKeyword(ExpectAndParseExpressionAndAnonymous(scriptExpressionStatement, out var hasAnonymousFunction));
		if (!hasAnonymousFunction)
		{
			ExpectEndOfStatement(scriptExpressionStatement);
		}
		return Close(scriptExpressionStatement);
	}

	private T ParseForStatement<T>() where T : ScriptForStatement, new()
	{
		T val = Open<T>();
		NextToken();
		val.Variable = ExpectAndParseExpression(val, null, 0, null, ParseExpressionMode.BasicExpression);
		if (val.Variable != null)
		{
			if (!(val.Variable is IScriptVariablePath))
			{
				LogError(val, $"Expecting a variable instead of `{val.Variable}`");
			}
			if (val.Variable is ScriptVariableGlobal scriptVariableGlobal)
			{
				ScriptVariable scriptVariable = ScriptVariable.Create(scriptVariableGlobal.Name, ScriptVariableScope.Loop);
				scriptVariable.Span = scriptVariableGlobal.Span;
				scriptVariable.Trivias = scriptVariableGlobal.Trivias;
				val.Variable = scriptVariable;
			}
			if (Current.Type != TokenType.Identifier || GetAsText(Current) != "in")
			{
				LogError(val, $"Expecting 'in' word instead of `{Current.Type} {GetAsText(Current)}`");
			}
			else
			{
				NextToken();
			}
			val.Iterator = ExpectAndParseExpression(val);
			if (ExpectEndOfStatement(val))
			{
				FlushTrivias(val.IteratorOrLastParameter, isBefore: false);
				val.Body = ParseBlockStatement(val);
			}
		}
		return Close(val);
	}

	private ScriptIfStatement ParseIfStatement(bool invert, bool isElseIf)
	{
		ScriptIfStatement scriptIfStatement = Open<ScriptIfStatement>();
		scriptIfStatement.IsElseIf = isElseIf;
		scriptIfStatement.InvertCondition = invert;
		NextToken();
		scriptIfStatement.Condition = ExpectAndParseExpression(scriptIfStatement, null, 0, null, ParseExpressionMode.Default, allowAssignment: false);
		if (ExpectEndOfStatement(scriptIfStatement))
		{
			FlushTrivias(scriptIfStatement.Condition, isBefore: false);
			scriptIfStatement.Then = ParseBlockStatement(scriptIfStatement);
		}
		return Close(scriptIfStatement);
	}

	private ScriptRawStatement ParseRawStatement()
	{
		ScriptRawStatement scriptRawStatement = Open<ScriptRawStatement>();
		TextPosition end = Current.End;
		if (Current.Type == TokenType.Escape)
		{
			NextToken();
			if (Current.Type < TokenType.EscapeCount1 && Current.Type > TokenType.EscapeCount9)
			{
				LogError(Current, "Unexpected token `" + GetAsText(Current) + "` found. Expecting EscapeCount1-9.");
			}
			else
			{
				scriptRawStatement.EscapeCount = (int)(Current.Type - 8 + 1);
			}
		}
		scriptRawStatement.Text = _lexer.Text;
		NextToken();
		Close(scriptRawStatement);
		scriptRawStatement.Span.End = end;
		return scriptRawStatement;
	}

	private ScriptWhenStatement ParseWhenStatement()
	{
		ScriptWhenStatement scriptWhenStatement = Open<ScriptWhenStatement>();
		NextToken();
		while (IsVariableOrLiteral(Current))
		{
			ScriptExpression item = ParseVariableOrLiteral();
			scriptWhenStatement.Values.Add(item);
			if (Current.Type == TokenType.Comma || (!_isLiquid && Current.Type == TokenType.Or) || (_isLiquid && GetAsText(Current) == "or"))
			{
				NextToken();
			}
		}
		if (scriptWhenStatement.Values.Count == 0)
		{
			LogError(Current, "When is expecting at least one value.");
		}
		if (ExpectEndOfStatement(scriptWhenStatement))
		{
			if (_isKeepTrivia && scriptWhenStatement.Values.Count > 0)
			{
				FlushTrivias(scriptWhenStatement.Values[scriptWhenStatement.Values.Count - 1], isBefore: false);
			}
			scriptWhenStatement.Body = ParseBlockStatement(scriptWhenStatement);
		}
		return Close(scriptWhenStatement);
	}

	private void CheckNotInCase(ScriptStatement parent, Token token)
	{
		if (parent is ScriptCaseStatement)
		{
			LogError(token, "Unexpected statement/expression `" + GetAsText(token) + "` in the body of a `case` statement. Only `when`/`else` are expected.");
		}
	}

	private ScriptVariable ExpectAndParseVariable(ScriptNode parentNode)
	{
		if (parentNode == null)
		{
			throw new ArgumentNullException("parentNode");
		}
		if (Current.Type == TokenType.Identifier || Current.Type == TokenType.IdentifierSpecial)
		{
			ScriptExpression scriptExpression = ParseVariable();
			ScriptVariable scriptVariable = scriptExpression as ScriptVariable;
			if (scriptVariable != null && scriptVariable.Scope != ScriptVariableScope.Loop)
			{
				return (ScriptVariable)scriptExpression;
			}
			LogError(parentNode, $"Unexpected variable `{scriptExpression}`");
		}
		else
		{
			LogError(parentNode, $"Expecting a variable instead of `{Current.Type}`");
		}
		return null;
	}

	private bool ExpectEndOfStatement(ScriptStatement statement)
	{
		if (_isLiquid)
		{
			if (Current.Type == TokenType.CodeExit || (_isLiquidTagSection && Current.Type == TokenType.LiquidTagExit))
			{
				return true;
			}
		}
		else if (Current.Type == TokenType.NewLine || Current.Type == TokenType.CodeExit || Current.Type == TokenType.SemiColon || Current.Type == TokenType.Eof)
		{
			if (Current.Type == TokenType.NewLine || Current.Type == TokenType.SemiColon)
			{
				PushTokenToTrivia();
				NextToken();
			}
			return true;
		}
		LogError(statement, "Invalid token found `" + GetAsText(Current) + "`. Expecting <EOL>/end of line after", isFatal: true);
		return false;
	}

	private static bool ExpectStatementEnd(ScriptNode scriptNode)
	{
		if ((!(scriptNode is ScriptIfStatement) || ((ScriptIfStatement)scriptNode).IsElseIf) && !(scriptNode is ScriptForStatement) && !(scriptNode is ScriptCaptureStatement) && !(scriptNode is ScriptWithStatement) && !(scriptNode is ScriptWhileStatement) && !(scriptNode is ScriptWrapStatement) && !(scriptNode is ScriptCaseStatement) && !(scriptNode is ScriptFunction))
		{
			return scriptNode is ScriptAnonymousFunction;
		}
		return true;
	}

	private ScriptStatement FindFirstStatementExpectingEnd()
	{
		foreach (ScriptNode block in Blocks)
		{
			if (ExpectStatementEnd(block))
			{
				return (ScriptStatement)block;
			}
		}
		return null;
	}

	private void ParseLiquidStatement(string identifier, ScriptStatement parent, ref ScriptStatement statement, ref bool hasEnd, ref bool nextStatement)
	{
		Token current = Current;
		if (!_isLiquidTagSection)
		{
			statement = ParseLiquidExpressionStatement(parent);
			return;
		}
		if (identifier != "when" && identifier != "case" && !identifier.StartsWith("end") && parent is ScriptCaseStatement)
		{
			LogError(current, "Unexpected statement/expression `" + GetAsText(current) + "` in the body of a `case` statement. Only `when`/`else` are expected.");
		}
		ScriptStatement scriptStatement = null;
		string text = null;
		switch (identifier)
		{
		case "endif":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptIfStatement;
			text = "`if`/`else`";
			break;
		case "endifchanged":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptIfStatement;
			text = "`ifchanged`";
			break;
		case "endunless":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptIfStatement;
			text = "`unless`";
			break;
		case "endfor":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptForStatement;
			text = "`for`";
			break;
		case "endcase":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptCaseStatement;
			text = "`case`";
			break;
		case "endcapture":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptCaptureStatement;
			text = "`capture`";
			break;
		case "endtablerow":
			scriptStatement = FindFirstStatementExpectingEnd() as ScriptTableRowStatement;
			text = "`tablerow`";
			break;
		case "case":
			statement = ParseCaseStatement();
			break;
		case "when":
		{
			ScriptWhenStatement scriptWhenStatement = ParseWhenStatement();
			ScriptConditionStatement scriptConditionStatement = parent as ScriptConditionStatement;
			if (parent is ScriptWhenStatement)
			{
				((ScriptWhenStatement)scriptConditionStatement).Next = scriptWhenStatement;
			}
			else if (parent is ScriptCaseStatement)
			{
				statement = scriptWhenStatement;
			}
			else
			{
				nextStatement = false;
				LogError(current, "A `when` condition must be preceded by another `when`/`else`/`case` condition");
			}
			hasEnd = true;
			break;
		}
		case "if":
			statement = ParseIfStatement(invert: false, isElseIf: false);
			break;
		case "ifchanged":
			statement = ParseLiquidIfChanged();
			break;
		case "unless":
			CheckNotInCase(parent, current);
			statement = ParseIfStatement(invert: true, isElseIf: false);
			break;
		case "elsif":
		case "else":
		{
			ScriptConditionStatement scriptConditionStatement2 = ParseElseStatement(identifier == "elsif");
			ScriptConditionStatement scriptConditionStatement3 = parent as ScriptConditionStatement;
			if (parent is ScriptIfStatement || parent is ScriptWhenStatement)
			{
				if (parent is ScriptIfStatement)
				{
					((ScriptIfStatement)scriptConditionStatement3).Else = scriptConditionStatement2;
				}
				else
				{
					if (identifier == "elseif")
					{
						LogError(current, "A elsif condition is not allowed within a when/case condition");
					}
					((ScriptWhenStatement)scriptConditionStatement3).Next = scriptConditionStatement2;
				}
			}
			else
			{
				nextStatement = false;
				LogError(current, "A else condition must be preceded by another if/else/when condition");
			}
			hasEnd = true;
			break;
		}
		case "for":
			statement = ParseForStatement<ScriptForStatement>();
			break;
		case "tablerow":
			statement = ParseForStatement<ScriptTableRowStatement>();
			break;
		case "cycle":
			statement = ParseLiquidCycleStatement();
			break;
		case "break":
			statement = Open<ScriptBreakStatement>();
			NextToken();
			ExpectEndOfStatement(statement);
			Close(statement);
			break;
		case "continue":
			statement = Open<ScriptContinueStatement>();
			NextToken();
			ExpectEndOfStatement(statement);
			Close(statement);
			break;
		case "assign":
		{
			if (_isKeepTrivia)
			{
				_trivias.Clear();
			}
			NextToken();
			Token token = _token;
			ScriptExpressionStatement scriptExpressionStatement = ParseExpressionStatement();
			if (!(scriptExpressionStatement.Expression is ScriptAssignExpression))
			{
				LogError(token, "Expecting an assign expression: <variable> = <expression>");
			}
			statement = scriptExpressionStatement;
			break;
		}
		case "capture":
			statement = ParseCaptureStatement();
			break;
		case "increment":
			statement = ParseLiquidIncDecStatement(isDec: false);
			break;
		case "decrement":
			statement = ParseLiquidIncDecStatement(isDec: true);
			break;
		case "include":
			statement = ParseLiquidIncludeStatement();
			break;
		default:
			statement = ParseLiquidExpressionStatement(parent);
			break;
		}
		if (text == null)
		{
			return;
		}
		if (_isKeepTrivia)
		{
			_trivias.Add(new ScriptTrivia(CurrentSpan, ScriptTriviaType.End));
		}
		NextToken();
		hasEnd = true;
		nextStatement = false;
		if (scriptStatement == null)
		{
			LogError(current, "Unable to find a pending " + text + " for this `" + identifier + "`");
		}
		else
		{
			ExpectEndOfStatement(scriptStatement);
			if (_isKeepTrivia)
			{
				FlushTrivias(scriptStatement, isBefore: false);
			}
		}
	}

	private ScriptExpressionStatement ParseLiquidCycleStatement()
	{
		ScriptExpressionStatement scriptExpressionStatement = Open<ScriptExpressionStatement>();
		ScriptFunctionCall scriptFunctionCall = (ScriptFunctionCall)(scriptExpressionStatement.Expression = Open<ScriptFunctionCall>());
		scriptFunctionCall.Target = ParseVariable();
		if (Options.LiquidFunctionsToScriban)
		{
			TransformLiquidFunctionCallToScriban(scriptFunctionCall);
		}
		ScriptArrayInitializerExpression scriptArrayInitializerExpression = null;
		bool flag = true;
		while (IsVariableOrLiteral(Current))
		{
			ScriptExpression scriptExpression2 = ParseVariableOrLiteral();
			if (flag && Current.Type == TokenType.Colon)
			{
				NextToken();
				ScriptNamedArgument scriptNamedArgument = Open<ScriptNamedArgument>();
				scriptNamedArgument.Name = "group";
				scriptNamedArgument.Value = scriptExpression2;
				Close(scriptNamedArgument);
				scriptNamedArgument.Span = scriptExpression2.Span;
				flag = false;
				scriptFunctionCall.Arguments.Add(scriptNamedArgument);
				continue;
			}
			if (scriptArrayInitializerExpression == null)
			{
				scriptArrayInitializerExpression = Open<ScriptArrayInitializerExpression>();
				scriptFunctionCall.Arguments.Insert(0, scriptArrayInitializerExpression);
				scriptArrayInitializerExpression.Span.Start = scriptExpression2.Span.Start;
			}
			scriptArrayInitializerExpression.Values.Add(scriptExpression2);
			scriptArrayInitializerExpression.Span.End = scriptExpression2.Span.End;
			if (Current.Type == TokenType.Comma)
			{
				NextToken();
				continue;
			}
			if (Current.Type != TokenType.LiquidTagExit)
			{
				LogError(Current, $"Unexpected token `{GetAsText(Current)}` after cycle value `{scriptExpression2}`. Expecting a `,`");
				NextToken();
			}
			break;
		}
		Close(scriptFunctionCall);
		ExpectEndOfStatement(scriptExpressionStatement);
		return Close(scriptExpressionStatement);
	}

	private ScriptStatement ParseLiquidExpressionStatement(ScriptStatement parent)
	{
		Token current = Current;
		CheckNotInCase(parent, current);
		return ParseExpressionStatement();
	}

	private ScriptStatement ParseLiquidIfChanged()
	{
		ScriptIfStatement scriptIfStatement = Open<ScriptIfStatement>();
		NextToken();
		scriptIfStatement.Condition = new ScriptMemberExpression
		{
			Target = ScriptVariable.Create(ScriptVariable.ForObject.Name, ScriptVariableScope.Loop),
			Member = ScriptVariable.Create("changed", ScriptVariableScope.Global)
		};
		scriptIfStatement.Then = ParseBlockStatement(scriptIfStatement);
		Close(scriptIfStatement);
		scriptIfStatement.Condition.Span = scriptIfStatement.Span;
		return scriptIfStatement;
	}

	private ScriptStatement ParseLiquidIncDecStatement(bool isDec)
	{
		ScriptExpressionStatement scriptExpressionStatement = Open<ScriptExpressionStatement>();
		NextToken();
		ScriptBinaryExpression scriptBinaryExpression = Open<ScriptBinaryExpression>();
		scriptBinaryExpression.Left = ExpectAndParseVariable(scriptExpressionStatement);
		scriptBinaryExpression.Right = new ScriptLiteral
		{
			Span = scriptBinaryExpression.Span,
			Value = 1
		};
		scriptBinaryExpression.Operator = ((!isDec) ? ScriptBinaryOperator.Add : ScriptBinaryOperator.Substract);
		ExpectEndOfStatement(scriptExpressionStatement);
		scriptExpressionStatement.Expression = scriptBinaryExpression;
		Close(scriptBinaryExpression);
		return Close(scriptExpressionStatement);
	}

	private ScriptStatement ParseLiquidIncludeStatement()
	{
		ScriptFunctionCall scriptFunctionCall = Open<ScriptFunctionCall>();
		scriptFunctionCall.Target = ParseVariable();
		Token current = Current;
		ScriptExpression scriptExpression = ExpectAndParseExpression(scriptFunctionCall, null, 0, null, ParseExpressionMode.BasicExpression);
		if (scriptExpression != null)
		{
			if (!((scriptExpression as ScriptLiteral)?.Value is string) && !(scriptExpression is IScriptVariablePath))
			{
				LogError(current, $"Unexpected include template name `{scriptExpression}` expecting a string or a variable path");
			}
			scriptFunctionCall.Arguments.Add(scriptExpression);
		}
		Close(scriptFunctionCall);
		ScriptExpressionStatement scriptExpressionStatement = new ScriptExpressionStatement
		{
			Span = scriptFunctionCall.Span,
			Expression = scriptFunctionCall
		};
		ScriptForStatement scriptForStatement = null;
		ScriptBlockStatement scriptBlockStatement = null;
		if (Current.Type == TokenType.Identifier)
		{
			string asText = GetAsText(Current);
			if (asText == "with")
			{
				NextToken();
				ScriptAssignExpression scriptAssignExpression = Open<ScriptAssignExpression>();
				scriptAssignExpression.Target = new ScriptIndexerExpression
				{
					Target = new ScriptThisExpression
					{
						Span = CurrentSpan
					},
					Index = scriptExpression
				};
				scriptAssignExpression.Value = ExpectAndParseExpression(scriptFunctionCall, null, 0, null, ParseExpressionMode.BasicExpression);
				Close(scriptAssignExpression);
				scriptBlockStatement = new ScriptBlockStatement
				{
					Span = scriptFunctionCall.Span
				};
				scriptBlockStatement.Statements.Add(new ScriptExpressionStatement
				{
					Span = scriptAssignExpression.Span,
					Expression = scriptAssignExpression
				});
				scriptBlockStatement.Statements.Add(scriptExpressionStatement);
				Close(scriptBlockStatement);
			}
			else if (asText == "for")
			{
				NextToken();
				scriptForStatement = Open<ScriptForStatement>();
				scriptForStatement.Variable = new ScriptIndexerExpression
				{
					Target = new ScriptThisExpression
					{
						Span = CurrentSpan
					},
					Index = scriptExpression
				};
				scriptForStatement.Iterator = ExpectAndParseExpression(scriptFunctionCall, null, 0, null, ParseExpressionMode.BasicExpression);
				scriptForStatement.Body = new ScriptBlockStatement
				{
					Span = scriptFunctionCall.Span
				};
				scriptForStatement.Body.Statements.Add(scriptExpressionStatement);
				Close(scriptForStatement);
			}
			while (Current.Type == TokenType.Identifier)
			{
				Token current2 = Current;
				ScriptVariable scriptVariable = ParseVariable() as ScriptVariable;
				if (scriptVariable == null)
				{
					LogError(current2, "Unexpected variable name `" + GetAsText(current2) + "` found in include parameter");
				}
				if (Current.Type == TokenType.Colon)
				{
					NextToken();
				}
				else
				{
					LogError(Current, $"Unexpected token `{GetAsText(Current)}` after variable `{scriptVariable}`. Expecting a `:`");
				}
				if (scriptBlockStatement == null)
				{
					scriptBlockStatement = new ScriptBlockStatement
					{
						Span = scriptFunctionCall.Span
					};
					scriptBlockStatement.Statements.Add(scriptExpressionStatement);
				}
				ScriptAssignExpression scriptAssignExpression2 = Open<ScriptAssignExpression>();
				scriptAssignExpression2.Target = scriptVariable;
				scriptAssignExpression2.Value = ExpectAndParseExpression(scriptFunctionCall, null, 0, null, ParseExpressionMode.BasicExpression);
				scriptBlockStatement.Statements.Insert(0, new ScriptExpressionStatement
				{
					Span = scriptAssignExpression2.Span,
					Expression = scriptAssignExpression2
				});
				if (Current.Type == TokenType.Comma)
				{
					NextToken();
				}
			}
			ExpectEndOfStatement(scriptExpressionStatement);
			if (scriptForStatement != null)
			{
				if (scriptBlockStatement == null)
				{
					return Close(scriptForStatement);
				}
				scriptBlockStatement.Statements.Add(scriptForStatement);
			}
			if (scriptBlockStatement != null)
			{
				Close(scriptBlockStatement);
				return scriptBlockStatement;
			}
		}
		ExpectEndOfStatement(scriptExpressionStatement);
		return Close(scriptExpressionStatement);
	}

	private void ParseScribanStatement(string identifier, ScriptStatement parent, ref ScriptStatement statement, ref bool hasEnd, ref bool nextStatement)
	{
		Token current = Current;
		switch (identifier)
		{
		case "end":
		{
			hasEnd = true;
			nextStatement = false;
			if (_isKeepTrivia)
			{
				_trivias.Add(new ScriptTrivia(CurrentSpan, ScriptTriviaType.End, _lexer.Text));
			}
			NextToken();
			ScriptStatement scriptStatement = FindFirstStatementExpectingEnd();
			ExpectEndOfStatement(scriptStatement);
			if (_isKeepTrivia)
			{
				FlushTrivias(scriptStatement, isBefore: false);
			}
			break;
		}
		case "wrap":
			CheckNotInCase(parent, current);
			statement = ParseWrapStatement();
			break;
		case "if":
			CheckNotInCase(parent, current);
			statement = ParseIfStatement(invert: false, isElseIf: false);
			break;
		case "case":
			CheckNotInCase(parent, current);
			statement = ParseCaseStatement();
			break;
		case "when":
		{
			ScriptWhenStatement scriptWhenStatement = ParseWhenStatement();
			ScriptConditionStatement scriptConditionStatement3 = parent as ScriptConditionStatement;
			if (parent is ScriptWhenStatement)
			{
				((ScriptWhenStatement)scriptConditionStatement3).Next = scriptWhenStatement;
			}
			else if (parent is ScriptCaseStatement)
			{
				statement = scriptWhenStatement;
			}
			else
			{
				nextStatement = false;
				LogError(current, "A `when` condition must be preceded by another `when`/`else`/`case` condition");
			}
			hasEnd = true;
			break;
		}
		case "else":
		{
			ScriptConditionStatement scriptConditionStatement = ParseElseStatement(isElseIf: false);
			ScriptConditionStatement scriptConditionStatement2 = parent as ScriptConditionStatement;
			if (parent is ScriptIfStatement || parent is ScriptWhenStatement)
			{
				if (parent is ScriptIfStatement)
				{
					((ScriptIfStatement)scriptConditionStatement2).Else = scriptConditionStatement;
				}
				else
				{
					((ScriptWhenStatement)scriptConditionStatement2).Next = scriptConditionStatement;
				}
			}
			else
			{
				nextStatement = false;
				LogError(current, "A else condition must be preceded by another if/else/when condition");
			}
			hasEnd = true;
			break;
		}
		case "for":
			CheckNotInCase(parent, current);
			if (PeekToken().Type == TokenType.Dot)
			{
				statement = ParseExpressionStatement();
			}
			else
			{
				statement = ParseForStatement<ScriptForStatement>();
			}
			break;
		case "tablerow":
			CheckNotInCase(parent, current);
			if (PeekToken().Type == TokenType.Dot)
			{
				statement = ParseExpressionStatement();
			}
			else
			{
				statement = ParseForStatement<ScriptTableRowStatement>();
			}
			break;
		case "with":
			CheckNotInCase(parent, current);
			statement = ParseWithStatement();
			break;
		case "import":
			CheckNotInCase(parent, current);
			statement = ParseImportStatement();
			break;
		case "readonly":
			CheckNotInCase(parent, current);
			statement = ParseReadOnlyStatement();
			break;
		case "while":
			CheckNotInCase(parent, current);
			if (PeekToken().Type == TokenType.Dot)
			{
				statement = ParseExpressionStatement();
			}
			else
			{
				statement = ParseWhileStatement();
			}
			break;
		case "break":
			CheckNotInCase(parent, current);
			statement = Open<ScriptBreakStatement>();
			NextToken();
			ExpectEndOfStatement(statement);
			Close(statement);
			break;
		case "continue":
			CheckNotInCase(parent, current);
			statement = Open<ScriptContinueStatement>();
			NextToken();
			ExpectEndOfStatement(statement);
			Close(statement);
			break;
		case "func":
			CheckNotInCase(parent, current);
			statement = ParseFunctionStatement(isAnonymous: false);
			break;
		case "ret":
			CheckNotInCase(parent, current);
			statement = ParseReturnStatement();
			break;
		case "capture":
			CheckNotInCase(parent, current);
			statement = ParseCaptureStatement();
			break;
		default:
			CheckNotInCase(parent, current);
			statement = ParseExpressionStatement();
			break;
		}
	}

	private ScriptFunction ParseFunctionStatement(bool isAnonymous)
	{
		ScriptFunction scriptFunction = Open<ScriptFunction>();
		NextToken();
		if (!isAnonymous)
		{
			scriptFunction.Name = ExpectAndParseVariable(scriptFunction);
		}
		ExpectEndOfStatement(scriptFunction);
		scriptFunction.Body = ParseBlockStatement(scriptFunction);
		return Close(scriptFunction);
	}

	private ScriptImportStatement ParseImportStatement()
	{
		ScriptImportStatement scriptImportStatement = Open<ScriptImportStatement>();
		NextToken();
		scriptImportStatement.Expression = ExpectAndParseExpression(scriptImportStatement);
		ExpectEndOfStatement(scriptImportStatement);
		return Close(scriptImportStatement);
	}

	private ScriptReadOnlyStatement ParseReadOnlyStatement()
	{
		ScriptReadOnlyStatement scriptReadOnlyStatement = Open<ScriptReadOnlyStatement>();
		NextToken();
		scriptReadOnlyStatement.Variable = ExpectAndParseVariable(scriptReadOnlyStatement);
		ExpectEndOfStatement(scriptReadOnlyStatement);
		return Close(scriptReadOnlyStatement);
	}

	private ScriptReturnStatement ParseReturnStatement()
	{
		ScriptReturnStatement scriptReturnStatement = Open<ScriptReturnStatement>();
		NextToken();
		if (StartAsExpression())
		{
			scriptReturnStatement.Expression = ParseExpression(scriptReturnStatement);
		}
		ExpectEndOfStatement(scriptReturnStatement);
		return Close(scriptReturnStatement);
	}

	private ScriptWhileStatement ParseWhileStatement()
	{
		ScriptWhileStatement scriptWhileStatement = Open<ScriptWhileStatement>();
		NextToken();
		scriptWhileStatement.Condition = ExpectAndParseExpression(scriptWhileStatement, null, 0, null, ParseExpressionMode.Default, allowAssignment: false);
		if (ExpectEndOfStatement(scriptWhileStatement))
		{
			FlushTrivias(scriptWhileStatement.Condition, isBefore: false);
			scriptWhileStatement.Body = ParseBlockStatement(scriptWhileStatement);
		}
		return Close(scriptWhileStatement);
	}

	private ScriptWithStatement ParseWithStatement()
	{
		ScriptWithStatement scriptWithStatement = Open<ScriptWithStatement>();
		NextToken();
		scriptWithStatement.Name = ExpectAndParseExpression(scriptWithStatement);
		if (ExpectEndOfStatement(scriptWithStatement))
		{
			scriptWithStatement.Body = ParseBlockStatement(scriptWithStatement);
		}
		return Close(scriptWithStatement);
	}

	private ScriptWrapStatement ParseWrapStatement()
	{
		ScriptWrapStatement scriptWrapStatement = Open<ScriptWrapStatement>();
		NextToken();
		scriptWrapStatement.Target = ExpectAndParseExpression(scriptWrapStatement);
		if (ExpectEndOfStatement(scriptWrapStatement))
		{
			FlushTrivias(scriptWrapStatement.Target, isBefore: false);
			scriptWrapStatement.Body = ParseBlockStatement(scriptWrapStatement);
		}
		return Close(scriptWrapStatement);
	}

	private void FixRawStatementAfterFrontMatter(ScriptPage page)
	{
		if (!(page.Body.Statements.FirstOrDefault() is ScriptRawStatement scriptRawStatement))
		{
			return;
		}
		int offset = scriptRawStatement.Span.Start.Offset;
		int offset2 = scriptRawStatement.Span.End.Offset;
		for (int i = offset; i <= offset2; i++)
		{
			switch (scriptRawStatement.Text[i])
			{
			case '\r':
				if (i + 1 <= offset2 && scriptRawStatement.Text[i + 1] == '\n')
				{
					scriptRawStatement.Span.Start = new TextPosition(i + 2, scriptRawStatement.Span.Start.Line + 1, 0);
				}
				return;
			case '\n':
				scriptRawStatement.Span.Start = new TextPosition(i + 1, scriptRawStatement.Span.Start.Line + 1, 0);
				return;
			case '\t':
			case ' ':
				break;
			default:
				return;
			}
		}
	}

	private static bool IsScribanKeyword(string text)
	{
		switch (text)
		{
		case "do":
		case "if":
		case "else":
		case "case":
		case "when":
		case "func":
		case "with":
		case "wrap":
		case "end":
		case "for":
		case "ret":
		case "break":
		case "while":
		case "continue":
		case "readonly":
		case "import":
		case "capture":
			return true;
		default:
			return false;
		}
	}

	private ScriptExpression ParseVariableOrLiteral()
	{
		ScriptExpression result = null;
		switch (Current.Type)
		{
		case TokenType.IdentifierSpecial:
		case TokenType.Identifier:
			result = ParseVariable();
			break;
		case TokenType.Integer:
			result = ParseInteger();
			break;
		case TokenType.Float:
			result = ParseFloat();
			break;
		case TokenType.String:
			result = ParseString();
			break;
		case TokenType.ImplicitString:
			result = ParseImplicitString();
			break;
		case TokenType.VerbatimString:
			result = ParseVerbatimString();
			break;
		default:
			LogError(Current, "Unexpected token found `{GetAsText(Current)}` while parsing a variable or literal");
			break;
		}
		return result;
	}

	private ScriptLiteral ParseFloat()
	{
		ScriptLiteral scriptLiteral = Open<ScriptLiteral>();
		string asText = GetAsText(Current);
		if (double.TryParse(asText, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			scriptLiteral.Value = result;
		}
		else
		{
			LogError("Unable to parse double value `" + asText + "`");
		}
		NextToken();
		return Close(scriptLiteral);
	}

	private ScriptLiteral ParseImplicitString()
	{
		ScriptLiteral scriptLiteral = Open<ScriptLiteral>();
		scriptLiteral.Value = GetAsText(Current);
		Close(scriptLiteral);
		NextToken();
		return scriptLiteral;
	}

	private ScriptLiteral ParseInteger()
	{
		ScriptLiteral scriptLiteral = Open<ScriptLiteral>();
		string asText = GetAsText(Current);
		if (!long.TryParse(asText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			LogError("Unable to parse the integer " + asText);
		}
		if (result >= int.MinValue && result <= int.MaxValue)
		{
			scriptLiteral.Value = (int)result;
		}
		else
		{
			scriptLiteral.Value = result;
		}
		NextToken();
		return Close(scriptLiteral);
	}

	private ScriptLiteral ParseString()
	{
		ScriptLiteral scriptLiteral = Open<ScriptLiteral>();
		string text = _lexer.Text;
		StringBuilder stringBuilder = new StringBuilder(Current.End.Offset - Current.Start.Offset - 1);
		scriptLiteral.StringQuoteType = ((_lexer.Text[Current.Start.Offset] == '\'') ? ScriptLiteralStringQuoteType.SimpleQuote : ScriptLiteralStringQuoteType.DoubleQuote);
		int offset = Current.End.Offset;
		for (int i = Current.Start.Offset + 1; i < offset; i++)
		{
			char value = text[i];
			if (text[i] == '\\')
			{
				i++;
				switch (text[i])
				{
				case '0':
					stringBuilder.Append('\0');
					break;
				case '\r':
					i++;
					break;
				case '\'':
					stringBuilder.Append('\'');
					break;
				case '"':
					stringBuilder.Append('"');
					break;
				case '\\':
					stringBuilder.Append('\\');
					break;
				case 'b':
					stringBuilder.Append('\b');
					break;
				case 'f':
					stringBuilder.Append('\f');
					break;
				case 'n':
					stringBuilder.Append('\n');
					break;
				case 'r':
					stringBuilder.Append('\r');
					break;
				case 't':
					stringBuilder.Append('\t');
					break;
				case 'v':
					stringBuilder.Append('\v');
					break;
				case 'u':
				{
					i++;
					int num2 = 0;
					if (i < text.Length)
					{
						num2 = text[i++].HexToInt();
					}
					if (i < text.Length)
					{
						num2 = (num2 << 4) | text[i++].HexToInt();
					}
					if (i < text.Length)
					{
						num2 = (num2 << 4) | text[i++].HexToInt();
					}
					if (i < text.Length)
					{
						num2 = (num2 << 4) | text[i].HexToInt();
					}
					stringBuilder.Append(ConvertFromUtf32(num2));
					break;
				}
				case 'x':
				{
					i++;
					int num = 0;
					if (i < text.Length)
					{
						num = text[i++].HexToInt();
					}
					if (i < text.Length)
					{
						num = (num << 4) | text[i].HexToInt();
					}
					stringBuilder.Append((char)num);
					break;
				}
				default:
					LogError($"Unexpected escape character `{text[i]}` in string");
					break;
				case '\n':
					break;
				}
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		scriptLiteral.Value = stringBuilder.ToString();
		NextToken();
		return Close(scriptLiteral);
	}

	private ScriptExpression ParseVariable()
	{
		Token current = Current;
		SourceSpan currentSpan = CurrentSpan;
		SourceSpan sourceSpan = currentSpan;
		string text = GetAsText(current);
		switch (text)
		{
		case "null":
		{
			ScriptLiteral statement2 = Open<ScriptLiteral>();
			NextToken();
			return Close(statement2);
		}
		case "true":
		{
			ScriptLiteral scriptLiteral2 = Open<ScriptLiteral>();
			scriptLiteral2.Value = true;
			NextToken();
			return Close(scriptLiteral2);
		}
		case "false":
		{
			ScriptLiteral scriptLiteral = Open<ScriptLiteral>();
			scriptLiteral.Value = false;
			NextToken();
			return Close(scriptLiteral);
		}
		case "do":
		{
			ScriptAnonymousFunction scriptAnonymousFunction = Open<ScriptAnonymousFunction>();
			scriptAnonymousFunction.Function = ParseFunctionStatement(isAnonymous: true);
			return Close(scriptAnonymousFunction);
		}
		case "this":
			if (!_isLiquid)
			{
				ScriptThisExpression statement = Open<ScriptThisExpression>();
				NextToken();
				return Close(statement);
			}
			break;
		}
		List<ScriptTrivia> list = null;
		if (_isKeepTrivia && _trivias.Count > 0)
		{
			list = new List<ScriptTrivia>();
			list.AddRange(_trivias);
			_trivias.Clear();
		}
		NextToken();
		ScriptVariableScope scope = ScriptVariableScope.Global;
		if (text.StartsWith("$"))
		{
			scope = ScriptVariableScope.Local;
			text = text.Substring(1);
			if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				ScriptIndexerExpression scriptIndexerExpression = new ScriptIndexerExpression
				{
					Span = currentSpan,
					Target = new ScriptVariableLocal(ScriptVariable.Arguments.Name)
					{
						Span = currentSpan
					},
					Index = new ScriptLiteral
					{
						Span = currentSpan,
						Value = result
					}
				};
				if (_isKeepTrivia)
				{
					if (list != null)
					{
						scriptIndexerExpression.Target.AddTrivias(list, before: true);
					}
					FlushTrivias(scriptIndexerExpression.Index, isBefore: false);
				}
				return scriptIndexerExpression;
			}
		}
		else
		{
			switch (text)
			{
			default:
				if (!_isLiquid || (!(text == "forloop") && !(text == "tablerowloop")))
				{
					if (_isLiquid && text == "continue")
					{
						scope = ScriptVariableScope.Local;
					}
					break;
				}
				goto case "for";
			case "for":
			case "while":
			case "tablerow":
			{
				if (Current.Type != TokenType.Dot)
				{
					break;
				}
				scope = ScriptVariableScope.Loop;
				Token localToken = PeekToken();
				if (localToken.Type == TokenType.Identifier)
				{
					string asText = GetAsText(localToken);
					if (_isLiquid)
					{
						switch (asText)
						{
						case "col":
							if (text != "tablerowloop")
							{
								LogError(current, "The loop variable <" + text + ".col> is invalid");
							}
							break;
						default:
							LogError(current, "The liquid loop variable <" + text + "." + asText + "> is not supported");
							break;
						case "first":
						case "index":
						case "index0":
						case "length":
						case "rindex":
						case "last":
						case "rindex0":
							break;
						}
						if (text == "forloop")
						{
							text = "for";
						}
						else if (text == "tablerowloop")
						{
							text = "tablerow";
						}
						break;
					}
					switch (asText)
					{
					case "last":
					case "length":
					case "rindex":
					case "changed":
						if (text == "while")
						{
							LogError(current, "The loop variable <while." + asText + "> is invalid");
						}
						break;
					case "col":
						if (text != "tablerow")
						{
							LogError(current, "The loop variable <" + text + ".col> is invalid");
						}
						break;
					default:
						LogError(current, "The loop variable <" + text + "." + asText + "> is not supported");
						break;
					case "first":
					case "index":
					case "even":
					case "odd":
						break;
					}
				}
				else
				{
					LogError(current, "Invalid token `" + GetAsText(Current) + "`. The loop variable <" + text + "> dot must be followed by an identifier");
				}
				break;
			}
			}
		}
		ScriptVariable scriptVariable = ScriptVariable.Create(text, scope);
		scriptVariable.Span = new SourceSpan
		{
			FileName = currentSpan.FileName,
			Start = currentSpan.Start,
			End = sourceSpan.End
		};
		if (_isLiquid && text.IndexOf('-') >= 0)
		{
			ScriptIndexerExpression scriptIndexerExpression2 = new ScriptIndexerExpression
			{
				Target = new ScriptThisExpression
				{
					Span = scriptVariable.Span
				},
				Index = new ScriptLiteral(text)
				{
					Span = scriptVariable.Span
				},
				Span = scriptVariable.Span
			};
			if (_isKeepTrivia)
			{
				if (list != null)
				{
					scriptIndexerExpression2.Target.AddTrivias(list, before: true);
				}
				FlushTrivias(scriptIndexerExpression2, isBefore: false);
			}
			return scriptIndexerExpression2;
		}
		if (_isKeepTrivia)
		{
			if (list != null)
			{
				scriptVariable.AddTrivias(list, before: true);
			}
			FlushTrivias(scriptVariable, isBefore: false);
		}
		return scriptVariable;
	}

	private ScriptLiteral ParseVerbatimString()
	{
		ScriptLiteral scriptLiteral = Open<ScriptLiteral>();
		string text = _lexer.Text;
		scriptLiteral.StringQuoteType = ScriptLiteralStringQuoteType.Verbatim;
		StringBuilder stringBuilder = null;
		int num = Current.Start.Offset + 1;
		int num2 = Current.End.Offset - 1;
		int num3 = num;
		while (true)
		{
			int num4 = text.IndexOf("`", num3, num2 - num3 + 1, StringComparison.OrdinalIgnoreCase);
			if (num4 < 0)
			{
				break;
			}
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder(num2 - num + 1);
			}
			stringBuilder.Append(text.Substring(num3, num4 - num3 + 1));
			num3 = num4 + 2;
		}
		if (stringBuilder != null)
		{
			int num5 = num2 - num3 + 1;
			if (num5 > 0)
			{
				stringBuilder.Append(text.Substring(num3, num5));
			}
			scriptLiteral.Value = stringBuilder.ToString();
		}
		else
		{
			scriptLiteral.Value = text.Substring(num3, num2 - num3 + 1);
		}
		NextToken();
		return Close(scriptLiteral);
	}

	private static string ConvertFromUtf32(int utf32)
	{
		if (utf32 < 65536)
		{
			return ((char)utf32).ToString();
		}
		utf32 -= 65536;
		return new string(new char[2]
		{
			(char)(utf32 / 1024 + 55296),
			(char)(utf32 % 1024 + 56320)
		});
	}

	private static bool IsVariableOrLiteral(Token token)
	{
		TokenType type = token.Type;
		if ((uint)(type - 22) <= 6u)
		{
			return true;
		}
		return false;
	}
}
