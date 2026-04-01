namespace Wcs.PlcService.Models
{
    public class CraneModel
    {
        public int X { get; set; }
        public int Z { get; set; }

        public bool Busy { get; set; }
        public bool Free { get; set; }
        public bool Error { get; set; }

        public int ErrorCode { get; set; }
    }
}