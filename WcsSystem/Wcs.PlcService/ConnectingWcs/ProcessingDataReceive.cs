namespace Wcs.PlcService.ConnectingWcs
{
    /// <summary>
    /// Khuôn mẫu dữ liệu duy nhất để WMS gửi lệnh xuống WCS.
    /// WMS chỉ cần truyền các trường liên quan đến nhóm lệnh muốn thực thi.
    /// </summary>
    public class WmsCommandModel
    {
        // Phân loại nhóm lệnh: "MoveTask" (Di chuyển), "SystemControl" (Hệ thống), "BarcodeResult" (Mã vạch)
        public string CommandGroup { get; set; } = "MoveTask"; 

        // ==========================================
        // 1. NHÓM LỆNH GẮP/THẢ HÀNG (MOVE TASK)
        // ==========================================
        public int? CommandType { get; set; } // 1: Import, 2: Export, 3: Transfer
        public int? TargetPlc { get; set; }   // 1: Gửi lệnh cho PLC1, 2: Gửi lệnh cho PLC2 (nếu WMS muốn chỉ định cứng)
        
        // Tọa độ nguồn
        public short? Xin { get; set; }
        public short? Zin { get; set; }
        public short? Bin { get; set; }
        
        // Tọa độ đích
        public short? Xout { get; set; }
        public short? Zout { get; set; }
        public short? Bout { get; set; }

        // ==========================================
        // 2. NHÓM LỆNH HỆ THỐNG (SYSTEM CONTROL)
        // ==========================================
        public bool? RequestAutoRun { get; set; }
        public bool? RequestStop { get; set; }
        public bool? ResetError { get; set; }

        // ==========================================
        // 3. NHÓM PHẢN HỒI MÃ VẠCH (BARCODE RESULT)
        // ==========================================
        // Khi WCS gửi mã vạch lên, WMS tra DB. 
        // Nếu đúng hàng, WMS gửi BarcodeOk = true. Sai gửi BarcodeNg = true.
        public bool? BarcodeOk { get; set; }
        public bool? BarcodeNg { get; set; }
    }
}
