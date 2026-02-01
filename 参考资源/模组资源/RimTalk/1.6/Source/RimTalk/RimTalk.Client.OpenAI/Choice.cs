using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class Choice
{
	[DataMember(Name = "index")]
	public int Index { get; set; }

	[DataMember(Name = "message")]
	public Message Message { get; set; }

	[DataMember(Name = "finish_reason")]
	public string FinishReason { get; set; }
}
