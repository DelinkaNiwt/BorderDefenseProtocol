using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class Choice
{
	[DataMember(Name = "message")]
	public Message Message { get; set; }

	[DataMember(Name = "delta")]
	public Delta Delta { get; set; }

	[DataMember(Name = "finish_reason")]
	public string FinishReason { get; set; }
}
