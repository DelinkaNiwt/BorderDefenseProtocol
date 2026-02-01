using System.Runtime.Serialization;

namespace RimTalk.Client.Player2;

[DataContract]
public class LocalPlayer2Response
{
	[DataMember(Name = "p2Key")]
	public string P2Key { get; set; }
}
