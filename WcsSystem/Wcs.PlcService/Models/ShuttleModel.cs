/* HIỂN THỊ LÊN GIAO DIỆN
 * ...
 * ...
 * ..
 * ..
 * ..
 * .
 * .
 * .
 * .
 * .
 * .
 * .
 */
namespace Wcs.PlcService.Models
{
	public class ShuttleModel
	{
		public int X { get; set; }
		public int Z { get; set; }
		public int B { get; set; }

		public bool Busy { get; set; }
		public bool Free { get; set; }
		public bool Error { get; set; }

		public int ErrorCode { get; set; }
		public int Pin { get; set; }       // Battery (DB500.DBW24 / DBW30)
	}
}