using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
/* ĐỌC DỮ LIỆU TỪ APP CONTROL SYSTEM GỒM LỆNH VÀ VỊ TRÍ LƯU HÀNG HÓA
 * XỬ LÝ DỮ LIỆU
 * CHỌN CRANE VÀ SHUTTLE PHÙ HỢP ĐỂ THỰC THI LỆNH
 * ...
 * ...
 * ..
 * ..
 * ..
 * .
 */

namespace Wcs.PlcService.Services
{
	public class LocationRouter
	{
		private readonly Plc1Connector _plc1;
		private readonly Plc2Connector _plc2;
		private readonly SystemState _state;

		//
		// COMMAND — 3 loại lệnh (DB500, cùng địa chỉ cho cả 2 PLC)
		//   Req_ImportPallet  → DB500.DBX0.0  (Lệnh Nhập Pallet)
		//   Req_ExportPallet  → DB500.DBX0.1  (Lệnh Xuất Pallet)
		//   Req_TransferPallet→ DB500.DBX0.2  (Lệnh Chuyển Pallet)
		//
		private const string REQ_IMPORT_PALLET   = "DB500.DBX0.0";
		private const string REQ_EXPORT_PALLET   = "DB500.DBX0.1";
		private const string REQ_TRANSFER_PALLET = "DB500.DBX0.2";

		//
		// COMPLETE — PLC báo xong: đọc từ đúng PLC đã nhận lệnh
		//
		private const string PLC1_DONE = "DB500.DBX48.0";
		private const string PLC2_DONE = "DB500.DBX48.0";

		//
		// MODE CRANE SHUTTLE  (Int, offset 2.0)
		//  = 0: chưa gửi gì
		//  = 1: đã gửi xuống PLC1
		//  = 2: đã gửi xuống PLC2
		//  → Write cùng lúc xuống cả 2 PLC
		//
		private const string PLC1_MODE_CRANE_SHUTTLE = "DB500.DBW2";
		private const string PLC2_MODE_CRANE_SHUTTLE = "DB500.DBW2";

		//
		// PLC1 LOCATION  (Position_Target – DB500)
		//
		private const string PLC1_XIN  = "DB500.DBW4";
		private const string PLC1_ZIN  = "DB500.DBW6";
		private const string PLC1_BIN  = "DB500.DBW8";

		private const string PLC1_XOUT = "DB500.DBW10";
		private const string PLC1_ZOUT = "DB500.DBW12";
		private const string PLC1_BOUT = "DB500.DBW14";

		//
		// PLC2 LOCATION  (Position_Target – DB500, cùng địa chỉ với PLC1)
		//
		private const string PLC2_XIN  = "DB500.DBW4";
		private const string PLC2_ZIN  = "DB500.DBW6";
		private const string PLC2_BIN  = "DB500.DBW8";

		private const string PLC2_XOUT = "DB500.DBW10";
		private const string PLC2_ZOUT = "DB500.DBW12";
		private const string PLC2_BOUT = "DB500.DBW14";

		//
		// STATE
		//
		private bool _jobActive    = false;
		private bool _commandSent  = false;
		private int  _targetPlc    = 0;
		private int  _commandType  = 0;   // lưu lại CommandType đang thực thi để reset đúng bit

		//
		// LOCATION BUFFER
		//
		private short Xin;
		private short Zin;
		private short Bin;

		private short Xout;
		private short Zout;
		private short Bout;

		public LocationRouter(Plc1Connector plc1, Plc2Connector plc2, SystemState state)
		{
			_plc1 = plc1;
			_plc2 = plc2;
			_state = state;
		}

		//
		// RECEIVE LOCATION FROM WEB
		//
		public void SetLocation(LocationModel loc)
		{
			Xin  = (short)(loc.Xin  ?? 0);
			Zin  = (short)(loc.Zin  ?? 0);
			Bin  = (short)(loc.Bin  ?? 0);

			Xout = (short)(loc.Xout ?? 0);
			Zout = (short)(loc.Zout ?? 0);
			Bout = (short)(loc.Bout ?? 0);

			_commandType = loc.CommandType;
            _state.LastLocation = loc;

			Console.WriteLine($"Router Receive: Cmd={_commandType} | {Xin} {Zin} {Bin} → {Xout} {Zout} {Bout}");

			_jobActive   = true;
			_commandSent = false;
		}

