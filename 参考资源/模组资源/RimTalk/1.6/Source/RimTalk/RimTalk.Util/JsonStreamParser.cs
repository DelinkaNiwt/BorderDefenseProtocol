using System.Collections.Generic;
using System.Text;

namespace RimTalk.Util;

public class JsonStreamParser<T> where T : class
{
	private readonly StringBuilder _buffer = new StringBuilder();

	public List<T> Parse(string textChunk)
	{
		_buffer.Append(textChunk);
		List<T> newObjects = new List<T>();
		string text = _buffer.ToString();
		int searchStart = 0;
		int lastSuccessfulEnd = 0;
		while (searchStart < text.Length)
		{
			int objStart = text.IndexOf('{', searchStart);
			if (objStart == -1)
			{
				break;
			}
			int objEnd = FindMatchingBrace(text, objStart);
			if (objEnd == -1)
			{
				break;
			}
			string jsonObj = text.Substring(objStart, objEnd - objStart + 1);
			try
			{
				T parsedObject = JsonUtil.DeserializeFromJson<T>(jsonObj);
				if (parsedObject != null)
				{
					newObjects.Add(parsedObject);
				}
			}
			catch
			{
			}
			searchStart = objEnd + 1;
			lastSuccessfulEnd = searchStart;
		}
		if (lastSuccessfulEnd > 0)
		{
			_buffer.Remove(0, lastSuccessfulEnd);
		}
		return newObjects;
	}

	private int FindMatchingBrace(string text, int openIndex)
	{
		int depth = 0;
		bool inString = false;
		bool escaped = false;
		for (int i = openIndex; i < text.Length; i++)
		{
			char c = text[i];
			if (escaped)
			{
				escaped = false;
				continue;
			}
			switch (c)
			{
			case '\\':
				escaped = true;
				continue;
			case '"':
				inString = !inString;
				continue;
			}
			if (inString)
			{
				continue;
			}
			switch (c)
			{
			case '{':
				depth++;
				break;
			case '}':
				depth--;
				if (depth == 0)
				{
					return i;
				}
				break;
			}
		}
		return -1;
	}
}
