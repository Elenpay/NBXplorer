using System.Collections.Generic;
using NBitcoin;
using NBXplorer.Models;

namespace NBXplorer.CoinSelection.SelectionStrategies;

public class UpToAmount: ISelectionStrategies
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

		var selectedCoins = new List<UTXO>();
		while (utxosQueued.Count > 0)
		{
			if (count > limit)
			{
				break;
			}

			var utxo = utxosQueued.Dequeue();
			var utxoValue = (Money)utxo.Value;
			var newAmount = currentAmount + utxoValue;
			if (newAmount > targetAmount)
			{
				continue;
			}
			selectedCoins.Add(utxo);
			currentAmount = newAmount;
			count++;

		}

		return selectedCoins;
	}
}