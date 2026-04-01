using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.Plc;

namespace Wcs.PlcService.Controllers
{
	[ApiController]
	[Route("api/plc")]
	public class PlcController : ControllerBase
	{
		private readonly Plc1Connector _plc1;

		public PlcController(Plc1Connector plc1)
		{
			_plc1 = plc1;
		}

		//
		// 🔹 Gửi BOOL xuống PLC
		//
		[HttpPost("write-bool")]
		public IActionResult WriteBool(string address, bool value)
		{
			var ok = _plc1.TryWriteBool(address, value);

			return Ok(new
			{
				success = ok,
				address,
				value
			});
		}

		//
		// 🔹 Gửi INT xuống PLC
		//
		[HttpPost("write-int")]
		public IActionResult WriteInt(string address, short value)
		{
			var ok = _plc1.TryWriteInt16(address, value);

			return Ok(new
			{
				success = ok,
				address,
				value
			});
		}
	}
}