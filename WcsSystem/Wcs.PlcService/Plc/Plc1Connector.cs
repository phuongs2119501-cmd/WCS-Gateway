using Wcs.PlcService.Models;

namespace Wcs.PlcService.Plc
{
    public class Plc1Connector : S7Connector
    {
        public Plc1Connector(PlcSettings settings)
            : base(settings, "PLC1")
        {
        }

        public Plc1Connector(PlcSettings settings, IS7Backend backend)
            : base(settings, backend, "PLC1")
        {
        }
    }
}
