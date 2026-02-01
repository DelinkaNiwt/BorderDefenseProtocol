using System;
using System.Reflection;
using Scriban.Parsing;
using Scriban.Syntax;

namespace Scriban.Runtime;

internal class GenericFunctionWrapper : DynamicCustomFunction
{
	private readonly object _target;

	private readonly bool _hasObjectParams;

	private readonly int _lastParamsIndex;

	private readonly bool _hasTemplateContext;

	private readonly bool _hasSpan;

	private readonly object[] _arguments;

	private readonly int _optionalParameterCount;

	private readonly Type _paramsElementType;

	public GenericFunctionWrapper(object target, MethodInfo method)
		: base(method)
	{
		_target = target;
		_lastParamsIndex = Parameters.Length - 1;
		if (Parameters.Length != 0)
		{
			if (typeof(TemplateContext).GetTypeInfo().IsAssignableFrom(Parameters[0].ParameterType.GetTypeInfo()))
			{
				_hasTemplateContext = true;
				if (Parameters.Length > 1)
				{
					_hasSpan = typeof(SourceSpan).GetTypeInfo().IsAssignableFrom(Parameters[1].ParameterType.GetTypeInfo());
				}
			}
			ParameterInfo parameterInfo = Parameters[_lastParamsIndex];
			if (parameterInfo.ParameterType.IsArray)
			{
				object[] customAttributes = parameterInfo.GetCustomAttributes(typeof(ParamArrayAttribute), inherit: false);
				int num = 0;
				if (num < customAttributes.Length)
				{
					_ = customAttributes[num];
					_hasObjectParams = true;
					_paramsElementType = parameterInfo.ParameterType.GetElementType();
				}
			}
		}
		if (!_hasObjectParams)
		{
			for (int i = 0; i < Parameters.Length; i++)
			{
				if (Parameters[i].IsOptional)
				{
					_optionalParameterCount++;
				}
			}
		}
		_arguments = new object[Parameters.Length];
	}

	public override object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
	{
		int num = Parameters.Length;
		if (_hasTemplateContext)
		{
			num--;
			if (_hasSpan)
			{
				num--;
			}
		}
		int num2 = num - _optionalParameterCount;
		if ((_hasObjectParams && arguments.Count < num2 - 1) || (!_hasObjectParams && arguments.Count < num2))
		{
			if (num2 != num)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `{num2}` arguments");
			}
			throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `{num}` arguments");
		}
		object[] array = null;
		int num3 = 0;
		if (_hasObjectParams)
		{
			int num4 = arguments.Count - _lastParamsIndex;
			if (_hasTemplateContext)
			{
				num4++;
				if (_hasSpan)
				{
					num4++;
				}
			}
			array = new object[num4];
			_arguments[_lastParamsIndex] = array;
			num3 |= 1 << _lastParamsIndex;
		}
		int num5 = 0;
		if (_hasTemplateContext)
		{
			_arguments[0] = context;
			num5++;
			num3 |= 1;
			if (_hasSpan)
			{
				_arguments[1] = callerContext.Span;
				num5++;
				num3 |= 2;
			}
		}
		int num6 = num5;
		if (_optionalParameterCount > 0)
		{
			for (int num7 = Parameters.Length - 1; num7 >= Parameters.Length - _optionalParameterCount; num7--)
			{
				_arguments[num7] = Parameters[num7].DefaultValue;
				num3 |= 1 << num7;
			}
		}
		int num8 = 0;
		for (int i = 0; i < arguments.Count; i++)
		{
			Type type = null;
			try
			{
				object obj = arguments[i];
				int num9;
				if (obj is ScriptNamedArgument namedArg)
				{
					ArgumentValue valueFromNamedArgument = GetValueFromNamedArgument(context, callerContext, namedArg);
					obj = valueFromNamedArgument.Value;
					num9 = valueFromNamedArgument.Index;
					type = valueFromNamedArgument.Type;
					if (_hasObjectParams && num9 == _lastParamsIndex)
					{
						type = _paramsElementType;
						num9 += num8;
						num8++;
					}
				}
				else
				{
					num9 = num6;
					if (_hasObjectParams && num9 == _lastParamsIndex)
					{
						type = _paramsElementType;
						num9 += num8;
						num8++;
					}
					else
					{
						type = Parameters[num9].ParameterType;
						num6++;
					}
				}
				object obj2 = context.ToObject(callerContext.Span, obj, type);
				if (array != null && num9 >= _lastParamsIndex)
				{
					array[num9 - _lastParamsIndex] = obj2;
					continue;
				}
				_arguments[num9] = obj2;
				num3 |= 1 << num9;
			}
			catch (Exception innerException)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Unable to convert parameter #{i} of type `{arguments[i]?.GetType()}` to type `{type}`", innerException);
			}
		}
		if (num3 != (1 << Parameters.Length) - 1)
		{
			if (num2 != num)
			{
				throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting at least `{num2}` arguments");
			}
			throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of arguments `{arguments.Count}` passed to `{callerContext}` while expecting `{num}` arguments");
		}
		try
		{
			return Method.Invoke(_target, _arguments);
		}
		catch (TargetInvocationException ex)
		{
			throw new ScriptRuntimeException(callerContext.Span, $"Unexpected exception when calling {callerContext}", ex.InnerException);
		}
	}
}
