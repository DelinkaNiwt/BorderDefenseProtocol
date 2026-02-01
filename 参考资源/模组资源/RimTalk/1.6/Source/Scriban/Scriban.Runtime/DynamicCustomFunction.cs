using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Syntax;

namespace Scriban.Runtime;

public abstract class DynamicCustomFunction : IScriptCustomFunction
{
	private class Functionbool_IEnumerable_object : DynamicCustomFunction
	{
		private delegate bool InternalDelegate(IEnumerable arg0, object arg1);

		private readonly InternalDelegate _delegate;

		public Functionbool_IEnumerable_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			IEnumerable arg = null;
			object arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
					break;
				case 1:
					arg2 = obj;
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class Functionbool_object : DynamicCustomFunction
	{
		private delegate bool InternalDelegate(object arg0);

		private readonly InternalDelegate _delegate;

		public Functionbool_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			object arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = obj;
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functionbool_string_string : DynamicCustomFunction
	{
		private delegate bool InternalDelegate(string arg0, string arg1);

		private readonly InternalDelegate _delegate;

		public Functionbool_string_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			string arg = null;
			string arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionDateTime : DynamicCustomFunction
	{
		private delegate DateTime InternalDelegate();

		private readonly InternalDelegate _delegate;

		public FunctionDateTime(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 0)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `0` arguments");
			}
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				if (arguments[i] is ScriptNamedArgument namedArg)
				{
					GetValueFromNamedArgument(context, callerContext, namedArg);
				}
				else
				{
					num2++;
				}
			}
			if (num != 0)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `0` arguments");
			}
			return _delegate();
		}
	}

	private class FunctionDateTime_DateTime_double : DynamicCustomFunction
	{
		private delegate DateTime InternalDelegate(DateTime arg0, double arg1);

		private readonly InternalDelegate _delegate;

		public FunctionDateTime_DateTime_double(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			DateTime arg = default(DateTime);
			double arg2 = 0.0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (DateTime)context.ToObject(callerContext.Span, obj, typeof(DateTime));
					num |= 1;
					break;
				case 1:
					arg2 = (double)context.ToObject(callerContext.Span, obj, typeof(double));
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionDateTime_DateTime_int : DynamicCustomFunction
	{
		private delegate DateTime InternalDelegate(DateTime arg0, int arg1);

		private readonly InternalDelegate _delegate;

		public FunctionDateTime_DateTime_int(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			DateTime arg = default(DateTime);
			int arg2 = 0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (DateTime)context.ToObject(callerContext.Span, obj, typeof(DateTime));
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class Functiondouble_double : DynamicCustomFunction
	{
		private delegate double InternalDelegate(double arg0);

		private readonly InternalDelegate _delegate;

		public Functiondouble_double(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			double arg = 0.0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = (double)context.ToObject(callerContext.Span, obj, typeof(double));
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functiondouble_double_int___Opt : DynamicCustomFunction
	{
		private delegate double InternalDelegate(double arg0, int arg1);

		private readonly InternalDelegate _delegate;

		private readonly int defaultArg1;

		public Functiondouble_double_int___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg1 = (int)Parameters[1].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 1 || arguments.Count > 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `1` arguments");
			}
			double arg = 0.0;
			int arg2 = defaultArg1;
			int num = 2;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (double)context.ToObject(callerContext.Span, obj, typeof(double));
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `1` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionIEnumerable_IEnumerable : DynamicCustomFunction
	{
		private delegate IEnumerable InternalDelegate(IEnumerable arg0);

		private readonly InternalDelegate _delegate;

		public FunctionIEnumerable_IEnumerable(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			IEnumerable arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class FunctionIEnumerable_IEnumerable_IEnumerable : DynamicCustomFunction
	{
		private delegate IEnumerable InternalDelegate(IEnumerable arg0, IEnumerable arg1);

		private readonly InternalDelegate _delegate;

		public FunctionIEnumerable_IEnumerable_IEnumerable(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			IEnumerable arg = null;
			IEnumerable arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
					break;
				case 1:
					arg2 = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionIEnumerable_string_string : DynamicCustomFunction
	{
		private delegate IEnumerable InternalDelegate(string arg0, string arg1);

		private readonly InternalDelegate _delegate;

		public FunctionIEnumerable_string_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			string arg = null;
			string arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionIEnumerable_TemplateContext_SourceSpan_object_string___Opt : DynamicCustomFunction
	{
		private delegate IEnumerable InternalDelegate(TemplateContext arg0, SourceSpan arg1, object arg2, string arg3);

		private readonly InternalDelegate _delegate;

		private readonly string defaultArg1;

		public FunctionIEnumerable_TemplateContext_SourceSpan_object_string___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg1 = (string)Parameters[3].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 1 || arguments.Count > 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `1` arguments");
			}
			object arg = null;
			string arg2 = defaultArg1;
			int num = 2;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = obj;
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `1` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2);
		}
	}

	private class FunctionIEnumerable_TemplateContext_SourceSpan_object_string : DynamicCustomFunction
	{
		private delegate IEnumerable InternalDelegate(TemplateContext arg0, SourceSpan arg1, object arg2, string arg3);

		private readonly InternalDelegate _delegate;

		public FunctionIEnumerable_TemplateContext_SourceSpan_object_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			object arg = null;
			string arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = obj;
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2);
		}
	}

	private class FunctionIList_IList_int : DynamicCustomFunction
	{
		private delegate IList InternalDelegate(IList arg0, int arg1);

		private readonly InternalDelegate _delegate;

		public FunctionIList_IList_int(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			IList arg = null;
			int arg2 = 0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToList(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionIList_IList_int_object : DynamicCustomFunction
	{
		private delegate IList InternalDelegate(IList arg0, int arg1, object arg2);

		private readonly InternalDelegate _delegate;

		public FunctionIList_IList_int_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `3` arguments");
			}
			IList arg = null;
			int arg2 = 0;
			object arg3 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToList(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = obj;
					num |= 4;
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `3` arguments");
			}
			return _delegate(arg, arg2, arg3);
		}
	}

	private class FunctionIList_IList_object : DynamicCustomFunction
	{
		private delegate IList InternalDelegate(IList arg0, object arg1);

		private readonly InternalDelegate _delegate;

		public FunctionIList_IList_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			IList arg = null;
			object arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToList(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = obj;
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class Functionint_IEnumerable : DynamicCustomFunction
	{
		private delegate int InternalDelegate(IEnumerable arg0);

		private readonly InternalDelegate _delegate;

		public Functionint_IEnumerable(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			IEnumerable arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functionint_string : DynamicCustomFunction
	{
		private delegate int InternalDelegate(string arg0);

		private readonly InternalDelegate _delegate;

		public Functionint_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			string arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functionint_TemplateContext_SourceSpan_object : DynamicCustomFunction
	{
		private delegate int InternalDelegate(TemplateContext arg0, SourceSpan arg1, object arg2);

		private readonly InternalDelegate _delegate;

		public Functionint_TemplateContext_SourceSpan_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			object arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = obj;
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(context, callerContext.Span, arg);
		}
	}

	private class Functionobject_IEnumerable : DynamicCustomFunction
	{
		private delegate object InternalDelegate(IEnumerable arg0);

		private readonly InternalDelegate _delegate;

		public Functionobject_IEnumerable(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			IEnumerable arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functionobject_object_object : DynamicCustomFunction
	{
		private delegate object InternalDelegate(object arg0, object arg1);

		private readonly InternalDelegate _delegate;

		public Functionobject_object_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			object arg = null;
			object arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = obj;
					num |= 1;
					break;
				case 1:
					arg2 = obj;
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class Functionobject_TemplateContext_SourceSpan_double_object : DynamicCustomFunction
	{
		private delegate object InternalDelegate(TemplateContext arg0, SourceSpan arg1, double arg2, object arg3);

		private readonly InternalDelegate _delegate;

		public Functionobject_TemplateContext_SourceSpan_double_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			double arg = 0.0;
			object arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (double)context.ToObject(callerContext.Span, obj, typeof(double));
					num |= 1;
					break;
				case 1:
					arg2 = obj;
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2);
		}
	}

	private class Functionobject_TemplateContext_SourceSpan_IList_object___Opt : DynamicCustomFunction
	{
		private delegate object InternalDelegate(TemplateContext arg0, SourceSpan arg1, IList arg2, object arg3);

		private readonly InternalDelegate _delegate;

		private readonly object defaultArg1;

		public Functionobject_TemplateContext_SourceSpan_IList_object___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg1 = Parameters[3].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 1 || arguments.Count > 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `1` arguments");
			}
			IList arg = null;
			object arg2 = defaultArg1;
			int num = 2;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToList(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = obj;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `1` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2);
		}
	}

	private class Functionobject_TemplateContext_SourceSpan_object : DynamicCustomFunction
	{
		private delegate object InternalDelegate(TemplateContext arg0, SourceSpan arg1, object arg2);

		private readonly InternalDelegate _delegate;

		public Functionobject_TemplateContext_SourceSpan_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			object arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = obj;
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(context, callerContext.Span, arg);
		}
	}

	private class Functionobject_TemplateContext_SourceSpan_object_object : DynamicCustomFunction
	{
		private delegate object InternalDelegate(TemplateContext arg0, SourceSpan arg1, object arg2, object arg3);

		private readonly InternalDelegate _delegate;

		public Functionobject_TemplateContext_SourceSpan_object_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			object arg = null;
			object arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = obj;
					num |= 1;
					break;
				case 1:
					arg2 = obj;
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2);
		}
	}

	private class Functionobject_TemplateContext_string : DynamicCustomFunction
	{
		private delegate object InternalDelegate(TemplateContext arg0, string arg1);

		private readonly InternalDelegate _delegate;

		public Functionobject_TemplateContext_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			string arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 1;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(context, arg);
		}
	}

	private class FunctionScriptArray_IEnumerable : DynamicCustomFunction
	{
		private delegate ScriptArray InternalDelegate(IEnumerable arg0);

		private readonly InternalDelegate _delegate;

		public FunctionScriptArray_IEnumerable(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			IEnumerable arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class FunctionScriptArray_IEnumerable_int : DynamicCustomFunction
	{
		private delegate ScriptArray InternalDelegate(IEnumerable arg0, int arg1);

		private readonly InternalDelegate _delegate;

		public FunctionScriptArray_IEnumerable_int(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			IEnumerable arg = null;
			int arg2 = 0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class FunctionScriptArray_TemplateContext_string_string_string___Opt : DynamicCustomFunction
	{
		private delegate ScriptArray InternalDelegate(TemplateContext arg0, string arg1, string arg2, string arg3);

		private readonly InternalDelegate _delegate;

		private readonly string defaultArg2;

		public FunctionScriptArray_TemplateContext_string_string_string___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg2 = (string)Parameters[3].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 2 || arguments.Count > 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			string arg = null;
			string arg2 = null;
			string arg3 = defaultArg2;
			int num = 4;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 1;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToString(callerContext.Span, obj);
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			return _delegate(context, arg, arg2, arg3);
		}
	}

	private class Functionstring_int_string_string : DynamicCustomFunction
	{
		private delegate string InternalDelegate(int arg0, string arg1, string arg2);

		private readonly InternalDelegate _delegate;

		public Functionstring_int_string_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `3` arguments");
			}
			int arg = 0;
			string arg2 = null;
			string arg3 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToInt(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToString(callerContext.Span, obj);
					num |= 4;
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `3` arguments");
			}
			return _delegate(arg, arg2, arg3);
		}
	}

	private class Functionstring_object : DynamicCustomFunction
	{
		private delegate string InternalDelegate(object arg0);

		private readonly InternalDelegate _delegate;

		public Functionstring_object(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			object arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = obj;
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functionstring_string : DynamicCustomFunction
	{
		private delegate string InternalDelegate(string arg0);

		private readonly InternalDelegate _delegate;

		public Functionstring_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			string arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class Functionstring_string_int : DynamicCustomFunction
	{
		private delegate string InternalDelegate(string arg0, int arg1);

		private readonly InternalDelegate _delegate;

		public Functionstring_string_int(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			string arg = null;
			int arg2 = 0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class Functionstring_string_int_int___Opt : DynamicCustomFunction
	{
		private delegate string InternalDelegate(string arg0, int arg1, int arg2);

		private readonly InternalDelegate _delegate;

		private readonly int defaultArg2;

		public Functionstring_string_int_int___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg2 = (int)Parameters[2].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 2 || arguments.Count > 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			string arg = null;
			int arg2 = 0;
			int arg3 = defaultArg2;
			int num = 4;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToInt(callerContext.Span, obj);
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			return _delegate(arg, arg2, arg3);
		}
	}

	private class Functionstring_string_int_string___Opt : DynamicCustomFunction
	{
		private delegate string InternalDelegate(string arg0, int arg1, string arg2);

		private readonly InternalDelegate _delegate;

		private readonly string defaultArg2;

		public Functionstring_string_int_string___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg2 = (string)Parameters[2].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 2 || arguments.Count > 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			string arg = null;
			int arg2 = 0;
			string arg3 = defaultArg2;
			int num = 4;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToInt(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToString(callerContext.Span, obj);
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			return _delegate(arg, arg2, arg3);
		}
	}

	private class Functionstring_string_string : DynamicCustomFunction
	{
		private delegate string InternalDelegate(string arg0, string arg1);

		private readonly InternalDelegate _delegate;

		public Functionstring_string_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			string arg = null;
			string arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(arg, arg2);
		}
	}

	private class Functionstring_string_string_string : DynamicCustomFunction
	{
		private delegate string InternalDelegate(string arg0, string arg1, string arg2);

		private readonly InternalDelegate _delegate;

		public Functionstring_string_string_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `3` arguments");
			}
			string arg = null;
			string arg2 = null;
			string arg3 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToString(callerContext.Span, obj);
					num |= 4;
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `3` arguments");
			}
			return _delegate(arg, arg2, arg3);
		}
	}

	private class Functionstring_TemplateContext_SourceSpan_IEnumerable_string : DynamicCustomFunction
	{
		private delegate string InternalDelegate(TemplateContext arg0, SourceSpan arg1, IEnumerable arg2, string arg3);

		private readonly InternalDelegate _delegate;

		public Functionstring_TemplateContext_SourceSpan_IEnumerable_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 2)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			IEnumerable arg = null;
			string arg2 = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = (IEnumerable)context.ToObject(callerContext.Span, obj, typeof(IEnumerable));
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				}
			}
			if (num != 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `2` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2);
		}
	}

	private class Functionstring_TemplateContext_SourceSpan_object_string_string___Opt : DynamicCustomFunction
	{
		private delegate string InternalDelegate(TemplateContext arg0, SourceSpan arg1, object arg2, string arg3, string arg4);

		private readonly InternalDelegate _delegate;

		private readonly string defaultArg2;

		public Functionstring_TemplateContext_SourceSpan_object_string_string___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg2 = (string)Parameters[4].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 2 || arguments.Count > 3)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			object arg = null;
			string arg2 = null;
			string arg3 = defaultArg2;
			int num = 4;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 2;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = obj;
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToString(callerContext.Span, obj);
					break;
				}
			}
			if (num != 7)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `2` arguments");
			}
			return _delegate(context, callerContext.Span, arg, arg2, arg3);
		}
	}

	private class Functionstring_TemplateContext_string : DynamicCustomFunction
	{
		private delegate string InternalDelegate(TemplateContext arg0, string arg1);

		private readonly InternalDelegate _delegate;

		public Functionstring_TemplateContext_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			string arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 1;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(context, arg);
		}
	}

	private class Functionstring_TemplateContext_string_string_string_string___Opt : DynamicCustomFunction
	{
		private delegate string InternalDelegate(TemplateContext arg0, string arg1, string arg2, string arg3, string arg4);

		private readonly InternalDelegate _delegate;

		private readonly string defaultArg3;

		public Functionstring_TemplateContext_string_string_string_string___Opt(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
			defaultArg3 = (string)Parameters[4].DefaultValue;
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count < 3 || arguments.Count > 4)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `3` arguments");
			}
			string arg = null;
			string arg2 = null;
			string arg3 = null;
			string arg4 = defaultArg3;
			int num = 8;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index - 1;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				switch (num3)
				{
				case 0:
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
					break;
				case 1:
					arg2 = context.ToString(callerContext.Span, obj);
					num |= 2;
					break;
				case 2:
					arg3 = context.ToString(callerContext.Span, obj);
					num |= 4;
					break;
				case 3:
					arg4 = context.ToString(callerContext.Span, obj);
					break;
				}
			}
			if (num != 15)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `3` arguments");
			}
			return _delegate(context, arg, arg2, arg3, arg4);
		}
	}

	private class FunctionTimeSpan_double : DynamicCustomFunction
	{
		private delegate TimeSpan InternalDelegate(double arg0);

		private readonly InternalDelegate _delegate;

		public FunctionTimeSpan_double(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			double arg = 0.0;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = (double)context.ToObject(callerContext.Span, obj, typeof(double));
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	private class FunctionTimeSpan_string : DynamicCustomFunction
	{
		private delegate TimeSpan InternalDelegate(string arg0);

		private readonly InternalDelegate _delegate;

		public FunctionTimeSpan_string(MethodInfo method)
			: base(method)
		{
			_delegate = (InternalDelegate)method.CreateDelegate(typeof(InternalDelegate));
		}

		public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
		{
			if (arguments.Count != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			string arg = null;
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < arguments.Count; i++)
			{
				int num3 = 0;
				object obj = arguments[i];
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num3 = valueFromNamedArgument.Index;
				}
				else
				{
					num3 = num2;
					num2++;
				}
				if (num3 == 0)
				{
					arg = context.ToString(callerContext.Span, obj);
					num |= 1;
				}
			}
			if (num != 1)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `1` arguments");
			}
			return _delegate(arg);
		}
	}

	protected struct ArgumentValue
	{
		public readonly int Index;

		public readonly Type Type;

		public readonly object Value;

		public ArgumentValue(int index, Type type, object value)
		{
			Index = index;
			Type = type;
			Value = value;
		}
	}

	private class MethodComparer : IEqualityComparer<MethodInfo>
	{
		public static readonly MethodComparer Default = new MethodComparer();

		public bool Equals(MethodInfo method, MethodInfo otherMethod)
		{
			if (method != null && otherMethod != null && method.ReturnType == otherMethod.ReturnType && method.IsStatic == otherMethod.IsStatic)
			{
				ParameterInfo[] parameters = method.GetParameters();
				ParameterInfo[] parameters2 = otherMethod.GetParameters();
				int num = parameters.Length;
				if (num == parameters2.Length)
				{
					for (int i = 0; i < num; i++)
					{
						ParameterInfo parameterInfo = parameters[i];
						ParameterInfo parameterInfo2 = parameters2[i];
						if (parameterInfo.ParameterType != parameterInfo2.ParameterType || parameterInfo.IsOptional != parameterInfo2.IsOptional)
						{
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		public int GetHashCode(MethodInfo method)
		{
			int num = method.ReturnType.GetHashCode();
			if (!method.IsStatic)
			{
				num = (num * 397) ^ 1;
			}
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				num = (num * 397) ^ parameters[i].ParameterType.GetHashCode();
			}
			return num;
		}
	}

	private static readonly Dictionary<MethodInfo, Func<MethodInfo, DynamicCustomFunction>> BuiltinFunctionDelegates;

	public readonly MethodInfo Method;

	protected readonly ParameterInfo[] Parameters;

	static DynamicCustomFunction()
	{
		BuiltinFunctionDelegates = new Dictionary<MethodInfo, Func<MethodInfo, DynamicCustomFunction>>(MethodComparer.Default);
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Contains"), (MethodInfo method) => new Functionbool_IEnumerable_object(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("IsNumber"), (MethodInfo method) => new Functionbool_object(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Contains"), (MethodInfo method) => new Functionbool_string_string(method));
		BuiltinFunctionDelegates.Add(typeof(DateTimeFunctions).GetTypeInfo().GetDeclaredMethod("Now"), (MethodInfo method) => new FunctionDateTime(method));
		BuiltinFunctionDelegates.Add(typeof(DateTimeFunctions).GetTypeInfo().GetDeclaredMethod("AddDays"), (MethodInfo method) => new FunctionDateTime_DateTime_double(method));
		BuiltinFunctionDelegates.Add(typeof(DateTimeFunctions).GetTypeInfo().GetDeclaredMethod("AddMonths"), (MethodInfo method) => new FunctionDateTime_DateTime_int(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("Ceil"), (MethodInfo method) => new Functiondouble_double(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("Round"), (MethodInfo method) => new Functiondouble_double_int___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Reverse"), (MethodInfo method) => new FunctionIEnumerable_IEnumerable(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("AddRange"), (MethodInfo method) => new FunctionIEnumerable_IEnumerable_IEnumerable(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Split"), (MethodInfo method) => new FunctionIEnumerable_string_string(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Sort"), (MethodInfo method) => new FunctionIEnumerable_TemplateContext_SourceSpan_object_string___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Map"), (MethodInfo method) => new FunctionIEnumerable_TemplateContext_SourceSpan_object_string(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("RemoveAt"), (MethodInfo method) => new FunctionIList_IList_int(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("InsertAt"), (MethodInfo method) => new FunctionIList_IList_int_object(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Add"), (MethodInfo method) => new FunctionIList_IList_object(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Size"), (MethodInfo method) => new Functionint_IEnumerable(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Size"), (MethodInfo method) => new Functionint_string(method));
		BuiltinFunctionDelegates.Add(typeof(ObjectFunctions).GetTypeInfo().GetDeclaredMethod("Size"), (MethodInfo method) => new Functionint_TemplateContext_SourceSpan_object(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("First"), (MethodInfo method) => new Functionobject_IEnumerable(method));
		BuiltinFunctionDelegates.Add(typeof(ObjectFunctions).GetTypeInfo().GetDeclaredMethod("Default"), (MethodInfo method) => new Functionobject_object_object(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("DividedBy"), (MethodInfo method) => new Functionobject_TemplateContext_SourceSpan_double_object(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Cycle"), (MethodInfo method) => new Functionobject_TemplateContext_SourceSpan_IList_object___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("Abs"), (MethodInfo method) => new Functionobject_TemplateContext_SourceSpan_object(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("Minus"), (MethodInfo method) => new Functionobject_TemplateContext_SourceSpan_object_object(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("ToInt"), (MethodInfo method) => new Functionobject_TemplateContext_string(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Compact"), (MethodInfo method) => new FunctionScriptArray_IEnumerable(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Limit"), (MethodInfo method) => new FunctionScriptArray_IEnumerable_int(method));
		BuiltinFunctionDelegates.Add(typeof(RegexFunctions).GetTypeInfo().GetDeclaredMethod("Match"), (MethodInfo method) => new FunctionScriptArray_TemplateContext_string_string_string___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Pluralize"), (MethodInfo method) => new Functionstring_int_string_string(method));
		BuiltinFunctionDelegates.Add(typeof(ObjectFunctions).GetTypeInfo().GetDeclaredMethod("Typeof"), (MethodInfo method) => new Functionstring_object(method));
		BuiltinFunctionDelegates.Add(typeof(HtmlFunctions).GetTypeInfo().GetDeclaredMethod("Escape"), (MethodInfo method) => new Functionstring_string(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("PadLeft"), (MethodInfo method) => new Functionstring_string_int(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Slice"), (MethodInfo method) => new Functionstring_string_int_int___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Truncate"), (MethodInfo method) => new Functionstring_string_int_string___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Append"), (MethodInfo method) => new Functionstring_string_string(method));
		BuiltinFunctionDelegates.Add(typeof(StringFunctions).GetTypeInfo().GetDeclaredMethod("Replace"), (MethodInfo method) => new Functionstring_string_string_string(method));
		BuiltinFunctionDelegates.Add(typeof(ArrayFunctions).GetTypeInfo().GetDeclaredMethod("Join"), (MethodInfo method) => new Functionstring_TemplateContext_SourceSpan_IEnumerable_string(method));
		BuiltinFunctionDelegates.Add(typeof(MathFunctions).GetTypeInfo().GetDeclaredMethod("Format"), (MethodInfo method) => new Functionstring_TemplateContext_SourceSpan_object_string_string___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(HtmlFunctions).GetTypeInfo().GetDeclaredMethod("Strip"), (MethodInfo method) => new Functionstring_TemplateContext_string(method));
		BuiltinFunctionDelegates.Add(typeof(RegexFunctions).GetTypeInfo().GetDeclaredMethod("Replace"), (MethodInfo method) => new Functionstring_TemplateContext_string_string_string_string___Opt(method));
		BuiltinFunctionDelegates.Add(typeof(TimeSpanFunctions).GetTypeInfo().GetDeclaredMethod("FromDays"), (MethodInfo method) => new FunctionTimeSpan_double(method));
		BuiltinFunctionDelegates.Add(typeof(TimeSpanFunctions).GetTypeInfo().GetDeclaredMethod("Parse"), (MethodInfo method) => new FunctionTimeSpan_string(method));
	}

	protected DynamicCustomFunction(MethodInfo method)
	{
		Method = method;
		Parameters = method.GetParameters();
	}

	protected ArgumentValue GetValueFromNamedArgument(TemplateContext context, ScriptNode callerContext, ScriptNamedArgument namedArg)
	{
		for (int i = 0; i < Parameters.Length; i++)
		{
			ParameterInfo parameterInfo = Parameters[i];
			if (parameterInfo.Name == namedArg.Name)
			{
				return new ArgumentValue(i, parameterInfo.ParameterType, context.Evaluate(namedArg));
			}
		}
		throw new ScriptRuntimeException(callerContext.Span, $"Invalid argument `{namedArg.Name}` not found for function `{callerContext}`");
	}

	public abstract object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement);

	public static DynamicCustomFunction Create(object target, MethodInfo method)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (target == null && method.IsStatic && BuiltinFunctionDelegates.TryGetValue(method, out var value))
		{
			return value(method);
		}
		return new GenericFunctionWrapper(target, method);
	}
}
