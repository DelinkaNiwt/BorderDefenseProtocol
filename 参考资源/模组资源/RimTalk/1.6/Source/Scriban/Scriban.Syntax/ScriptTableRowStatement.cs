using System.Globalization;
using Scriban.Helpers;
using Scriban.Parsing;

namespace Scriban.Syntax;

public class ScriptTableRowStatement : ScriptForStatement
{
	protected class TableRowLoopState : LoopState
	{
		public int Col { get; set; }

		public bool ColFirst { get; set; }

		public bool ColLast { get; set; }

		public override bool Contains(string member)
		{
			if (!base.Contains(member))
			{
				switch (member)
				{
				case "col":
				case "col0":
				case "col_first":
				case "col_last":
					return true;
				default:
					return false;
				}
			}
			return true;
		}

		public override bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
		{
			if (!base.TryGetValue(context, span, member, out value))
			{
				switch (member)
				{
				case "col":
					value = (context.IsLiquid ? (Col + 1) : Col);
					return true;
				case "col0":
					value = Col;
					return true;
				case "col_first":
					value = (ColFirst ? BoxHelper.TrueObject : BoxHelper.FalseObject);
					return true;
				case "col_last":
					value = (ColLast ? BoxHelper.TrueObject : BoxHelper.FalseObject);
					return true;
				default:
					return false;
				}
			}
			return true;
		}
	}

	private int _columnsCount;

	public ScriptTableRowStatement()
	{
		_columnsCount = 1;
	}

	protected override ScriptVariable GetLoopVariable(TemplateContext context)
	{
		return ScriptVariable.TablerowObject;
	}

	protected override void ProcessArgument(TemplateContext context, ScriptNamedArgument argument)
	{
		_columnsCount = 1;
		if (argument.Name == "cols")
		{
			_columnsCount = context.ToInt(argument.Value.Span, context.Evaluate(argument.Value));
			if (_columnsCount <= 0)
			{
				_columnsCount = 1;
			}
		}
		else
		{
			base.ProcessArgument(context, argument);
		}
	}

	protected override void BeforeLoop(TemplateContext context)
	{
		context.Write("<tr class=\"row1\">");
	}

	protected override void AfterLoop(TemplateContext context)
	{
		context.Write("</tr>").WriteLine();
	}

	protected override object LoopItem(TemplateContext context, LoopState state)
	{
		int localIndex = state.LocalIndex;
		int num = localIndex % _columnsCount;
		TableRowLoopState obj = (TableRowLoopState)state;
		obj.Col = num;
		obj.ColFirst = num == 0;
		obj.ColLast = (localIndex + 1) % _columnsCount == 0;
		if (num == 0 && localIndex > 0)
		{
			context.Write("</tr>").Write(context.NewLine);
			int num2 = localIndex / _columnsCount + 1;
			context.Write("<tr class=\"row").Write(num2.ToString(CultureInfo.InvariantCulture)).Write("\">");
		}
		context.Write("<td class=\"col").Write((num + 1).ToString(CultureInfo.InvariantCulture)).Write("\">");
		object result = base.LoopItem(context, state);
		context.Write("</td>");
		return result;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("tablerow").ExpectSpace();
		context.Write(base.Variable).ExpectSpace();
		context.Write("in").ExpectSpace();
		context.Write(base.Iterator);
		context.Write(base.NamedArguments);
		context.ExpectEos();
		context.Write(base.Body);
		context.ExpectEnd();
	}

	protected override LoopState CreateLoopState()
	{
		return new TableRowLoopState();
	}
}
