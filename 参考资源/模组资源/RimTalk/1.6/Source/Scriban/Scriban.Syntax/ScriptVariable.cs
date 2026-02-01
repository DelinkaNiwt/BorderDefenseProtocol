using System;

namespace Scriban.Syntax;

[ScriptSyntax("variable", "<variable_name>")]
public abstract class ScriptVariable : ScriptExpression, IScriptVariablePath, IEquatable<ScriptVariable>
{
	private readonly int _hashCode;

	public static readonly ScriptVariableLocal Arguments = new ScriptVariableLocal(string.Empty);

	public static readonly ScriptVariableLocal BlockDelegate = new ScriptVariableLocal("$");

	public static readonly ScriptVariableLocal Continue = new ScriptVariableLocal("continue");

	public static readonly ScriptVariableLoop ForObject = new ScriptVariableLoop("for");

	public static readonly ScriptVariableLoop TablerowObject = new ScriptVariableLoop("tablerow");

	public static readonly ScriptVariableLoop WhileObject = new ScriptVariableLoop("while");

	public string Name { get; }

	public ScriptVariableScope Scope { get; }

	protected ScriptVariable(string name, ScriptVariableScope scope)
	{
		Name = name;
		Scope = scope;
		_hashCode = (Name.GetHashCode() * 397) ^ (int)Scope;
	}

	public static ScriptVariable Create(string name, ScriptVariableScope scope)
	{
		return scope switch
		{
			ScriptVariableScope.Global => new ScriptVariableGlobal(name), 
			ScriptVariableScope.Local => new ScriptVariableLocal(name), 
			ScriptVariableScope.Loop => new ScriptVariableLoop(name), 
			_ => throw new InvalidOperationException($"Scope `{scope}` is not supported"), 
		};
	}

	public string GetFirstPath()
	{
		return ToString();
	}

	public virtual bool Equals(ScriptVariable other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (string.Equals(Name, other.Name))
		{
			return Scope == other.Scope;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj is ScriptVariable)
		{
			return Equals((ScriptVariable)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}

	public override string ToString()
	{
		if (Scope != ScriptVariableScope.Local)
		{
			return Name;
		}
		return "$" + Name;
	}

	public static bool operator ==(ScriptVariable left, ScriptVariable right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(ScriptVariable left, ScriptVariable right)
	{
		return !object.Equals(left, right);
	}

	public override object Evaluate(TemplateContext context)
	{
		return context.GetValue((ScriptExpression)this);
	}

	public virtual object GetValue(TemplateContext context)
	{
		return context.GetValue(this);
	}

	public void SetValue(TemplateContext context, object valueToSet)
	{
		context.SetValue(this, valueToSet);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(ToString());
	}
}
