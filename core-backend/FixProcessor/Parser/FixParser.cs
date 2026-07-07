using System.Globalization;
using FixBackendShared.Grpc;

namespace FixProcessor.Parser;

public static class FixParser
{
	public static PersistOrderRequest ToPersistRequest(FixProcessRequest fixProcessRequest)
	{
		var f = ParseFields(fixProcessRequest.Message);

		return new PersistOrderRequest() {
			OrderId = f.GetValueOrDefault("11") ?? fixProcessRequest.Id,
			Symbol = Require(f, "55"),
			Side = ParseSide(Require(f, "54")),
			Quantity = NormalizeDecimal(Require(f, "38"), "38"),
			Price = NormalizeDecimal(Require(f, "44"), "44")
		};
	}

	private static Dictionary<string, string> ParseFields(string fix)
	{
		var map = new Dictionary<string, string>();
		foreach (var token in fix.Split('|', StringSplitOptions.RemoveEmptyEntries))
		{
			var i = token.IndexOf('=');
			if (i > 0)
				map[token[..i]] = token[(i + 1)..];
		}

		return map;
	}

	private static string Require(Dictionary<string, string> dict, string tag)
		=> dict.TryGetValue(tag, out var v) && v.Length > 0
			? v
			: throw new FormatException($"FIX missing required tag {tag}");

	private static Side ParseSide(string v) => v switch
	{
		"1" => Side.Buy,
		"2" => Side.Sell,
		_ => throw new FormatException($"Unsupported FIX side '{v}'")
	};

	private static string NormalizeDecimal(string v, string tag)
		=> decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
			? d.ToString(CultureInfo.InvariantCulture)
			: throw new FormatException($"FIX tag {tag}, not a number: '{v}'");
}