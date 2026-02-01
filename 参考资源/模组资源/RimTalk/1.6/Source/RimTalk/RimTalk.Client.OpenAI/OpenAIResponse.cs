using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class OpenAIResponse
{
	[DataMember(Name = "id")]
	public string Id { get; set; }

	[DataMember(Name = "object")]
	public string Object { get; set; }

	[DataMember(Name = "created")]
	public long Created { get; set; }

	[DataMember(Name = "model")]
	public string Model { get; set; }

	[DataMember(Name = "choices")]
	public List<Choice> Choices { get; set; }

	[DataMember(Name = "usage")]
	public Usage Usage { get; set; }
}
