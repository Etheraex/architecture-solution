using System.Globalization;
using FixBackendShared.Models;
using TradeData.Entities;

namespace FixProcessor.Parser;

public static class FixParser
{
	public static Order ToOrder(FixProcessRequest fixProcessRequest)
	{
		var f = ParseFields(fixProcessRequest.Message);

		return new Order() {
			OrderId = f.GetValueOrDefault("11") ?? fixProcessRequest.Id,
			SecurityId = Require(f, "55"),
			Side = ParseSide(Require(f, "54")),
			Quantity = ParseDecimal(Require(f, "38"), "38"),
			Price = ParseDecimal(Require(f, "44"), "44")
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

	private static OrderSide ParseSide(string v) => v switch
	{
		"1" => OrderSide.Buy,
		"2" => OrderSide.Sell,
		_ => throw new FormatException($"Unsupported FIX side '{v}'")
	};

	private static decimal ParseDecimal(string v, string tag)
		=> decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
			? d
			: throw new FormatException($"FIX tag {tag}, not a number: '{v}'");
}