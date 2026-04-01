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

        //
        // 📊 DASHBOARD PAGE
        //
        public IActionResult Index()
        {
            return View();
        }

        //
        // 📡 API: Dashboard Status
        //
        [HttpGet("/api/status")]
        public IActionResult Status()
        {
            return Json(new
            {
                plc1 = _state.Plc1Connected,
                plc2 = _state.Plc2Connected,

                barcode1 = _state.Barcode1,
                barcode2 = _state.Barcode2,

                crane1 = new
                {
                    x = _state.Crane1.X,
                    z = _state.Crane1.Z,
                    busy = _state.Crane1.Busy,
                    free = _state.Crane1.Free,
                    error = _state.Crane1.Error,
                    errorCode = _state.Crane1.ErrorCode
                },
                crane2 = new
                {
                    x = _state.Crane2.X,
                    z = _state.Crane2.Z,
                    busy = _state.Crane2.Busy,
                    free = _state.Crane2.Free,
                    error = _state.Crane2.Error,
                    errorCode = _state.Crane2.ErrorCode
                },
                shuttle1 = new
                {
                    x = _state.Shuttle1.X,
                    z = _state.Shuttle1.Z,
                    b = _state.Shuttle1.B,
                    busy = _state.Shuttle1.Busy,
                    free = _state.Shuttle1.Free,
                    error = _state.Shuttle1.Error,
                    errorCode = _state.Shuttle1.ErrorCode
                },
                shuttle2 = new
                {
                    x = _state.Shuttle2.X,
                    z = _state.Shuttle2.Z,
                    b = _state.Shuttle2.B,
                    busy = _state.Shuttle2.Busy,
                    free = _state.Shuttle2.Free,
                    error = _state.Shuttle2.Error,
                    errorCode = _state.Shuttle2.ErrorCode
                }
            });
        }
    }
}