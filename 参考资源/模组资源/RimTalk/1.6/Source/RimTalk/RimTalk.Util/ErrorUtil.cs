using System;
using RimTalk.Data;

namespace RimTalk.Util;

public static class ErrorUtil
{
	public static string ExtractErrorMessage(string jsonResponse)
	{
		if (string.IsNullOrEmpty(jsonResponse))
		{
			return null;
		}
		try
		{
			ErrorResponse errorResponse = JsonUtil.DeserializeFromJson<ErrorResponse>(jsonResponse);
			if (errorResponse?.Error != null && !string.IsNullOrEmpty(errorResponse.Error.Message))
			{
				return errorResponse.Error.Message;
			}
		}
		catch (Exception)
		{
		}
		return null;
	}
}
