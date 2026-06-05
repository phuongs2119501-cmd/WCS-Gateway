using System.Text;
using System.Text.RegularExpressions;
using S7.Net;
using Wcs.PlcService.Plc.Sim;

namespace Wcs.PlcService.Plc;

public sealed class SimS7Backend : IS7Backend
{
    private const int DbNumber = 500;
    private const int DbLength = 128;
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly byte[] _db = new byte[DbLength];
    private readonly int _ticksPerMove;
    private bool _isOpen;
    private DateTime _lastTick = DateTime.UtcNow;
    private JobState? _job;

    public SimS7Backend(PlcSimFixture fixture, int ticksPerMove)
    {
        _ticksPerMove = Math.Max(1, ticksPerMove);
        Seed(fixture);
    }

    public bool IsOpen => _isOpen;

    public void Open()
    {
        _isOpen = true;
        _lastTick = DateTime.UtcNow;
    }

    public void Close()
    {
        _isOpen = false;
    }

    public object Read(string address)
    {
        EnsureOpen();
        Pump();

        var parsed = ParseAddress(address);
        return parsed.Kind switch
        {
            AddressKind.Bit => GetBit(parsed.ByteOffset, parsed.BitOffset),
            AddressKind.Word => (ushort)GetWord(parsed.ByteOffset),
            AddressKind.Byte => _db[parsed.ByteOffset],
            _ => throw new InvalidOperationException($"Unsupported address: {address}")
        };
    }

    public object Read(DataType dataType, int db, int startByte, VarType varType, int count)
    {
        EnsureOpen();
        Pump();
        EnsureDb(dataType, db);
        EnsureRange(startByte, Math.Max(1, count));

        if (varType == VarType.Byte)
        {
            if (count == 1)
                return _db[startByte];

            var buffer = new byte[count];
            Array.Copy(_db, startByte, buffer, 0, count);
            return buffer;
        }

        if (varType == VarType.Int)
            return GetWord(startByte);

        if (varType == VarType.Bit)
            return GetBit(startByte, 0);

        throw new NotSupportedException($"SimS7Backend does not support VarType.{varType}");
    }

    public void Write(string address, bool value)
    {
        EnsureOpen();
        Pump();

        var parsed = ParseAddress(address);
        if (parsed.Kind != AddressKind.Bit)
            throw new InvalidOperationException($"Address is not DBX: {address}");

        bool oldValue = GetBit(parsed.ByteOffset, parsed.BitOffset);
        SetBit(parsed.ByteOffset, parsed.BitOffset, value);
        HandleBitWrite(parsed.ByteOffset, parsed.BitOffset, oldValue, value);
    }

    public void Write(string address, short value)
    {
        EnsureOpen();
        Pump();

        var parsed = ParseAddress(address);
        if (parsed.Kind != AddressKind.Word)
            throw new InvalidOperationException($"Address is not DBW: {address}");

        SetWord(parsed.ByteOffset, value);
    }

    public void Write(string address, byte value)
    {
        EnsureOpen();
        Pump();

        var parsed = ParseAddress(address);
        if (parsed.Kind != AddressKind.Byte)
            throw new InvalidOperationException($"Address is not DBB: {address}");

        _db[parsed.ByteOffset] = value;
    }

    private void Seed(PlcSimFixture fixture)
    {
        SetBit(16, 0, fixture.CraneFree);
        SetBit(16, 1, fixture.CraneBusy);
        SetBit(16, 2, fixture.CraneError);
        SetWord(18, fixture.CraneErrorCode);

        SetBit(20, 0, fixture.Shuttle1Free);
        SetBit(20, 1, fixture.Shuttle1Busy);
        SetBit(20, 2, fixture.Shuttle1Error);
        SetWord(22, fixture.Shuttle1ErrorCode);
        SetWord(24, fixture.Shuttle1Battery);

        SetBit(26, 0, fixture.Shuttle2Free);
        SetBit(26, 1, fixture.Shuttle2Busy);
        SetBit(26, 2, fixture.Shuttle2Error);
        SetWord(28, fixture.Shuttle2ErrorCode);
        SetWord(30, fixture.Shuttle2Battery);

        SeedBarcode(fixture.Barcode);
        SetBit(46, 0, fixture.BarcodeOk);
        SetBit(46, 1, fixture.BarcodeNg);

        SetBit(50, 0, fixture.SysAuto);
        SetBit(50, 1, fixture.SysRunning);
        SetBit(50, 2, fixture.SysStop);
        SetBit(50, 3, fixture.SysError);
        SetWord(52, fixture.SysErrorCode);

        SetWord(54, fixture.CraneX);
        SetWord(56, fixture.CraneZ);
        SetWord(58, fixture.Shuttle1X);
        SetWord(60, fixture.Shuttle1Z);
        SetWord(62, fixture.Shuttle1B);
        SetWord(64, fixture.Shuttle2X);
        SetWord(66, fixture.Shuttle2Z);
        SetWord(68, fixture.Shuttle2B);
        SetWord(70, fixture.Gate);
    }

    private void SeedBarcode(string value)
    {
        var bytes = Encoding.ASCII.GetBytes((value ?? string.Empty).PadRight(14).Substring(0, 14));
        Array.Copy(bytes, 0, _db, 32, bytes.Length);
    }

    private void HandleBitWrite(int byteOffset, int bitOffset, bool oldValue, bool newValue)
    {
        // DBX74.0 la heartbeat: sim chi nhan gia tri, khong can xu ly them.
        if (byteOffset == 74 && bitOffset == 0)
            return;

        if (byteOffset == 0 && bitOffset is >= 0 and <= 2)
        {
            if (!oldValue && newValue)
                StartJob(bitOffset);

            if (oldValue && !newValue && _job is { Done: true })
                FinishJob();
        }
    }

