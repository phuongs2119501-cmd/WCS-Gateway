using S7.Net;

namespace Wcs.PlcService.Plc;

public interface IS7Backend
{
    bool IsOpen { get; }

    void Open();
    void Close();

    object Read(string address);
    object Read(DataType dataType, int db, int startByte, VarType varType, int count);

    void Write(string address, bool value);
    void Write(string address, short value);
    void Write(string address, byte value);
}
