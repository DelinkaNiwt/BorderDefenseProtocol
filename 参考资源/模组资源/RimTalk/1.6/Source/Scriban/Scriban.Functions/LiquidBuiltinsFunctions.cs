using System;
using Scriban.Runtime;

namespace Scriban.Functions;

public class LiquidBuiltinsFunctions : ScriptObject
{
	private class DefaultBuiltins : ScriptObject
	{
		public DefaultBuiltins()
			: base(50, false)
		{
			ScriptObject scriptObject = (ScriptObject)BuiltinFunctions.Default["math"];
			ScriptObject scriptObject2 = (ScriptObject)BuiltinFunctions.Default["string"];
			ScriptObject scriptObject3 = (ScriptObject)BuiltinFunctions.Default["array"];
			ScriptObject value = (ScriptObject)BuiltinFunctions.Default[DateTimeFunctions.DateVariable.Name];
			ScriptObject scriptObject4 = (ScriptObject)BuiltinFunctions.Default["html"];
			ScriptObject scriptObject5 = (ScriptObject)BuiltinFunctions.Default["object"];
			SetValue("abs", scriptObject["abs"], readOnly: true);
			SetValue("append", scriptObject2["append"], readOnly: true);
			SetValue("capitalize", scriptObject2["capitalize"], readOnly: true);
			SetValue("ceil", scriptObject["ceil"], readOnly: true);
			SetValue("compact", scriptObject3["compact"], readOnly: true);
			SetValue("concat", scriptObject3["concat"], readOnly: true);
			SetValue("cycle", scriptObject3["cycle"], readOnly: true);
			SetValue("date", value, readOnly: true);
			SetValue("default", scriptObject5["default"], readOnly: true);
			SetValue("divided_by", scriptObject["divided_by"], readOnly: true);
			SetValue("downcase", scriptObject2["downcase"], readOnly: true);
			SetValue("escape", scriptObject4["escape"], readOnly: true);
			SetValue("first", scriptObject3["first"], readOnly: true);
			SetValue("floor", scriptObject["floor"], readOnly: true);
			SetValue("join", scriptObject3["join"], readOnly: true);
			SetValue("last", scriptObject3["last"], readOnly: true);
			SetValue("lstrip", scriptObject2["lstrip"], readOnly: true);
			SetValue("map", scriptObject3["map"], readOnly: true);
			SetValue("minus", scriptObject["minus"], readOnly: true);
			SetValue("modulo", scriptObject["modulo"], readOnly: true);
			SetValue("plus", scriptObject["plus"], readOnly: true);
			SetValue("prepend", scriptObject2["prepend"], readOnly: true);
			SetValue("remove", scriptObject2["remove"], readOnly: true);
			SetValue("remove_first", scriptObject2["remove_first"], readOnly: true);
			SetValue("replace", scriptObject2["replace"], readOnly: true);
			SetValue("replace_first", scriptObject2["replace_first"], readOnly: true);
			SetValue("reverse", scriptObject3["reverse"], readOnly: true);
			SetValue("round", scriptObject["round"], readOnly: true);
			SetValue("rstrip", scriptObject2["rstrip"], readOnly: true);
			SetValue("size", scriptObject5["size"], readOnly: true);
			SetValue("slice", scriptObject2["slice1"], readOnly: true);
			SetValue("sort", scriptObject3["sort"], readOnly: true);
			SetValue("split", scriptObject2["split"], readOnly: true);
			SetValue("strip", scriptObject2["strip"], readOnly: true);
			SetValue("strip_html", scriptObject4["strip"], readOnly: true);
			SetValue("strip_newlines", scriptObject2["strip_newlines"], readOnly: true);
			SetValue("times", scriptObject["times"], readOnly: true);
			SetValue("truncate", scriptObject2["truncate"], readOnly: true);
			SetValue("truncatewords", scriptObject2["truncatewords"], readOnly: true);
			SetValue("uniq", scriptObject3["uniq"], readOnly: true);
			SetValue("contains", scriptObject3["contains"], readOnly: true);
			this.Import(typeof(LiquidBuiltinsFunctions), ScriptMemberImportFlags.All);
		}
	}

