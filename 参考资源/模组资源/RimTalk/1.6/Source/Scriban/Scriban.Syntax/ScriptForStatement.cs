using System;
using System.Collections;
using System.Collections.Generic;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("for statement", "for <variable> in <expression> ... end")]
public class ScriptForStatement : ScriptLoopStatementBase, IScriptNamedArgumentContainer
{
	public ScriptExpression Variable { get; set; }

	public ScriptExpression Iterator { get; set; }

	public List<ScriptNamedArgument> NamedArguments { get; set; }

	internal ScriptNode IteratorOrLastParameter
	{
		get
		{
			if (NamedArguments == null || NamedArguments.Count <= 0)
			{
				return Iterator;
			}
			return NamedArguments[NamedArguments.Count - 1];
		}
	}

	protected override object LoopItem(TemplateContext context, LoopState state)
	{
		return context.Evaluate(base.Body);
	}

	protected virtual ScriptVariable GetLoopVariable(TemplateContext context)
	{
		return ScriptVariable.ForObject;
	}

	protected override object EvaluateImpl(TemplateContext context)
	{
		object obj = context.Evaluate(Iterator);
		IList list = obj as IList;
		if (list == null && obj is IEnumerable values)
		{
			list = new ScriptArray(values);
		}
		if (list != null)
		{
			object result = null;
			object objA = null;
			bool flag = false;
			int num = 0;
			int num2 = list.Count;
			if (NamedArguments != null)
			{
				foreach (ScriptNamedArgument namedArgument in NamedArguments)
				{
					switch (namedArgument.Name)
					{
					case "offset":
						num = context.ToInt(namedArgument.Value.Span, context.Evaluate(namedArgument.Value));
						break;
					case "reversed":
						flag = true;
						break;
					case "limit":
						num2 = context.ToInt(namedArgument.Value.Span, context.Evaluate(namedArgument.Value));
						break;
					default:
						ProcessArgument(context, namedArgument);
						break;
					}
				}
			}
			int num3 = Math.Min(num2 + num, list.Count) - 1;
			int num4 = (flag ? num3 : num);
			int num5 = ((!flag) ? 1 : (-1));
			bool flag2 = true;
			int num6 = 0;
			BeforeLoop(context);
			LoopState loopState = CreateLoopState();
			context.SetValue(GetLoopVariable(context), loopState);
			loopState.Length = list.Count;
			while ((!flag && num4 <= num3) || (flag && num4 >= num))
			{
				if (!context.StepLoop(this))
				{
					return null;
				}
				object obj2 = list[num4];
				bool isLast = (flag ? (num4 == num) : (num4 == num3));
				loopState.Index = num4;
				loopState.LocalIndex = num6;
				loopState.IsLast = isLast;
				loopState.ValueChanged = flag2 || !object.Equals(objA, obj2);
				context.SetValue(Variable, obj2);
				result = LoopItem(context, loopState);
				if (!ContinueLoop(context))
				{
					break;
				}
				objA = obj2;
				flag2 = false;
				num4 += num5;
				num6++;
			}
			AfterLoop(context);
			context.SetValue(ScriptVariable.Continue, num4);
			return result;
		}
		if (obj != null)
		{
			throw new ScriptRuntimeException(Iterator.Span, $"Unexpected type `{obj.GetType()}` for iterator");
		}
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("for").ExpectSpace();
		context.Write(Variable).ExpectSpace();
		if (!context.PreviousHasSpace)
		{
			context.Write(" ");
		}
		context.Write("in").ExpectSpace();
		context.Write(Iterator);
		context.Write(NamedArguments);
		context.ExpectEos();
		context.Write(base.Body);
		context.ExpectEnd();
	}

	protected virtual void ProcessArgument(TemplateContext context, ScriptNamedArgument argument)
	{
		throw new ScriptRuntimeException(argument.Span, $"Unsupported argument `{argument.Name}` for statement: `{this}`");
	}

	public override string ToString()
	{
		return $"for {Variable} in {Iterator} ... end";
	}
}
