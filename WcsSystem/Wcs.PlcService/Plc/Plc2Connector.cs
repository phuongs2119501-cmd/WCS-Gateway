using Wcs.PlcService.Models;

namespace Wcs.PlcService.Plc
{
    public class Plc2Connector : S7Connector
    {
        public Plc2Connector(PlcSettings settings)
            : base(settings, "PLC2")
        {
        }
    }
}