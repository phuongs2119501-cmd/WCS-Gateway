using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.Services;
using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;

namespace Wcs.PlcService.ConnectingWcs;

public class WcsStatusSendController : ControllerBase
{
    private readonly SystemState _state;
    private readonly Plc1Connector _plc1;
    private readonly Plc2Connector _plc2;

    public WcsStatusSendController(SystemState state, Plc1Connector plc1, Plc2Connector plc2)
    {
        _state = state;
        _plc1 = plc1;
        _plc2 = plc2;
    }

    // 🌐 Serve the beautiful HTML UI
    [HttpGet("/")]
    [HttpGet("/status")]
    public IActionResult GetHtml()
    {
        // Đọc thẳng từ file gốc, tránh bị phục vụ file cũ trong bin/Debug
        var path = "C:\\Users\\Admin\\Desktop\\TIN QUANG\\6. SEAPREMEXCO\\11.WCS\\TestTools\\wcs_monitor.html";

        if (System.IO.File.Exists(path))
        {
            var html = System.IO.File.ReadAllText(path);
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        return NotFound("Không tìm thấy file wcs_monitor.html. Đường dẫn: " + path);
    }

    // 📡 API returns the real-time PLC system state JSON
    [HttpGet("/api/status")]
    public IActionResult GetJson()
    {
        return Ok(new
        {
            plc1 = _state.DataPlc1.Connected,
            plc2 = _state.DataPlc2.Connected,
            barcode1 = _state.DataPlc1.Barcode,
            barcodeOk1 = _state.DataPlc1.BarcodeOk,
            barcodeNg1 = _state.DataPlc1.BarcodeNg,
            gateImport1 = _state.DataPlc1.GateImport,
            gateExport1 = _state.DataPlc1.GateExport,
            gate1       = _state.DataPlc1.GateImport,
            barcode2 = _state.DataPlc2.Barcode,
            barcodeOk2 = _state.DataPlc2.BarcodeOk,
            barcodeNg2 = _state.DataPlc2.BarcodeNg,
            gateImport2 = _state.DataPlc2.GateImport,
            gateExport2 = _state.DataPlc2.GateExport,
            gate2       = _state.DataPlc2.GateImport,
            crane1   = _state.DataPlc1.Crane1,
            crane2   = _state.DataPlc1.Crane2,
            shuttle1 = _state.DataPlc1.Shuttle1,
            shuttle2 = _state.DataPlc1.Shuttle2,
            system1  = _state.DataPlc1.System,   // Trạng thái hệ thống PLC1
            system2  = _state.DataPlc2.System,    // Trạng thái hệ thống PLC2
            done1    = _state.DataPlc1.Done,
            fail1    = _state.DataPlc1.Fail,
            done2    = _state.DataPlc2.Done,
            fail2    = _state.DataPlc2.Fail,
            directionBlock2_Plc1 = _state.DataPlc1.DirectionBlock2,
            directionBlock2_Plc2 = _state.DataPlc2.DirectionBlock2
        });
    }
}