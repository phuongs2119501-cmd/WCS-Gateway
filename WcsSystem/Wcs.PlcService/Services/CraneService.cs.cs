using Wcs.PlcService.Plc;
using Wcs.PlcService.Models;
using Wcs.PlcService.DataMappingPlc;
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
		private const string XCRANE1 = DataPlc1.XCRANE1;
		private const string ZCRANE1 = DataPlc1.ZCRANE1;
		private const string XCRANE2 = DataPlc1.XCRANE2;
		private const string ZCRANE2 = DataPlc1.ZCRANE2;

		// STATE — Crane 1
		private const string CRANE1_FREE       = DataPlc1.CRANE1_FREE;
		private const string CRANE1_BUSY       = DataPlc1.CRANE1_BUSY;
		private const string CRANE1_ERROR      = DataPlc1.CRANE1_ERROR;
		private const string CRANE1_ERROR_CODE = DataPlc1.CRANE1_ERROR_CODE;

		// STATE — Crane 2
		private const string CRANE2_FREE       = DataPlc1.CRANE2_FREE;
		private const string CRANE2_BUSY       = DataPlc1.CRANE2_BUSY;
		private const string CRANE2_ERROR      = DataPlc1.CRANE2_ERROR;
		private const string CRANE2_ERROR_CODE = DataPlc1.CRANE2_ERROR_CODE;

		public void Update()
		{
			ReadPosition();
			ReadState();
		}

		private void ReadPosition()
		{
			// Read PLC1
			if (_plc1.TryRead<ushort>(XCRANE1, out ushort x1_1) &&
				_plc1.TryRead<ushort>(ZCRANE1, out ushort z1_1))
			{
				_state.DataPlc1.Crane1.X = x1_1;
				_state.DataPlc1.Crane1.Z = z1_1;
			}
			if (_plc1.TryRead<ushort>(XCRANE2, out ushort x1_2) &&
				_plc1.TryRead<ushort>(ZCRANE2, out ushort z1_2))
			{
				_state.DataPlc1.Crane2.X = x1_2;
				_state.DataPlc1.Crane2.Z = z1_2;
			}

			// Read PLC2
			if (_plc2.TryRead<ushort>(XCRANE1, out ushort x2_1) &&
				_plc2.TryRead<ushort>(ZCRANE1, out ushort z2_1))
			{
				_state.DataPlc2.Crane1.X = x2_1;
				_state.DataPlc2.Crane1.Z = z2_1;
			}
			if (_plc2.TryRead<ushort>(XCRANE2, out ushort x2_2) &&
				_plc2.TryRead<ushort>(ZCRANE2, out ushort z2_2))
			{
				_state.DataPlc2.Crane2.X = x2_2;
				_state.DataPlc2.Crane2.Z = z2_2;
			}
		}

		private void ReadState()
		{
			// PLC1 Crane 1
			if (_plc1.TryRead<bool>(CRANE1_BUSY, out bool busy1_1) &&
				_plc1.TryRead<bool>(CRANE1_ERROR, out bool error1_1) &&
				_plc1.TryRead<bool>(CRANE1_FREE, out bool free1_1))
			{
				_state.DataPlc1.Crane1.Busy = busy1_1;
				_state.DataPlc1.Crane1.Error = error1_1;
				_state.DataPlc1.Crane1.Free = free1_1;
			}
			if (_plc1.TryRead<ushort>(CRANE1_ERROR_CODE, out ushort errorCode1_1))
			{
				_state.DataPlc1.Crane1.ErrorCode = errorCode1_1;
			}

			// PLC1 Crane 2
			if (_plc1.TryRead<bool>(CRANE2_BUSY, out bool busy1_2) &&
				_plc1.TryRead<bool>(CRANE2_ERROR, out bool error1_2) &&
				_plc1.TryRead<bool>(CRANE2_FREE, out bool free1_2))
			{
				_state.DataPlc1.Crane2.Busy = busy1_2;
				_state.DataPlc1.Crane2.Error = error1_2;
				_state.DataPlc1.Crane2.Free = free1_2;
			}
			if (_plc1.TryRead<ushort>(CRANE2_ERROR_CODE, out ushort errorCode1_2))
			{
				_state.DataPlc1.Crane2.ErrorCode = errorCode1_2;
			}

			// PLC2 Crane 1
			if (_plc2.TryRead<bool>(CRANE1_BUSY, out bool busy2_1) &&
				_plc2.TryRead<bool>(CRANE1_ERROR, out bool error2_1) &&
				_plc2.TryRead<bool>(CRANE1_FREE, out bool free2_1))
			{
				_state.DataPlc2.Crane1.Busy = busy2_1;
				_state.DataPlc2.Crane1.Error = error2_1;
				_state.DataPlc2.Crane1.Free = free2_1;
			}
			if (_plc2.TryRead<ushort>(CRANE1_ERROR_CODE, out ushort errorCode2_1))
			{
				_state.DataPlc2.Crane1.ErrorCode = errorCode2_1;
			}

			// PLC2 Crane 2
			if (_plc2.TryRead<bool>(CRANE2_BUSY, out bool busy2_2) &&
				_plc2.TryRead<bool>(CRANE2_ERROR, out bool error2_2) &&
				_plc2.TryRead<bool>(CRANE2_FREE, out bool free2_2))
			{
				_state.DataPlc2.Crane2.Busy = busy2_2;
				_state.DataPlc2.Crane2.Error = error2_2;
				_state.DataPlc2.Crane2.Free = free2_2;
			}
			if (_plc2.TryRead<ushort>(CRANE2_ERROR_CODE, out ushort errorCode2_2))
			{
				_state.DataPlc2.Crane2.ErrorCode = errorCode2_2;
			}
		}
	}
}