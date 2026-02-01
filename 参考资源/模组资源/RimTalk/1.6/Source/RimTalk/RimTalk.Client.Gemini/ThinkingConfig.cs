using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class ThinkingConfig
{
	[DataMember(Name = "thinkingBudget")]
	public int ThinkingBudget { get; set; }
}
