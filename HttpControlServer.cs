using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VPet.Plugin.VpetAPI
{
    public sealed class HttpControlServer
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly Func<string, string, CancellationToken, Task<(int statusCode, object payload)>> handler;
        private readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(60);
        private CancellationTokenSource? cts;
        private Task? loopTask;

        public int Port { get; }

        public HttpControlServer(VpetStateController controller) : this(controller.HandleAsync, 52814)
        {
        }

        public HttpControlServer(Func<string, string, CancellationToken, Task<(int statusCode, object payload)>> handler, int port)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Port = port;
        }

        public void Start()
        {
            if (cts != null)
                return;

            cts = new CancellationTokenSource();

            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();

            loopTask = Task.Run(() => AcceptLoopAsync(cts.Token));
        }

        public void Stop()
        {
            var current = cts;
            if (current == null)
                return;

            try { current.Cancel(); } catch { }
            cts = null;

            try { listener.Stop(); } catch { }
            try { listener.Close(); } catch { }
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch when (token.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    await Task.Delay(100, token).ConfigureAwait(false);
                    continue;
                }

                _ = Task.Run(() => HandleContextAsync(ctx, token), token);
            }
        }

        private async Task HandleContextAsync(HttpListenerContext ctx, CancellationToken token)
        {
            var req = ctx.Request;
            var res = ctx.Response;
            res.Headers["Server"] = "VpetAPI";
            res.ContentType = "application/json; charset=utf-8";
            ApplyCors(res);

            if (string.Equals(req.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await WriteEmptyAsync(res, 204, token).ConfigureAwait(false);
                return;
            }

            if (!string.Equals(req.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(res, 405, new { error = "仅支持 POST" }, token).ConfigureAwait(false);
                return;
            }

            var path = req.Url?.AbsolutePath?.TrimEnd('/') ?? string.Empty;
            if (path.Length == 0)
                path = "/";

            string bodyText = string.Empty;
            try
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
                bodyText = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
                bodyText = string.Empty;
            }

            try
            {
                using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                requestCts.CancelAfter(requestTimeout);

                var (status, payload) = await handler(path, bodyText, requestCts.Token).ConfigureAwait(false);
                await WriteJsonAsync(res, status, payload, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                await WriteJsonAsync(res, 504, new { error = "timeout", timeoutSeconds = (int)requestTimeout.TotalSeconds }, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await WriteJsonAsync(res, 500, new { error = ex.Message }, token).ConfigureAwait(false);
            }
        }

        private static async Task WriteJsonAsync(HttpListenerResponse response, int statusCode, object payload, CancellationToken token)
        {
            response.StatusCode = statusCode;
            var json = JsonSerializer.Serialize(payload ?? new { });
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.LongLength;

            try
            {
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
            }
        }

        private static void ApplyCors(HttpListenerResponse response)
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            response.Headers["Access-Control-Max-Age"] = "86400";
        }

        private static async Task WriteEmptyAsync(HttpListenerResponse response, int statusCode, CancellationToken token)
        {
            response.StatusCode = statusCode;
            response.ContentLength64 = 0;
            try
            {
                await response.OutputStream.FlushAsync(token).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
            }
        }
    }
}
