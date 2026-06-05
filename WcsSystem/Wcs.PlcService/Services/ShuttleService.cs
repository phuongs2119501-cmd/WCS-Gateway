using Wcs.PlcService.Plc;
using Wcs.PlcService.Models;
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

		//
		// POSITION — Shuttle 1 (DB500, địa chỉ theo ảnh)
		//
		private const string XSHUTTLE1 = Db500Map.shuttle1X;
		private const string ZSHUTTLE1 = Db500Map.shuttle1Z;
		private const string BSHUTTLE1 = Db500Map.shuttle1B;

		//
		// POSITION — Shuttle 2 (DB500, địa chỉ theo ảnh)
		//
		private const string XSHUTTLE2 = Db500Map.shuttle2X;
		private const string ZSHUTTLE2 = Db500Map.shuttle2Z;
		private const string BSHUTTLE2 = Db500Map.shuttle2B;

		//
		// STATE — Shuttle 1 (DB500, địa chỉ theo ảnh)
		//  Free      → DB500.DBX20.0
		//  Busy      → DB500.DBX20.1
		//  Error Flag→ DB500.DBX20.2
		//  Error Code→ DB500.DBW22
		//  Battery   → DB500.DBW24
		//
		private const string SHUTTLE1_FREE       = Db500Map.shuttle1Free;
		private const string SHUTTLE1_BUSY       = Db500Map.shuttle1Busy;
		private const string SHUTTLE1_ERROR      = Db500Map.shuttle1Error;
		private const string SHUTTLE1_ERROR_CODE = Db500Map.shuttle1ErrorCode;
		private const string SHUTTLE1_PIN        = Db500Map.shuttle1Battery;

		//
		// STATE — Shuttle 2 (DB500, địa chỉ theo ảnh)
		//  Free      → DB500.DBX26.0
		//  Busy      → DB500.DBX26.1
		//  Error Flag→ DB500.DBX26.2
		//  Error Code→ DB500.DBW28
		//  Battery   → DB500.DBW30
		//
		private const string SHUTTLE2_FREE       = Db500Map.shuttle2Free;
		private const string SHUTTLE2_BUSY       = Db500Map.shuttle2Busy;
		private const string SHUTTLE2_ERROR      = Db500Map.shuttle2Error;
		private const string SHUTTLE2_ERROR_CODE = Db500Map.shuttle2ErrorCode;
		private const string SHUTTLE2_PIN        = Db500Map.shuttle2Battery;

		//
		// MAIN UPDATE
		//
		public void Update()
		{
			ReadPosition();
			ReadState();
		}

		//
		// ĐỌC VỊ TRÍ — cùng địa chỉ DB500, chỉ khác connector
		//
		private void ReadPosition()
		{
			// Shuttle 1 đọc từ PLC 1
			if (_plc1.TryRead<ushort>(XSHUTTLE1, out ushort x1) &&
				_plc1.TryRead<ushort>(ZSHUTTLE1, out ushort z1) &&
				_plc1.TryRead<ushort>(BSHUTTLE1, out ushort b1))
			{
				_state.Shuttle1.X = x1;
				_state.Shuttle1.Z = z1;
				_state.Shuttle1.B = b1;
			}

			// Shuttle 2 đọc từ PLC 2
			if (_plc2.TryRead<ushort>(XSHUTTLE2, out ushort x2) &&
				_plc2.TryRead<ushort>(ZSHUTTLE2, out ushort z2) &&
				_plc2.TryRead<ushort>(BSHUTTLE2, out ushort b2))
			{
				_state.Shuttle2.X = x2;
				_state.Shuttle2.Z = z2;
				_state.Shuttle2.B = b2;
			}
		}

		//
		// ĐỌC TRẠNG THÁI
		//
		private void ReadState()
		{
			// Shuttle 1 đọc từ PLC 1
			if (_plc1.TryRead<bool>(SHUTTLE1_FREE,  out bool free1) &&
				_plc1.TryRead<bool>(SHUTTLE1_BUSY,  out bool busy1) &&
				_plc1.TryRead<bool>(SHUTTLE1_ERROR, out bool error1))
			{
				_state.Shuttle1.Free  = free1;
				_state.Shuttle1.Busy  = busy1;
				_state.Shuttle1.Error = error1;
			}

			if (_plc1.TryRead<ushort>(SHUTTLE1_ERROR_CODE, out ushort errCode1))
				_state.Shuttle1.ErrorCode = errCode1;

			if (_plc1.TryRead<ushort>(SHUTTLE1_PIN, out ushort pin1))
				_state.Shuttle1.Pin = pin1;

			// Shuttle 2 đọc từ PLC 2
			if (_plc2.TryRead<bool>(SHUTTLE2_FREE,  out bool free2) &&
				_plc2.TryRead<bool>(SHUTTLE2_BUSY,  out bool busy2) &&
				_plc2.TryRead<bool>(SHUTTLE2_ERROR, out bool error2))
			{
				_state.Shuttle2.Free  = free2;
				_state.Shuttle2.Busy  = busy2;
				_state.Shuttle2.Error = error2;
			}

			if (_plc2.TryRead<ushort>(SHUTTLE2_ERROR_CODE, out ushort errCode2))
				_state.Shuttle2.ErrorCode = errCode2;

			if (_plc2.TryRead<ushort>(SHUTTLE2_PIN, out ushort pin2))
				_state.Shuttle2.Pin = pin2;
		}
	}
}
