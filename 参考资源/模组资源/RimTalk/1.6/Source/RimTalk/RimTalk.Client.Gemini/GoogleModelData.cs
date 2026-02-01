using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class GoogleModelData
{
	[DataMember(Name = "name")]
	public string Name;

	[DataMember(Name = "displayName")]
	public string DisplayName;

	[DataMember(Name = "description")]
	public string Description;

	[DataMember(Name = "supportedGenerationMethods")]
	public List<string> SupportedGenerationMethods;
}
