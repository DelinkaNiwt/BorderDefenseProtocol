using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class Part
{
	[DataMember(Name = "text")]
	public string Text { get; set; } = string.Empty;
}
