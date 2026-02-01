using System;
using System.Runtime.Serialization;
using RimTalk.Source.Data;
using Verse;

namespace RimTalk.Data;

[DataContract]
public class TalkResponse : IJsonData
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public TalkType TalkType { get; set; }

	[DataMember(Name = "name")]
	public string Name { get; set; }

	[DataMember(Name = "text")]
	public string Text { get; set; }

	[DataMember(Name = "act", EmitDefaultValue = false)]
	public string? InteractionRaw { get; set; }

	[DataMember(Name = "target", EmitDefaultValue = false)]
	public string? TargetName { get; set; }

	public Guid ParentTalkId { get; set; }

	public TalkResponse(TalkType talkType, string name, string text)
	{
		TalkType = talkType;
		Name = name;
		Text = text;
		base._002Ector();
	}

	public bool IsReply()
	{
		return ParentTalkId != Guid.Empty;
	}

	public string GetText()
	{
		return Text;
	}

	public InteractionType GetInteractionType()
	{
		if (string.IsNullOrWhiteSpace(InteractionRaw))
		{
			return InteractionType.None;
		}
		InteractionType result;
		return Enum.TryParse<InteractionType>(InteractionRaw, ignoreCase: true, out result) ? result : InteractionType.None;
	}

	public Pawn? GetTarget()
	{
		return (TargetName == null) ? null : Cache.GetByName(TargetName)?.Pawn;
	}

	public override string ToString()
	{
		return $"Type: {TalkType} | Name: {Name} | Text: \"{Text}\" | " + "Int: " + InteractionRaw + " | Target: " + TargetName;
	}
}
