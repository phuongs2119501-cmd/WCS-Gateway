using S7.Net;

namespace Wcs.PlcService.Models
{
    public class PlcSettings
    {
        public string IpAddress { get; set; } = string.Empty;

        // Đổi int -> short để khỏi lỗi convert
        public short Rack { get; set; }
        public short Slot { get; set; }

        // Thêm CpuType để phân biệt 1200 / 1500
        public CpuType CpuType { get; set; }

        // UseMock=true: chay fake-PLC DB500 trong RAM, khong ket noi PLC that
        public bool UseMock { get; set; }
    }
}
