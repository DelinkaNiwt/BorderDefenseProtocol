using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Scriban.Functions;
using Scriban.Helpers;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Runtime.Accessors;
using Scriban.Syntax;

namespace Scriban;

public class TemplateContext
{
	public delegate bool TryGetMemberDelegate(TemplateContext context, SourceSpan span, object target, string member, out object value);

	public delegate bool TryGetVariableDelegate(TemplateContext context, SourceSpan span, ScriptVariable variable, out object value);

	public delegate string RenderRuntimeExceptionDelegate(ScriptRuntimeException exception);

	private FastStack<ScriptObject> _availableStores;

	internal FastStack<ScriptBlockStatement> BlockDelegates;

	private FastStack<IScriptObject> _globalStores;

	private FastStack<CultureInfo> _cultures;

	private readonly Dictionary<Type, IListAccessor> _listAccessors;

	private FastStack<ScriptObject> _localStores;

	private FastStack<ScriptLoopStatementBase> _loops;

	private FastStack<ScriptObject> _loopStores;

	private readonly Dictionary<Type, IObjectAccessor> _memberAccessors;

	private FastStack<IScriptOutput> _outputs;

	private IScriptOutput _output;

	private FastStack<string> _sourceFiles;

	private FastStack<object> _caseValues;

	private int _callDepth;

	private bool _isFunctionCallDisabled;

	private int _loopStep;

	private int _getOrSetValueLevel;

	private FastStack<ScriptPipeArguments> _availablePipeArguments;

	private FastStack<ScriptPipeArguments> _pipeArguments;

	private FastStack<Dictionary<object, object>> _localTagsStack;

	private FastStack<Dictionary<object, object>> _loopTagsStack;

	private FastStack<Dictionary<object, object>> _availableTags;

	private ScriptPipeArguments _currentPipeArguments;

	public static RenderRuntimeExceptionDelegate RenderRuntimeExceptionDefault = (ScriptRuntimeException ex) => $"[{ex.OriginalMessage}]";

	private static readonly object TrueObject = true;

	private static readonly object FalseObject = false;

	internal bool AllowPipeArguments => _getOrSetValueLevel <= 1;

	public CultureInfo CurrentCulture
	{
		get
		{
			if (_cultures.Count != 0)
			{
				return _cultures.Peek();
			}
			return CultureInfo.InvariantCulture;
		}
	}

	public ITemplateLoader TemplateLoader { get; set; }

	public bool IsLiquid { get; protected set; }

	public string NewLine { get; set; }

	public ParserOptions TemplateLoaderParserOptions { get; set; }

	public LexerOptions TemplateLoaderLexerOptions { get; set; }

	public MemberRenamerDelegate MemberRenamer { get; set; }

	public MemberFilterDelegate MemberFilter { get; set; }

	public int LoopLimit { get; set; }

	public int RecursiveLimit { get; set; }

	public bool EnableOutput { get; set; }

	public IScriptOutput Output => _output;

	public ScriptObject BuiltinObject { get; }

	public IScriptObject CurrentGlobal => _globalStores.Peek();

	public Dictionary<string, Template> CachedTemplates { get; }

	public string CurrentSourceFile => _sourceFiles.Peek();

	public TryGetVariableDelegate TryGetVariable { get; set; }

	public RenderRuntimeExceptionDelegate RenderRuntimeException { get; set; }

	public TryGetMemberDelegate TryGetMember { get; set; }

	public Dictionary<object, object> Tags { get; }

	public Dictionary<object, object> TagsCurrentLocal
	{
		get
		{
			if (_localTagsStack.Count != 0)
			{
				return _localTagsStack.Peek();
			}
			return null;
		}
	}

	public Dictionary<object, object> TagsCurrentLoop
	{
		get
		{
			if (_loopTagsStack.Count != 0)
			{
				return _loopTagsStack.Peek();
			}
			return null;
		}
	}

	internal ScriptPipeArguments PipeArguments => _currentPipeArguments;

	internal ScriptFlowState FlowState { get; set; }

