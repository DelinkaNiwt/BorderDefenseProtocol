using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class GenerationConfig
{
	[DataMember(Name = "temperature")]
	public float Temperature { get; set; } = 0.7f;

	[DataMember(Name = "topK")]
	public int TopK { get; set; } = 40;

	[DataMember(Name = "topP")]
	public float TopP { get; set; } = 0.95f;

	[DataMember(Name = "maxOutputTokens")]
	public int MaxOutputTokens { get; set; } = 8192;

	[DataMember(Name = "thinkingConfig")]
	public ThinkingConfig ThinkingConfig { get; set; }
}
