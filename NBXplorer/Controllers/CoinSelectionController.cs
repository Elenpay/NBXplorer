using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBXplorer.DerivationStrategy;
using NBXplorer.ModelBinders;
using NBXplorer.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NBXplorer.Backend;
using NBXplorer.CoinSelection.SelectionStrategies;

namespace NBXplorer.Controllers
{
	[Route("v1")]
	[Authorize]
	public class CoinSelectionController : Controller
	{
		public CoinSelectionController(DbConnectionFactory connectionFactory)
		{
			ConnectionFactory = connectionFactory;
		}

		private DbConnectionFactory ConnectionFactory { get; }

		/// <summary>
		/// Same as utxos endpoint but with a limit on the utxos
		/// </summary>
		/// <param name="trackedSourceContext"></param>
		/// <param name="cryptoCode"></param>
		/// <param name="derivationScheme"></param>
		/// <param name="address"></param>
		/// <param name="amount"></param>
		/// <param name="limit"></param>
		/// <param name="closestTo"></param>
		/// <param name="strategy"></param>
		/// <param name="minimumValue">UTXOs worth this many satoshis or less are never selected.</param>
		/// <param name="ignoreOutpoint">
		/// Outpoints to exclude, one repeated query parameter each. Capped by the request line size;
		/// POST them in <paramref name="body"/> instead when the list is long.
		/// </param>
		/// <param name="body">POST-only. Carries the ignored outpoints off the request line.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		[HttpGet]
		[HttpPost]
		[Route("cryptos/{cryptoCode}/derivations/{derivationScheme}/selectutxos")]
		[Route("cryptos/{cryptoCode}/addresses/{address}/selectutxos")]
		public async Task<IActionResult> GetUTXOsByLimit(
			TrackedSourceContext trackedSourceContext,
			string cryptoCode,
			[ModelBinder(BinderType = typeof(DerivationStrategyModelBinder))]
			DerivationStrategyBase derivationScheme,
			[ModelBinder(BinderType = typeof(BitcoinAddressModelBinder))]
			BitcoinAddress address,
			// Added selection parameters
			[FromQuery(Name = "amount")] long amount,
			[FromQuery(Name = "limit")] int limit = 0,
			[FromQuery(Name = "closestTo")] long? closestTo = null,
			[FromQuery(Name = "strategy")] CoinSelectionStrategy strategy = CoinSelectionStrategy.SmallestFirst,
			[FromQuery(Name = "minimumValue")] long minimumValue = 546,
			[FromQuery(Name = "ignoreOutpoint")] string[] ignoreOutpoint = null,
			[FromBody] SelectUTXOsRequest body = null)
		{
			// The query parameters cap out at the request line size, so a caller with a long list
			// sends it in the body instead. Accept both and merge, so either transport works.
			var ignoredOutpoints = (ignoreOutpoint ?? Array.Empty<string>())
				.Concat(body?.IgnoreOutpoints ?? Array.Empty<string>())
				.ToHashSet();

			var trackedSource = trackedSourceContext.TrackedSource;
			var repo = trackedSourceContext.Repository;
			var network = trackedSourceContext.Network;

			await using var conn = await ConnectionFactory.CreateConnection();
			var height = await conn.ExecuteScalarAsync<long>("SELECT height FROM get_tip(@code)", new { code = network.CryptoCode });


			// On elements, we can't get blinded address from the scriptPubKey, so we need to fetch it rather than compute it
			string addrColumns = "NULL as address";
			if (network.IsElement && !derivationScheme.Unblinded())
			{
				addrColumns = "ds.metadata->>'blindedAddress' as address";
			}

			string descriptorJoin = string.Empty;
			string descriptorColumns = "NULL as redeem, NULL as keypath, NULL as feature";
			if (derivationScheme is not null)
			{
				descriptorJoin = " JOIN descriptors_scripts ds USING (code, script) JOIN descriptors d USING (code, descriptor)";
				descriptorColumns = "ds.metadata->>'redeem' redeem, nbxv1_get_keypath(d.metadata, ds.idx) AS keypath, d.metadata->>'feature' feature";
			}

			var belowAmount = strategy == CoinSelectionStrategy.UpToAmount ? $"AND value <= {amount} " : "";
			// Added OrderBy to the query
			var utxos = await conn.QueryAsync<(
				long? blk_height,
				string tx_id,
				int idx,
				long value,
				string script,
				string address,
				string redeem,
				string keypath,
				string feature,
				bool mempool,
				bool input_mempool,
				DateTime tx_seen_at)>(
				$"SELECT blk_height, tx_id, wu.idx, value, script, {addrColumns}, {descriptorColumns}, mempool, input_mempool, seen_at " +
				$"FROM wallets_utxos wu{descriptorJoin} WHERE code='{network.CryptoCode}' AND wallet_id='{repo.GetWalletKey(trackedSource).wid}' AND immature IS FALSE AND value > @minimumValue {belowAmount}" +
				$"ORDER BY {CoinSelectionHelpers.OrderBy(strategy, closestTo ?? 0)}",
				new { minimumValue });
			UTXOChanges changes = new UTXOChanges()
			{
				CurrentHeight = (int)height,
				TrackedSource = trackedSource,
				DerivationStrategy = derivationScheme
			};
			// Removed ordering from this line with respect to the original endpoint
			foreach (var utxo in utxos)
			{
				var u = new UTXO()
				{
					Index = utxo.idx,
					Timestamp = new DateTimeOffset(utxo.tx_seen_at),
					Value = Money.Satoshis(utxo.value),
					ScriptPubKey = Script.FromHex(utxo.script),
					Redeem = utxo.redeem is null ? null : Script.FromHex(utxo.redeem),
					TransactionHash = uint256.Parse(utxo.tx_id)
				};
				u.Outpoint = new OutPoint(u.TransactionHash, u.Index);
				if (utxo.blk_height is long)
				{
					u.Confirmations = (int)(height - utxo.blk_height + 1);
				}

				if (utxo.keypath is not null)
				{
					u.KeyPath = KeyPath.Parse(utxo.keypath);
					u.Feature = Enum.Parse<DerivationFeature>(utxo.feature);
				}
				u.Address = utxo.address is null ? u.ScriptPubKey.GetDestinationAddress(network.NBitcoinNetwork) : BitcoinAddress.Create(utxo.address, network.NBitcoinNetwork);

				if (ignoredOutpoints.Contains(u.Outpoint.ToString())) continue;

				// Inverted clauses for clarity
				if (utxo.mempool)
					changes.Unconfirmed.UTXOs.Add(u);
				else if (utxo.input_mempool)
					changes.Unconfirmed.SpentOutpoints.Add(u.Outpoint);
				else
					changes.Confirmed.UTXOs.Add(u);
			}

			changes.Confirmed.UTXOs = changes.Confirmed.UTXOs.DistinctBy(x => x.Outpoint).ToList();
			changes.Unconfirmed.UTXOs = changes.Unconfirmed.UTXOs.DistinctBy(x => x.Outpoint).ToList();

			ISelectionStrategies selectionStrategy;
			switch (strategy)
			{
				case CoinSelectionStrategy.SmallestFirst:
					selectionStrategy = new SmallestFirst();
					break;
				case CoinSelectionStrategy.UpToAmount:
					selectionStrategy = new UpToAmount();
					break;
				default:
					selectionStrategy = new SmallestFirst();
					break;
			}

			changes.Confirmed.UTXOs = selectionStrategy.SelectCoins(changes.Confirmed.UTXOs, limit, amount);
			changes.Unconfirmed.UTXOs = selectionStrategy.SelectCoins(changes.Unconfirmed.UTXOs, limit, amount);
			// Added the coin selection
			return Json(changes, network.JsonSerializerSettings);
		}
	}
}
