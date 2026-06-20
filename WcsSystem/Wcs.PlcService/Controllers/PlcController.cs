using Microsoft.AspNetCore.Mvc;
using Wcs.PlcService.Plc;

namespace Wcs.PlcService.Controllers
{
	[ApiController]
	[Route("api/plc")]
	public class PlcController : ControllerBase
	{
		private readonly Plc1Connector _plc1;
		private readonly Plc2Connector _plc2;

		public PlcController(Plc1Connector plc1, Plc2Connector plc2)
		{
			_plc1 = plc1;
			_plc2 = plc2;
		}

		//
		// 🔹 Gửi BOOL xuống PLC (plcNumber = 1 hoặc 2)
		//
		[HttpPost("write-bool")]
		public IActionResult WriteBool(string address, bool value, int plcNumber = 1)
		{
			bool ok;
			if (plcNumber == 2)
			{
				ok = _plc2.TryWriteBool(address, value);
			}
			else
			{
				ok = _plc1.TryWriteBool(address, value);
			}

			return Ok(new
			{
				success = ok,
				plcNumber,
				address,
				value
			});
		}

		//
		// 🔹 Gửi INT xuống PLC (plcNumber = 1 hoặc 2)
		//
		[HttpPost("write-int")]
		public IActionResult WriteInt(string address, short value, int plcNumber = 1)
		{
			bool ok;
			if (plcNumber == 2)
			{
				ok = _plc2.TryWriteInt16(address, value);
			}
			else
			{
				ok = _plc1.TryWriteInt16(address, value);
			}

			return Ok(new
			{
				success = ok,
				plcNumber,
				address,
				value
			});
		}
	}
}