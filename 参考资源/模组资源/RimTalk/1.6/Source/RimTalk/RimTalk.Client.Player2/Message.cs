using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class Message
{
	[DataMember(Name = "role")]
	public string Role { get; set; }

	[DataMember(Name = "content")]
	public string Content { get; set; }
}
