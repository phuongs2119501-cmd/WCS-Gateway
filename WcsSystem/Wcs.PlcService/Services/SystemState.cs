namespace Wcs.PlcService.Models
{
    public class SystemState
    {
        public bool Plc1Connected { get; set; }
        public bool Plc2Connected { get; set; }

        // Mode Crane Shuttle: 0=chưa gửi, 1=gửi PLC1, 2=gửi PLC2
        public int ModeCraneShuttle { get; set; } = 0;

        // 🏗 Crane
        public CraneModel Crane1 { get; set; } = new();
        public CraneModel Crane2 { get; set; } = new();

        // 🚗 Shuttle
        public ShuttleModel Shuttle1 { get; set; } = new();
        public ShuttleModel Shuttle2 { get; set; } = new();

        // 📦 Barcode — đọc độc lập từng PLC
        public string Barcode1 { get; set; } = "";  // Barcode từ PLC1
        public ushort Gate1 { get; set; }           // Cổng Import từ PLC1 (DBW78)
        public ushort GateExport1 { get; set; }     // Cổng Export từ PLC1 (DBW80)

        public string Barcode2 { get; set; } = "";  // Barcode từ PLC2
        public ushort Gate2 { get; set; }           // Cổng Import từ PLC2 (DBW78)
        public ushort GateExport2 { get; set; }     // Cổng Export từ PLC2 (DBW80)

        public bool BarcodeOk1 { get; set; }
        public bool BarcodeNg1 { get; set; }
        public bool BarcodeOk2 { get; set; }
        public bool BarcodeNg2 { get; set; }

        // ⚙ System State — đọc độc lập từng PLC
        public PlcSystemStateModel System1 { get; set; } = new();  // Trạng thái từ PLC1
        public PlcSystemStateModel System2 { get; set; } = new();  // Trạng thái từ PLC2

        // ✅ Tín hiệu hoàn thành job — đọc từng PLC (DBX52.0 / DBX52.1)
        public bool Done1 { get; set; }
        public bool Fail1 { get; set; }
        public bool Done2 { get; set; }
        public bool Fail2 { get; set; }

        // 🧭 Direction Block 2 — đọc từng PLC (DBW84)
        public int DirectionBlock2_1 { get; set; }
        public int DirectionBlock2_2 { get; set; }

        // 📍 Vị trí gửi xuống gần nhất
        public LocationModel LastLocation { get; set; } = new();
    }
}
