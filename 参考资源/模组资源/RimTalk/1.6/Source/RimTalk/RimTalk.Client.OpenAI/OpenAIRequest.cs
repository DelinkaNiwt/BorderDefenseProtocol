using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimTalk.Client.OpenAI;

[DataContract]
public class OpenAIRequest
{
	[DataMember(Name = "model")]
	public string Model { get; set; }

	[DataMember(Name = "messages")]
	public List<Message> Messages { get; set; } = new List<Message>();

	[DataMember(Name = "temperature", EmitDefaultValue = false)]
	public double? Temperature { get; set; }

	[DataMember(Name = "max_tokens", EmitDefaultValue = false)]
	public int? MaxTokens { get; set; }

	[DataMember(Name = "top_p", EmitDefaultValue = false)]
	public double? TopP { get; set; }

	[DataMember(Name = "top_k", EmitDefaultValue = false)]
	public int? TopK { get; set; }

	[DataMember(Name = "frequency_penalty", EmitDefaultValue = false)]
	public double? FrequencyPenalty { get; set; }

	[DataMember(Name = "presence_penalty", EmitDefaultValue = false)]
	public double? PresencePenalty { get; set; }

	[DataMember(Name = "logit_bias", EmitDefaultValue = false)]
	public Dictionary<string, double> LogitBias { get; set; }

	[DataMember(Name = "stop", EmitDefaultValue = false)]
	public List<string> Stop { get; set; }

	[DataMember(Name = "stream", EmitDefaultValue = false)]
	public bool? Stream { get; set; }

	[DataMember(Name = "stream_options", EmitDefaultValue = false)]
	public StreamOptions StreamOptions { get; set; }
}
