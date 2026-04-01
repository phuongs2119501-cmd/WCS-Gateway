using System.Text;
using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
/* ĐỌC BARCODE TỪ 2 PLC ĐỘC LẬP — DB500
 *
 * ĐỌC BARCODE (14 ký tự):
 *   Char_0  → DB500.DBB32
 *   Char_1  → DB500.DBB33
 *   ...
 *   Char_13 → DB500.DBB45
 *
 * GHI KẾT QUẢ XUỐNG CẢ 2 PLC (song song):
 *   Barcode_OK → DB500.DBX46.0  (Bool)
 *   Barcode_NG → DB500.DBX46.1  (Bool)
 *
 * 2 PLC hoàn toàn độc lập — đọc riêng, ghi riêng
 */
namespace Wcs.PlcService.Services
{
    public class PlcBarcodeReader
    {
        private readonly Plc1Connector _plc1;
        private readonly Plc2Connector _plc2;
        private readonly SystemState   _state;

        //
        // BARCODE — 14 ký tự, DB500 offset 32.0 → 45.0
        //
        private const int    DB_NUMBER      = 500;
        private const int    CHAR_START     = 32;   // DBB32 = Char_0
        private const int    CHAR_COUNT     = 14;   // Char_0 .. Char_13

        //
        // BARCODE RESULT FLAGS (DB500)
        //
        private const string BARCODE_OK = "DB500.DBX46.0";  // Biến barcode hợp lệ
        private const string BARCODE_NG = "DB500.DBX46.1";  // Biến barcode không hợp lệ

        public PlcBarcodeReader(Plc1Connector plc1, Plc2Connector plc2, SystemState state)
        {
            _plc1  = plc1;
            _plc2  = plc2;
            _state = state;
        }

        // Track thay đổi — chỉ log khi barcode khác lần trước
        private string _lastBarcode1 = "";
        private string _lastBarcode2 = "";

        //
        // ĐỌC BARCODE TỪ CẢ 2 PLC — độc lập, song song
        //
        public void ReadAll()
        {
            var b1 = ReadBarcodeFrom(_plc1);
            var b2 = ReadBarcodeFrom(_plc2);

            // Chỉ log khi barcode thay đổi
            if (b1 != _lastBarcode1) { Console.WriteLine($"[BarcodeReader] PLC1 = \"{b1}\""); _lastBarcode1 = b1; }
            if (b2 != _lastBarcode2) { Console.WriteLine($"[BarcodeReader] PLC2 = \"{b2}\""); _lastBarcode2 = b2; }

            _state.Barcode1 = b1;
            _state.Barcode2 = b2;

            // Đọc trạng thái OK, NG từ PLC 1
            if (_plc1.TryRead<bool>(BARCODE_OK, out bool ok1) &&
                _plc1.TryRead<bool>(BARCODE_NG, out bool ng1))
            {
                _state.BarcodeOk1 = ok1;
                _state.BarcodeNg1 = ng1;
            }

            // Đọc trạng thái OK, NG từ PLC 2
            if (_plc2.TryRead<bool>(BARCODE_OK, out bool ok2) &&
                _plc2.TryRead<bool>(BARCODE_NG, out bool ng2))
            {
                _state.BarcodeOk2 = ok2;
                _state.BarcodeNg2 = ng2;
            }
        }

        //
        // GHI BARCODE_OK XUỐNG CẢ 2 PLC — song song
        //
        public void WriteBarcodeOk(bool value)
        {
            _plc1.TryWriteBool(BARCODE_OK, value);
            _plc2.TryWriteBool(BARCODE_OK, value);
            Console.WriteLine($"[BarcodeReader] Write Barcode_OK={value} → PLC1 & PLC2");
        }

        //
        // GHI BARCODE_NG XUỐNG CẢ 2 PLC — song song
        //
        public void WriteBarcodeNg(bool value)
        {
            _plc1.TryWriteBool(BARCODE_NG, value);
            _plc2.TryWriteBool(BARCODE_NG, value);
            Console.WriteLine($"[BarcodeReader] Write Barcode_NG={value} → PLC1 & PLC2");
        }

        //
        // ĐỌC BARCODE TỪ 1 PLC — 14 ký tự liên tiếp từ DB500.DBB32
        //
        private string ReadBarcodeFrom(S7Connector connector)
        {
            var buffer = new char[CHAR_COUNT];

            for (int i = 0; i < CHAR_COUNT; i++)
            {
                string address = $"DB{DB_NUMBER}.DBB{CHAR_START + i}";
                buffer[i] = connector.TryReadChar(address, out char c) ? c : ' ';
            }

            return new string(buffer).Trim();
        }
    }
}