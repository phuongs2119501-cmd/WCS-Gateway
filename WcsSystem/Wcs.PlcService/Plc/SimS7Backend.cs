using System.Text;
using System.Text.RegularExpressions;
using S7.Net;
using Wcs.PlcService.Plc.Sim;

namespace Wcs.PlcService.Plc;

/// <summary>
/// Fake DB500 backend. Every offset is derived from <see cref="Db500Map"/> (the generated,
/// hardware-verified map) — there are NO literal byte offsets in this class, so the simulator
/// can never drift from the real PLC layout. See SPEC-GW-006.
/// </summary>
public sealed class SimS7Backend : IS7Backend
{
    private const int DbNumber = 500;
    private const int DbLength = 128; // >= minLengthBytes (86); covers DBW84 + headroom
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly byte[] _db = new byte[DbLength];
    private readonly int _ticksPerMove;
    private bool _isOpen;
    private DateTime _lastTick = DateTime.UtcNow;
    private JobState? _job;

    // --- Offsets resolved once from the canonical map (no literals below) ---
    private static readonly int CmdByte    = Off(Db500Map.reqImportPallet).Byte;   // command region byte
    private static readonly int CmdBit0    = Off(Db500Map.reqImportPallet).Bit;     // import
    private static readonly int CmdBit2    = Off(Db500Map.reqTransferPallet).Bit;   // transfer (top of cmd range)
    private static readonly (int Byte, int Bit) Heartbeat = Off(Db500Map.wcsHeartbeat);

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

    // ------------------------------------------------------------------
    // Seed — initial DB500 image from the fixture, addressed via Db500Map
    // ------------------------------------------------------------------
    private void Seed(PlcSimFixture fixture)
    {
        // Crane slot (read as crane1 on PLC1, crane2 on PLC2)
        SetBitAddr(Db500Map.craneFree, fixture.CraneFree);
        SetBitAddr(Db500Map.craneBusy, fixture.CraneBusy);
        SetBitAddr(Db500Map.craneError, fixture.CraneError);
        SetWordAddr(Db500Map.craneErrorCode, fixture.CraneErrorCode);

        // Crane-2 slot — idle default (real binding deferred; not moved by the sim)
        SetBitAddr(Db500Map.crane2Free, true);

        // Shuttle-1 slot
        SetBitAddr(Db500Map.shuttle1Free, fixture.Shuttle1Free);
        SetBitAddr(Db500Map.shuttle1Busy, fixture.Shuttle1Busy);
        SetBitAddr(Db500Map.shuttle1Error, fixture.Shuttle1Error);
        SetWordAddr(Db500Map.shuttle1ErrorCode, fixture.Shuttle1ErrorCode);
        SetWordAddr(Db500Map.shuttle1Battery, fixture.Shuttle1Battery);

        // Shuttle-2 slot
        SetBitAddr(Db500Map.shuttle2Free, fixture.Shuttle2Free);
        SetBitAddr(Db500Map.shuttle2Busy, fixture.Shuttle2Busy);
        SetBitAddr(Db500Map.shuttle2Error, fixture.Shuttle2Error);
        SetWordAddr(Db500Map.shuttle2ErrorCode, fixture.Shuttle2ErrorCode);
        SetWordAddr(Db500Map.shuttle2Battery, fixture.Shuttle2Battery);

        SeedBarcode(fixture.Barcode);
        SetBitAddr(Db500Map.barcodeOk, fixture.BarcodeOk);
        SetBitAddr(Db500Map.barcodeNg, fixture.BarcodeNg);

        SetBitAddr(Db500Map.sysAuto, fixture.SysAuto);
        SetBitAddr(Db500Map.sysRunning, fixture.SysRunning);
        SetBitAddr(Db500Map.sysStop, fixture.SysStop);
        SetBitAddr(Db500Map.sysError, fixture.SysError);
        SetWordAddr(Db500Map.sysErrorCode, fixture.SysErrorCode);

        SetWordAddr(Db500Map.craneX, fixture.CraneX);
        SetWordAddr(Db500Map.craneZ, fixture.CraneZ);
        SetWordAddr(Db500Map.shuttle1X, fixture.Shuttle1X);
        SetWordAddr(Db500Map.shuttle1Z, fixture.Shuttle1Z);
        SetWordAddr(Db500Map.shuttle1B, fixture.Shuttle1B);
        SetWordAddr(Db500Map.shuttle2X, fixture.Shuttle2X);
        SetWordAddr(Db500Map.shuttle2Z, fixture.Shuttle2Z);
        SetWordAddr(Db500Map.shuttle2B, fixture.Shuttle2B);
        SetWordAddr(Db500Map.gate, fixture.Gate);
    }

