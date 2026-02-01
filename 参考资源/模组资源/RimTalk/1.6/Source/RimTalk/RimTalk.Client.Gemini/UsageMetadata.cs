using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class UsageMetadata
{
	[DataMember(Name = "promptTokenCount")]
	public int PromptTokenCount { get; set; }

	[DataMember(Name = "candidatesTokenCount")]
	public int CandidatesTokenCount { get; set; }

	[DataMember(Name = "totalTokenCount")]
	public int TotalTokenCount { get; set; }
}
