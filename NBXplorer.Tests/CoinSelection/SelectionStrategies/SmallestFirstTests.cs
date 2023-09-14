using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBXplorer;
using NBXplorer.CoinSelection.SelectionStrategies;
using NBXplorer.Models;
using Xunit;

namespace NBXplorer.Tests.CoinSelection.SelectionStrategies;

public class SmallestFirstTests
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
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(new List<UTXO>(), limit, amount);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_NoChangeless_OneUtxoCoversAll()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(10) },
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(10) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_Changeless_InsideToleranceAbove_OneUtxoCoversAll()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(8) },
		};
		int limit = 3;
		long amount = 7;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(8) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_NoChangeless_NoUtxoCoversTheAmount()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(5) },
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Empty(GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_Changeless_NoUtxoCoversTheAmount()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(5) },
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Empty(GetValues(result));
	}

	/// <summary>
	/// Tests for SelectCoins when UTXOs are provided in ascending order
	/// </summary>
	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_NoChangeless_WhenUTXOsListHasSufficientValuesOnFirstIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(6) }
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(1), new Money(2), new Money(6) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_NoChangeless_WhenUTXOsListHasSufficientValuesOnSecondIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(5) }
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(1), new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_NoChangeless_WhenUTXOsListHasSufficientValuesOnThirdIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(5) }
		};
		int limit = 3;
		long amount = 12;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_NoChangeless_WhenUTXOsListHasSufficientValuesOnTheNIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(10) }
		};
		int limit = 3;
		long amount = 20;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5), new Money(10) }, GetValues(result));
	}

	/// <summary>
	/// Tests for SelectCoins when UTXOs are provided in descending order
	/// </summary>
	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_BiggestFirst_NoChangeless_WhenUTXOsListHasSufficientValues()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(6) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(1) }
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(6), new Money(3) }, GetValues(result));
	}

	/// <summary>
	/// Tests for SelectCoins when UTXOs are provided in closest to amount order
	/// </summary>
	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_TwoUtxosCoversAll()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(5) },
		};
		int limit = 3;
		long amount = 9;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_NoChangeless_WhenUTXOsListHasSufficientValues()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(6) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(3) }
		};
		int limit = 3;
		long amount = 14;
		var coinSelector = new SmallestFirst();

		// Act
		var result = coinSelector.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(6), new Money(4) }, GetValues(result));
	}
}