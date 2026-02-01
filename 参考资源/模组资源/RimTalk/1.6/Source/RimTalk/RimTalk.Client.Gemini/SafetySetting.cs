using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class SafetySetting
{
	[DataMember(Name = "category")]
	public string Category { get; set; } = string.Empty;

	[DataMember(Name = "threshold")]
	public string Threshold { get; set; } = string.Empty;
}
