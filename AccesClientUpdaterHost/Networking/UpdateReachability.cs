using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace AccesClientUpdaterHost.Networking
{
    internal static class UpdateReachability
    {
        // Teste "réseau OK + endpoint update joignable" avec timeout court.
        // - HEAD d'abord (rapide)
        // - si HEAD interdit (405) ou pas supporté => GET Range 0-0
        public static async Task<bool> CanReachUpdateEndpointAsync(
            Uri endpoint,
            TimeSpan timeout,
            Action<string>? log = null,
            CancellationToken ct = default)
        {
            if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));

            // 1) Check basique: carte réseau up
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                log?.Invoke("[Reachability] No network interface available => OFFLINE");
                return false;
            }

            // 2) Optionnel: évite de bloquer sur DNS foireux trop longtemps
            // (On laisse HttpClient gérer, mais au moins on log mieux)
            try
            {
                // Si endpoint.Host est une IP, DNS ne fera rien
                // On ne force pas de DNS resolve long; on garde un timeout global.
                _ = endpoint.Host;
            }
            catch { /* ignore */ }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            try
            {
                using var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    // Proxy système par défaut => OK (ne pas toucher)
                };

                using var http = new HttpClient(handler)
                {
                    Timeout = timeout
                };

                // 3) HEAD
                using (var headReq = new HttpRequestMessage(HttpMethod.Head, endpoint))
                {
                    var headResp = await http.SendAsync(headReq, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                                             .ConfigureAwait(false);

                    if (headResp.IsSuccessStatusCode)
                    {
                        log?.Invoke($"[Reachability] HEAD OK {((int)headResp.StatusCode)} => REACHABLE");
                        return true;
                    }

                    // HEAD interdit (souvent 405) => fallback GET Range
                    if ((int)headResp.StatusCode != 405)
                    {
                        log?.Invoke($"[Reachability] HEAD KO {((int)headResp.StatusCode)} => NOT REACHABLE");
                        return false;
                    }

                    log?.Invoke("[Reachability] HEAD not allowed (405) => fallback GET Range");
                }

                // 4) GET minimal (Range 0-0) pour éviter download
                using var getReq = new HttpRequestMessage(HttpMethod.Get, endpoint);
                getReq.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

                var getResp = await http.SendAsync(getReq, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                                        .ConfigureAwait(false);

                if (getResp.IsSuccessStatusCode || getResp.StatusCode == HttpStatusCode.PartialContent)
                {
                    log?.Invoke($"[Reachability] GET Range OK {((int)getResp.StatusCode)} => REACHABLE");
                    return true;
                }

                log?.Invoke($"[Reachability] GET Range KO {((int)getResp.StatusCode)} => NOT REACHABLE");
                return false;
            }
            catch (OperationCanceledException)
            {
                log?.Invoke($"[Reachability] TIMEOUT after {timeout.TotalMilliseconds:0}ms => NOT REACHABLE");
                return false;
            }
            catch (HttpRequestException ex)
            {
                log?.Invoke($"[Reachability] HttpRequestException => NOT REACHABLE: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Reachability] Unexpected => NOT REACHABLE: {ex.GetType().Name} {ex.Message}");
                return false;
            }
        }
    }
}