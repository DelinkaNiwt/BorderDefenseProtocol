namespace Scriban.Runtime.Accessors;

internal class DictionaryStringObjectAccessor : GenericDictionaryAccessor<string, object>
{
	public static readonly DictionaryStringObjectAccessor Default = new DictionaryStringObjectAccessor();
}
