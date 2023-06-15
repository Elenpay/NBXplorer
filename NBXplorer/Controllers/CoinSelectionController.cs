using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBXplorer.Backends;
using NBXplorer.Backends.Postgres;
using NBXplorer.DerivationStrategy;
using NBXplorer.ModelBinders;
using NBXplorer.Models;
using System;
using System.Threading.Tasks;

namespace NBXplorer.Controllers
{
	[Route("v1")]
	[Authorize]
	public class CoinSelectionController : ControllerBase
	{
		public CoinSelectionController(
			DbConnectionFactory connectionFactory,
			NBXplorerNetworkProvider networkProvider,
			IRPCClients rpcClients,
			IIndexers indexers,
			IRepositoryProvider repositoryProvider) : base(networkProvider, rpcClients, repositoryProvider, indexers)
		{
			ConnectionFactory = connectionFactory;
		}

		private DbConnectionFactory ConnectionFactory { get; }

		[HttpGet]
		[Route("cryptos/{cryptoCode}/derivations/{derivationScheme}/selectutxos")]
		[Route("cryptos/{cryptoCode}/addresses/{address}/selectutxos")]
		[PostgresImplementationActionConstraint(true)]
		public async Task<IActionResult> GetUTXOsByLimit(
			string cryptoCode,
			[ModelBinder(BinderType = typeof(DerivationStrategyModelBinder))]
			DerivationStrategyBase derivationScheme,
			[ModelBinder(BinderType = typeof(BitcoinAddressModelBinder))]
			BitcoinAddress address,
			[FromQuery(Name = "amount")] long amount,
			[FromQuery(Name = "limit")] int limit = 0,
			[FromQuery(Name = "tolerance")] int tolerance = 0,
			[FromQuery(Name = "closestTo")] long? closestTo = null,
			[FromQuery(Name = "strategy")] CoinSelectionStrategy strategy = CoinSelectionStrategy.SmallestFirst)
		{
			var trackedSource = GetTrackedSource(derivationScheme, address);
			if (trackedSource == null)
				throw new ArgumentNullException(nameof(trackedSource));
			var network = GetNetwork(cryptoCode, false);
			var repo = (PostgresRepository)RepositoryProvider.GetRepository(cryptoCode);

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

			var utxos = (await conn.QueryAsync<(
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
				$"FROM wallets_utxos wu{descriptorJoin} WHERE code=@code AND wallet_id=@walletId AND immature IS FALSE AND value > 546" +
				$"ORDER BY {CoinSelectionHelpers.OrderBy(strategy, closestTo ?? 0)}", new { code = network.CryptoCode, walletId = repo.GetWalletKey(trackedSource).wid }));
			UTXOChanges changes = new UTXOChanges()
			{
				CurrentHeight = (int)height,
				TrackedSource = trackedSource,
				DerivationStrategy = derivationScheme
			};
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
				if (!utxo.mempool)
					changes.Confirmed.UTXOs.Add(u);
				else if (!utxo.input_mempool)
					changes.Unconfirmed.UTXOs.Add(u);
				if (utxo.input_mempool && !utxo.mempool)
					changes.Unconfirmed.SpentOutpoints.Add(u.Outpoint);
			}

			changes.Confirmed.UTXOs = CoinSelectionHelpers.SelectCoins(changes.Confirmed.UTXOs, limit, amount, tolerance);
			changes.Unconfirmed.UTXOs = CoinSelectionHelpers.SelectCoins(changes.Unconfirmed.UTXOs, limit, amount, tolerance);
			return Json(changes, network.JsonSerializerSettings);
		}
	}
}
