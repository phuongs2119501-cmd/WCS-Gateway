using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Wcs.PlcService.Models;

/*
 * TCP CLIENT — Gửi lệnh đến TcpSocket Server để lấy WCS Status
 * ─────────────────────────────────────────────────────────────
 * Protocol:
 *   → Gửi:  "GET_STATUS"  (UTF-8 string)
 *   ← Nhận: JSON toàn bộ SystemState từ WcsSystem
 *
 *   → Gửi:  "PING"
 *   ← Nhận: "PONG"
 * ─────────────────────────────────────────────────────────────
 */
namespace Wcs.PlcService.Services
{
    public class TcpClientService
    {
        private const string SERVER_IP   = "127.0.0.1";
        private const int    SERVER_PORT = 9000;

        // ── PING: kiểm tra kết nối TCP Server ───────────────
        public async Task<bool> PingAsync()
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(SERVER_IP, SERVER_PORT);

                using var stream = client.GetStream();

                // Gửi PING
                byte[] data = Encoding.UTF8.GetBytes("PING");
                await stream.WriteAsync(data);

                // Nhận PONG
                byte[] buffer = new byte[64];
                int    length = await stream.ReadAsync(buffer);
                string reply  = Encoding.UTF8.GetString(buffer, 0, length).Trim();

                Console.WriteLine($"[TcpClient] Ping → Server replied: \"{reply}\"");
                return reply == "PONG";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpClient] Ping failed: {ex.Message}");
                return false;
            }
        }

        // ── GET_STATUS: lấy toàn bộ dữ liệu từ TCP Server ──
        public async Task<WcsStatusDto?> GetStatusAsync()
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(SERVER_IP, SERVER_PORT);

                using var stream = client.GetStream();
                stream.ReadTimeout  = 5000;
                stream.WriteTimeout = 5000;

                // Gửi lệnh GET_STATUS
                byte[] request = Encoding.UTF8.GetBytes("GET_STATUS");
                await stream.WriteAsync(request);

                // Nhận JSON response (có thể lớn hơn 1024 bytes)
                using var ms = new MemoryStream();
                byte[] buf   = new byte[4096];
                int    n;

                // Đọc toàn bộ dữ liệu cho đến khi server đóng stream
                do
                {
                    n = await stream.ReadAsync(buf);
                    if (n > 0) ms.Write(buf, 0, n);
                }
                while (n == buf.Length); // còn dữ liệu thì đọc tiếp

                string json = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine($"[TcpClient] Received {json.Length} chars from TCP Server");

                var dto = JsonSerializer.Deserialize<WcsStatusDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto != null)
                {
                    Console.WriteLine("[TcpClient] ── Parsed WCS Status ───────────────────");
                    Console.WriteLine($"             PLC1={dto.Plc1}  PLC2={dto.Plc2}");
                    Console.WriteLine($"             Barcode1=\"{dto.Barcode1}\"  Barcode2=\"{dto.Barcode2}\"");
                    Console.WriteLine($"             Crane1  X={dto.Crane1?.X}  Z={dto.Crane1?.Z}  Busy={dto.Crane1?.Busy}");
                    Console.WriteLine($"             Crane2  X={dto.Crane2?.X}  Z={dto.Crane2?.Z}  Busy={dto.Crane2?.Busy}");
                    Console.WriteLine($"             Shuttle1 X={dto.Shuttle1?.X}  Z={dto.Shuttle1?.Z}  B={dto.Shuttle1?.B}  Busy={dto.Shuttle1?.Busy}");
                    Console.WriteLine($"             Shuttle2 X={dto.Shuttle2?.X}  Z={dto.Shuttle2?.Z}  B={dto.Shuttle2?.B}  Busy={dto.Shuttle2?.Busy}");
                    Console.WriteLine("[TcpClient] ──────────────────────────────────────");
                }

                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpClient] GetStatus failed: {ex.Message}");
                return null;
            }
        }
    }

    // ── DTO khớp với JSON trả về từ StatusController ────────
    public class WcsStatusDto
    {
        public bool   Plc1     { get; set; }
        public bool   Plc2     { get; set; }
        public string Barcode1 { get; set; } = "";
        public string Barcode2 { get; set; } = "";
        public CraneModel?   Crane1   { get; set; }
        public CraneModel?   Crane2   { get; set; }
        public ShuttleModel? Shuttle1 { get; set; }
        public ShuttleModel? Shuttle2 { get; set; }
    }
}