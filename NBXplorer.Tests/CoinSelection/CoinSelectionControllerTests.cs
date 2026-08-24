using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBXplorer.DerivationStrategy;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace NBXplorer.Tests.CoinSelection
{
	public class CoinSelectionControllerTests(ITestOutputHelper helper) : UnitTestBase(helper)
	{
		/// <summary>
		/// Enough outpoints that the query-string form of the request exceeds Kestrel's default
		/// 8KB MaxRequestLineSize. Each one costs roughly 83 bytes once "&amp;ignoreOutpoint=" and the
		/// txid-n text are counted.
		/// </summary>
		private const int OutpointsBeyondRequestLine = 120;

		private static string[] CreateOutpoints(int count)
		{
			return Enumerable.Range(1, count)
				.Select(i => new OutPoint(new uint256((uint)i), 0).ToString())
				.ToArray();
		}

		/// <summary>
		/// The raw HttpClient starts unauthenticated, unlike tester.Client. Same cookie, same Basic
		/// header the typed client builds in ExplorerClient.CookieAuthentication.
		/// </summary>
		private static HttpClient Authenticated(ServerTester tester)
		{
			var cookie = File.ReadAllText(Path.Combine(tester.Configuration.DataDir, ".cookie"));
			tester.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
				Encoders.Base64.EncodeData(Encoders.ASCII.DecodeData(cookie)));
			return tester.HttpClient;
		}

		private static string SelectUtxosPath(string derivationScheme, string[] ignoreOutpoints)
		{
			var path = $"v1/cryptos/BTC/derivations/{Uri.EscapeDataString(derivationScheme)}/selectutxos" +
				"?strategy=SmallestFirst&limit=0&amount=0";
			return ignoreOutpoints.Aggregate(path,
				(current, outpoint) => current + $"&ignoreOutpoint={Uri.EscapeDataString(outpoint)}");
		}

		[Fact]
		public async Task CanSelectUTXOsIgnoringOutpointsOnTheQueryString()
		{
			using var tester = CreateTester();
			var bob = tester.CreateDerivationStrategy();
			await tester.Client.TrackAsync(bob);

			var response = await Authenticated(tester).GetAsync(SelectUtxosPath(bob.ToString(), CreateOutpoints(5)));

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		/// <summary>
		/// Documents the ceiling the POST form exists to get around. If this ever stops returning
		/// 414, the limit moved and the threshold in NodeGuard's NBXplorerService can move with it.
		/// </summary>
		[Fact]
		public async Task SelectUTXOsRejectsAnOverlongQueryString()
		{
			using var tester = CreateTester();
			var bob = tester.CreateDerivationStrategy();
			await tester.Client.TrackAsync(bob);

			var response = await Authenticated(tester).GetAsync(
				SelectUtxosPath(bob.ToString(), CreateOutpoints(OutpointsBeyondRequestLine)));

			Assert.Equal(HttpStatusCode.RequestUriTooLong, response.StatusCode);
		}

		[Fact]
		public async Task CanSelectUTXOsIgnoringOutpointsPostedInTheBody()
		{
			using var tester = CreateTester();
			var bob = tester.CreateDerivationStrategy();
			await tester.Client.TrackAsync(bob);

			// The same list that 414s on the query string goes through fine in the body
			var ignoreOutpoints = CreateOutpoints(OutpointsBeyondRequestLine);
			var body = new JObject
			{
				["ignoreOutpoints"] = new JArray(ignoreOutpoints)
			};

			var response = await Authenticated(tester).PostAsync(
				SelectUtxosPath(bob.ToString(), Array.Empty<string>()),
				new StringContent(body.ToString(), Encoding.UTF8, "application/json"));

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task SelectUTXOsExcludesTheOutpointsPostedInTheBody()
		{
			using var tester = CreateTester();
			var bob = tester.CreateDerivationStrategy();
			await tester.Client.TrackAsync(bob);

			// Fund the wallet, then ask for a selection that ignores the funded outpoint
			var address = await tester.Client.GetUnusedAsync(bob, DerivationFeature.Deposit, reserve: true);
			var txId = await tester.RPC.SendToAddressAsync(address.Address, Money.Coins(1.0m));
			tester.Notifications.WaitForTransaction(bob, txId);
			tester.Explorer.Generate(1);
			tester.WaitSynchronized();

			var utxos = await tester.Client.GetUTXOsAsync(bob);
			var funded = Assert.Single(utxos.Confirmed.UTXOs);

			var body = new JObject
			{
				["ignoreOutpoints"] = new JArray(
					CreateOutpoints(OutpointsBeyondRequestLine).Append(funded.Outpoint.ToString()))
			};

			var response = await Authenticated(tester).PostAsync(
				SelectUtxosPath(bob.ToString(), Array.Empty<string>()),
				new StringContent(body.ToString(), Encoding.UTF8, "application/json"));

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var selected = JObject.Parse(await response.Content.ReadAsStringAsync());
			Assert.Empty(selected["confirmed"]["utxOs"]);

			// Same request without the funded outpoint in the list, so the empty result above is
			// the ignore list doing its job rather than the selection coming back empty anyway
			var withoutIgnore = new JObject
			{
				["ignoreOutpoints"] = new JArray(CreateOutpoints(OutpointsBeyondRequestLine))
			};
			var control = await Authenticated(tester).PostAsync(
				SelectUtxosPath(bob.ToString(), Array.Empty<string>()),
				new StringContent(withoutIgnore.ToString(), Encoding.UTF8, "application/json"));

			Assert.Equal(HttpStatusCode.OK, control.StatusCode);
			var controlSelected = JObject.Parse(await control.Content.ReadAsStringAsync());
			Assert.Single(controlSelected["confirmed"]["utxOs"]);
		}
	}
}
