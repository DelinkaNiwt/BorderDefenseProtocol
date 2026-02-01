using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class Player2StreamChunk
{
	[DataMember(Name = "id")]
	public string Id { get; set; }

	[DataMember(Name = "choices")]
	public List<Choice> Choices { get; set; }

	[DataMember(Name = "usage")]
	public Usage Usage { get; set; }

	[DataMember(Name = "error")]
	public string Error { get; set; }
}