		//
		// MAIN LOOP
		//
		public void Execute()
		{
			if (!_jobActive)
				return;

			if (_commandSent)
			{
				CheckComplete();
				return;
			}

			//
			// ROUTING
			//
			if (Bin == 1)
			{
				SendToPlc1();
			}
			else if (Bin == 3)
			{
				SendToPlc2();
			}
			else if (Bin == 2)
			{
				if (Xin >= 1 && Xin <= 13)
					SendToPlc1();
				else if (Xin >= 14 && Xin <= 26)
					SendToPlc2();
				else
				{
					// Default fallback if Xin is out of expected ranges
					Console.WriteLine($"WARNING: Bin=2 but Xin ({Xin}) is out of bounds (1-26). Sending to PLC1 by default...");
					SendToPlc1();
				}
			}
		}

		//
		// LẤY ĐỊA CHỈ BIT COMMAND THEO LOẠI LỆNH
		//
		private string GetCommandAddress()
		{
			return _commandType switch
			{
				1 => REQ_IMPORT_PALLET,    // Nhập Pallet → DB500.DBX0.0
				2 => REQ_EXPORT_PALLET,    // Xuất Pallet → DB500.DBX0.1
				3 => REQ_TRANSFER_PALLET,  // Chuyển Pallet → DB500.DBX0.2
				_ => REQ_IMPORT_PALLET
			};
		}

		//
		// WRITE MODE_CRANE_SHUTTLE XUỐNG CẢ 2 PLC + CẬP NHẬT SYSTEM STATE
		//
		private void WriteModeCraneShuttle(short mode)
		{
			_plc1.TryWriteInt16(PLC1_MODE_CRANE_SHUTTLE, mode);
			_plc2.TryWriteInt16(PLC2_MODE_CRANE_SHUTTLE, mode);
			_state.ModeCraneShuttle = mode;   // đồng bộ để ShuttleService đọc được
		}

		//
		// CALCULATE MODE CRANE SHUTTLE (Target PLC = 1 or 2)
		//
		private short CalculateModeCraneShuttle(int targetPlc)
		{
			CraneModel targetCrane = targetPlc == 1 ? _state.Crane1 : _state.Crane2;
			
			Console.WriteLine($"[Mode Selection] TARGET PLC={targetPlc} | Crane Pos: X={targetCrane.X}, Z={targetCrane.Z}");

			var validShuttles = new System.Collections.Generic.List<(int mode, ShuttleModel shuttle)>();
			
			if (targetPlc == 1)
			{
				// Crane 1 hoạt động ở khu vực X từ 1 đến 13
				if ((_state.Shuttle1.B == 1 || _state.Shuttle1.B == 2) && (_state.Shuttle1.X >= 1 && _state.Shuttle1.X <= 13)) 
					validShuttles.Add((1, _state.Shuttle1));
				
				if ((_state.Shuttle2.B == 1 || _state.Shuttle2.B == 2) && (_state.Shuttle2.X >= 1 && _state.Shuttle2.X <= 13)) 
					validShuttles.Add((2, _state.Shuttle2));
			}
			else
			{
				// Crane 2 hoạt động ở khu vực X từ 14 đến 26
				if ((_state.Shuttle1.B == 2 || _state.Shuttle1.B == 3) && (_state.Shuttle1.X >= 14 && _state.Shuttle1.X <= 26)) 
					validShuttles.Add((1, _state.Shuttle1));
				
				if ((_state.Shuttle2.B == 2 || _state.Shuttle2.B == 3) && (_state.Shuttle2.X >= 14 && _state.Shuttle2.X <= 26)) 
					validShuttles.Add((2, _state.Shuttle2));
			}

			if (validShuttles.Count == 0)
			{
				Console.WriteLine($"[Mode Selection] WARNING: No shuttle found with valid B and reachable X for PLC {targetPlc}. Falling back to mode {targetPlc}.");
				return (short)targetPlc;
			}

			int bestMode = validShuttles[0].mode;
			int minDistanceX = Math.Abs(validShuttles[0].shuttle.X - targetCrane.X);
			int minDistanceZ = Math.Abs(validShuttles[0].shuttle.Z - targetCrane.Z);

			for (int i = 1; i < validShuttles.Count; i++)
			{
				var current = validShuttles[i];
				int distX = Math.Abs(current.shuttle.X - targetCrane.X);
				int distZ = Math.Abs(current.shuttle.Z - targetCrane.Z);

				if (distX < minDistanceX)
				{
					minDistanceX = distX;
					minDistanceZ = distZ;
					bestMode = current.mode;
				}
				else if (distX == minDistanceX)
				{
					if (distZ < minDistanceZ)
					{
						minDistanceZ = distZ;
						bestMode = current.mode;
					}
				}
			}

			// (Đã dọn dẹp Console.WriteLine)
			return (short)bestMode;
		}