	public TimeSpan RegexTimeOut { get; set; }

	public bool StrictVariables { get; set; }

	public bool EnableBreakAndContinueAsReturnOutsideLoop { get; set; }

	public bool EnableRelaxedMemberAccess { get; set; }

	internal bool IsInLoop => _loops.Count > 0;

	public TemplateContext()
		: this(null)
	{
	}

	public TemplateContext(ScriptObject builtin)
	{
		BuiltinObject = builtin ?? GetDefaultBuiltinObject();
		EnableOutput = true;
		EnableBreakAndContinueAsReturnOutsideLoop = false;
		LoopLimit = 1000;
		RecursiveLimit = 100;
		MemberRenamer = StandardMemberRenamer.Default;
		RegexTimeOut = TimeSpan.FromSeconds(10.0);
		TemplateLoaderParserOptions = default(ParserOptions);
		TemplateLoaderLexerOptions = LexerOptions.Default;
		NewLine = Environment.NewLine;
		_outputs = new FastStack<IScriptOutput>(4);
		_output = new StringBuilderOutput();
		_outputs.Push(_output);
		_globalStores = new FastStack<IScriptObject>(4);
		_localStores = new FastStack<ScriptObject>(4);
		_loopStores = new FastStack<ScriptObject>(4);
		_availableStores = new FastStack<ScriptObject>(4);
		_cultures = new FastStack<CultureInfo>(4);
		_caseValues = new FastStack<object>(4);
		_localTagsStack = new FastStack<Dictionary<object, object>>(1);
		_loopTagsStack = new FastStack<Dictionary<object, object>>(1);
		_availableTags = new FastStack<Dictionary<object, object>>(4);
		_sourceFiles = new FastStack<string>(4);
		_memberAccessors = new Dictionary<Type, IObjectAccessor>();
		_listAccessors = new Dictionary<Type, IListAccessor>();
		_loops = new FastStack<ScriptLoopStatementBase>(4);
		BlockDelegates = new FastStack<ScriptBlockStatement>(4);
		_availablePipeArguments = new FastStack<ScriptPipeArguments>(4);
		_pipeArguments = new FastStack<ScriptPipeArguments>(4);
		_isFunctionCallDisabled = false;
		CachedTemplates = new Dictionary<string, Template>();
		Tags = new Dictionary<object, object>();
		PushGlobal(BuiltinObject);
	}

