using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class Player2Request
{
	[DataMember(Name = "messages")]
	public List<Message> Messages { get; set; } = new List<Message>();

	[DataMember(Name = "stream", EmitDefaultValue = false)]
	public bool? Stream { get; set; }
}
