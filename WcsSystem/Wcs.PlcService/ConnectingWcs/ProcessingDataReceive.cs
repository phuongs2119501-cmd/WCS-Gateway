namespace Wcs.PlcService.ConnectingWcs
{
    /// <summary>
    /// Khuon mau du lieu duy nhat de WMS gui lenh xuong WCS.
    /// WMS chi can truyen cac truong lien quan den nhom lenh muon thuc thi.
    /// (Da duoc gop tu WmsCommandModel sang)
    /// </summary>
    public class ProcessingDataReceive
    {
        // Phan loai nhom lenh: "MoveTask", "SystemControl", "BarcodeResult".
        public string CommandGroup { get; set; } = "MoveTask";

        // 1. Nhom lenh gap/tha hang.
        public int? CommandType { get; set; } // 1: Import, 2: Export, 3: Transfer
        public int? TargetPlc { get; set; }   // 1: PLC1, 2: PLC2

        public short? Xin { get; set; }
        public short? Zin { get; set; }
        public short? Bin { get; set; }

        public short? Xout { get; set; }
        public short? Zout { get; set; }
        public short? Bout { get; set; }

        // 2. Nhom lenh he thong.
        public bool? RequestAutoRun { get; set; }
        public bool? RequestStop { get; set; }
        public bool? ResetError { get; set; }

        // 3. Nhom phan hoi ma vach.
        public bool? BarcodeOk { get; set; }
        public bool? BarcodeNg { get; set; }
    }
}
