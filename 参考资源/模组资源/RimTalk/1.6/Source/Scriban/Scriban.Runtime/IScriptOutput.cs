namespace Scriban.Runtime;

public interface IScriptOutput
{
	IScriptOutput Write(string text, int offset, int count);
}
