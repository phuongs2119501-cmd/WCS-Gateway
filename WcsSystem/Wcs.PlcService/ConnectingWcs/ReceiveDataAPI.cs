using Microsoft.AspNetCore.Mvc;

namespace Wcs.PlcService.ConnectingWcs
{
    [ApiController]
    [Route("api/wms")]
    public class ReceiveDataAPIController : ControllerBase
    {
        private readonly Wcs.PlcService.Services.WmsCommandWriterService _wmsWriter;

        public ReceiveDataAPIController(
            Wcs.PlcService.Services.WmsCommandWriterService wmsWriter)
        {
            _wmsWriter = wmsWriter;
        }

        [HttpPost("send-command")]
        public IActionResult ReceiveCommand([FromBody] ProcessingDataReceive payload)
        {
            if (payload == null)
            {
                return BadRequest("Payload khong hop le.");
            }

            // Truyền dữ liệu sang Service để ghi xuống PLC
            _wmsWriter.WriteCommandToPlc(payload);

            return Ok(new
            {
                success = true,
                data = payload
            });
        }
    }
}
