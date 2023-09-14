using System;
using System.Collections.Generic;
using NBitcoin;
using NBXplorer.Models;

namespace NBXplorer;

public static class CoinSelectionHelpers
{
	public static string OrderBy(CoinSelectionStrategy strategy, long target = 0)
	{
		return strategy switch
		{
			CoinSelectionStrategy.BiggestFirst => "value DESC",
			CoinSelectionStrategy.UpToAmount => "value DESC",
			CoinSelectionStrategy.ClosestToTargetFirst => $"abs(value - {target})",
			CoinSelectionStrategy.SmallestFirst => "value ASC",
			_ => throw new ArgumentOutOfRangeException(nameof(strategy), $@"Not expected strategy value: {strategy}"),
		};
	}
}

public interface ISelectionStrategies
{
	public List<UTXO> SelectCoins(List<UTXO> UTXOs, int limit, long amount);
}