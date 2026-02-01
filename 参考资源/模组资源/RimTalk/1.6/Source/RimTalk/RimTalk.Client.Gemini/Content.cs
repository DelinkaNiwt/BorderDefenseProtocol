using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class Content
{
	[DataMember(Name = "role")]
	public string Role { get; set; } = "user";

	[DataMember(Name = "parts")]
	public List<Part> Parts { get; set; } = new List<Part>();
}
