using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class SystemInstruction
{
	[DataMember(Name = "parts")]
	public List<Part> Parts { get; set; } = new List<Part>();
}
