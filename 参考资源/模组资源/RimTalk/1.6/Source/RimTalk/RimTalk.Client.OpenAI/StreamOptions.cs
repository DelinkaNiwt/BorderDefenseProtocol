using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class StreamOptions
{
	[DataMember(Name = "include_usage", EmitDefaultValue = false)]
	public bool? IncludeUsage { get; set; }
}
