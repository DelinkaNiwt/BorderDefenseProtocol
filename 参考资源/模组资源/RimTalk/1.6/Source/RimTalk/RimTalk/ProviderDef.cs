using System.Collections.Generic;

namespace RimTalk;

public struct ProviderDef
{
	public string Label;

	public string EndpointUrl;

	public string ListModelsUrl;

	public Dictionary<string, string> ExtraHeaders;
}
