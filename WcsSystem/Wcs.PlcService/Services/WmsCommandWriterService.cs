using Wcs.PlcService.ConnectingWcs;
using Wcs.PlcService.Plc;

namespace Wcs.PlcService.Services
{
    public class WmsCommandWriterService
    {
        private readonly Plc1Connector _plc1;

        public WmsCommandWriterService(Plc1Connector plc1)
        {
            _plc1 = plc1;
        }

        public void WriteCommandToPlc(ProcessingDataReceive payload)
        {
            try
            {
                if (payload.CommandType.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.CMD_TYPE, payload.CommandType.Value);
                
                if (payload.Xin.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.XIN, payload.Xin.Value);
                if (payload.Zin.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.ZIN, payload.Zin.Value);
                if (payload.Bin.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.BIN, payload.Bin.Value);
                
                if (payload.Xout.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.XOUT, payload.Xout.Value);
                if (payload.Zout.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.ZOUT, payload.Zout.Value);
                if (payload.Bout.HasValue) _plc1.TryWriteInt16(Wcs.PlcService.DataMappingPlc.DataPlc1.BOUT, payload.Bout.Value);
            }
            catch (Exception)
            {
                // Xử lý lỗi nếu cần
            }
        }
    }
}
