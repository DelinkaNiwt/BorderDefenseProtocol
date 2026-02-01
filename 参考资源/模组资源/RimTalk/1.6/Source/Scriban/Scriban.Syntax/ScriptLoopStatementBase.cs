using System;
using System.Collections.Generic;
using System.Linq;
using Scriban.Helpers;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Scriban.Syntax;

public abstract class ScriptLoopStatementBase : ScriptStatement
{
	protected class LoopState : IScriptObject
	{
		private int _length;

		private object _lengthObject;

		public int Index { get; set; }

		public int LocalIndex { get; set; }

		public bool IsFirst => Index == 0;

		public bool IsEven => (Index & 1) == 0;

		public bool IsOdd => !IsEven;

		public bool ValueChanged { get; set; }

		public bool IsLast { get; set; }

		public int Length
		{
			get
			{
				return _length;
			}
			set
			{
				_length = value;
				_lengthObject = value;
			}
		}

		public int Count { get; set; }

		public bool IsReadOnly { get; set; }

		public IEnumerable<string> GetMembers()
		{
			return Enumerable.Empty<string>();
		}

		public virtual bool Contains(string member)
		{
			switch (member)
			{
			case "first":
			case "index":
			case "index0":
			case "even":
			case "last":
			case "odd":
				return true;
			case "length":
				return _lengthObject != null;
			case "rindex":
			case "rindex0":
				return _lengthObject != null;
			case "changed":
				return true;
			default:
				return false;
			}
		}

		public virtual bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
		{
			value = null;
			bool isLiquid = context.IsLiquid;
			switch (member)
			{
			case "index":
				value = (isLiquid ? (Index + 1) : Index);
				return true;
			case "length":
				value = _lengthObject;
				return _lengthObject != null;
			case "first":
				value = (IsFirst ? BoxHelper.TrueObject : BoxHelper.FalseObject);
				return true;
			case "even":
				value = (IsEven ? BoxHelper.TrueObject : BoxHelper.FalseObject);
				return true;
			case "odd":
				value = (IsOdd ? BoxHelper.TrueObject : BoxHelper.FalseObject);
				return true;
			case "last":
				value = (IsLast ? BoxHelper.TrueObject : BoxHelper.FalseObject);
				return true;
			case "changed":
				value = (ValueChanged ? BoxHelper.TrueObject : BoxHelper.FalseObject);
				return true;
			case "rindex":
				if (_lengthObject != null)
				{
					value = (isLiquid ? (_length - Index) : (_length - Index - 1));
				}
				return _lengthObject != null;
			default:
				if (isLiquid)
				{
					if (member == "index0")
					{
						value = Index;
						return true;
					}
					if (member == "rindex0")
					{
						value = _length - Index - 1;
						return true;
					}
				}
				return false;
			}
		}

		public bool CanWrite(string member)
		{
			throw new NotImplementedException();
		}

		public void SetValue(TemplateContext context, SourceSpan span, string member, object value, bool readOnly)
		{
		}

		public bool Remove(string member)
		{
			return false;
		}

		public void SetReadOnly(string member, bool readOnly)
		{
		}

		public IScriptObject Clone(bool deep)
		{
			return (IScriptObject)MemberwiseClone();
		}
	}

	public ScriptBlockStatement Body { get; set; }

	protected virtual void BeforeLoop(TemplateContext context)
	{
	}

	protected abstract object LoopItem(TemplateContext context, LoopState state);

	protected virtual LoopState CreateLoopState()
	{
		return new LoopState();
	}

	protected bool ContinueLoop(TemplateContext context)
	{
		if (context.FlowState == ScriptFlowState.Return)
		{
			return false;
		}
		bool result = context.FlowState != ScriptFlowState.Break;
		context.FlowState = ScriptFlowState.None;
		return result;
	}

	protected virtual void AfterLoop(TemplateContext context)
	{
	}

	public override object Evaluate(TemplateContext context)
	{
		object obj = null;
		context.EnterLoop(this);
		try
		{
			return EvaluateImpl(context);
		}
		finally
		{
			context.ExitLoop(this);
			if (context.FlowState != ScriptFlowState.Return)
			{
				context.FlowState = ScriptFlowState.None;
			}
		}
	}

	protected abstract object EvaluateImpl(TemplateContext context);
}
