using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBXplorer;
using NBXplorer.CoinSelection.SelectionStrategies;
using NBXplorer.Models;
using Xunit;

namespace NBXplorer.Tests.CoinSelection.SelectionStrategies;

public class UpToAmountTests
{
	public static List<IMoney> GetValues(List<UTXO> utxos)
	{
		return utxos.Select(u => u.Value).ToList();
	}

	/// <summary>
	/// General tests for SelectCoins
	/// </summary>
	[Fact]
	public void SelectCoins_ShouldReturnEmptyList_WhenUTXOsListIsEmpty()
	{
		// Arrange
		int limit = 3;
		long amount = 10;
		var coinSelector = new UpToAmount();

		// Act
		var result = coinSelector.SelectCoins(new List<UTXO>(), limit, amount);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_Changeless_InsideToleranceAbove_OneUtxoCoversAll()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(6) },
		};
		int limit = 3;
		long amount = 7;
		var coinSelector = new UpToAmount();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(7) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_Changeless_InsideToleranceAbove_OneUtxoCoversBelowTarget()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(6) },
		};
		int limit = 3;
		long amount = 8;
		var coinSelector = new UpToAmount();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(7) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_Changeless_NoUtxoCoversTheAmount()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(1) },
		};
		int limit = 3;
		long amount = 6;
		var coinSelector = new UpToAmount();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(1) }, GetValues(result));
	}
}