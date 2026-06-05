using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

/*
 * TCP SERVER — WCS Data Bridge
 * ─────────────────────────────────────────────────────────────
 * Lắng nghe client kết nối vào port 9000.
 * Client gửi lệnh (string):
 *   "GET_STATUS"  → Server lấy dữ liệu từ WcsSystem (HTTP) rồi trả về JSON.
 *   "PING"        → Server trả về "PONG"
 *
 * WcsSystem phải đang chạy tại http://127.0.0.1:5050
 * ─────────────────────────────────────────────────────────────
 */

const string WCS_STATUS_URL = "http://127.0.0.1:5050/status";
const int    TCP_PORT       = 9000;

// HttpClient dùng chung cho toàn bộ vòng lặp
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(3)
};

TcpListener server = new TcpListener(IPAddress.Any, TCP_PORT);
server.Start();
Console.WriteLine($"[Server] TCP Server started — listening on port {TCP_PORT}");
Console.WriteLine($"[Server] Fetching WCS data from: {WCS_STATUS_URL}");
Console.WriteLine("─────────────────────────────────────────────────────");

while (true)
{
    // Chờ client kết nối
    TcpClient client = await server.AcceptTcpClientAsync();
    string remoteEp  = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    Console.WriteLine($"\n[Server] Client connected: {remoteEp}");

    // Mỗi client xử lý trên một Task riêng (non-blocking)
    _ = Task.Run(() => HandleClientAsync(client, remoteEp, httpClient));
}

// ─────────────────────────────────────────────────────────────
static async Task HandleClientAsync(TcpClient client, string remoteEp, HttpClient httpClient)
{
    try
    {
        using var stream = client.GetStream();
        stream.ReadTimeout  = 5000;
        stream.WriteTimeout = 5000;

        // 1. Đọc lệnh từ client
        byte[] buffer = new byte[256];
        int    length = await stream.ReadAsync(buffer);
        string command = Encoding.UTF8.GetString(buffer, 0, length).Trim();
        Console.WriteLine($"[Server] [{remoteEp}] Received command: \"{command}\"");

        string responseText;

        switch (command.ToUpperInvariant())
        {
            // ── PING ────────────────────────────────────────
            case "PING":
                responseText = "PONG";
                break;

            // ── GET_STATUS: lấy dữ liệu từ WcsSystem ────────
            case "GET_STATUS":
                responseText = await FetchWcsStatusAsync(httpClient);
                break;

            default:
                responseText = JsonSerializer.Serialize(new
                {
                    error   = "Unknown command",
                    command = command,
                    hint    = "Available commands: GET_STATUS, PING"
                });
                break;
        }

        // 2. Gửi phản hồi về client
        byte[] data = Encoding.UTF8.GetBytes(responseText);
        await stream.WriteAsync(data);
        Console.WriteLine($"[Server] [{remoteEp}] Response sent ({data.Length} bytes)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Server] [{remoteEp}] Error: {ex.Message}");
    }
    finally
    {
        client.Close();
        Console.WriteLine($"[Server] [{remoteEp}] Connection closed");
    }
}

// ─────────────────────────────────────────────────────────────
static async Task<string> FetchWcsStatusAsync(HttpClient httpClient)
{
    try
    {
        // Gọi HTTP GET đến WcsSystem
        string json = await httpClient.GetStringAsync("http://127.0.0.1:5050/status");

        // Parse và format lại để in log đẹp
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;

        Console.WriteLine("[Server] ── WCS Status ─────────────────────────");
        Console.WriteLine($"         PLC1 Connected : {GetBool(root, "plc1")}");
        Console.WriteLine($"         PLC2 Connected : {GetBool(root, "plc2")}");
        Console.WriteLine($"         Barcode1       : {GetStr(root, "barcode1")}");
        Console.WriteLine($"         Barcode2       : {GetStr(root, "barcode2")}");
        Console.WriteLine($"         Crane1 X={GetInt(root, "crane1", "x")}  Z={GetInt(root, "crane1", "z")}  Busy={GetBool2(root, "crane1", "busy")}");
        Console.WriteLine($"         Crane2 X={GetInt(root, "crane2", "x")}  Z={GetInt(root, "crane2", "z")}  Busy={GetBool2(root, "crane2", "busy")}");
        Console.WriteLine($"         Shuttle1 X={GetInt(root, "shuttle1", "x")}  Z={GetInt(root, "shuttle1", "z")}  B={GetInt(root, "shuttle1", "b")}  Busy={GetBool2(root, "shuttle1", "busy")}");
        Console.WriteLine($"         Shuttle2 X={GetInt(root, "shuttle2", "x")}  Z={GetInt(root, "shuttle2", "z")}  B={GetInt(root, "shuttle2", "b")}  Busy={GetBool2(root, "shuttle2", "busy")}");
        Console.WriteLine("[Server] ──────────────────────────────────────");

        return json;
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"[Server] ERROR: Cannot reach WcsSystem — {ex.Message}");
        return JsonSerializer.Serialize(new
        {
            error   = "WcsSystem unreachable",
            detail  = ex.Message,
            hint    = "Make sure WcsSystem is running on http://127.0.0.1:5050"
        });
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("[Server] ERROR: WcsSystem request timed out");
        return JsonSerializer.Serialize(new { error = "WcsSystem request timed out" });
    }
}

// ── Helpers để đọc an toàn từ JsonElement ───────────────────
static string GetStr(JsonElement root, string key)
    => root.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

static bool GetBool(JsonElement root, string key)
    => root.TryGetProperty(key, out var v) && v.GetBoolean();

static int GetInt(JsonElement root, string parent, string key)
{
    if (!root.TryGetProperty(parent, out var p)) return 0;
    if (!p.TryGetProperty(key, out var v))       return 0;
    return v.GetInt32();
}

static bool GetBool2(JsonElement root, string parent, string key)
{
    if (!root.TryGetProperty(parent, out var p)) return false;
    if (!p.TryGetProperty(key, out var v))        return false;
    return v.GetBoolean();
}