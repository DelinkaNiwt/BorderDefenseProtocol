using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace RimTalk.Util;

public static class JsonUtil
{
	public static string SerializeToJson<T>(T obj)
	{
		using MemoryStream stream = new MemoryStream();
		DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
		serializer.WriteObject((Stream)stream, (object)obj);
		return Encoding.UTF8.GetString(stream.ToArray());
	}

	public static T DeserializeFromJson<T>(string json)
	{
		string sanitizedJson = Sanitize(json, typeof(T));
		try
		{
			using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(sanitizedJson));
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
			return (T)serializer.ReadObject((Stream)stream);
		}
		catch (Exception)
		{
			Logger.Error("Json deserialization failed for " + typeof(T).Name + "\n" + json);
			throw;
		}
	}

	public static string Sanitize(string text, Type targetType)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string sanitized = text.Replace("```json", "").Replace("```", "").Trim();
		int startIndex = sanitized.IndexOfAny(new char[2] { '{', '[' });
		int endIndex = sanitized.LastIndexOfAny(new char[2] { '}', ']' });
		if (startIndex >= 0 && endIndex > startIndex)
		{
			sanitized = sanitized.Substring(startIndex, endIndex - startIndex + 1).Trim();
			sanitized = Regex.Replace(sanitized, "\"([^\"]+)\"\\s*:\\s*([,}])", "\"$1\":null$2");
			if (sanitized.Contains("]["))
			{
				sanitized = sanitized.Replace("][", ",");
			}
			if (sanitized.Contains("}{"))
			{
				sanitized = sanitized.Replace("}{", "},{");
			}
			if (sanitized.StartsWith("{") && sanitized.EndsWith("}"))
			{
				string innerContent = sanitized.Substring(1, sanitized.Length - 2).Trim();
				if (innerContent.StartsWith("[") && innerContent.EndsWith("]"))
				{
					sanitized = innerContent;
				}
			}
			if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string) && sanitized.StartsWith("{"))
			{
				sanitized = "[" + sanitized + "]";
			}
			return sanitized;
		}
		return string.Empty;
	}
}
