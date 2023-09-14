using System.Collections.Generic;
using NBitcoin;
using NBXplorer.Models;

namespace NBXplorer.CoinSelection.SelectionStrategies;

public class SmallestFirst: ISelectionStrategies
{
	public List<UTXO> SelectCoins(List<UTXO> UTXOs, int limit, long amount)
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