    private void StartJob(int commandBit)
    {
        _job = new JobState(
            commandBit,
            GetWord(54),
            GetWord(56),
            GetWord(10),
            GetWord(12));

        SetBit(16, 0, false);
        SetBit(16, 1, true);
        SetBit(48, 0, false);
    }

    private void FinishJob()
    {
        SetBit(48, 0, false);
        _job = null;
    }

    private void Pump()
    {
        if (_job is null)
        {
            _lastTick = DateTime.UtcNow;
            return;
        }

        var now = DateTime.UtcNow;
        int ticks = (int)((now - _lastTick).Ticks / TickInterval.Ticks);
        if (ticks <= 0)
            return;

        _lastTick = _lastTick.AddTicks(TickInterval.Ticks * ticks);

        for (int i = 0; i < ticks && _job is not null && !_job.Done; i++)
            AdvanceJob();
    }

    private void AdvanceJob()
    {
        if (_job is null)
            return;

        _job.ElapsedTicks++;
        short nextX = StepToward(_job.StartX, _job.TargetX, _job.ElapsedTicks, _ticksPerMove);
        short nextZ = StepToward(_job.StartZ, _job.TargetZ, _job.ElapsedTicks, _ticksPerMove);

        SetWord(54, nextX);
        SetWord(56, nextZ);

        if (_job.ElapsedTicks >= _ticksPerMove || (nextX == _job.TargetX && nextZ == _job.TargetZ))
        {
            SetWord(54, _job.TargetX);
            SetWord(56, _job.TargetZ);
            SetBit(48, 0, true);
            SetBit(16, 0, true);
            SetBit(16, 1, false);
            _job.Done = true;
        }
    }

    private static short StepToward(short start, short target, int elapsedTicks, int totalTicks)
    {
        if (elapsedTicks >= totalTicks)
            return target;

        double ratio = elapsedTicks / (double)totalTicks;
        return (short)Math.Round(start + ((target - start) * ratio), MidpointRounding.AwayFromZero);
    }

    private static ParsedAddress ParseAddress(string address)
    {
        var bit = Regex.Match(address, @"^DB(?<db>\d+)\.DBX(?<byte>\d+)\.(?<bit>[0-7])$", RegexOptions.IgnoreCase);
        if (bit.Success)
            return CreateParsed(AddressKind.Bit, bit.Groups["db"].Value, bit.Groups["byte"].Value, bit.Groups["bit"].Value);

        var word = Regex.Match(address, @"^DB(?<db>\d+)\.DBW(?<byte>\d+)$", RegexOptions.IgnoreCase);
        if (word.Success)
            return CreateParsed(AddressKind.Word, word.Groups["db"].Value, word.Groups["byte"].Value, "0");

        var dbByte = Regex.Match(address, @"^DB(?<db>\d+)\.DBB(?<byte>\d+)$", RegexOptions.IgnoreCase);
        if (dbByte.Success)
            return CreateParsed(AddressKind.Byte, dbByte.Groups["db"].Value, dbByte.Groups["byte"].Value, "0");

        throw new InvalidOperationException($"Unsupported address: {address}");
    }

    private static ParsedAddress CreateParsed(AddressKind kind, string dbText, string byteText, string bitText)
    {
        int db = int.Parse(dbText);
        if (db != DbNumber)
            throw new InvalidOperationException($"Only DB{DbNumber} is supported by SimS7Backend");

        int byteOffset = int.Parse(byteText);
        int width = kind == AddressKind.Word ? 2 : 1;
        EnsureRange(byteOffset, width);

        return new ParsedAddress(kind, byteOffset, int.Parse(bitText));
    }

    private static void EnsureDb(DataType dataType, int db)
    {
        if (dataType != DataType.DataBlock || db != DbNumber)
            throw new InvalidOperationException($"Only DB{DbNumber} is supported by SimS7Backend");
    }

    private static void EnsureRange(int byteOffset, int width)
    {
        if (byteOffset < 0 || byteOffset + width > DbLength)
            throw new InvalidOperationException("Address out of range");
    }

    private void EnsureOpen()
    {
        if (!_isOpen)
            throw new InvalidOperationException("PLC is not connected");
    }

    private bool GetBit(int byteOffset, int bitOffset)
    {
        return (_db[byteOffset] & (1 << bitOffset)) != 0;
    }

    private void SetBit(int byteOffset, int bitOffset, bool value)
    {
        if (value)
            _db[byteOffset] |= (byte)(1 << bitOffset);
        else
            _db[byteOffset] &= (byte)~(1 << bitOffset);
    }

    private short GetWord(int byteOffset)
    {
        return (short)((_db[byteOffset] << 8) | _db[byteOffset + 1]);
    }

    private void SetWord(int byteOffset, short value)
    {
        _db[byteOffset] = (byte)((value >> 8) & 0xFF);
        _db[byteOffset + 1] = (byte)(value & 0xFF);
    }

    private enum AddressKind
    {
        Bit,
        Word,
        Byte
    }

    private readonly record struct ParsedAddress(AddressKind Kind, int ByteOffset, int BitOffset);

    private sealed class JobState
    {
        public JobState(int commandBit, short startX, short startZ, short targetX, short targetZ)
        {
            CommandBit = commandBit;
            StartX = startX;
            StartZ = startZ;
            TargetX = targetX;
            TargetZ = targetZ;
        }

        public int CommandBit { get; }
        public short StartX { get; }
        public short StartZ { get; }
        public short TargetX { get; }
        public short TargetZ { get; }
        public int ElapsedTicks { get; set; }
        public bool Done { get; set; }
    }
}
