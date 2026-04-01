namespace Wcs.PlcService.Models
{
	/// <summary>
	/// Loại lệnh gửi xuống PLC:
	///   1 = Req_ImportPallet  (Lệnh Nhập Pallet)  → DB500.DBX0.0
	///   2 = Req_ExportPallet  (Lệnh Xuất Pallet)  → DB500.DBX0.1
	///   3 = Req_TransferPallet(Lệnh Chuyển Pallet) → DB500.DBX0.2
	/// </summary>
	public class LocationModel
	{
		public int CommandType { get; set; }   // 1=Nhập, 2=Xuất, 3=Chuyển

		public int? Xin { get; set; }
		public int? Zin { get; set; }
		public int? Bin { get; set; }

		public int? Xout { get; set; }
		public int? Zout { get; set; }
		public int? Bout { get; set; }
	}
}