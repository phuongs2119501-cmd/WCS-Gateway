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
        public string Barcode2 { get; set; } = "";  // Barcode từ PLC2
        public bool BarcodeOk1 { get; set; }
        public bool BarcodeNg1 { get; set; }
        public bool BarcodeOk2 { get; set; }
        public bool BarcodeNg2 { get; set; }

        // ⚙ System State — đọc độc lập từng PLC
        public PlcSystemStateModel System1 { get; set; } = new();  // Trạng thái từ PLC1
        public PlcSystemStateModel System2 { get; set; } = new();  // Trạng thái từ PLC2

        // 📍 Vị trí gửi xuống gần nhất
        public LocationModel LastLocation { get; set; } = new();
    }
}
