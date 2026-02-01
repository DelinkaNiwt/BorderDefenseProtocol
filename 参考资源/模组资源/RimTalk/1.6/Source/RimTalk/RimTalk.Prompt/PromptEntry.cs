using System;
using System.Text.RegularExpressions;
using Verse;

namespace RimTalk.Prompt;

public class PromptEntry : IExposable
{
	private string _id = Guid.NewGuid().ToString();

	private string _sourceModId;

	private string _name = "New Prompt";

	public string Content = "";

	public PromptRole Role = PromptRole.System;

	public PromptPosition Position = PromptPosition.Relative;

	public int InChatDepth = 0;

	public bool Enabled = true;

	public bool IsMainChatHistory = false;

	public string Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			UpdateIdIfModEntry();
		}
	}

	public string SourceModId
	{
		get
		{
			return _sourceModId;
		}
		set
		{
			_sourceModId = value;
			UpdateIdIfModEntry();
		}
	}

	private void UpdateIdIfModEntry()
	{
		if (!string.IsNullOrEmpty(_sourceModId) && !string.IsNullOrEmpty(_name))
		{
			_id = GenerateDeterministicId(_sourceModId, _name);
		}
	}

	public static string GenerateDeterministicId(string modId, string name)
	{
		string sanitizedModId = SanitizeForId(modId);
		string sanitizedName = SanitizeForId(name);
		return "mod_" + sanitizedModId + "_" + sanitizedName;
	}

	private static string SanitizeForId(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "unknown";
		}
		return Regex.Replace(input.ToLowerInvariant(), "[^a-z0-9]", "");
	}

	public PromptEntry()
	{
	}

	public PromptEntry(string name, string content, PromptRole role = PromptRole.System, int inChatDepth = 0)
	{
		_name = name;
		Content = content;
		Role = role;
		InChatDepth = inChatDepth;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref _id, "id", Guid.NewGuid().ToString());
		Scribe_Values.Look(ref _name, "name", "New Prompt");
		Scribe_Values.Look(ref Content, "content", "");
		Scribe_Values.Look(ref Role, "role", PromptRole.System);
		Scribe_Values.Look(ref Position, "position", PromptPosition.Relative);
		Scribe_Values.Look(ref InChatDepth, "inChatDepth", 0);
		Scribe_Values.Look(ref Enabled, "enabled", defaultValue: true);
		Scribe_Values.Look(ref _sourceModId, "sourceModId");
		Scribe_Values.Look(ref IsMainChatHistory, "isMainChatHistory", defaultValue: false);
		if (string.IsNullOrEmpty(_id))
		{
			_id = Guid.NewGuid().ToString();
		}
	}

	public PromptEntry Clone()
	{
		return new PromptEntry
		{
			Id = Guid.NewGuid().ToString(),
			Name = Name,
			Content = Content,
			Role = Role,
			Position = Position,
			InChatDepth = InChatDepth,
			Enabled = Enabled,
			IsMainChatHistory = IsMainChatHistory,
			SourceModId = null
		};
	}

	public override string ToString()
	{
		return $"[{Role}] {Name} (Enabled: {Enabled})";
	}
}
