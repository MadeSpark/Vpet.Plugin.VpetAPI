using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using VPet.Plugin.VpetAPI;

static int GetFreePort()
{
    var l = new TcpListener(IPAddress.Loopback, 0);
    l.Start();
    var port = ((IPEndPoint)l.LocalEndpoint).Port;
    l.Stop();
    return port;
}

var port = GetFreePort();
var server = new HttpControlServer((path, body, ct) =>
{
    if (path == "/ping")
        return Task.FromResult((200, (object)new { pong = true }));
    if (path == "/echo")
        return Task.FromResult((200, (object)new { body }));
    return Task.FromResult((404, (object)new { error = "not_found" }));
}, port);

server.Start();

try
{
    using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };

    var ping = await http.PostAsJsonAsync("ping", new { });
    if (ping.StatusCode != HttpStatusCode.OK)
        return 1;

    var pingJson = await ping.Content.ReadAsStringAsync();
    if (!pingJson.Contains("\"pong\":true", StringComparison.OrdinalIgnoreCase))
        return 2;

    var echo = await http.PostAsJsonAsync("echo", new { a = 1 });
    if (echo.StatusCode != HttpStatusCode.OK)
        return 3;

    var missing = await http.PostAsJsonAsync("missing", new { });
    if (missing.StatusCode != HttpStatusCode.NotFound)
        return 4;

    var get = await http.GetAsync("ping");
    if (get.StatusCode != HttpStatusCode.MethodNotAllowed)
        return 5;

    Console.WriteLine("OK");
    return 0;
}
finally
{
    server.Stop();
}
