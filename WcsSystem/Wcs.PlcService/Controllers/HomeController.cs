using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
using Wcs.PlcService.Services;

namespace Wcs.PlcService.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;
        private readonly Plc1Connector _plc1;
        private readonly Plc2Connector _plc2;
        private readonly SystemState _state;

        public HomeController(
            IConfiguration config,
            Plc1Connector plc1,
            Plc2Connector plc2,
            SystemState state)
        {
            _config = config;
            _plc1 = plc1;
            _plc2 = plc2;
            _state = state;
        }

// Dashboard page logic has been moved to TestTools/wcs_monitor.html

        //
        // 📡 API: Owner-compatible flat status (GET /api/status)
        //   Same SystemState as our nested GET /status, reshaped to the field names WMS already
        //   consumes from main's SendDataAPI, so WCS sees one stable contract across versions.
        //   gateExport / done-fail / directionBlock2 are now bound live (SPEC-GW-007).
        //   FAIL semantics use a documented default (latched, surface-only) pending PLC-owner Q1/Q2.
        //
        [HttpGet("/api/status")]
        public IActionResult Status()
        {
            return Json(new
            {
                plc1 = _plc1.IsConnected,
                plc2 = _plc2.IsConnected,

                barcode1 = _state.Barcode1,
                barcodeOk1 = _state.BarcodeOk1,
                barcodeNg1 = _state.BarcodeNg1,
                gateImport1 = _state.Gate1,
                gateExport1 = _state.GateExport1,
                gate1 = _state.Gate1,

                barcode2 = _state.Barcode2,
                barcodeOk2 = _state.BarcodeOk2,
                barcodeNg2 = _state.BarcodeNg2,
                gateImport2 = _state.Gate2,
                gateExport2 = _state.GateExport2,
                gate2 = _state.Gate2,

                crane1 = _state.Crane1,
                crane2 = _state.Crane2,
                shuttle1 = _state.Shuttle1,
                shuttle2 = _state.Shuttle2,

                system1 = _state.System1,
                system2 = _state.System2,

                done1 = _state.Done1,
                fail1 = _state.Fail1,
                done2 = _state.Done2,
                fail2 = _state.Fail2,

                directionBlock2_Plc1 = _state.DirectionBlock2_1,
                directionBlock2_Plc2 = _state.DirectionBlock2_2
            });
        }
    }
}