using S7.Net;
using Wcs.PlcService.Models;

namespace Wcs.PlcService.Plc;

public class S7Connector
{
    private readonly S7.Net.Plc _plc;
    private readonly string _tag;
    private readonly object _lock = new();

    // State tự quản lý — không phụ thuộc vào _plc.IsConnected làm nguồn truth
    private volatile bool _isConnected = false;

    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);

    // Throttle log lỗi: chỉ in lần đầu gặp lỗi cho mỗi địa chỉ
    private readonly HashSet<string> _loggedErrors = new();

    public S7Connector(PlcSettings settings, string tag = "PLC")
    {
        _tag = tag;
        _plc = new S7.Net.Plc(
            settings.CpuType,
            settings.IpAddress,
            settings.Rack,
            settings.Slot);

        Console.WriteLine($"[{_tag}] Initialized → IP={settings.IpAddress} Rack={settings.Rack} Slot={settings.Slot} CPU={settings.CpuType}");
    }

    // Property đơn thuần — an toàn để đọc từ nhiều thread
    public bool IsConnected => _isConnected;

    // Chủ động connect (gọi từ Worker startup)
    public void Connect() => EnsureConnected();


    private void EnsureConnected()
    {
        if (_isConnected)
            return;

        lock (_lock)
        {
            if (_isConnected)
                return;

            // Throttle reconnect
            if (DateTime.Now - _lastReconnectAttempt < _reconnectDelay)
                return;

            _lastReconnectAttempt = DateTime.Now;

            Console.WriteLine($"[{_tag}] 🔄 Attempting connect → {_plc.IP}:{_plc.Port}");

            try
            {
                // Đảm bảo đóng session cũ trước khi mở mới
                try { _plc.Close(); } catch { }

                _plc.Open();

                _isConnected = true;
                Console.WriteLine($"[{_tag}] ✅ CONNECTED successfully");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Console.WriteLine($"[{_tag}] ❌ Connect FAILED: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private void MarkDisconnected()
    {
        if (_isConnected)
        {
            _isConnected = false;
            Console.WriteLine($"[{_tag}] ⚠️ DISCONNECTED");
            try { _plc.Close(); } catch { }
        }
    }

    public bool TryWriteBool(string address, bool value)
    {
        EnsureConnected();
        if (!_isConnected) return false;

        lock (_lock)
        {
            try
            {
                _plc.Write(address, value);
                return true;
            }
            catch (Exception ex)
            {
                if (_loggedErrors.Add($"WBool:{address}"))
                    Console.WriteLine($"[{_tag}] Write bool error [{address}]: {ex.Message}");
                    
                if (!IsAddressError(ex)) MarkDisconnected();
                return false;
            }
        }
    }

    public bool TryWriteInt16(string address, short value)
    {
        EnsureConnected();
        if (!_isConnected) return false;

        lock (_lock)
        {
            try
            {
                _plc.Write(address, value);
                return true;
            }
            catch (Exception ex)
            {
                if (_loggedErrors.Add($"WInt:{address}"))
                    Console.WriteLine($"[{_tag}] Write int error [{address}]: {ex.Message}");
                    
                if (!IsAddressError(ex)) MarkDisconnected();
                return false;
            }
        }
    }

    public bool TryRead<T>(string address, out T value)
    {
        value = default!;

        EnsureConnected();
        if (!_isConnected) return false;

        lock (_lock)
        {
            try
            {
                var result = _plc.Read(address);

                if (result is T typed)
                {
                    value = typed;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Chỉ log lỗi lần đầu cho mỗi địa chỉ, tránh spam
                if (_loggedErrors.Add($"R:{address}"))
                    Console.WriteLine($"[{_tag}] Read error [{address}]: {ex.Message}");

                if (!IsAddressError(ex))
                {
                    _loggedErrors.Clear();  // Reset khi disconnect để log lại sau reconnect
                    MarkDisconnected();
                }

                return false;
            }
        }
    }


    public bool TryReadChar(string address, out char value)
    {
        value = '\0';

        EnsureConnected();
        if (!_isConnected) return false;

        lock (_lock)
        {
            try
            {
                var result = _plc.Read(address);

                if (result is byte b)
                {
                    value = (char)b;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Chỉ log lỗi lần đầu cho mỗi địa chỉ, tránh spam
                if (_loggedErrors.Add($"C:{address}"))
                    Console.WriteLine($"[{_tag}] ReadChar error [{address}]: {ex.Message}");

                if (!IsAddressError(ex))
                {
                    _loggedErrors.Clear();
                    MarkDisconnected();
                }

                return false;
            }
        }
    }

    public object Read(DataType dataType, int db, int startByte, VarType varType, int count)
    {
        return _plc.Read(dataType, db, startByte, varType, count);
    }

    // Phân biệt lỗi địa chỉ/cấu hình với lỗi mất kết nối thực sự
    // "Address out of range" = DB tồn tại nhưng offset sai → KHÔNG disconnect
    private static bool IsAddressError(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("Address out of range", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Data type mismatch", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("object does not exist", StringComparison.OrdinalIgnoreCase);
    }
}