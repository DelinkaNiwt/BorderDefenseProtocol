using System;
using System.Collections.Generic;
using System.Text;
using RimTalk.Util;
using UnityEngine.Networking;

namespace RimTalk.Client.OpenAI;

public class OpenAIStreamHandler(Action<string> onContentReceived) : DownloadHandlerScript()
{
	private readonly StringBuilder _buffer = new StringBuilder();

	private readonly StringBuilder _fullText = new StringBuilder();

	private readonly StringBuilder _allReceivedData = new StringBuilder();

	private string _id;

	private string _object;

	private long _created;

	private string _model;

	private string _finishReason;

	private Usage _usage;

	protected override bool ReceiveData(byte[] data, int dataLength)
	{
		if (data == null || dataLength == 0)
		{
			return false;
		}
		string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
		_buffer.Append(chunk);
		_allReceivedData.Append(chunk);
		string bufferContent = _buffer.ToString();
		string[] lines = bufferContent.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		_buffer.Clear();
		if (!bufferContent.EndsWith("\n"))
		{
			_buffer.Append(lines[lines.Length - 1]);
		}
		int linesToProcess = (bufferContent.EndsWith("\n") ? lines.Length : (lines.Length - 1));
		for (int i = 0; i < linesToProcess; i++)
		{
			string line = lines[i].Trim();
			if (!line.StartsWith("data: "))
			{
				continue;
			}
			string jsonData = line.Substring(6);
			if (jsonData.Trim() == "[DONE]")
			{
				continue;
			}
			try
			{
				OpenAIStreamChunk openAIChunk = JsonUtil.DeserializeFromJson<OpenAIStreamChunk>(jsonData);
				if (!string.IsNullOrEmpty(openAIChunk.Id))
				{
					_id = openAIChunk.Id;
					_object = openAIChunk.Object;
					_created = openAIChunk.Created;
					_model = openAIChunk.Model;
				}
				if (openAIChunk?.Choices != null && openAIChunk.Choices.Count > 0)
				{
					StreamChoice choice = openAIChunk.Choices[0];
					string content = choice?.Delta?.Content;
					if (!string.IsNullOrEmpty(content))
					{
						_fullText.Append(content);
						onContentReceived?.Invoke(content);
					}
					if (!string.IsNullOrEmpty(choice.FinishReason))
					{
						_finishReason = choice.FinishReason;
					}
				}
				if (openAIChunk?.Usage != null)
				{
					_usage = openAIChunk.Usage;
				}
			}
			catch (Exception ex)
			{
				Logger.Warning("Failed to parse stream chunk: " + ex.Message + "\nJSON: " + jsonData);
			}
		}
		return true;
	}

	public string GetFullText()
	{
		return _fullText.ToString();
	}

	public int GetTotalTokens()
	{
		return _usage?.TotalTokens ?? 0;
	}

	public string GetAllReceivedText()
	{
		return _allReceivedData.ToString();
	}

	public string GetRawJson()
	{
		OpenAIResponse response = new OpenAIResponse
		{
			Id = _id,
			Object = _object,
			Created = _created,
			Model = _model,
			Choices = new List<Choice>(1)
			{
				new Choice
				{
					Index = 0,
					Message = new Message
					{
						Role = "assistant",
						Content = GetFullText()
					},
					FinishReason = _finishReason
				}
			},
			Usage = _usage
		};
		return JsonUtil.SerializeToJson(response);
	}
}
