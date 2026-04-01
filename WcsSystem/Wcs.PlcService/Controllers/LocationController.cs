using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.Models;
using Wcs.PlcService.Services;

namespace Wcs.PlcService.Controllers
{
	[ApiController]
	[Route("api/location")]
	public class LocationController : ControllerBase
	{
		private readonly LocationRouter _router;

		public LocationController(LocationRouter router)
		{
			_router = router;
		}

		[HttpPost("send")]
		public IActionResult SendLocation([FromBody] LocationModel location)
		{
			// (Đã dọn dẹp các dòng Console.WriteLine báo tọa độ)

			// gửi data vào router
			_router.SetLocation(location);

			return Ok();
		}
	}
}