		//
		// SEND PLC1
		//
		private void SendToPlc1()
		{
			if (!_plc1.IsConnected)
			{
				Console.WriteLine("PLC1 NOT CONNECTED");
				return;
			}

			string cmdAddr = GetCommandAddress();
			Console.WriteLine($"SEND JOB → PLC1 | Cmd={_commandType} ({cmdAddr})");

			// Vị trí hàng hóa
			_plc1.TryWriteInt16(PLC1_XIN,  Xin);
			_plc1.TryWriteInt16(PLC1_ZIN,  Zin);
			_plc1.TryWriteInt16(PLC1_BIN,  Bin);

			_plc1.TryWriteInt16(PLC1_XOUT, Xout);
			_plc1.TryWriteInt16(PLC1_ZOUT, Zout);
			_plc1.TryWriteInt16(PLC1_BOUT, Bout);

			// Lệnh: ghi đúng bit theo loại (Nhập/Xuất/Chuyển)
			_plc1.TryWriteBool(cmdAddr, true);

			// Tính Mode Crane/Shuttle rồi write xuống cả 2 PLC
			short mode = CalculateModeCraneShuttle(1);
			WriteModeCraneShuttle(mode);

			_commandSent = true;
			_targetPlc   = 1;
		}

		//
		// SEND PLC2
		//
		private void SendToPlc2()
		{
			if (!_plc2.IsConnected)
			{
				Console.WriteLine("PLC2 NOT CONNECTED");
				return;
			}

			string cmdAddr = GetCommandAddress();
			Console.WriteLine($"SEND JOB → PLC2 | Cmd={_commandType} ({cmdAddr})");

			// Vị trí hàng hóa
			_plc2.TryWriteInt16(PLC2_XIN,  Xin);
			_plc2.TryWriteInt16(PLC2_ZIN,  Zin);
			_plc2.TryWriteInt16(PLC2_BIN,  Bin);

			_plc2.TryWriteInt16(PLC2_XOUT, Xout);
			_plc2.TryWriteInt16(PLC2_ZOUT, Zout);
			_plc2.TryWriteInt16(PLC2_BOUT, Bout);

			// Lệnh: ghi đúng bit theo loại (Nhập/Xuất/Chuyển)
			_plc2.TryWriteBool(cmdAddr, true);

			// Tính Mode Crane/Shuttle rồi write xuống cả 2 PLC
			short mode = CalculateModeCraneShuttle(2);
			WriteModeCraneShuttle(mode);

			_commandSent = true;
			_targetPlc   = 2;
		}

		//
		// WAIT PLC DONE
		//
		private void CheckComplete()
		{
			string cmdAddr = GetCommandAddress();

			if (_targetPlc == 1)
			{
				// Đọc tín hiệu DONE từ PLC1
				if (_plc1.TryRead<bool>(PLC1_DONE, out bool done1) && done1)
				{
					Console.WriteLine("PLC1 DONE → RESET CMD");
					_plc1.TryWriteBool(cmdAddr, false);
					FinishJob();
				}
			}
			else if (_targetPlc == 2)
			{
				// Đọc tín hiệu DONE từ PLC2
				if (_plc2.TryRead<bool>(PLC2_DONE, out bool done2) && done2)
				{
					Console.WriteLine("PLC2 DONE → RESET CMD");
					_plc2.TryWriteBool(cmdAddr, false);
					FinishJob();
				}
			}
		}

		//
		// FINISH JOB
		//
		private void FinishJob()
		{
			Console.WriteLine("JOB FINISHED");

			_jobActive   = false;
			_commandSent = false;
			_targetPlc   = 0;
			_commandType = 0;

			Xin  = 0;
			Zin  = 0;
			Bin  = 0;

			Xout = 0;
			Zout = 0;
			Bout = 0;

			// Reset Mode_CraneShuttle = 0 xuống cả 2 PLC
			WriteModeCraneShuttle(0);
		}
	}
}