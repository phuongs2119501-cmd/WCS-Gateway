using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.ConnectingWcs;

namespace Wcs.PlcService.ConnectingWcs
{
    [ApiController]
    [Route("api/wms")]
    public class WmsCommandController : ControllerBase
    {
        [HttpPost("send-command")]
        public IActionResult ReceiveCommand([FromBody] WmsCommandModel payload)
        {
            if (payload == null)
            {
                return BadRequest("Payload không hợp lệ.");
            }

            // TẠI ĐÂY: Dữ liệu đã được hứng thành công.
            // (Tương lai bạn sẽ móc dữ liệu này truyền vào Hàng đợi Queue hoặc Logic Router ở đây)
            // ...

            return Ok(new { 
                success = true, 
                message = $"Đã nhận thành công lệnh thuộc nhóm {payload.CommandGroup}. Chờ xử lý logic." 
            });
        }
    }
}
