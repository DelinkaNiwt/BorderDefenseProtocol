using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class Usage
{
	[DataMember(Name = "total_tokens")]
	public int TotalTokens { get; set; }
}
