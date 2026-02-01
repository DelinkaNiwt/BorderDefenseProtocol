using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Scriban.Parsing;

public class Lexer : IEnumerable<Token>, IEnumerable
{
	private enum BlockType
	{
		Code,
		Escape,
		Raw
	}

	public struct Enumerator : IEnumerator<Token>, IDisposable, IEnumerator
	{
		private readonly Lexer lexer;

		public Token Current => lexer._token;

		object IEnumerator.Current => Current;

		public Enumerator(Lexer lexer)
		{
			this.lexer = lexer;
			lexer.Reset();
		}

		public bool MoveNext()
		{
			return lexer.MoveNext();
		}

		public void Reset()
		{
			lexer.Reset();
		}

		public void Dispose()
		{
		}
	}

	private TextPosition _position;

	private readonly int _textLength;

	private Token _token;

	private char c;

	private BlockType _blockType;

	private bool _isLiquidTagBlock;

	private List<LogMessage> _errors;

	private int _openBraceCount;

	private int _escapeRawCharCount;

	private bool _isExpectingFrontMatter;

	private readonly bool _isLiquid;

	private readonly char _stripWhiteSpaceFullSpecialChar;

	private readonly char _stripWhiteSpaceRestrictedSpecialChar;

	private const char RawEscapeSpecialChar = '%';

	private readonly Queue<Token> _pendingTokens;

	public readonly LexerOptions Options;

	public string Text { get; }

	public string SourcePath { get; private set; }

	public bool HasErrors
	{
		get
		{
			if (_errors != null)
			{
				return _errors.Count > 0;
			}
			return false;
		}
	}

	public IEnumerable<LogMessage> Errors
	{
		get
		{
			IEnumerable<LogMessage> errors = _errors;
			return errors ?? Enumerable.Empty<LogMessage>();
		}
	}

