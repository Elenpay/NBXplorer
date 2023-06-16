using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBXplorer;
using NBXplorer.Models;
using Xunit;

public class CoinSelectionHelpersTests
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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(new List<UTXO>(), limit, amount);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(10) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_Changeless_InsideToleranceBelow_OneUtxoCoversAll()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(8) },
		};
		int limit = 3;
		long amount = 9;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(8) }, GetValues(result));
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
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

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
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5), new Money(10) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnFirstIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(5) }
		};
		int limit = 3;
		long amount = 7;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(1), new Money(2), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnSecondIteration()
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
		long amount = 10;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(1), new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnThirdIteration()
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
		long amount = 14;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnTheNIteration()
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
		long amount = 19;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5), new Money(10) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnFirstIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(5) }
		};
		int limit = 3;
		long amount = 9;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(1), new Money(2), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnSecondIteration()
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
		long amount = 12;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(1), new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnThirdIteration()
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
		long amount = 16;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(5), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_SmallestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnTheNIteration()
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
		long amount = 21;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(6), new Money(3) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_BiggestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValues()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(2) },
			new UTXO { Value = new Money(1) }
		};
		int limit = 3;
		long amount = 7;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(3) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_BiggestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValues()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(1) }
		};
		int limit = 3;
		long amount = 9;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(4), new Money(3), new Money(1) }, GetValues(result));
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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

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

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount);

		// Assert
		Assert.Equal(new[] { new Money(5), new Money(6), new Money(4) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnSecondIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(6) }
		};
		int limit = 3;
		long amount = 11;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(4), new Money(3), new Money(5) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnThirdIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(6) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(12) },
		};
		int limit = 3;
		long amount = 20;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(6), new Money(12), new Money(3) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_Changeless_InsideToleranceAbove_WhenUTXOsListHasSufficientValuesOnTheNIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(6) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(12) },
			new UTXO { Value = new Money(15) },
			new UTXO { Value = new Money(16) }
		};
		int limit = 3;
		long amount = 21;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(6), new Money(12), new Money(4) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnSecondIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(1) },
			new UTXO { Value = new Money(6) }
		};
		int limit = 3;
		long amount = 9;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(3), new Money(1), new Money(4) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnThirdIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(6) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(10) },
		};
		int limit = 3;
		long amount = 18;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(6), new Money(4), new Money(7) }, GetValues(result));
	}

	[Fact]
	public void SelectCoins_ShouldReturnSelectedCoins_ClosestFirst_Changeless_InsideToleranceBelow_WhenUTXOsListHasSufficientValuesOnTheNIteration()
	{
		// Arrange
		var utxos = new List<UTXO>()
		{
			new UTXO { Value = new Money(6) },
			new UTXO { Value = new Money(5) },
			new UTXO { Value = new Money(7) },
			new UTXO { Value = new Money(4) },
			new UTXO { Value = new Money(8) },
			new UTXO { Value = new Money(3) },
			new UTXO { Value = new Money(9) }
		};
		int limit = 3;
		long amount = 19;
		int tolerance = 10;

		// Act
		var result = CoinSelectionHelpers.SelectCoins(utxos, limit, amount, tolerance);

		// Assert
		Assert.Equal(new[] { new Money(6), new Money(5), new Money(7) }, GetValues(result));
	}
}