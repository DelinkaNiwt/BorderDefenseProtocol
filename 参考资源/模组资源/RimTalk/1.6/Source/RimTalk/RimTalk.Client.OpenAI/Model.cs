using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class Model
{
	[DataMember(Name = "id")]
	public string Id { get; set; }
}
