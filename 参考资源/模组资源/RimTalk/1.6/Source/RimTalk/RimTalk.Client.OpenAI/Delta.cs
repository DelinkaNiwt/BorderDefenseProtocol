using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class Delta
{
	[DataMember(Name = "content")]
	public string Content { get; set; }
}
