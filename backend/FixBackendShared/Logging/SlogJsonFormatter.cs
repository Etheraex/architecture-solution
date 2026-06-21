using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace FixBackendShared.Logging;

public class SlogJsonFormatter : ITextFormatter
{
	public static readonly JsonValueFormatter ValueFormatter = new("$type");

	public void Format(LogEvent logEvent, TextWriter output)
	{
		output.Write("{\"time\":\"");
		output.Write(logEvent.Timestamp.UtcDateTime.ToString("o"));
		output.Write("\",\"level\":\"");
		output.Write(LevelName(logEvent.Level));
		output.Write("\",\"msg\":");
		JsonValueFormatter.WriteQuotedJsonString(logEvent.RenderMessage(), output);
		
		foreach (var prop in logEvent.Properties)
		{
			output.Write(',');
			JsonValueFormatter.WriteQuotedJsonString(prop.Key, output);
			output.Write(':');
			ValueFormatter.Format(prop.Value, output);
		}

		if (logEvent.Exception is not null)
		{
			output.Write(",\"err\":");
			JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
		}

		output.Write("}");
		output.Write(Environment.NewLine);
	}

	private static string LevelName(LogEventLevel level) => level switch
	{
		LogEventLevel.Verbose or LogEventLevel.Debug => "DEBUG",
		LogEventLevel.Information => "INFO",
		LogEventLevel.Warning => "WARN",
		LogEventLevel.Error or LogEventLevel.Fatal => "ERROR",
		_ => "INFO"
	};
}