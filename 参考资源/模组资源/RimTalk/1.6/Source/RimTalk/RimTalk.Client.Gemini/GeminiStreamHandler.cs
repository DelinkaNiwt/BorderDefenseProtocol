using System;
using System.Collections.Generic;
using System.Text;
using RimTalk.Util;
using UnityEngine.Networking;

namespace RimTalk.Client.Gemini;

public class GeminiStreamHandler(Action<string> onJsonReceived) : DownloadHandlerScript()
{
	private readonly StringBuilder _buffer = new StringBuilder();

	private readonly StringBuilder _fullText = new StringBuilder();

	private readonly StringBuilder _allReceivedData = new StringBuilder();

	private string _finishReason;

	private UsageMetadata _usageMetadata;

	protected override bool ReceiveData(byte[] data, int dataLength)
	{
		if (data == null || dataLength == 0)
		{
			return false;
		}
		string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
		_buffer.Append(chunk);
		_allReceivedData.Append(chunk);
		ProcessBuffer();
		return true;
	}

	private void ProcessBuffer()
	{
		string bufferContent = _buffer.ToString();
		string[] lines = bufferContent.Split(new char[1] { '\n' }, StringSplitOptions.None);
		_buffer.Clear();
		_buffer.Append(lines[lines.Length - 1]);
		for (int i = 0; i < lines.Length - 1; i++)
		{
			string line = lines[i].Trim();
			if (line.StartsWith("data: "))
			{
				string jsonData = line.Substring(6);
				ProcessStreamChunk(jsonData);
			}
		}
	}

	private void ProcessStreamChunk(string jsonData)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(jsonData))
			{
				return;
			}
			GeminiResponse response = JsonUtil.DeserializeFromJson<GeminiResponse>(jsonData);
			List<Candidate> list = response?.Candidates;
			if (list != null && list.Count > 0)
			{
				List<Part> list2 = response.Candidates[0]?.Content?.Parts;
				if (list2 != null && list2.Count > 0)
				{
					Candidate candidate = response.Candidates[0];
					string content = candidate.Content.Parts[0].Text;
					if (!string.IsNullOrEmpty(content))
					{
						_fullText.Append(content);
						onJsonReceived?.Invoke(content);
					}
					if (!string.IsNullOrEmpty(candidate.FinishReason))
					{
						_finishReason = candidate.FinishReason;
					}
				}
			}
			if (response?.UsageMetadata != null)
			{
				_usageMetadata = response.UsageMetadata;
			}
		}
		catch (Exception ex)
		{
			Logger.Warning("Failed to parse streaming chunk: " + ex.Message + "\nJSON: " + jsonData);
		}
	}

	public string GetFullText()
	{
		return _fullText.ToString();
	}

	public int GetTotalTokens()
	{
		return _usageMetadata?.TotalTokenCount ?? 0;
	}

	public string GetAllReceivedText()
	{
		return _allReceivedData.ToString();
	}

	public string GetRawJson()
	{
		GeminiResponse response = new GeminiResponse
		{
			Candidates = new List<Candidate>(1)
			{
				new Candidate
				{
					Content = new Content
					{
						Role = "model",
						Parts = new List<Part>(1)
						{
							new Part
							{
								Text = GetFullText()
							}
						}
					},
					FinishReason = _finishReason
				}
			},
			UsageMetadata = _usageMetadata
		};
		return JsonUtil.SerializeToJson(response);
	}
}
