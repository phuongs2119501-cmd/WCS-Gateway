using Wcs.PlcService.Plc;
using Wcs.PlcService.Models;
/* ĐỌC TÍN HIỆU V HIỂN THỊ LÊN GIAO DIỆN
 * ...
 * ...
 * ..
 * ..
 * ..
PLC
 ↓
CraneService
 ↓
SystemState
 ↓
UI
 * .
 * .
 */
namespace Wcs.PlcService.Services
{
	public class CraneService
	{
		private readonly Plc1Connector _plc1;
		private readonly Plc2Connector _plc2;
		private readonly SystemState _state;

		public CraneService(
			Plc1Connector plc1,
			Plc2Connector plc2,
			SystemState state)
		{
			_plc1 = plc1;
			_plc2 = plc2;
			_state = state;
		}

		// POSITION
		private const string XCRANE1 = "DB500.DBW54";
		private const string ZCRANE1 = "DB500.DBW56";

		private const string XCRANE2 = "DB500.DBW54";
		private const string ZCRANE2 = "DB500.DBW56";

		// STATE — Crane 1 (PLC1 → DB500)
		private const string CRANE1_FREE       = "DB500.DBX16.0";
		private const string CRANE1_BUSY       = "DB500.DBX16.1";
		private const string CRANE1_ERROR      = "DB500.DBX16.2";
		private const string CRANE1_ERROR_CODE = "DB500.DBW18";

		// STATE — Crane 2 (PLC2 → cùng DB500, chỉ khác connector)
		private const string CRANE2_FREE       = "DB500.DBX16.0";
		private const string CRANE2_BUSY       = "DB500.DBX16.1";
		private const string CRANE2_ERROR      = "DB500.DBX16.2";
		private const string CRANE2_ERROR_CODE = "DB500.DBW18";

		public void Update()
		{
			ReadPosition();
			ReadState();
		}

		private void ReadPosition()
		{
			if (_plc1.TryRead<ushort>(XCRANE1, out ushort x1) &&
				_plc1.TryRead<ushort>(ZCRANE1, out ushort z1))
			{
				_state.Crane1.X = x1;
				_state.Crane1.Z = z1;
			}

			if (_plc2.TryRead<ushort>(XCRANE2, out ushort x2) &&
				_plc2.TryRead<ushort>(ZCRANE2, out ushort z2))
			{
				_state.Crane2.X = x2;
				_state.Crane2.Z = z2;
			}
		}

		private void ReadState()
		{
			if (_plc1.TryRead<bool>(CRANE1_BUSY, out bool busy) &&
				_plc1.TryRead<bool>(CRANE1_ERROR, out bool error) &&
				_plc1.TryRead<bool>(CRANE1_FREE, out bool free))
			{
				_state.Crane1.Busy = busy;
				_state.Crane1.Error = error;
				_state.Crane1.Free = free;
			}

			if (_plc1.TryRead<ushort>(CRANE1_ERROR_CODE, out ushort errorCode1))
			{
				_state.Crane1.ErrorCode = errorCode1;
			}

			if (_plc2.TryRead<bool>(CRANE2_BUSY, out bool busy2) &&
				_plc2.TryRead<bool>(CRANE2_ERROR, out bool error2) &&
				_plc2.TryRead<bool>(CRANE2_FREE, out bool free2))
			{
				_state.Crane2.Busy = busy2;
				_state.Crane2.Error = error2;
				_state.Crane2.Free = free2;
			}

			if (_plc2.TryRead<ushort>(CRANE2_ERROR_CODE, out ushort errorCode2))
			{
				_state.Crane2.ErrorCode = errorCode2;
			}
		}
	}
}