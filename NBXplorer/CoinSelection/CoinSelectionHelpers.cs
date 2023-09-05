using System;
using System.Collections.Generic;
using NBitcoin;
using NBXplorer.Models;

namespace NBXplorer;

public static class CoinSelectionHelpers
{
	public static string OrderBy(CoinSelectionStrategy strategy, long target = 0)
	{
		switch (strategy)
		{
			case CoinSelectionStrategy.BiggestFirst:
			case CoinSelectionStrategy.UpToAmount:
				return "value DESC";
			case CoinSelectionStrategy.ClosestToTargetFirst:
				return $"abs(value - {target})";
			case CoinSelectionStrategy.SmallestFirst:
			default:
				return "value ASC";
		}
	}
}

public interface ISelectionStrategies
{
	public List<UTXO> SelectCoins(List<UTXO> UTXOs, int limit, long amount);
}