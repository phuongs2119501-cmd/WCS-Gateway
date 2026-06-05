using S7.Net;
using Wcs.PlcService.Models;

namespace Wcs.PlcService.Plc;

public sealed class RealS7Backend : IS7Backend
{
    private readonly S7.Net.Plc _plc;

    public RealS7Backend(PlcSettings settings)
    {
        _plc = new S7.Net.Plc(
            settings.CpuType,
            settings.IpAddress,
            settings.Rack,
            settings.Slot);
    }

    public bool IsOpen => _plc.IsConnected;

    public void Open() => _plc.Open();

    public void Close() => _plc.Close();

    public object Read(string address) => _plc.Read(address)!;

    public object Read(DataType dataType, int db, int startByte, VarType varType, int count)
    {
        return _plc.Read(dataType, db, startByte, varType, count)!;
    }

    public void Write(string address, bool value) => _plc.Write(address, value);

    public void Write(string address, short value) => _plc.Write(address, value);

    public void Write(string address, byte value) => _plc.Write(address, value);
}
