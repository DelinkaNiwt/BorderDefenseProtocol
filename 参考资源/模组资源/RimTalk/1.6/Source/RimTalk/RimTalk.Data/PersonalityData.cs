using System.Runtime.Serialization;

namespace RimTalk.Data;

[DataContract]
public class PersonalityData([property: DataMember(Name = "persona")] string persona, [property: DataMember(Name = "chattiness")] float chattiness = 1f) : IJsonData
{
	[DataMember(Name = "persona")]
	public string Persona { get; set; } = persona;

	[DataMember(Name = "chattiness")]
	public float Chattiness { get; set; } = chattiness;

	public string GetText()
	{
		return Persona;
	}
}
