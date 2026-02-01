using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.Gemini;

[DataContract]
public class GeminiResponse
{
	[DataMember(Name = "candidates")]
	public List<Candidate> Candidates { get; set; } = new List<Candidate>();

	[DataMember(Name = "usageMetadata")]
	public UsageMetadata UsageMetadata { get; set; }
}
