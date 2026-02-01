using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class Candidate
{
	[DataMember(Name = "content")]
	public Content Content { get; set; } = new Content();

	[DataMember(Name = "finishReason")]
	public string FinishReason { get; set; } = string.Empty;

	[DataMember(Name = "index")]
	public int Index { get; set; }
}
