using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class StreamChoice
{
	[DataMember(Name = "index")]
	public int Index { get; set; }

	[DataMember(Name = "delta")]
	public Delta Delta { get; set; }

	[DataMember(Name = "finish_reason")]
	public string FinishReason { get; set; }
}