    private void SeedBarcode(string value)
    {
        var bytes = Encoding.ASCII.GetBytes((value ?? string.Empty).PadRight(Db500Map.barcodeLength).Substring(0, Db500Map.barcodeLength));
        Array.Copy(bytes, 0, _db, Db500Map.barcodeByteStart, bytes.Length);
    }

    // ------------------------------------------------------------------
    // Command/heartbeat write handling
    // ------------------------------------------------------------------
    private void HandleBitWrite(int byteOffset, int bitOffset, bool oldValue, bool newValue)
    {
        // Heartbeat (DBX82.0): sim only latches the value, no side effect.
        if (byteOffset == Heartbeat.Byte && bitOffset == Heartbeat.Bit)
            return;

        // Command bits (DBX0.0..0.2 = import/export/transfer)
        if (byteOffset == CmdByte && bitOffset >= CmdBit0 && bitOffset <= CmdBit2)
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
            // crane
            GetWordAddr(Db500Map.craneX), GetWordAddr(Db500Map.craneZ),
            // shuttle-1
            GetWordAddr(Db500Map.shuttle1X), GetWordAddr(Db500Map.shuttle1Z), GetWordAddr(Db500Map.shuttle1B),
            // shuttle-2
            GetWordAddr(Db500Map.shuttle2X), GetWordAddr(Db500Map.shuttle2Z), GetWordAddr(Db500Map.shuttle2B),
            // targets: dest column/level (xout/zout) + dest row (bout)
            GetWordAddr(Db500Map.xout), GetWordAddr(Db500Map.zout), GetWordAddr(Db500Map.bout));

        // Crane goes Busy immediately. Shuttle is resolved lazily on first pump tick because
        // LocationRouter writes the command bit before modeCraneShuttle in the same loop pass.
        SetBitAddr(Db500Map.craneFree, false);
        SetBitAddr(Db500Map.craneBusy, true);

