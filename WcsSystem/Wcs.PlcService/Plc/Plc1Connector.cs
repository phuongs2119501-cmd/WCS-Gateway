using Wcs.PlcService.Models;

namespace Wcs.PlcService.Plc
{
    public class Plc1Connector : S7Connector
    {
        public Plc1Connector(PlcSettings settings)
            : base(settings, "PLC1")
        {
        }
    }
}