	private static readonly DefaultBuiltins Default = new DefaultBuiltins();

	public LiquidBuiltinsFunctions()
		: base(50, false)
	{
		((ScriptObject)BuiltinFunctions.Default.Clone(deep: true)).CopyTo(this);
		((ScriptObject)Default.Clone(deep: false)).CopyTo(this);
	}

	public static bool TryLiquidToScriban(string liquidBuiltin, out string target, out string member)
	{
		if (liquidBuiltin == null)
		{
			throw new ArgumentNullException("liquidBuiltin");
		}
		target = null;
		member = null;
		switch (liquidBuiltin)
		{
		case "abs":
			target = "math";
			member = "abs";
			return true;
		case "append":
			target = "string";
			member = "append";
			return true;
		case "capitalize":
			target = "string";
			member = "capitalize";
			return true;
		case "ceil":
			target = "math";
			member = "ceil";
			return true;
		case "compact":
			target = "array";
			member = "compact";
			return true;
		case "concat":
			target = "array";
			member = "concat";
			return true;
		case "cycle":
			target = "array";
			member = "cycle";
			return true;
		case "date":
			target = "date";
			member = "parse";
			return true;
		case "default":
			target = "object";
			member = "default";
			return true;
		case "divided_by":
			target = "math";
			member = "divided_by";
			return true;
		case "downcase":
			target = "string";
			member = "downcase";
			return true;
		case "escape":
			target = "html";
			member = "escape";
			return true;
		case "escape_once":
			target = "html";
			member = "escape_once";
			return true;
		case "first":
			target = "array";
			member = "first";
			return true;
		case "floor":
			target = "math";
			member = "floor";
			return true;
		case "join":
			target = "array";
			member = "join";
			return true;
		case "last":
			target = "array";
			member = "last";
			return true;
		case "lstrip":
			target = "string";
			member = "lstrip";
			return true;
		case "map":
			target = "array";
			member = "map";
			return true;
		case "minus":
			target = "math";
			member = "minus";
			return true;
		case "modulo":
			target = "math";
			member = "modulo";
			return true;
		case "newline_to_br":
			target = "html";
			member = "newline_to_br";
			return true;
		case "plus":
			target = "math";
			member = "plus";
			return true;
		case "prepend":
			target = "string";
			member = "prepend";
			return true;
		case "remove":
			target = "string";
			member = "remove";
			return true;
		case "remove_first":
			target = "string";
			member = "remove_first";
			return true;
		case "replace":
			target = "string";
			member = "replace";
			return true;
		case "replace_first":
			target = "string";
			member = "replace_first";
			return true;
		case "reverse":
			target = "array";
			member = "reverse";
			return true;
		case "round":
			target = "math";
			member = "round";
			return true;
		case "rstrip":
			target = "string";
			member = "rstrip";
			return true;
		case "size":
			target = "object";
			member = "size";
			return true;
		case "slice":
			target = "string";
			member = "slice1";
			return true;
		case "sort":
			target = "array";
			member = "sort";
			return true;
		case "split":
			target = "string";
			member = "split";
			return true;
		case "strip":
			target = "string";
			member = "strip";
			return true;
		case "strip_html":
			target = "html";
			member = "strip";
			return true;
		case "strip_newlines":
			target = "string";
			member = "strip_newlines";
			return true;
		case "times":
			target = "math";
			member = "times";
			return true;
		case "truncate":
			target = "string";
			member = "truncate";
			return true;
		case "truncatewords":
			target = "string";
			member = "truncatewords";
			return true;
		case "uniq":
			target = "array";
			member = "uniq";
			return true;
		case "contains":
			target = "array";
			member = "contains";
			return true;
		default:
			return false;
		}
	}
}
