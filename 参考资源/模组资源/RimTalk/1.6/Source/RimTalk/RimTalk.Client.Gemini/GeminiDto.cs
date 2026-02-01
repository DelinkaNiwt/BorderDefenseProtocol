using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class GeminiDto
{
	[DataMember(Name = "system_instruction")]
	public SystemInstruction SystemInstruction { get; set; }

	[DataMember(Name = "contents")]
	public List<Content> Contents { get; set; } = new List<Content>();

	[DataMember(Name = "generationConfig")]
	public GenerationConfig GenerationConfig { get; set; }

	[DataMember(Name = "safetySettings")]
	public List<SafetySetting> SafetySettings { get; set; }
}
