using System;
using System.Collections.Generic;
using NBitcoin;
using NBXplorer.Models;

namespace NBXplorer;

public class Tolerance
{
	public bool enabled = false;
	private Money minTolerance = new Money(0);
	private Money maxTolerance = new Money(0);

	public Tolerance()
	{
	}

	public Tolerance(long amount, int tolerance)
	{
		enabled = true;
		minTolerance = new Money((long)Math.Floor(amount * (1 - (decimal)tolerance / 100)));
		maxTolerance = new Money((long)Math.Ceiling(amount * (1 + (decimal)tolerance / 100)));
	}

	public bool Equals(Money amount)
	{
		if (enabled)
		{
			return minTolerance <= amount && maxTolerance >= amount;
		}

		return false;
	}
}

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

	public static List<UTXO> SelectCoins(List<UTXO> UTXOs, int limit, long amount, int tol = 0)
	{
		if (limit == 0)
		{
			return UTXOs;
		}

		var utxosQueued = new Queue<UTXO>(UTXOs);
		var targetAmount = new Money(amount);
		var tolerance = new Tolerance();
		if (tol > 0)
		{
			tolerance = new Tolerance(amount, tol);
		}

		var currentAmount = new Money(0);
		var count = 0;
		var retroCount = limit;

		var selectedCoins = new List<UTXO>();
		while (utxosQueued.Count > 0)
		{
			var utxo = utxosQueued.Dequeue();
			var utxoValue = (Money)utxo.Value;
			if (!tolerance.enabled && currentAmount < targetAmount || tolerance.enabled && !tolerance.Equals(currentAmount))
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

		if (!tolerance.enabled && currentAmount < targetAmount || tolerance.enabled && !tolerance.Equals(currentAmount))
		{
			selectedCoins.Clear();
		}

		return selectedCoins;
	}
}