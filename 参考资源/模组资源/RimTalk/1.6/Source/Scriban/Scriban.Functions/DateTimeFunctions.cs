using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Scriban.Functions;

public class DateTimeFunctions : ScriptObject, IScriptCustomFunction
{
	private const string FormatKey = "format";

	public const string DefaultFormat = "%d %b %Y";

	[ScriptMemberIgnore]
	public static readonly ScriptVariable DateVariable = new ScriptVariableGlobal("date");

	private static readonly Dictionary<char, Func<DateTime, CultureInfo, string>> Formats = new Dictionary<char, Func<DateTime, CultureInfo, string>>
	{
		{
			'a',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("ddd", cultureInfo)
		},
		{
			'A',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("dddd", cultureInfo)
		},
		{
			'b',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("MMM", cultureInfo)
		},
		{
			'B',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("MMMM", cultureInfo)
		},
		{
			'c',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("ddd MMM dd HH:mm:ss yyyy", cultureInfo)
		},
		{
			'C',
			(DateTime dateTime, CultureInfo cultureInfo) => (dateTime.Year / 100).ToString("D2", cultureInfo)
		},
		{
			'd',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("dd", cultureInfo)
		},
		{
			'D',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("MM/dd/yy", cultureInfo)
		},
		{
			'e',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("%d", cultureInfo).PadLeft(2, ' ')
		},
		{
			'F',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("yyyy-MM-dd", cultureInfo)
		},
		{
			'h',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("MMM", cultureInfo)
		},
		{
			'H',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("HH", cultureInfo)
		},
		{
			'I',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("hh", cultureInfo)
		},
		{
			'j',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.DayOfYear.ToString("D3", cultureInfo)
		},
		{
			'k',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("%H", cultureInfo).PadLeft(2, ' ')
		},
		{
			'l',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("%h", cultureInfo).PadLeft(2, ' ')
		},
		{
			'L',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("FFF", cultureInfo)
		},
		{
			'm',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("MM", cultureInfo)
		},
		{
			'M',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("mm", cultureInfo)
		},
		{
			'n',
			(DateTime dateTime, CultureInfo cultureInfo) => "\n"
		},
		{
			'N',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("fffffff00", cultureInfo)
		},
		{
			'p',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("tt", cultureInfo)
		},
		{
			'P',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("tt", cultureInfo).ToLowerInvariant()
		},
		{
			'r',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("hh:mm:ss tt", cultureInfo)
		},
		{
			'R',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("HH:mm", cultureInfo)
		},
		{
			's',
			(DateTime dateTime, CultureInfo cultureInfo) => ((dateTime.ToUniversalTime().Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks) / 10000000).ToString(cultureInfo)
		},
		{
			'S',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("ss", cultureInfo)
		},
		{
			't',
			(DateTime dateTime, CultureInfo cultureInfo) => "\t"
		},
		{
			'T',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("HH:mm:ss", cultureInfo)
		},
		{
			'u',
			(DateTime dateTime, CultureInfo cultureInfo) => ((int)((dateTime.DayOfWeek == DayOfWeek.Sunday) ? ((DayOfWeek)7) : dateTime.DayOfWeek)).ToString(cultureInfo)
		},
		{
			'U',
			(DateTime dateTime, CultureInfo cultureInfo) => cultureInfo.Calendar.GetWeekOfYear(dateTime, cultureInfo.DateTimeFormat.CalendarWeekRule, DayOfWeek.Sunday).ToString("D2", cultureInfo)
		},
		{
			'v',
			(DateTime dateTime, CultureInfo cultureInfo) => string.Format(CultureInfo.InvariantCulture, "{0,2}-{1}-{2:D4}", dateTime.Day, dateTime.ToString("MMM", CultureInfo.InvariantCulture).ToUpper(), dateTime.Year)
		},
		{
			'V',
			(DateTime dateTime, CultureInfo cultureInfo) => cultureInfo.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("D2", cultureInfo)
		},
		{
			'W',
			(DateTime dateTime, CultureInfo cultureInfo) => cultureInfo.Calendar.GetWeekOfYear(dateTime, cultureInfo.DateTimeFormat.CalendarWeekRule, DayOfWeek.Monday).ToString("D2", cultureInfo)
		},
		{
			'w',
			(DateTime dateTime, CultureInfo cultureInfo) => ((int)dateTime.DayOfWeek).ToString(cultureInfo)
		},
		{
			'x',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("d", cultureInfo)
		},
		{
			'X',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("T", cultureInfo)
		},
		{
			'y',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("yy", cultureInfo)
		},
		{
			'Y',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("yyyy", cultureInfo)
		},
		{
			'Z',
			(DateTime dateTime, CultureInfo cultureInfo) => dateTime.ToString("zzz", cultureInfo)
		},
		{
			'%',
			(DateTime dateTime, CultureInfo cultureInfo) => "%"
		}
	};

