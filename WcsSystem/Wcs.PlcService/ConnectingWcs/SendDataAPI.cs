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
        var path = @"C:\Users\Admin\Desktop\TIN QUANG\16. TRIEN LAM 2026 SECC\5. WCS\WCS-Gateway\TestTools\wcs_monitor.html";

        if (System.IO.File.Exists(path))
        {
            var html = System.IO.File.ReadAllText(path);
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        return NotFound("Không tìm thấy file wcs_monitor.html. Đường dẫn: " + path);
    }

    [HttpGet("/api/status")]
    public IActionResult GetJson()
    {
        return Ok(new
        {
            plc1 = _state.DataPlc1.Connected,
            lastLocation = _state.LastLocation,
            stateSystem = _state.DataPlc1.System,
            currentPositionCrane = new 
            { 
                x = _state.DataPlc1.Crane1.X, 
                z = _state.DataPlc1.Crane1.Z 
            },
            stateRequest = new
            {
                done = _state.DataPlc1.Done,
                fail = _state.DataPlc1.Fail
            }
        });
    }
}