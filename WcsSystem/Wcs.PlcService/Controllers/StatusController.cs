using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.Services;
using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;

namespace Wcs.PlcService.Controllers;

[ApiController]
[Route("status")]
public class StatusController : Controller
{
    private readonly SystemState _state;
    private readonly Plc1Connector _plc1;
    private readonly Plc2Connector _plc2;

    public StatusController(SystemState state, Plc1Connector plc1, Plc2Connector plc2)
    {
        _state = state;
        _plc1 = plc1;
        _plc2 = plc2;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            plc1 = _plc1.IsConnected,
            plc2 = _plc2.IsConnected,
            barcode1 = _state.Barcode1,
            barcodeOk1 = _state.BarcodeOk1,
            barcodeNg1 = _state.BarcodeNg1,
            barcode2 = _state.Barcode2,
            barcodeOk2 = _state.BarcodeOk2,
            barcodeNg2 = _state.BarcodeNg2,
            crane1   = _state.Crane1,
            crane2   = _state.Crane2,
            shuttle1 = _state.Shuttle1,
            shuttle2 = _state.Shuttle2,
            system1  = _state.System1,   // Trạng thái hệ thống PLC1
            system2  = _state.System2,    // Trạng thái hệ thống PLC2
            lastLocation = _state.LastLocation,
            modeCraneShuttle = _state.ModeCraneShuttle
        });
    }
}