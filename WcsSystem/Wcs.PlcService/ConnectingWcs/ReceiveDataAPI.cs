using Microsoft.AspNetCore.Mvc;

namespace Wcs.PlcService.ConnectingWcs
{
    [ApiController]
    [Route("api/wms")]
    public class ReceiveDataAPIController : ControllerBase
    {
        private readonly ILogger<ReceiveDataAPIController> _logger;

        public ReceiveDataAPIController(ILogger<ReceiveDataAPIController> logger)
        {
            _logger = logger;
        }

        [HttpPost("send-command")]
        public IActionResult ReceiveCommand([FromBody] ProcessingDataReceive payload)
        {
            if (payload == null)
            {
                _logger.LogWarning("WMS inbound command rejected: payload is null");
                return BadRequest("Payload khong hop le.");
            }

            _logger.LogInformation(
                "WMS inbound command received: CommandGroup={CommandGroup}, CommandType={CommandType}, TargetPlc={TargetPlc}, Xin={Xin}, Zin={Zin}, Bin={Bin}, Xout={Xout}, Zout={Zout}, Bout={Bout}, RequestAutoRun={RequestAutoRun}, RequestStop={RequestStop}, ResetError={ResetError}, BarcodeOk={BarcodeOk}, BarcodeNg={BarcodeNg}",
                payload.CommandGroup,
                payload.CommandType,
                payload.TargetPlc,
                payload.Xin,
                payload.Zin,
                payload.Bin,
                payload.Xout,
                payload.Zout,
                payload.Bout,
                payload.RequestAutoRun,
                payload.RequestStop,
                payload.ResetError,
                payload.BarcodeOk,
                payload.BarcodeNg);

            // Nơi xử lý logic tương lai
            // ...

            return Ok(new
            {
                success = true,
                commandGroup = payload.CommandGroup,
                message = $"Da nhan thanh cong lenh thuoc nhom {payload.CommandGroup}."
            });
        }
    }
}
