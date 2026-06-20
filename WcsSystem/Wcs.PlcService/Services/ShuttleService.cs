using Wcs.PlcService.Plc;
using Wcs.PlcService.Models;
using Wcs.PlcService.DataMappingPlc;
/* ĐỌC VÀ HIỂN THỊ LÊN GIAO DIỆN
 * ...
 * Luồng dữ liệu hiện tại
PLC
 ↓
CraneService
 ↓
ShuttleService
 ↓
SystemState
 ↓
UI
 *
 * Shuttle 1 đọc từ PLC 1
 * Shuttle 2 đọc từ PLC 2
 */
namespace Wcs.PlcService.Services
{
	public class ShuttleService
	{
		private readonly Plc1Connector _plc1;
		private readonly Plc2Connector _plc2;
		private readonly SystemState _state;

		public ShuttleService(
			Plc1Connector plc1,
			Plc2Connector plc2,
			SystemState state)
		{
			_plc1 = plc1;
			_plc2 = plc2;
			_state = state;
		}

		// POSITION — Shuttle 1
		private const string XSHUTTLE1 = DataPlc1.XSHUTTLE1;
		private const string ZSHUTTLE1 = DataPlc1.ZSHUTTLE1;
		private const string BSHUTTLE1 = DataPlc1.BSHUTTLE1;

		// POSITION — Shuttle 2
		private const string XSHUTTLE2 = DataPlc1.XSHUTTLE2;
		private const string ZSHUTTLE2 = DataPlc1.ZSHUTTLE2;
		private const string BSHUTTLE2 = DataPlc1.BSHUTTLE2;

		// STATE — Shuttle 1
		private const string SHUTTLE1_FREE       = DataPlc1.SHUTTLE1_FREE;
		private const string SHUTTLE1_BUSY       = DataPlc1.SHUTTLE1_BUSY;
		private const string SHUTTLE1_ERROR      = DataPlc1.SHUTTLE1_ERROR;
		private const string SHUTTLE1_ERROR_CODE = DataPlc1.SHUTTLE1_ERROR_CODE;
		private const string SHUTTLE1_PIN        = DataPlc1.SHUTTLE1_PIN;

		// STATE — Shuttle 2
		private const string SHUTTLE2_FREE       = DataPlc1.SHUTTLE2_FREE;
		private const string SHUTTLE2_BUSY       = DataPlc1.SHUTTLE2_BUSY;
		private const string SHUTTLE2_ERROR      = DataPlc1.SHUTTLE2_ERROR;
		private const string SHUTTLE2_ERROR_CODE = DataPlc1.SHUTTLE2_ERROR_CODE;
		private const string SHUTTLE2_PIN        = DataPlc1.SHUTTLE2_PIN;

		public void Update()
		{
			ReadPosition();
			ReadState();
		}

		private void ReadPosition()
		{
			// Read PLC1
			if (_plc1.TryRead<ushort>(XSHUTTLE1, out ushort x1_1) &&
				_plc1.TryRead<ushort>(ZSHUTTLE1, out ushort z1_1) &&
				_plc1.TryRead<ushort>(BSHUTTLE1, out ushort b1_1))
			{
				_state.DataPlc1.Shuttle1.X = x1_1;
				_state.DataPlc1.Shuttle1.Z = z1_1;
				_state.DataPlc1.Shuttle1.B = b1_1;
			}
			if (_plc1.TryRead<ushort>(XSHUTTLE2, out ushort x1_2) &&
				_plc1.TryRead<ushort>(ZSHUTTLE2, out ushort z1_2) &&
				_plc1.TryRead<ushort>(BSHUTTLE2, out ushort b1_2))
			{
				_state.DataPlc1.Shuttle2.X = x1_2;
				_state.DataPlc1.Shuttle2.Z = z1_2;
				_state.DataPlc1.Shuttle2.B = b1_2;
			}

			// Read PLC2
			if (_plc2.TryRead<ushort>(XSHUTTLE1, out ushort x2_1) &&
				_plc2.TryRead<ushort>(ZSHUTTLE1, out ushort z2_1) &&
				_plc2.TryRead<ushort>(BSHUTTLE1, out ushort b2_1))
			{
				_state.DataPlc2.Shuttle1.X = x2_1;
				_state.DataPlc2.Shuttle1.Z = z2_1;
				_state.DataPlc2.Shuttle1.B = b2_1;
			}
			if (_plc2.TryRead<ushort>(XSHUTTLE2, out ushort x2_2) &&
				_plc2.TryRead<ushort>(ZSHUTTLE2, out ushort z2_2) &&
				_plc2.TryRead<ushort>(BSHUTTLE2, out ushort b2_2))
			{
				_state.DataPlc2.Shuttle2.X = x2_2;
				_state.DataPlc2.Shuttle2.Z = z2_2;
				_state.DataPlc2.Shuttle2.B = b2_2;
			}
		}