	public void PushCulture(CultureInfo culture)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		_cultures.Push(culture);
	}

	public CultureInfo PopCulture()
	{
		if (_cultures.Count == 0)
		{
			throw new InvalidOperationException("Cannot PopCulture more than PushCulture");
		}
		return _cultures.Pop();
	}

	internal void PushPipeArguments()
	{
		ScriptPipeArguments scriptPipeArguments = ((_availablePipeArguments.Count > 0) ? _availablePipeArguments.Pop() : new ScriptPipeArguments(4));
		_pipeArguments.Push(scriptPipeArguments);
		_currentPipeArguments = scriptPipeArguments;
	}

	internal void PopPipeArguments()
	{
		if (_pipeArguments.Count == 0)
		{
			throw new InvalidOperationException("Cannot PopPipeArguments more than PushPipeArguments");
		}
		ScriptPipeArguments scriptPipeArguments = _pipeArguments.Pop();
		scriptPipeArguments.Clear();
		_availablePipeArguments.Push(scriptPipeArguments);
		_currentPipeArguments = ((_pipeArguments.Count > 0) ? _pipeArguments.Peek() : null);
	}

	public void PushSourceFile(string sourceFile)
	{
		if (sourceFile == null)
		{
			throw new ArgumentNullException("sourceFile");
		}
		_sourceFiles.Push(sourceFile);
	}

	public string PopSourceFile()
	{
		if (_sourceFiles.Count == 0)
		{
			throw new InvalidOperationException("Cannot PopSourceFile more than PushSourceFile");
		}
		return _sourceFiles.Pop();
	}

	public object GetValue(ScriptExpression target)
	{
		_getOrSetValueLevel++;
		try
		{
			return GetOrSetValue(target, null, setter: false);
		}
		finally
		{
			_getOrSetValueLevel--;
		}
	}

	public void SetValue(ScriptVariableLoop variable, bool value)
	{
		SetValue(variable, value ? TrueObject : FalseObject);
	}

	public void SetValue(ScriptVariableLoop variable, object value)
	{
		if (variable == null)
		{
			throw new ArgumentNullException("variable");
		}
		if (_loopStores.Count > 0)
		{
			if (!_loopStores.Peek().TrySetValue(variable.Name, value, readOnly: false))
			{
				throw new ScriptRuntimeException(variable.Span, $"Cannot set value on the readonly variable `{variable}`");
			}
			return;
		}
		throw new ScriptRuntimeException(variable.Span, $"Invalid usage of the loop variable `{variable}` not inside a loop");
	}

	public void SetValue(ScriptVariable variable, object value, bool asReadOnly = false)
	{
		if (variable == null)
		{
			throw new ArgumentNullException("variable");
		}
		ScriptVariableScope scope = variable.Scope;
		IScriptObject scriptObject = null;
		switch (scope)
		{
		case ScriptVariableScope.Global:
		{
			for (int num = _globalStores.Count - 1; num >= 0; num--)
			{
				IScriptObject scriptObject2 = _globalStores.Items[num];
				if (scriptObject == null)
				{
					scriptObject = scriptObject2;
				}
				if (!scriptObject2.CanWrite(variable.Name))
				{
					string arg = ((scriptObject2 == BuiltinObject) ? "builtin " : string.Empty);
					throw new ScriptRuntimeException(variable.Span, $"Cannot set the {arg}readonly variable `{variable}`");
				}
			}
			break;
		}
		case ScriptVariableScope.Local:
			if (_localStores.Count > 0)
			{
				scriptObject = _localStores.Peek();
				break;
			}
			throw new ScriptRuntimeException(variable.Span, $"Invalid usage of the local variable `{variable}` in the current context");
		case ScriptVariableScope.Loop:
			if (_loopStores.Count > 0)
			{
				scriptObject = _loopStores.Peek();
				break;
			}
			throw new ScriptRuntimeException(variable.Span, $"Invalid usage of the loop variable `{variable}` not inside a loop");
		default:
			throw new NotImplementedException($"Variable scope `{scope}` is not implemented");
		}
		if (!scriptObject.TrySetValue(variable.Name, value, asReadOnly))
		{
			throw new ScriptRuntimeException(variable.Span, $"Cannot set value on the readonly variable `{variable}`");
		}
	}

	public void SetReadOnly(ScriptVariable variable, bool isReadOnly = true)
	{
		if (variable == null)
		{
			throw new ArgumentNullException("variable");
		}
		GetStoreForSet(variable).First().SetReadOnly(variable.Name, isReadOnly);
	}

	public void SetValue(ScriptExpression target, object value)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		_getOrSetValueLevel++;
		try
		{
			GetOrSetValue(target, value, setter: true);
		}
		finally
		{
			_getOrSetValueLevel--;
		}
	}

	public void PushGlobal(IScriptObject scriptObject)
	{
		if (scriptObject == null)
		{
			throw new ArgumentNullException("scriptObject");
		}
		_globalStores.Push(scriptObject);
		PushVariableScope(ScriptVariableScope.Local);
	}

	public IScriptObject PopGlobal()
	{
		if (_globalStores.Count == 1)
		{
			throw new InvalidOperationException("Unexpected PopGlobal() not matching a PushGlobal");
		}
		IScriptObject result = _globalStores.Pop();
		PopVariableScope(ScriptVariableScope.Local);
		return result;
	}

	public void PushOutput()
	{
		PushOutput(new StringBuilderOutput());
	}

	public void PushOutput(IScriptOutput output)
	{
		_output = output ?? throw new ArgumentNullException("output");
		_outputs.Push(_output);
	}

	public IScriptOutput PopOutput()
	{
		if (_outputs.Count == 1)
		{
			throw new InvalidOperationException("Unexpected PopOutput for top level writer");
		}
		IScriptOutput result = _outputs.Pop();
		_output = _outputs.Peek();
		return result;
	}

	public TemplateContext Write(SourceSpan span, object textAsObject)
	{
		if (textAsObject != null)
		{
			string text = ToString(span, textAsObject);
			Write(text);
		}
		return this;
	}

	public TemplateContext Write(string text)
	{
		if (text != null)
		{
			Output.Write(text);
		}
		return this;
	}

	public TemplateContext WriteLine()
	{
		Output.Write(NewLine);
		return this;
	}

	public TemplateContext Write(string text, int startIndex, int count)
	{
		if (text != null)
		{
			Output.Write(text, startIndex, count);
		}
		return this;
	}

	public object Evaluate(ScriptNode scriptNode)
	{
		return Evaluate(scriptNode, aliasReturnedFunction: false);
	}

	public object Evaluate(ScriptNode scriptNode, bool aliasReturnedFunction)
	{
		bool isFunctionCallDisabled = _isFunctionCallDisabled;
		int getOrSetValueLevel = _getOrSetValueLevel;
		try
		{
			_getOrSetValueLevel = 0;
			_isFunctionCallDisabled = aliasReturnedFunction;
			return EvaluateImpl(scriptNode);
		}
		finally
		{
			_getOrSetValueLevel = getOrSetValueLevel;
			_isFunctionCallDisabled = isFunctionCallDisabled;
		}
	}

	protected virtual object EvaluateImpl(ScriptNode scriptNode)
	{
		try
		{
			return scriptNode?.Evaluate(this);
		}
		catch (ScriptRuntimeException exception) when (RenderRuntimeException != null)
		{
			return RenderRuntimeException(exception);
		}
	}

	public IObjectAccessor GetMemberAccessor(object target)
	{
		if (target == null)
		{
			return NullAccessor.Default;
		}
		Type type = target.GetType();
		if (!_memberAccessors.TryGetValue(type, out var value))
		{
			value = GetMemberAccessorImpl(target) ?? NullAccessor.Default;
			_memberAccessors.Add(type, value);
		}
		return value;
	}

	protected virtual IObjectAccessor GetMemberAccessorImpl(object target)
	{
		Type type = target.GetType();
		if (target is IScriptObject)
		{
			return ScriptObjectAccessor.Default;
		}
		if (!DictionaryAccessor.TryGet(target, out var accessor))
		{
			if (type.GetTypeInfo().IsArray)
			{
				return ArrayAccessor.Default;
			}
			if (target is IList)
			{
				return ListAccessor.Default;
			}
			return new TypedObjectAccessor(type, MemberFilter, MemberRenamer);
		}
		return accessor;
	}

	public static ScriptObject GetDefaultBuiltinObject()
	{
		return new BuiltinFunctions();
	}

	public void EnterRecursive(ScriptNode node)
	{
		_callDepth++;
		if (_callDepth > RecursiveLimit)
		{
			throw new ScriptRuntimeException(node.Span, $"Exceeding number of recursive depth limit `{RecursiveLimit}` for node: `{node}`");
		}
	}

	public void ExitRecursive(ScriptNode node)
	{
		_callDepth--;
		if (_callDepth < 0)
		{
			throw new InvalidOperationException($"unexpected ExitRecursive not matching EnterRecursive for `{node}`");
		}
	}

	internal void EnterFunction(ScriptNode caller)
	{
		EnterRecursive(caller);
		PushVariableScope(ScriptVariableScope.Local);
	}

	internal void ExitFunction()
	{
		PopVariableScope(ScriptVariableScope.Local);
		_callDepth--;
	}

	internal void PushVariableScope(ScriptVariableScope scope)
	{
		ScriptObject item = ((_availableStores.Count > 0) ? _availableStores.Pop() : new ScriptObject());
		Dictionary<object, object> item2 = ((_availableTags.Count > 0) ? _availableTags.Pop() : new Dictionary<object, object>());
		if (scope == ScriptVariableScope.Local)
		{
			_localStores.Push(item);
			_localTagsStack.Push(item2);
		}
		else
		{
			_loopStores.Push(item);
			_loopTagsStack.Push(item2);
		}
	}

	internal void PopVariableScope(ScriptVariableScope scope)
	{
		Dictionary<object, object> dictionary;
		if (scope == ScriptVariableScope.Local)
		{
			PopVariableScope(ref _localStores);
			dictionary = _localTagsStack.Pop();
		}
		else
		{
			PopVariableScope(ref _loopStores);
			dictionary = _loopTagsStack.Pop();
		}
		dictionary.Clear();
		_availableTags.Push(dictionary);
	}

	internal void PopVariableScope(ref FastStack<ScriptObject> stores)
	{
		if (stores.Count == 0)
		{
			throw new InvalidOperationException("Invalid number of matching push/pop VariableScope.");
		}
		ScriptObject scriptObject = stores.Pop();
		scriptObject.Clear();
		_availableStores.Push(scriptObject);
	}

	internal void EnterLoop(ScriptLoopStatementBase loop)
	{
		if (loop == null)
		{
			throw new ArgumentNullException("loop");
		}
		_loops.Push(loop);
		_loopStep = 0;
		PushVariableScope(ScriptVariableScope.Loop);
		OnEnterLoop(loop);
	}

	protected virtual void OnEnterLoop(ScriptLoopStatementBase loop)
	{
	}

	internal void ExitLoop(ScriptLoopStatementBase loop)
	{
		OnExitLoop(loop);
		PopVariableScope(ScriptVariableScope.Loop);
		_loops.Pop();
		_loopStep = 0;
	}

	protected virtual void OnExitLoop(ScriptLoopStatementBase loop)
	{
	}

	internal bool StepLoop(ScriptLoopStatementBase loop)
	{
		_loopStep++;
		if (_loopStep > LoopLimit)
		{
			ScriptLoopStatementBase scriptLoopStatementBase = _loops.Peek();
			throw new ScriptRuntimeException(scriptLoopStatementBase.Span, $"Exceeding number of iteration limit `{LoopLimit}` for statement: {scriptLoopStatementBase}");
		}
		return OnStepLoop(loop);
	}

	protected virtual bool OnStepLoop(ScriptLoopStatementBase loop)
	{
		return true;
	}

	internal void PushCase(object caseValue)
	{
		_caseValues.Push(caseValue);
	}

	internal object PeekCase()
	{
		return _caseValues.Peek();
	}

	internal object PopCase()
	{
		if (_caseValues.Count == 0)
		{
			throw new InvalidOperationException("Cannot PopCase more than PushCase");
		}
		return _caseValues.Pop();
	}

	public object GetValue(ScriptVariable variable)
	{
		if (variable == null)
		{
			throw new ArgumentNullException("variable");
		}
		IEnumerable<IScriptObject> storeForSet = GetStoreForSet(variable);
		object value = null;
		foreach (IScriptObject item in storeForSet)
		{
			if (item.TryGetValue(this, variable.Span, variable.Name, out value))
			{
				return value;
			}
		}
		bool flag = false;
		if (TryGetVariable != null && TryGetVariable(this, variable.Span, variable, out value))
		{
			flag = true;
		}
		if (StrictVariables && !flag)
		{
			throw new ScriptRuntimeException(variable.Span, $"The variable `{variable}` was not found");
		}
		return value;
	}

	public object GetValue(ScriptVariableGlobal variable)
	{
		if (variable == null)
		{
			throw new ArgumentNullException("variable");
		}
		object value = null;
		if (IsInLoop)
		{
			int count = _loopStores.Count;
			ScriptObject[] items = _loopStores.Items;
			for (int num = count - 1; num >= 0; num--)
			{
				if (items[num].TryGetValue(this, variable.Span, variable.Name, out value))
				{
					return value;
				}
			}
		}
		int count2 = _globalStores.Count;
		IScriptObject[] items2 = _globalStores.Items;
		for (int num2 = count2 - 1; num2 >= 0; num2--)
		{
			if (items2[num2].TryGetValue(this, variable.Span, variable.Name, out value))
			{
				return value;
			}
		}
		bool flag = false;
		if (TryGetVariable != null && TryGetVariable(this, variable.Span, variable, out value))
		{
			flag = true;
		}
		if (StrictVariables && !flag)
		{
			throw new ScriptRuntimeException(variable.Span, $"The variable `{variable}` was not found");
		}
		return value;
	}

	private object GetOrSetValue(ScriptExpression targetExpression, object valueToSet, bool setter)
	{
		object obj = null;
		try
		{
			if (targetExpression is IScriptVariablePath scriptVariablePath)
			{
				if (setter)
				{
					scriptVariablePath.SetValue(this, valueToSet);
				}
				else
				{
					obj = scriptVariablePath.GetValue(this);
				}
			}
			else
			{
				if (setter)
				{
					throw new ScriptRuntimeException(targetExpression.Span, $"Unsupported expression for target for assignment: {targetExpression} = ...");
				}
				obj = Evaluate(targetExpression);
			}
		}
		catch (Exception ex) when (_getOrSetValueLevel == 1 && !(ex is ScriptRuntimeException))
		{
			throw new ScriptRuntimeException(targetExpression.Span, $"Unexpected exception while accessing `{targetExpression}`", ex);
		}
		if (((_isFunctionCallDisabled && _getOrSetValueLevel > 1) || !_isFunctionCallDisabled) && ScriptFunctionCall.IsFunction(obj))
		{
			obj = ScriptFunctionCall.Call(this, targetExpression, obj, _getOrSetValueLevel == 1);
		}
		return obj;
	}

	public IListAccessor GetListAccessor(object target)
	{
		Type type = target.GetType();
		if (!_listAccessors.TryGetValue(type, out var value))
		{
			value = GetListAccessorImpl(target, type);
			_listAccessors.Add(type, value);
		}
		return value;
	}

	protected virtual IListAccessor GetListAccessorImpl(object target, Type type)
	{
		if (type.GetTypeInfo().IsArray)
		{
			return ArrayAccessor.Default;
		}
		if (target is IList)
		{
			return ListAccessor.Default;
		}
		return null;
	}

	private IEnumerable<IScriptObject> GetStoreForSet(ScriptVariable variable)
	{
		ScriptVariableScope scope = variable.Scope;
		switch (scope)
		{
		case ScriptVariableScope.Global:
		{
			for (int i = _globalStores.Count - 1; i >= 0; i--)
			{
				yield return _globalStores.Items[i];
			}
			break;
		}
		case ScriptVariableScope.Local:
			if (_localStores.Count > 0)
			{
				yield return _localStores.Peek();
				break;
			}
			throw new ScriptRuntimeException(variable.Span, $"Invalid usage of the local variable `{variable}` in the current context");
		case ScriptVariableScope.Loop:
			if (_loopStores.Count > 0)
			{
				yield return _loopStores.Peek();
				break;
			}
			throw new ScriptRuntimeException(variable.Span, $"Invalid usage of the loop variable `{variable}` not inside a loop");
		default:
			throw new NotImplementedException($"Variable scope `{scope}` is not implemented");
		}
	}

	public virtual object IsEmpty(SourceSpan span, object against)
	{
		if (against == null)
		{
			return null;
		}
		if (against is IList)
		{
			return ((IList)against).Count == 0;
		}
		if (against is IEnumerable)
		{
			return !((IEnumerable)against).GetEnumerator().MoveNext();
		}
		if (against.GetType().IsPrimitiveOrDecimal())
		{
			return false;
		}
		return GetMemberAccessor(against).GetMemberCount(this, span, against) > 0;
	}

	public virtual IList ToList(SourceSpan span, object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is IList)
		{
			return (IList)value;
		}
		return new ScriptArray((value as IEnumerable) ?? throw new ScriptRuntimeException(span, "Unexpected list value. Expecting an array, list or iterator. Unablet to convert to a list"));
	}

	public virtual string ToString(SourceSpan span, object value)
	{
		if (value is string)
		{
			return (string)value;
		}
		if (value == null || value == EmptyScriptObject.Default)
		{
			return null;
		}
		if (value is bool)
		{
			if (!(bool)value)
			{
				return "false";
			}
			return "true";
		}
		Type type = value.GetType();
		if (type.IsPrimitiveOrDecimal())
		{
			try
			{
				return Convert.ToString(value, CurrentCulture);
			}
			catch (Exception innerException)
			{
				throw new ScriptRuntimeException(span, $"Unable to convert value of type `{value.GetType()}` to string", innerException);
			}
		}
		if (value is DateTime && GetValue(DateTimeFunctions.DateVariable) is DateTimeFunctions dateTimeFunctions)
		{
			return dateTimeFunctions.ToString((DateTime)value, dateTimeFunctions.Format, CurrentCulture);
		}
		if (value is ScriptObject scriptObject)
		{
			return scriptObject.ToString(this, span);
		}
		if (value is IFormattable formattable)
		{
			return formattable.ToString();
		}
		if (value is IEnumerable enumerable)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("[");
			bool flag = true;
			foreach (object item in enumerable)
			{
				if (!flag)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(ToString(span, item));
				flag = false;
			}
			stringBuilder.Append("]");
			return stringBuilder.ToString();
		}
		string fullName = type.FullName;
		if (fullName != null && fullName.StartsWith("System.Collections.Generic.KeyValuePair"))
		{
			ScriptObject scriptObject2 = new ScriptObject(2);
			scriptObject2.Import(value, null, MemberRenamer);
			return ToString(span, scriptObject2);
		}
		if (value is IScriptCustomFunction)
		{
			return "<function>";
		}
		return value.ToString();
	}

	public virtual bool ToBool(SourceSpan span, object value)
	{
		if (value == null || value == EmptyScriptObject.Default)
		{
			return false;
		}
		if (value is bool)
		{
			return (bool)value;
		}
		return true;
	}

	public virtual int ToInt(SourceSpan span, object value)
	{
		try
		{
			if (value == null)
			{
				return 0;
			}
			if (!(value is int result))
			{
				return Convert.ToInt32(value, CurrentCulture);
			}
			return result;
		}
		catch (Exception innerException)
		{
			throw new ScriptRuntimeException(span, $"Unable to convert type `{value.GetType()}` to int", innerException);
		}
	}

	public virtual object ToObject(SourceSpan span, object value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
		if (destinationType == typeof(string))
		{
			return ToString(span, value);
		}
		if (destinationType == typeof(int))
		{
			return ToInt(span, value);
		}
		if (destinationType == typeof(bool))
		{
			return ToBool(span, value);
		}
		if (value == null)
		{
			if (destinationType == typeof(double))
			{
				return 0.0;
			}
			if (destinationType == typeof(float))
			{
				return 0f;
			}
			if (destinationType == typeof(long))
			{
				return 0L;
			}
			if (destinationType == typeof(decimal))
			{
				return 0m;
			}
			return null;
		}
		Type type = value.GetType();
		if (destinationType == type)
		{
			return value;
		}
		TypeInfo typeInfo = type.GetTypeInfo();
		TypeInfo typeInfo2 = destinationType.GetTypeInfo();
		if (type.IsPrimitiveOrDecimal() && destinationType.IsPrimitiveOrDecimal())
		{
			try
			{
				return Convert.ChangeType(value, destinationType, CurrentCulture);
			}
			catch (Exception innerException)
			{
				throw new ScriptRuntimeException(span, $"Unable to convert type `{value.GetType()}` to `{destinationType}`", innerException);
			}
		}
		if (destinationType == typeof(IList))
		{
			return ToList(span, value);
		}
		if (typeInfo2.IsAssignableFrom(typeInfo))
		{
			return value;
		}
		throw new ScriptRuntimeException(span, $"Unable to convert type `{value.GetType()}` to `{destinationType}`");
	}
}
