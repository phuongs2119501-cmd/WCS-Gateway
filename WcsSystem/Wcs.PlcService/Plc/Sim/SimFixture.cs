namespace Wcs.PlcService.Plc.Sim;

public sealed class SimFixture
{
    public int TicksPerMove { get; set; } = 40;
    public PlcSimFixture Plc1 { get; set; } = PlcSimFixture.CreateDefault(1);
    public PlcSimFixture Plc2 { get; set; } = PlcSimFixture.CreateDefault(2);
}

public sealed class PlcSimFixture
{
    public bool CraneFree { get; set; } = true;
    public bool CraneBusy { get; set; }
    public bool CraneError { get; set; }
    public short CraneErrorCode { get; set; }

    public bool Shuttle1Free { get; set; } = true;
    public bool Shuttle1Busy { get; set; }
    public bool Shuttle1Error { get; set; }
    public short Shuttle1ErrorCode { get; set; }
    public short Shuttle1Battery { get; set; } = 90;

    public bool Shuttle2Free { get; set; } = true;
    public bool Shuttle2Busy { get; set; }
    public bool Shuttle2Error { get; set; }
    public short Shuttle2ErrorCode { get; set; }
    public short Shuttle2Battery { get; set; } = 90;

    public string Barcode { get; set; } = "SIM00000000001";
    public bool BarcodeOk { get; set; } = true;
    public bool BarcodeNg { get; set; }

    public bool SysAuto { get; set; } = true;
    public bool SysRunning { get; set; } = true;
    public bool SysStop { get; set; }
    public bool SysError { get; set; }
    public short SysErrorCode { get; set; }

    public short CraneX { get; set; } = 1;
    public short CraneZ { get; set; } = 1;
    public short Shuttle1X { get; set; } = 1;
    public short Shuttle1Z { get; set; } = 1;
    public short Shuttle1B { get; set; } = 1;
    public short Shuttle2X { get; set; } = 14;
    public short Shuttle2Z { get; set; } = 1;
    public short Shuttle2B { get; set; } = 3;
    public short Gate { get; set; } = 1;

    public static PlcSimFixture CreateDefault(int plcNumber)
    {
        return new PlcSimFixture
        {
            Barcode = plcNumber == 1 ? "SIMPLC1000001" : "SIMPLC2000001",
            CraneX = plcNumber == 1 ? (short)1 : (short)14,
            Shuttle1X = plcNumber == 1 ? (short)1 : (short)14,
            Shuttle1B = plcNumber == 1 ? (short)1 : (short)3,
            Shuttle2X = plcNumber == 1 ? (short)6 : (short)20,
            Shuttle2B = plcNumber == 1 ? (short)1 : (short)3,
            Gate = plcNumber == 1 ? (short)1 : (short)2
        };
    }
}
