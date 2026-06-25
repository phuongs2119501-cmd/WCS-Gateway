using Wcs.PlcService.DataMappingPlc;

namespace Wcs.PlcService.Models
{
    public class SystemState
    {
        // Data PLC1 mapping
        public DataPlc1 DataPlc1 { get; set; } = new();

        // Data PLC2 mapping
        public DataPlc2 DataPlc2 { get; set; } = new();

        // Data received from WMS
        public ConnectingWcs.ProcessingDataReceive LastLocation { get; set; } = new();
    }
}
