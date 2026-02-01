using Scriban.Runtime;

namespace Scriban.Functions;

public class BuiltinFunctions : ScriptObject
{
	private class DefaultBuiltins : ScriptObject
	{
		public DefaultBuiltins()
			: base(10, false)
		{
			SetValue("array", new ArrayFunctions(), readOnly: true);
			SetValue("empty", EmptyScriptObject.Default, readOnly: true);
			SetValue("include", new IncludeFunction(), readOnly: true);
			SetValue(DateTimeFunctions.DateVariable.Name, new DateTimeFunctions(), readOnly: true);
			SetValue("html", new HtmlFunctions(), readOnly: true);
			SetValue("math", new MathFunctions(), readOnly: true);
			SetValue("object", new ObjectFunctions(), readOnly: true);
			SetValue("regex", new RegexFunctions(), readOnly: true);
			SetValue("string", new StringFunctions(), readOnly: true);
			SetValue("timespan", new TimeSpanFunctions(), readOnly: true);
		}
	}

	internal static readonly ScriptObject Default = new DefaultBuiltins();

	public BuiltinFunctions()
		: base(10)
	{
		((ScriptObject)Default.Clone(deep: true)).CopyTo(this);
	}
}