        SetBitAddr(Db500Map.plcDone, false);
    }

    private void FinishJob()
    {
        SetBitAddr(Db500Map.plcDone, false);
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
        int e = _job.ElapsedTicks;
        int n = _ticksPerMove;

        var selected = ResolveSelectedShuttle(_job);

        // Crane: step X/Z toward dest column/level
        SetWordAddr(Db500Map.craneX, StepToward(_job.CraneStartX, _job.TargetX, e, n));
        SetWordAddr(Db500Map.craneZ, StepToward(_job.CraneStartZ, _job.TargetZ, e, n));

        // Selected shuttle only: step X/Z toward dest, B toward dest row
        if (selected == ShuttleSlot.Shuttle1)
        {
            SetWordAddr(Db500Map.shuttle1X, StepToward(_job.S1StartX, _job.TargetX, e, n));
            SetWordAddr(Db500Map.shuttle1Z, StepToward(_job.S1StartZ, _job.TargetZ, e, n));
            SetWordAddr(Db500Map.shuttle1B, StepToward(_job.S1StartB, _job.TargetB, e, n));
        }
        else
        {
            SetWordAddr(Db500Map.shuttle2X, StepToward(_job.S2StartX, _job.TargetX, e, n));
            SetWordAddr(Db500Map.shuttle2Z, StepToward(_job.S2StartZ, _job.TargetZ, e, n));
            SetWordAddr(Db500Map.shuttle2B, StepToward(_job.S2StartB, _job.TargetB, e, n));
        }

        if (e >= n)
        {
            // Snap crane + selected shuttle to target and complete the handshake
            SetWordAddr(Db500Map.craneX, _job.TargetX);
            SetWordAddr(Db500Map.craneZ, _job.TargetZ);
            if (selected == ShuttleSlot.Shuttle1)
            {
                SetWordAddr(Db500Map.shuttle1X, _job.TargetX);
                SetWordAddr(Db500Map.shuttle1Z, _job.TargetZ);
                SetWordAddr(Db500Map.shuttle1B, _job.TargetB);
            }
            else
            {
                SetWordAddr(Db500Map.shuttle2X, _job.TargetX);
                SetWordAddr(Db500Map.shuttle2Z, _job.TargetZ);
                SetWordAddr(Db500Map.shuttle2B, _job.TargetB);
            }

            SetBitAddr(Db500Map.plcDone, true);

            SetBitAddr(Db500Map.craneFree, true);
            SetBitAddr(Db500Map.craneBusy, false);
            SetShuttleBusy(selected, false);

            _job.Done = true;
        }
    }

    private ShuttleSlot ResolveSelectedShuttle(JobState job)
    {
        if (job.SelectedShuttle is { } selected)
            return selected;

        selected = GetWordAddr(Db500Map.modeCraneShuttle) == 2
            ? ShuttleSlot.Shuttle2
            : ShuttleSlot.Shuttle1;

        job.SelectedShuttle = selected;
        SetShuttleBusy(selected, true);
        return selected;
    }

    private void SetShuttleBusy(ShuttleSlot selected, bool busy)
    {
        if (selected == ShuttleSlot.Shuttle1)
        {
            SetBitAddr(Db500Map.shuttle1Free, !busy);
            SetBitAddr(Db500Map.shuttle1Busy, busy);
            return;
        }

        SetBitAddr(Db500Map.shuttle2Free, !busy);
        SetBitAddr(Db500Map.shuttle2Busy, busy);
    }

    private static short StepToward(short start, short target, int elapsedTicks, int totalTicks)
    {
        if (elapsedTicks >= totalTicks)
            return target;

        double ratio = elapsedTicks / (double)totalTicks;
        return (short)Math.Round(start + ((target - start) * ratio), MidpointRounding.AwayFromZero);
    }

    // ------------------------------------------------------------------
    // Address-string helpers (keep callers in Db500Map terms, not offsets)
    // ------------------------------------------------------------------
    private void SetBitAddr(string address, bool value)
    {
        var p = ParseAddress(address);
        SetBit(p.ByteOffset, p.BitOffset, value);
    }

    private void SetWordAddr(string address, short value)
    {
        var p = ParseAddress(address);
        SetWord(p.ByteOffset, value);
    }

    private short GetWordAddr(string address)
    {
        var p = ParseAddress(address);
        return GetWord(p.ByteOffset);
    }

    private static (int Byte, int Bit) Off(string address)
    {
        var p = ParseAddress(address);
        return (p.ByteOffset, p.BitOffset);
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

    private enum ShuttleSlot
    {
        Shuttle1,
        Shuttle2
    }

    private readonly record struct ParsedAddress(AddressKind Kind, int ByteOffset, int BitOffset);

    private sealed class JobState
    {
        public JobState(
            int commandBit,
            short craneStartX, short craneStartZ,
            short s1StartX, short s1StartZ, short s1StartB,
            short s2StartX, short s2StartZ, short s2StartB,
            short targetX, short targetZ, short targetB)
        {
            CommandBit = commandBit;
            CraneStartX = craneStartX; CraneStartZ = craneStartZ;
            S1StartX = s1StartX; S1StartZ = s1StartZ; S1StartB = s1StartB;
            S2StartX = s2StartX; S2StartZ = s2StartZ; S2StartB = s2StartB;
            TargetX = targetX; TargetZ = targetZ; TargetB = targetB;
        }

        public int CommandBit { get; }
        public short CraneStartX { get; }
        public short CraneStartZ { get; }
        public short S1StartX { get; }
        public short S1StartZ { get; }
        public short S1StartB { get; }
        public short S2StartX { get; }
        public short S2StartZ { get; }
        public short S2StartB { get; }
        public short TargetX { get; }
        public short TargetZ { get; }
        public short TargetB { get; }
        public ShuttleSlot? SelectedShuttle { get; set; }
        public int ElapsedTicks { get; set; }
        public bool Done { get; set; }
    }
}
