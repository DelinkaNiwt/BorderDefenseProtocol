using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class OpenAIModelsResponse
{
	[DataMember(Name = "data")]
	public List<Model> Data { get; set; }
}