	public Lexer(string text, string sourcePath = null, LexerOptions? options = null)
	{
		Text = text ?? throw new ArgumentNullException("text");
		LexerOptions options2 = options ?? LexerOptions.Default;
		if (options2.FrontMatterMarker == null)
		{
			options2.FrontMatterMarker = "+++";
		}
		Options = options2;
		_position = Options.StartPosition;
		if (_position.Offset > text.Length)
		{
			throw new ArgumentOutOfRangeException($"The starting position `{_position.Offset}` of range [0, {text.Length - 1}]");
		}
		_textLength = text.Length;
		SourcePath = sourcePath ?? "<input>";
		_blockType = ((Options.Mode != ScriptMode.ScriptOnly) ? BlockType.Raw : BlockType.Code);
		_pendingTokens = new Queue<Token>();
		_isExpectingFrontMatter = Options.Mode == ScriptMode.FrontMatterOnly || Options.Mode == ScriptMode.FrontMatterAndContent;
		_isLiquid = Options.Mode == ScriptMode.Liquid;
		_stripWhiteSpaceFullSpecialChar = '-';
		_stripWhiteSpaceRestrictedSpecialChar = '~';
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	private bool MoveNext()
	{
		TextPosition textPosition = default(TextPosition);
		bool flag = true;
		while (true)
		{
			if (_pendingTokens.Count > 0)
			{
				_token = _pendingTokens.Dequeue();
				return true;
			}
			if (HasErrors || _token.Type == TokenType.Eof)
			{
				return false;
			}
			if (_position.Offset == _textLength)
			{
				_token = Token.Eof;
				return true;
			}
			if (!flag && textPosition == _position)
			{
				throw new InvalidOperationException("Invalid internal state of the lexer in a forever loop");
			}
			flag = false;
			textPosition = _position;
			if (Options.Mode != ScriptMode.ScriptOnly)
			{
				bool flag2 = false;
				if (_blockType == BlockType.Raw)
				{
					if (IsCodeEnterOrEscape(out var _))
					{
						ReadCodeEnterOrEscape();
						flag2 = true;
						if (_blockType == BlockType.Code || _blockType == BlockType.Raw)
						{
							return true;
						}
					}
					else if (_isExpectingFrontMatter && TryParseFrontMatterMarker())
					{
						_blockType = BlockType.Code;
						return true;
					}
				}
				if (!flag2 && _blockType != BlockType.Raw && IsCodeExit())
				{
					bool num = _blockType == BlockType.Code;
					ReadCodeExitOrEscape();
					if (num)
					{
						return true;
					}
					continue;
				}
				if (_blockType == BlockType.Code && _isExpectingFrontMatter && TryParseFrontMatterMarker())
				{
					_blockType = BlockType.Raw;
					_isExpectingFrontMatter = false;
					return true;
				}
			}
			if (_position.Offset == _textLength)
			{
				_token = Token.Eof;
				return true;
			}
			if (_blockType == BlockType.Code)
			{
				if (_isLiquid)
				{
					if (ReadCodeLiquid())
					{
						break;
					}
				}
				else if (ReadCode())
				{
					break;
				}
			}
			else if (ReadRaw())
			{
				break;
			}
		}
		return true;
	}

	private bool TryParseFrontMatterMarker()
	{
		TextPosition position = _position;
		TextPosition position2 = _position;
		string frontMatterMarker = Options.FrontMatterMarker;
		int i;
		for (i = 0; i < frontMatterMarker.Length; i++)
		{
			if (PeekChar(i) != frontMatterMarker[i])
			{
				return false;
			}
		}
		char c = PeekChar(i);
		while (c == ' ' || c == '\t')
		{
			i++;
			c = PeekChar(i);
		}
		bool flag = false;
		switch (c)
		{
		case '\n':
			flag = true;
			break;
		case '\r':
			flag = true;
			if (PeekChar(i + 1) == '\n')
			{
				i++;
			}
			break;
		}
		if (flag)
		{
			while (i-- >= 0)
			{
				position2 = _position;
				NextChar();
			}
			_token = new Token(TokenType.FrontMatterMarker, position, position2);
			return true;
		}
		return false;
	}

	private bool IsCodeEnterOrEscape(out TokenType whitespaceMode)
	{
		whitespaceMode = TokenType.Invalid;
		if (this.c == '{')
		{
			int num = 1;
			char c = PeekChar(num);
			if (!_isLiquid)
			{
				while (c == '%')
				{
					num++;
					c = PeekChar(num);
				}
			}
			if (c == '{' || (_isLiquid && c == '%'))
			{
				char c2 = PeekChar(num + 1);
				if (c2 == _stripWhiteSpaceFullSpecialChar)
				{
					whitespaceMode = TokenType.WhitespaceFull;
				}
				else if (!_isLiquid && c2 == _stripWhiteSpaceRestrictedSpecialChar)
				{
					whitespaceMode = TokenType.Whitespace;
				}
				return true;
			}
		}
		return false;
	}

	private void ReadCodeEnterOrEscape()
	{
		TextPosition position = _position;
		TextPosition textPosition = _position;
		NextChar();
		if (!_isLiquid)
		{
			while (c == '%')
			{
				_escapeRawCharCount++;
				textPosition = textPosition.NextColumn();
				NextChar();
			}
		}
		textPosition = textPosition.NextColumn();
		if (_isLiquid && c == '%')
		{
			_isLiquidTagBlock = true;
		}
		NextChar();
		if (c == _stripWhiteSpaceFullSpecialChar || (!_isLiquid && c == _stripWhiteSpaceRestrictedSpecialChar))
		{
			textPosition = textPosition.NextColumn();
			NextChar();
		}
		if (_escapeRawCharCount > 0)
		{
			_blockType = BlockType.Escape;
		}
		else if (!_isLiquid || !_isLiquidTagBlock || !TryReadLiquidCommentOrRaw(position, textPosition))
		{
			_blockType = BlockType.Code;
			_token = new Token(_isLiquidTagBlock ? TokenType.LiquidTagEnter : TokenType.CodeEnter, position, textPosition);
		}
	}

	private bool TryReadLiquidCommentOrRaw(TextPosition codeEnterStart, TextPosition codeEnterEnd)
	{
		TextPosition position = _position;
		int i = 0;
		PeekSkipSpaces(ref i);
		bool flag;
		if ((flag = TryMatchPeek("comment", i, out i)) || TryMatchPeek("raw", i, out i))
		{
			PeekSkipSpaces(ref i);
			if (TryMatchPeek("%}", i, out i))
			{
				position = new TextPosition(position.Offset + i, position.Line, position.Column + i);
				_position = new TextPosition(position.Offset - 1, position.Line, position.Column - 1);
				c = '}';
				while (true)
				{
					TextPosition position2 = _position;
					NextChar();
					if (c == '{')
					{
						NextChar();
						if (c != '%')
						{
							continue;
						}
						NextChar();
						if (c == '-')
						{
							NextChar();
						}
						SkipSpaces();
						if (!TryMatch(flag ? "endcomment" : "endraw"))
						{
							continue;
						}
						SkipSpaces();
						TextPosition position3 = _position;
						if (c == '-')
						{
							NextChar();
						}
						if (c != '%')
						{
							continue;
						}
						NextChar();
						if (c == '}')
						{
							TextPosition position4 = _position;
							NextChar();
							_blockType = BlockType.Raw;
							if (flag)
							{
								_token = new Token(TokenType.CodeEnter, codeEnterStart, codeEnterEnd);
								_pendingTokens.Enqueue(new Token(TokenType.CommentMulti, position, position2));
								_pendingTokens.Enqueue(new Token(TokenType.CodeExit, position3, position4));
							}
							else
							{
								_token = new Token(TokenType.Escape, position, position2);
								_pendingTokens.Enqueue(new Token(TokenType.EscapeCount1, position2, position2));
							}
							_isLiquidTagBlock = false;
							return true;
						}
					}
					else if (c == '\0')
					{
						break;
					}
				}
			}
		}
		return false;
	}

	private void SkipSpaces()
	{
		while (IsWhitespace(c))
		{
			NextChar();
		}
	}

	private void PeekSkipSpaces(ref int i)
	{
		while (true)
		{
			char c = PeekChar(i);
			if (c == ' ' || c == '\t')
			{
				i++;
				continue;
			}
			break;
		}
	}

	private bool TryMatchPeek(string text, int offset, out int offsetOut)
	{
		offsetOut = offset;
		for (int i = 0; i < text.Length; i++)
		{
			if (PeekChar(offset) != text[i])
			{
				return false;
			}
			offset++;
		}
		offsetOut = offset;
		return true;
	}

	private bool TryMatch(string text)
	{
		for (int i = 0; i < text.Length; i++)
		{
			if (c != text[i])
			{
				return false;
			}
			NextChar();
		}
		return true;
	}

	private bool IsCodeExit()
	{
		if (_openBraceCount > 0)
		{
			return false;
		}
		int num = 0;
		if (c == _stripWhiteSpaceFullSpecialChar || (!_isLiquid && c == _stripWhiteSpaceRestrictedSpecialChar))
		{
			num = 1;
		}
		if (PeekChar(num) != (_isLiquidTagBlock ? 37 : 125))
		{
			return false;
		}
		num++;
		if (!_isLiquid)
		{
			for (int i = 0; i < _escapeRawCharCount; i++)
			{
				if (PeekChar(i + num) != '%')
				{
					return false;
				}
			}
		}
		return PeekChar(_escapeRawCharCount + num) == '}';
	}

	private void ReadCodeExitOrEscape()
	{
		TextPosition position = _position;
		TokenType tokenType = TokenType.Invalid;
		if (c == _stripWhiteSpaceFullSpecialChar)
		{
			tokenType = TokenType.WhitespaceFull;
			NextChar();
		}
		else if (!_isLiquid && c == _stripWhiteSpaceRestrictedSpecialChar)
		{
			tokenType = TokenType.Whitespace;
			NextChar();
		}
		NextChar();
		if (!_isLiquid)
		{
			for (int i = 0; i < _escapeRawCharCount; i++)
			{
				NextChar();
			}
		}
		TextPosition position2 = _position;
		NextChar();
		if (_escapeRawCharCount > 0)
		{
			_pendingTokens.Enqueue(new Token((TokenType)(8 + Math.Min(_escapeRawCharCount - 1, 8)), position, position2));
			_escapeRawCharCount = 0;
		}
		else
		{
			_token = new Token(_isLiquidTagBlock ? TokenType.LiquidTagExit : TokenType.CodeExit, position, position2);
		}
		if (tokenType != TokenType.Invalid)
		{
			TextPosition position3 = _position;
			TextPosition lastSpace = default(TextPosition);
			if (ConsumeWhitespace(tokenType == TokenType.Whitespace, ref lastSpace, tokenType == TokenType.Whitespace))
			{
				_pendingTokens.Enqueue(new Token(tokenType, position3, lastSpace));
			}
		}
		_isLiquidTagBlock = false;
		_blockType = BlockType.Raw;
	}

	private bool ReadRaw()
	{
		TextPosition position = _position;
		TextPosition textPosition = new TextPosition(-1, 0, 0);
		bool flag = false;
		TokenType whitespaceMode = TokenType.Invalid;
		bool flag2 = false;
		TextPosition textPosition2 = TextPosition.Eof;
		TextPosition textPosition3 = TextPosition.Eof;
		TextPosition textPosition4 = TextPosition.Eof;
		TextPosition textPosition5 = TextPosition.Eof;
		while (c != 0)
		{
			if ((_blockType == BlockType.Raw && IsCodeEnterOrEscape(out whitespaceMode)) || (_blockType == BlockType.Escape && IsCodeExit()))
			{
				flag2 = textPosition.Offset < 0;
				flag = true;
				break;
			}
			if (char.IsWhiteSpace(c))
			{
				if (textPosition4.Offset < 0)
				{
					textPosition4 = _position;
					textPosition2 = textPosition;
				}
				if (c != '\n' && (c != '\r' || PeekChar() == '\n'))
				{
					if (textPosition5.Offset < 0)
					{
						textPosition5 = _position;
						textPosition3 = textPosition;
					}
				}
				else
				{
					textPosition5.Offset = -1;
					textPosition3.Offset = -1;
				}
			}
			else
			{
				textPosition4.Offset = -1;
				textPosition2.Offset = -1;
				textPosition5.Offset = -1;
				textPosition3.Offset = -1;
			}
			textPosition = _position;
			NextChar();
		}
		if (textPosition.Offset < 0)
		{
			textPosition = position;
		}
		TextPosition start = textPosition4;
		TextPosition textPosition6 = textPosition2;
		if (whitespaceMode == TokenType.Whitespace)
		{
			start = textPosition5;
			textPosition6 = textPosition3;
		}
		if (whitespaceMode != TokenType.Invalid && start.Offset >= 0)
		{
			_pendingTokens.Enqueue(new Token(whitespaceMode, start, textPosition));
			if (textPosition6.Offset < 0)
			{
				return false;
			}
			textPosition = textPosition6;
		}
		if (flag && flag2)
		{
			textPosition = new TextPosition(position.Offset - 1, position.Line, position.Column - 1);
		}
		_token = new Token((_blockType == BlockType.Escape) ? TokenType.Escape : TokenType.Raw, position, textPosition);
		if (!flag)
		{
			NextChar();
		}
		return true;
	}

	private bool ReadCode()
	{
		bool result = true;
		TextPosition position = _position;
		switch (c)
		{
		case '\n':
			_token = new Token(TokenType.NewLine, position, _position);
			NextChar();
			ConsumeWhitespace(stopAtNewLine: false, ref _token.End);
			break;
		case ';':
			_token = new Token(TokenType.SemiColon, position, _position);
			NextChar();
			break;
		case '\r':
			NextChar();
			if (c == '\n')
			{
				_token = new Token(TokenType.NewLine, position, _position);
				NextChar();
				ConsumeWhitespace(stopAtNewLine: false, ref _token.End);
			}
			else
			{
				_token = new Token(TokenType.NewLine, position, position);
				ConsumeWhitespace(stopAtNewLine: false, ref _token.End);
			}
			break;
		case ':':
			_token = new Token(TokenType.Colon, position, position);
			NextChar();
			break;
		case '@':
			_token = new Token(TokenType.Arroba, position, position);
			NextChar();
			break;
		case '^':
			_token = new Token(TokenType.Caret, position, position);
			NextChar();
			break;
		case '*':
			_token = new Token(TokenType.Multiply, position, position);
			NextChar();
			break;
		case '/':
			NextChar();
			if (c == '/')
			{
				_token = new Token(TokenType.DoubleDivide, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Divide, position, position);
			}
			break;
		case '+':
			_token = new Token(TokenType.Plus, position, position);
			NextChar();
			break;
		case '-':
			_token = new Token(TokenType.Minus, position, position);
			NextChar();
			break;
		case '%':
			_token = new Token(TokenType.Modulus, position, position);
			NextChar();
			break;
		case ',':
			_token = new Token(TokenType.Comma, position, position);
			NextChar();
			break;
		case '&':
			NextChar();
			if (c == '&')
			{
				_token = new Token(TokenType.And, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Invalid, position, position);
			}
			break;
		case '?':
			NextChar();
			if (c == '?')
			{
				_token = new Token(TokenType.EmptyCoalescing, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Question, position, position);
			}
			break;
		case '|':
			NextChar();
			if (c == '|')
			{
				_token = new Token(TokenType.Or, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Pipe, position, position);
			}
			break;
		case '.':
			NextChar();
			if (c == '.')
			{
				TextPosition position2 = _position;
				NextChar();
				if (c == '<')
				{
					_token = new Token(TokenType.DoubleDotLess, position, _position);
					NextChar();
				}
				else
				{
					_token = new Token(TokenType.DoubleDot, position, position2);
				}
			}
			else
			{
				_token = new Token(TokenType.Dot, position, position);
			}
			break;
		case '!':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareNotEqual, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Not, position, position);
			}
			break;
		case '=':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareEqual, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Equal, position, position);
			}
			break;
		case '<':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareLessOrEqual, position, _position);
				NextChar();
			}
			else if (c == '<')
			{
				_token = new Token(TokenType.ShiftLeft, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.CompareLess, position, position);
			}
			break;
		case '>':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareGreaterOrEqual, position, _position);
				NextChar();
			}
			else if (c == '>')
			{
				_token = new Token(TokenType.ShiftRight, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.CompareGreater, position, position);
			}
			break;
		case '(':
			_token = new Token(TokenType.OpenParent, _position, _position);
			NextChar();
			break;
		case ')':
			_token = new Token(TokenType.CloseParent, _position, _position);
			NextChar();
			break;
		case '[':
			_token = new Token(TokenType.OpenBracket, _position, _position);
			NextChar();
			break;
		case ']':
			_token = new Token(TokenType.CloseBracket, _position, _position);
			NextChar();
			break;
		case '{':
			_openBraceCount++;
			_token = new Token(TokenType.OpenBrace, _position, _position);
			NextChar();
			break;
		case '}':
			if (_openBraceCount > 0)
			{
				_openBraceCount--;
				_token = new Token(TokenType.CloseBrace, _position, _position);
				NextChar();
			}
			else if (Options.Mode != ScriptMode.ScriptOnly && IsCodeExit())
			{
				result = false;
			}
			else
			{
				_token = new Token(TokenType.CloseBrace, _position, _position);
				AddError("Unexpected } while no matching {", _position, _position);
				NextChar();
			}
			break;
		case '#':
			ReadComment();
			break;
		case '"':
		case '\'':
			ReadString();
			break;
		case '`':
			ReadVerbatimString();
			break;
		case '\0':
			_token = Token.Eof;
			break;
		default:
		{
			TextPosition lastSpace = default(TextPosition);
			if (ConsumeWhitespace(stopAtNewLine: true, ref lastSpace))
			{
				if (Options.KeepTrivia)
				{
					_token = new Token(TokenType.Whitespace, position, lastSpace);
				}
				else
				{
					result = false;
				}
				break;
			}
			bool flag = c == '$';
			if (IsFirstIdentifierLetter(c) || flag)
			{
				ReadIdentifier(flag);
				break;
			}
			if (char.IsDigit(c))
			{
				ReadNumber();
				break;
			}
			_token = new Token(TokenType.Invalid, _position, _position);
			NextChar();
			break;
		}
		}
		return result;
	}

	private bool ReadCodeLiquid()
	{
		bool result = true;
		TextPosition position = _position;
		switch (c)
		{
		case ':':
			_token = new Token(TokenType.Colon, position, position);
			NextChar();
			break;
		case ',':
			_token = new Token(TokenType.Comma, position, position);
			NextChar();
			break;
		case '|':
			_token = new Token(TokenType.Pipe, position, position);
			NextChar();
			break;
		case '?':
			NextChar();
			_token = new Token(TokenType.Question, position, position);
			break;
		case '.':
			NextChar();
			if (c == '.')
			{
				_token = new Token(TokenType.DoubleDot, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Dot, position, position);
			}
			break;
		case '!':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareNotEqual, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Invalid, position, position);
			}
			break;
		case '=':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareEqual, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.Equal, position, position);
			}
			break;
		case '<':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareLessOrEqual, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.CompareLess, position, position);
			}
			break;
		case '>':
			NextChar();
			if (c == '=')
			{
				_token = new Token(TokenType.CompareGreaterOrEqual, position, _position);
				NextChar();
			}
			else
			{
				_token = new Token(TokenType.CompareGreater, position, position);
			}
			break;
		case '(':
			_token = new Token(TokenType.OpenParent, _position, _position);
			NextChar();
			break;
		case ')':
			_token = new Token(TokenType.CloseParent, _position, _position);
			NextChar();
			break;
		case '[':
			_token = new Token(TokenType.OpenBracket, _position, _position);
			NextChar();
			break;
		case ']':
			_token = new Token(TokenType.CloseBracket, _position, _position);
			NextChar();
			break;
		case '"':
		case '\'':
			ReadString();
			break;
		case '\0':
			_token = Token.Eof;
			break;
		default:
		{
			TextPosition lastSpace = default(TextPosition);
			if (ConsumeWhitespace(stopAtNewLine: true, ref lastSpace))
			{
				if (Options.KeepTrivia)
				{
					_token = new Token(TokenType.Whitespace, position, lastSpace);
				}
				else
				{
					result = false;
				}
			}
			else if (IsFirstIdentifierLetter(c))
			{
				ReadIdentifier(special: false);
			}
			else if (char.IsDigit(c))
			{
				ReadNumber();
			}
			else
			{
				_token = new Token(TokenType.Invalid, _position, _position);
				NextChar();
			}
			break;
		}
		}
		return result;
	}

	private bool ConsumeWhitespace(bool stopAtNewLine, ref TextPosition lastSpace, bool keepNewLine = false)
	{
		TextPosition position = _position;
		while (char.IsWhiteSpace(c))
		{
			if (stopAtNewLine && IsNewLine(c))
			{
				if (keepNewLine)
				{
					lastSpace = _position;
					NextChar();
				}
				break;
			}
			lastSpace = _position;
			NextChar();
		}
		return position != _position;
	}

	private static bool IsNewLine(char c)
	{
		return c == '\n';
	}

	private void ReadIdentifier(bool special)
	{
		TextPosition position = _position;
		bool flag = true;
		TextPosition position2;
		do
		{
			position2 = _position;
			NextChar();
			if (flag && special && c == '$')
			{
				_token = new Token(TokenType.IdentifierSpecial, position, _position);
				NextChar();
				return;
			}
			flag = false;
		}
		while (IsIdentifierLetter(c));
		_token = new Token(special ? TokenType.IdentifierSpecial : TokenType.Identifier, position, position2);
		if (_isLiquid && Options.EnableIncludeImplicitString && _token.Match("include", Text) && char.IsWhiteSpace(c))
		{
			TextPosition lastSpace = _position;
			TextPosition end = lastSpace;
			ConsumeWhitespace(stopAtNewLine: false, ref lastSpace);
			_pendingTokens.Enqueue(new Token(TokenType.Whitespace, lastSpace, end));
			TextPosition position3 = _position;
			TextPosition end2 = position3;
			while (!char.IsWhiteSpace(c) && c != 0 && c != '%' && PeekChar() != '}')
			{
				end2 = _position;
				NextChar();
			}
			_pendingTokens.Enqueue(new Token(TokenType.ImplicitString, position3, end2));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsFirstIdentifierLetter(char c)
	{
		if (c != '_')
		{
			return char.IsLetter(c);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsIdentifierLetter(char c)
	{
		if (!IsFirstIdentifierLetter(c) && !char.IsDigit(c))
		{
			if (_isLiquid)
			{
				return c == '-';
			}
			return false;
		}
		return true;
	}

	private void ReadNumber()
	{
		TextPosition position = _position;
		TextPosition position2 = _position;
		bool flag = false;
		do
		{
			position2 = _position;
			NextChar();
		}
		while (char.IsDigit(c));
		if (c == '.' && PeekChar() != '.')
		{
			flag = true;
			position2 = _position;
			NextChar();
			while (char.IsDigit(c))
			{
				position2 = _position;
				NextChar();
			}
		}
		if (c == 'e' || c == 'E')
		{
			position2 = _position;
			NextChar();
			if (c == '+' || c == '-')
			{
				position2 = _position;
				NextChar();
			}
			if (!char.IsDigit(c))
			{
				AddError("Expecting at least one digit after the exponent", _position, _position);
				return;
			}
			while (char.IsDigit(c))
			{
				position2 = _position;
				NextChar();
			}
		}
		_token = new Token(flag ? TokenType.Float : TokenType.Integer, position, position2);
	}

	private void ReadString()
	{
		TextPosition position = _position;
		TextPosition position2 = _position;
		char c = this.c;
		NextChar();
		while (true)
		{
			if (this.c == '\\')
			{
				position2 = _position;
				NextChar();
				switch (this.c)
				{
				case '\n':
					position2 = _position;
					NextChar();
					continue;
				case '\r':
					position2 = _position;
					NextChar();
					if (this.c == '\n')
					{
						position2 = _position;
						NextChar();
					}
					continue;
				case '"':
				case '\'':
				case '0':
				case '\\':
				case 'b':
				case 'f':
				case 'n':
				case 'r':
				case 't':
				case 'v':
					position2 = _position;
					NextChar();
					continue;
				case 'u':
					position2 = _position;
					NextChar();
					if (this.c.IsHex())
					{
						position2 = _position;
						NextChar();
						if (this.c.IsHex())
						{
							position2 = _position;
							NextChar();
							if (this.c.IsHex())
							{
								position2 = _position;
								NextChar();
								if (this.c.IsHex())
								{
									position2 = _position;
									NextChar();
									continue;
								}
							}
						}
					}
					AddError($"Unexpected hex number `{this.c}` following `\\u`. Expecting `\\u0000` to `\\uffff`.", _position, _position);
					break;
				case 'x':
					position2 = _position;
					NextChar();
					if (this.c.IsHex())
					{
						position2 = _position;
						NextChar();
						if (this.c.IsHex())
						{
							position2 = _position;
							NextChar();
							continue;
						}
					}
					AddError($"Unexpected hex number `{this.c}` following `\\x`. Expecting `\\x00` to `\\xff`", _position, _position);
					break;
				}
				AddError($"Unexpected escape character `{this.c}` in string. Only 0 ' \\ \" b f n r t v u0000-uFFFF x00-xFF are allowed", _position, _position);
			}
			else
			{
				if (this.c == '\0')
				{
					AddError($"Unexpected end of file while parsing a string not terminated by a {c}", position2, position2);
					return;
				}
				if (this.c == c)
				{
					break;
				}
				position2 = _position;
				NextChar();
			}
		}
		position2 = _position;
		NextChar();
		_token = new Token(TokenType.String, position, position2);
	}

	private void ReadVerbatimString()
	{
		TextPosition position = _position;
		TextPosition position2 = _position;
		char c = this.c;
		NextChar();
		while (true)
		{
			if (this.c == '\0')
			{
				AddError($"Unexpected end of file while parsing a verbatim string not terminated by a {c}", position2, position2);
				return;
			}
			if (this.c == c)
			{
				position2 = _position;
				NextChar();
				if (this.c != c)
				{
					break;
				}
				position2 = _position;
				NextChar();
			}
			else
			{
				position2 = _position;
				NextChar();
			}
		}
		_token = new Token(TokenType.VerbatimString, position, position2);
	}

	private void ReadComment()
	{
		TextPosition position = _position;
		TextPosition position2 = _position;
		NextChar();
		bool flag = false;
		if (c == '#')
		{
			flag = true;
			position2 = _position;
			NextChar();
			while (!IsCodeExit() && c != 0)
			{
				bool num = c == '#';
				position2 = _position;
				NextChar();
				if (num && c == '#')
				{
					position2 = _position;
					NextChar();
					break;
				}
			}
		}
		else
		{
			while (!IsCodeExit() && c != 0 && c != '\r' && c != '\n')
			{
				position2 = _position;
				NextChar();
			}
		}
		_token = new Token(flag ? TokenType.CommentMulti : TokenType.Comment, position, position2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private char PeekChar(int count = 1)
	{
		int num = _position.Offset + count;
		if (num < 0 || num >= _textLength)
		{
			return '\0';
		}
		return Text[num];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void NextChar()
	{
		_position.Offset++;
		if (_position.Offset < _textLength)
		{
			char c = Text[_position.Offset];
			if (this.c == '\n' || (this.c == '\r' && c != '\n'))
			{
				_position.Column = 0;
				_position.Line++;
			}
			else
			{
				_position.Column++;
			}
			this.c = c;
		}
		else
		{
			_position.Offset = _textLength;
			this.c = '\0';
		}
	}

	IEnumerator<Token> IEnumerable<Token>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void AddError(string message, TextPosition start, TextPosition end)
	{
		_token = new Token(TokenType.Invalid, start, end);
		if (_errors == null)
		{
			_errors = new List<LogMessage>();
		}
		_errors.Add(new LogMessage(ParserMessageType.Error, new SourceSpan(SourcePath, start, end), message));
	}

	private void Reset()
	{
		c = ((Text.Length > 0) ? Text[Options.StartPosition.Offset] : '\0');
		_position = Options.StartPosition;
		_errors = null;
	}

	private static bool IsWhitespace(char c)
	{
		if (c != ' ')
		{
			return c == '\t';
		}
		return true;
	}
}
