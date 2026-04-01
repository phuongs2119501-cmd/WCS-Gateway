using Wcs.PlcService.Plc;
using Wcs.PlcService.Models;

/* ĐỌC TRẠNG THÁI HỆ THỐNG TỪ 2 PLC — ĐỘC LẬP, SONG SONG
 *
 * DB500 — State Main System:
 *   Mode Auto/Manual → DB500.DBX50.0  (Bool)
 *   Running          → DB500.DBX50.1  (Bool)
 *   Stop             → DB500.DBX50.2  (Bool)
 *   Error            → DB500.DBX50.3  (Bool)
 *   Error Code       → DB500.DBW52    (Int)
 *
 * PLC1 → SystemState.System1
 * PLC2 → SystemState.System2
 * Mỗi PLC đọc độc lập — không ảnh hưởng nhau
 */
namespace Wcs.PlcService.Services
{
    public class SystemService
    {
        private readonly Plc1Connector _plc1;
        private readonly Plc2Connector _plc2;
        private readonly SystemState   _state;

        // ── Địa chỉ DB500 ───────────────────────────────────
        private const string AUTO_MODE  = "DB500.DBX50.0";
        private const string RUNNING    = "DB500.DBX50.1";
        private const string STOP       = "DB500.DBX50.2";
        private const string ERROR      = "DB500.DBX50.3";
        private const string ERROR_CODE = "DB500.DBW52";

        // ── Track thay đổi — chỉ log khi khác lần trước ────
        private bool   _p1Auto, _p1Running, _p1Stop, _p1Error;
        private ushort _p1Code = 0;
        private bool   _p2Auto, _p2Running, _p2Stop, _p2Error;
        private ushort _p2Code = 0;
        private bool   _init1, _init2;

        public SystemService(Plc1Connector plc1, Plc2Connector plc2, SystemState state)
        {
            _plc1  = plc1;
            _plc2  = plc2;
            _state = state;
        }

        // ── Gọi từ Worker mỗi vòng lặp ─────────────────────
        public void Update()
        {
            ReadFromPlc1();
            ReadFromPlc2();
        }

        // ── ĐỌC TỪ PLC1 ─────────────────────────────────────
        private void ReadFromPlc1()
        {
            if (_plc1.TryRead<bool>(AUTO_MODE, out bool auto)   &&
                _plc1.TryRead<bool>(RUNNING,   out bool run)    &&
                _plc1.TryRead<bool>(STOP,      out bool stop)   &&
                _plc1.TryRead<bool>(ERROR,     out bool err))
            {
                _state.System1.Auto    = auto;
                _state.System1.Running = run;
                _state.System1.Stop    = stop;
                _state.System1.Error   = err;

                if (!_init1 || auto != _p1Auto || run != _p1Running ||
                    stop != _p1Stop || err != _p1Error)
                {
                    // Đã tắt log SYS PLC1
                    _p1Auto = auto; _p1Running = run; _p1Stop = stop; _p1Error = err;
                    _init1 = true;
                }
            }

            if (_plc1.TryRead<ushort>(ERROR_CODE, out ushort code1))
            {
                _state.System1.ErrorCode = code1;
                if (code1 != _p1Code)
                {
                    // if (code1 != 0) Console.WriteLine($"[SYS PLC1] ErrorCode={code1}");
                    _p1Code = code1;
                }
            }
        }

        // ── ĐỌC TỪ PLC2 ─────────────────────────────────────
        private void ReadFromPlc2()
        {
            if (_plc2.TryRead<bool>(AUTO_MODE, out bool auto)   &&
                _plc2.TryRead<bool>(RUNNING,   out bool run)    &&
                _plc2.TryRead<bool>(STOP,      out bool stop)   &&
                _plc2.TryRead<bool>(ERROR,     out bool err))
            {
                _state.System2.Auto    = auto;
                _state.System2.Running = run;
                _state.System2.Stop    = stop;
                _state.System2.Error   = err;

                if (!_init2 || auto != _p2Auto || run != _p2Running ||
                    stop != _p2Stop || err != _p2Error)
                {
                    // Đã tắt log SYS PLC2
                    _p2Auto = auto; _p2Running = run; _p2Stop = stop; _p2Error = err;
                    _init2 = true;
                }
            }

            if (_plc2.TryRead<ushort>(ERROR_CODE, out ushort code2))
            {
                _state.System2.ErrorCode = code2;
                if (code2 != _p2Code)
                {
                    // if (code2 != 0) Console.WriteLine($"[SYS PLC2] ErrorCode={code2}");
                    _p2Code = code2;
                }
            }
        }
    }
}