using S7.Net;
using Wcs.PlcService.Models;

namespace Wcs.PlcService.Plc;

public class S7Connector
{
    private readonly IS7Backend _backend;
    private readonly PlcSettings _settings;
    private readonly string _tag;
    private readonly object _lock = new();

    // State tu quan ly - khong phu thuoc vao backend.IsOpen lam nguon truth
    private volatile bool _isConnected = false;

    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);

    // Throttle log loi: chi in lan dau gap loi cho moi dia chi
    private readonly HashSet<string> _loggedErrors = new();

    public S7Connector(PlcSettings settings, string tag = "PLC")
        : this(settings, new RealS7Backend(settings), tag)
    {
    }

    public S7Connector(PlcSettings settings, IS7Backend backend, string tag = "PLC")
    {
        _settings = settings;
        _backend = backend;
        _tag = tag;

        Console.WriteLine($"[{_tag}] Initialized -> IP={settings.IpAddress} Rack={settings.Rack} Slot={settings.Slot} CPU={settings.CpuType}");
    }

    // Property don thuan - an toan de doc tu nhieu thread
    public bool IsConnected => _isConnected;

    // Chu dong connect (goi tu Worker startup)
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

            Console.WriteLine($"[{_tag}] Attempting connect -> {_settings.IpAddress}:102");

            try
            {
                // Dam bao dong session cu truoc khi mo moi
                try { _backend.Close(); } catch { }

                _backend.Open();

                _isConnected = true;
                Console.WriteLine($"[{_tag}] CONNECTED successfully");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Console.WriteLine($"[{_tag}] Connect FAILED: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private void MarkDisconnected()
    {
        if (_isConnected)
        {
            _isConnected = false;
            Console.WriteLine($"[{_tag}] DISCONNECTED");
            try { _backend.Close(); } catch { }
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
                _backend.Write(address, value);
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
                _backend.Write(address, value);
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

    public bool TryWriteByte(string address, byte value)
    {
        EnsureConnected();
        if (!_isConnected) return false;

        lock (_lock)
        {
            try
            {
                _backend.Write(address, value);
                return true;
            }
            catch (Exception ex)
            {
                if (_loggedErrors.Add($"WByte:{address}"))
                    Console.WriteLine($"[{_tag}] Write byte error [{address}]: {ex.Message}");

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
                var result = _backend.Read(address);

                if (result is T typed)
                {
                    value = typed;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Chi log loi lan dau cho moi dia chi, tranh spam
                if (_loggedErrors.Add($"R:{address}"))
                    Console.WriteLine($"[{_tag}] Read error [{address}]: {ex.Message}");

                if (!IsAddressError(ex))
                {
                    _loggedErrors.Clear();  // Reset khi disconnect de log lai sau reconnect
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
                var result = _backend.Read(address);

                if (result is byte b)
                {
                    value = (char)b;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Chi log loi lan dau cho moi dia chi, tranh spam
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
        return _backend.Read(dataType, db, startByte, varType, count);
    }

    // Phan biet loi dia chi/cau hinh voi loi mat ket noi thuc su
    // "Address out of range" = DB ton tai nhung offset sai -> KHONG disconnect
    private static bool IsAddressError(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("Address out of range", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Data type mismatch", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("object does not exist", StringComparison.OrdinalIgnoreCase);
    }
}
