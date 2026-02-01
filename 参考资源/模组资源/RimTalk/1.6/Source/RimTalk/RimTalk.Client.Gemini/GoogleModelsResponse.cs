using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class GoogleModelsResponse
{
	[DataMember(Name = "models")]
	public List<GoogleModelData> Models;
}