		private void ReadState()
		{
			// PLC1 Shuttle 1
			if (_plc1.TryRead<bool>(SHUTTLE1_FREE,  out bool free1_1) &&
				_plc1.TryRead<bool>(SHUTTLE1_BUSY,  out bool busy1_1) &&
				_plc1.TryRead<bool>(SHUTTLE1_ERROR, out bool error1_1))
			{
				_state.DataPlc1.Shuttle1.Free  = free1_1;
				_state.DataPlc1.Shuttle1.Busy  = busy1_1;
				_state.DataPlc1.Shuttle1.Error = error1_1;
			}
			if (_plc1.TryRead<ushort>(SHUTTLE1_ERROR_CODE, out ushort errCode1_1))
				_state.DataPlc1.Shuttle1.ErrorCode = errCode1_1;
			if (_plc1.TryRead<ushort>(SHUTTLE1_PIN, out ushort pin1_1))
				_state.DataPlc1.Shuttle1.Pin = pin1_1;

			// PLC1 Shuttle 2
			if (_plc1.TryRead<bool>(SHUTTLE2_FREE,  out bool free1_2) &&
				_plc1.TryRead<bool>(SHUTTLE2_BUSY,  out bool busy1_2) &&
				_plc1.TryRead<bool>(SHUTTLE2_ERROR, out bool error1_2))
			{
				_state.DataPlc1.Shuttle2.Free  = free1_2;
				_state.DataPlc1.Shuttle2.Busy  = busy1_2;
				_state.DataPlc1.Shuttle2.Error = error1_2;
			}
			if (_plc1.TryRead<ushort>(SHUTTLE2_ERROR_CODE, out ushort errCode1_2))
				_state.DataPlc1.Shuttle2.ErrorCode = errCode1_2;
			if (_plc1.TryRead<ushort>(SHUTTLE2_PIN, out ushort pin1_2))
				_state.DataPlc1.Shuttle2.Pin = pin1_2;

			// PLC2 Shuttle 1
			if (_plc2.TryRead<bool>(SHUTTLE1_FREE,  out bool free2_1) &&
				_plc2.TryRead<bool>(SHUTTLE1_BUSY,  out bool busy2_1) &&
				_plc2.TryRead<bool>(SHUTTLE1_ERROR, out bool error2_1))
			{
				_state.DataPlc2.Shuttle1.Free  = free2_1;
				_state.DataPlc2.Shuttle1.Busy  = busy2_1;
				_state.DataPlc2.Shuttle1.Error = error2_1;
			}
			if (_plc2.TryRead<ushort>(SHUTTLE1_ERROR_CODE, out ushort errCode2_1))
				_state.DataPlc2.Shuttle1.ErrorCode = errCode2_1;
			if (_plc2.TryRead<ushort>(SHUTTLE1_PIN, out ushort pin2_1))
				_state.DataPlc2.Shuttle1.Pin = pin2_1;

			// PLC2 Shuttle 2
			if (_plc2.TryRead<bool>(SHUTTLE2_FREE,  out bool free2_2) &&
				_plc2.TryRead<bool>(SHUTTLE2_BUSY,  out bool busy2_2) &&
				_plc2.TryRead<bool>(SHUTTLE2_ERROR, out bool error2_2))
			{
				_state.DataPlc2.Shuttle2.Free  = free2_2;
				_state.DataPlc2.Shuttle2.Busy  = busy2_2;
				_state.DataPlc2.Shuttle2.Error = error2_2;
			}
			if (_plc2.TryRead<ushort>(SHUTTLE2_ERROR_CODE, out ushort errCode2_2))
				_state.DataPlc2.Shuttle2.ErrorCode = errCode2_2;
			if (_plc2.TryRead<ushort>(SHUTTLE2_PIN, out ushort pin2_2))
				_state.DataPlc2.Shuttle2.Pin = pin2_2;
		}
	}
}