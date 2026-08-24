namespace NBXplorer;

/// <summary>
/// Body of a POST selectutxos request.
/// </summary>
/// <remarks>
/// The GET form of the endpoint takes the ignored outpoints as one repeated query parameter each,
/// which puts them on the request line and caps the list at Kestrel's MaxRequestLineSize (8KB by
/// default, so roughly ninety outpoints once the derivation scheme is accounted for). Callers with
/// a longer list send it here instead; everything else stays in the query string.
/// </remarks>
public class SelectUTXOsRequest
{
	/// <summary>
	/// Outpoints to exclude from the selection, formatted as OutPoint.ToString() (txid-n).
	/// Merged with any ignoreOutpoint query parameters.
	/// </summary>
	public string[] IgnoreOutpoints { get; set; }
}
