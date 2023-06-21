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
				return "value DESC";
			case CoinSelectionStrategy.ClosestToTargetFirst:
				return $"abs(value - {target})";
			case CoinSelectionStrategy.SmallestFirst:
			default:
				return "value ASC";
		}
	}

	public static List<UTXO> SelectCoins(List<UTXO> UTXOs, int limit, long amount)
	{
		if (limit == 0)
		{
			return UTXOs;
		}

		var utxosQueued = new Queue<UTXO>(UTXOs);
		var targetAmount = new Money(amount);
		var currentAmount = new Money(0);
		var count = 0;
		var retroCount = limit;

		var selectedCoins = new List<UTXO>();
		while (utxosQueued.Count > 0)
		{
			var utxo = utxosQueued.Dequeue();
			var utxoValue = (Money)utxo.Value;
			if (currentAmount < targetAmount)
			{
				if (count >= limit && currentAmount < targetAmount)
				{
					retroCount = retroCount <= 0 ? limit - 1 : retroCount - 1;
					var prevUtxo = selectedCoins[retroCount];
					currentAmount -= (Money)prevUtxo.Value;
					selectedCoins[retroCount] = utxo;
					currentAmount += utxoValue;
				}

				if (count < limit)
				{
					var newAmount = currentAmount + utxoValue;
					selectedCoins.Add(utxo);
					currentAmount = newAmount;
					count++;
				}
			}
		}

		if (currentAmount < targetAmount)
		{
			selectedCoins.Clear();
		}

		return selectedCoins;
	}
}