	public string Format
	{
		get
		{
			return GetSafeValue<string>("format") ?? "%d %b %Y";
		}
		set
		{
			SetValue("format", value, readOnly: false);
		}
	}

	public DateTimeFunctions()
	{
		Format = "%d %b %Y";
		CreateImportFunctions();
	}

	public static DateTime Now()
	{
		return DateTime.Now;
	}

	public static DateTime AddDays(DateTime date, double days)
	{
		return date.AddDays(days);
	}

	public static DateTime AddMonths(DateTime date, int months)
	{
		return date.AddMonths(months);
	}

	public static DateTime AddYears(DateTime date, int years)
	{
		return date.AddYears(years);
	}

	public static DateTime AddHours(DateTime date, double hours)
	{
		return date.AddHours(hours);
	}

	public static DateTime AddMinutes(DateTime date, double minutes)
	{
		return date.AddMinutes(minutes);
	}

	public static DateTime AddSeconds(DateTime date, double seconds)
	{
		return date.AddSeconds(seconds);
	}

	public static DateTime AddMilliseconds(DateTime date, double millis)
	{
		return date.AddMilliseconds(millis);
	}

	public static DateTime? Parse(TemplateContext context, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (DateTime.TryParse(text, context.CurrentCulture, DateTimeStyles.None, out var result))
		{
			return result;
		}
		return default(DateTime);
	}

	public override IScriptObject Clone(bool deep)
	{
		DateTimeFunctions obj = (DateTimeFunctions)base.Clone(deep);
		obj.CreateImportFunctions();
		return obj;
	}

	public virtual string ToString(DateTime? datetime, string pattern, CultureInfo culture)
	{
		if (pattern == null)
		{
			throw new ArgumentNullException("pattern");
		}
		if (!datetime.HasValue)
		{
			return null;
		}
		if (pattern == "%g")
		{
			pattern = "%g " + Format;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < pattern.Length; i++)
		{
			char c = pattern[i];
			if (c == '%' && i + 1 < pattern.Length)
			{
				i++;
				char c2 = pattern[i];
				if (c2 == 'g')
				{
					culture = CultureInfo.InvariantCulture;
					continue;
				}
				if (Formats.TryGetValue(c2, out var value))
				{
					stringBuilder.Append(value(datetime.Value, culture));
					continue;
				}
				stringBuilder.Append('%');
				stringBuilder.Append(c2);
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
	{
		return arguments.Count switch
		{
			0 => this, 
			1 => Parse(context, context.ToString(callerContext.Span, arguments[0])), 
			_ => throw new ScriptRuntimeException(callerContext.Span, $"Invalid number of parameters `{arguments.Count}` for `date` object/function."), 
		};
	}

	private void CreateImportFunctions()
	{
		this.Import("to_string", (Func<TemplateContext, DateTime?, string, string>)((TemplateContext context, DateTime? date, string pattern) => ToString(date, pattern, context.CurrentCulture)));
	}
}
