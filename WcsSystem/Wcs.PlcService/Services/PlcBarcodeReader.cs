using System.Text;
using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
using Wcs.PlcService.DataMappingPlc;

namespace Wcs.PlcService.Services
{
    public class PlcBarcodeReader
    {
        private readonly Plc1Connector _plc1;
        private readonly Plc2Connector _plc2;
        private readonly SystemState   _state;

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
            var b1 = _state.DataPlc1.ReadBarcode(_plc1);
            var b2 = _state.DataPlc2.ReadBarcode(_plc2);

            // Đọc thêm mã Gate hiện tại (Import & Export)
            _plc1.TryRead<ushort>(DataPlc1.GATE_IMPORT, out ushort gateImport1);
            _plc1.TryRead<ushort>(DataPlc1.GATE_EXPORT, out ushort gateExport1);
            _plc2.TryRead<ushort>(DataPlc2.GATE_IMPORT, out ushort gateImport2);
            _plc2.TryRead<ushort>(DataPlc2.GATE_EXPORT, out ushort gateExport2);

            // Chỉ log khi barcode thay đổi
            if (b1 != _lastBarcode1) { Console.WriteLine($"[BarcodeReader] PLC1 = \"{b1}\" (Gate Import {gateImport1}, Export {gateExport1})"); _lastBarcode1 = b1; }
            if (b2 != _lastBarcode2) { Console.WriteLine($"[BarcodeReader] PLC2 = \"{b2}\" (Gate Import {gateImport2}, Export {gateExport2})"); _lastBarcode2 = b2; }

            _state.DataPlc1.Barcode = b1;
            _state.DataPlc1.GateImport = gateImport1;
            _state.DataPlc1.GateExport = gateExport1;

            _state.DataPlc2.Barcode = b2;
            _state.DataPlc2.GateImport = gateImport2;
            _state.DataPlc2.GateExport = gateExport2;

            // Đọc trạng thái OK, NG từ PLC 1
            if (_plc1.TryRead<bool>(DataPlc1.BARCODE_OK, out bool ok1) &&
                _plc1.TryRead<bool>(DataPlc1.BARCODE_NG, out bool ng1))
            {
                _state.DataPlc1.BarcodeOk = ok1;
                _state.DataPlc1.BarcodeNg = ng1;
            }

            // Đọc trạng thái OK, NG từ PLC 2
            if (_plc2.TryRead<bool>(DataPlc2.BARCODE_OK, out bool ok2) &&
                _plc2.TryRead<bool>(DataPlc2.BARCODE_NG, out bool ng2))
            {
                _state.DataPlc2.BarcodeOk = ok2;
                _state.DataPlc2.BarcodeNg = ng2;
            }
        }

        //
        // GHI BARCODE_OK XUỐNG CẢ 2 PLC — song song
        //
        public void WriteBarcodeOk(bool value)
        {
            _plc1.TryWriteBool(DataPlc1.BARCODE_OK, value);
            _plc2.TryWriteBool(DataPlc2.BARCODE_OK, value);
            Console.WriteLine($"[BarcodeReader] Write Barcode_OK={value} → PLC1 & PLC2");
        }

        //
        // GHI BARCODE_NG XUỐNG CẢ 2 PLC — song song
        //
        public void WriteBarcodeNg(bool value)
        {
            _plc1.TryWriteBool(DataPlc1.BARCODE_NG, value);
            _plc2.TryWriteBool(DataPlc2.BARCODE_NG, value);
            Console.WriteLine($"[BarcodeReader] Write Barcode_NG={value} → PLC1 & PLC2");
        }
    }
}