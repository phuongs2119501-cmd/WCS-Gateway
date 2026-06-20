using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;

namespace Wcs.PlcService.DataMappingPlc
{
    public class DataPlc2
    {
        // ==========================================
        // DB500 PLC2 ADDRESS CONSTANTS
        // ==========================================

        // Commands (DB500, same address offset for both, but defined here for PLC2 context)
        public const string REQ_IMPORT_PALLET   = "DB500.DBX0.0";
        public const string REQ_EXPORT_PALLET   = "DB500.DBX0.1";
        public const string REQ_TRANSFER_PALLET = "DB500.DBX0.2";
        public const string REQ_AUTORUN_SYSTEM  = "DB500.DBX0.3";
        public const string REQ_STOP_SYSTEM     = "DB500.DBX0.4";
        public const string MODE_CRANE_SHUTTLE   = "DB500.DBW2";

        // Coordinates Target Input
        public const string XIN  = "DB500.DBW4";
        public const string ZIN  = "DB500.DBW6";
        public const string BIN  = "DB500.DBW8";

        // Coordinates Target Output
        public const string XOUT = "DB500.DBW10";
        public const string ZOUT = "DB500.DBW12";
        public const string BOUT = "DB500.DBW14";

        // Crane 1 State
        public const string CRANE1_FREE       = "DB500.DBX16.0";
        public const string CRANE1_BUSY       = "DB500.DBX16.1";
        public const string CRANE1_ERROR      = "DB500.DBX16.2";
        public const string CRANE1_ERROR_CODE = "DB500.DBW18";

        // Crane 2 State
        public const string CRANE2_FREE       = "DB500.DBX20.0";
        public const string CRANE2_BUSY       = "DB500.DBX20.1";
        public const string CRANE2_ERROR      = "DB500.DBX20.2";
        public const string CRANE2_ERROR_CODE = "DB500.DBW22";

        // Shuttle 1 State
        public const string SHUTTLE1_FREE       = "DB500.DBX24.0";
        public const string SHUTTLE1_BUSY       = "DB500.DBX24.1";
        public const string SHUTTLE1_ERROR      = "DB500.DBX24.2";
        public const string SHUTTLE1_ERROR_CODE = "DB500.DBW26";
        public const string SHUTTLE1_PIN        = "DB500.DBW28";

        // Shuttle 2 State
        public const string SHUTTLE2_FREE       = "DB500.DBX30.0";
        public const string SHUTTLE2_BUSY       = "DB500.DBX30.1";
        public const string SHUTTLE2_ERROR      = "DB500.DBX30.2";
        public const string SHUTTLE2_ERROR_CODE = "DB500.DBW32";
        public const string SHUTTLE2_PIN        = "DB500.DBW34";

        // Barcode Reading
        public const int    DB_NUMBER      = 500;
        public const int    CHAR_START     = 36;   // DBB36 = Char_0
        public const int    CHAR_COUNT     = 14;   // Char_0 .. Char_13
        public const string BARCODE_OK     = "DB500.DBX50.0";
        public const string BARCODE_NG     = "DB500.DBX50.1";

        // State Result
        public const string PLC2_DONE      = "DB500.DBX52.0";
        public const string PLC2_FAIL      = "DB500.DBX52.1";

        // System 2 State
        public const string AUTO_MODE  = "DB500.DBX54.0";
        public const string RUNNING    = "DB500.DBX54.1";
        public const string STOP       = "DB500.DBX54.2";
        public const string ERROR      = "DB500.DBX54.3";
        public const string ERROR_CODE = "DB500.DBW56";

        // Crane 1 Position
        public const string XCRANE1 = "DB500.DBW58";
        public const string ZCRANE1 = "DB500.DBW60";

        // Crane 2 Position
        public const string XCRANE2 = "DB500.DBW62";
        public const string ZCRANE2 = "DB500.DBW64";

        // Shuttle 1 Position
        public const string XSHUTTLE1 = "DB500.DBW66";
        public const string ZSHUTTLE1 = "DB500.DBW68";
        public const string BSHUTTLE1 = "DB500.DBW70";

        // Shuttle 2 Position
        public const string XSHUTTLE2 = "DB500.DBW72";
        public const string ZSHUTTLE2 = "DB500.DBW74";
        public const string BSHUTTLE2 = "DB500.DBW76";

        // Gate Mappings
        public const string GATE_IMPORT = "DB500.DBW78";
        public const string GATE_EXPORT = "DB500.DBW80";

        // Heartbeat / Lifebit WCS
        public const string WCS_HEARTBEAT  = "DB500.DBX82.0";

        // Direction Block 2
        public const string DIRECTION_BLOCK2 = "DB500.DBW84";


        // ==========================================
        // PLC2 RUNTIME STATE VARIABLES
        // ==========================================

        public bool Connected { get; set; }

        public CraneModel Crane1 { get; set; } = new();
        public CraneModel Crane2 { get; set; } = new();

        public ShuttleModel Shuttle1 { get; set; } = new();
        public ShuttleModel Shuttle2 { get; set; } = new();

        // Legacy compatibility
        public CraneModel Crane { get => Crane1; set => Crane1 = value; }
        public ShuttleModel Shuttle { get => Shuttle1; set => Shuttle1 = value; }

        public string Barcode { get; set; } = "";

        // Backward compatibility
        public ushort Gate { get => GateImport; set => GateImport = value; }

        public ushort GateImport { get; set; }
        public ushort GateExport { get; set; }

        public bool BarcodeOk { get; set; }

        public bool BarcodeNg { get; set; }

        public bool Done { get; set; }
        public bool Fail { get; set; }

        public ushort DirectionBlock2 { get; set; }

        public PlcSystemStateModel System { get; set; } = new();

        public string ReadBarcode(S7Connector connector)
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
