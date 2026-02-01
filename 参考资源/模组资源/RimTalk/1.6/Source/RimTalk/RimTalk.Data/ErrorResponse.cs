using System.Runtime.Serialization;

namespace RimTalk.Data;

[DataContract]
public class ErrorResponse
{
	[DataMember(Name = "error")]
	public ErrorDetail Error { get; set; }
}
