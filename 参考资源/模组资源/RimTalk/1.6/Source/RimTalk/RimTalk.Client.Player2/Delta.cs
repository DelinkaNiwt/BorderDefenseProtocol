using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class Delta
{
	[DataMember(Name = "content")]
	public string Content { get; set; }
}
