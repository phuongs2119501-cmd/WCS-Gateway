using Wcs.PlcService.Plc;
using Wcs.PlcService.Models;
using Wcs.PlcService.DataMappingPlc;

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
            if (_plc1.TryRead<bool>(DataPlc1.AUTO_MODE, out bool auto)   &&
                _plc1.TryRead<bool>(DataPlc1.RUNNING,   out bool run)    &&
                _plc1.TryRead<bool>(DataPlc1.STOP,      out bool stop)   &&
                _plc1.TryRead<bool>(DataPlc1.ERROR,     out bool err))
            {
                _state.DataPlc1.System.Auto    = auto;
                _state.DataPlc1.System.Running = run;
                _state.DataPlc1.System.Stop    = stop;
                _state.DataPlc1.System.Error   = err;

                if (!_init1 || auto != _p1Auto || run != _p1Running ||
                     stop != _p1Stop || err != _p1Error)
                {
                    _p1Auto = auto; _p1Running = run; _p1Stop = stop; _p1Error = err;
                    _init1 = true;
                }
            }

            if (_plc1.TryRead<ushort>(DataPlc1.ERROR_CODE, out ushort code1))
            {
                _state.DataPlc1.System.ErrorCode = code1;
                if (code1 != _p1Code)
                {
                    _p1Code = code1;
                }
            }

            if (_plc1.TryRead<bool>(DataPlc1.PLC1_DONE, out bool done1))
            {
                _state.DataPlc1.Done = done1;
            }

            if (_plc1.TryRead<bool>(DataPlc1.PLC1_FAIL, out bool fail1))
            {
                _state.DataPlc1.Fail = fail1;
            }

            if (_plc1.TryRead<ushort>(DataPlc1.DIRECTION_BLOCK2, out ushort dir1))
            {
                _state.DataPlc1.DirectionBlock2 = dir1;
            }
        }

        // ── ĐỌC TỪ PLC2 ─────────────────────────────────────
        private void ReadFromPlc2()
        {
            if (_plc2.TryRead<bool>(DataPlc2.AUTO_MODE, out bool auto)   &&
                _plc2.TryRead<bool>(DataPlc2.RUNNING,   out bool run)    &&
                _plc2.TryRead<bool>(DataPlc2.STOP,      out bool stop)   &&
                _plc2.TryRead<bool>(DataPlc2.ERROR,     out bool err))
            {
                _state.DataPlc2.System.Auto    = auto;
                _state.DataPlc2.System.Running = run;
                _state.DataPlc2.System.Stop    = stop;
                _state.DataPlc2.System.Error   = err;

                if (!_init2 || auto != _p2Auto || run != _p2Running ||
                    stop != _p2Stop || err != _p2Error)
                {
                    _p2Auto = auto; _p2Running = run; _p2Stop = stop; _p2Error = err;
                    _init2 = true;
                }
            }

            if (_plc2.TryRead<ushort>(DataPlc2.ERROR_CODE, out ushort code2))
            {
                _state.DataPlc2.System.ErrorCode = code2;
                if (code2 != _p2Code)
                {
                    _p2Code = code2;
                }
            }

            if (_plc2.TryRead<bool>(DataPlc2.PLC2_DONE, out bool done2))
            {
                _state.DataPlc2.Done = done2;
            }

            if (_plc2.TryRead<bool>(DataPlc2.PLC2_FAIL, out bool fail2))
            {
                _state.DataPlc2.Fail = fail2;
            }

            if (_plc2.TryRead<ushort>(DataPlc2.DIRECTION_BLOCK2, out ushort dir2))
            {
                _state.DataPlc2.DirectionBlock2 = dir2;
            }
        }
    }
}