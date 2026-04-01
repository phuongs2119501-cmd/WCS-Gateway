namespace Wcs.PlcService.Models
{
    /// <summary>
    /// Trạng thái hệ thống đọc từ 1 PLC — DB500
    ///   Mode Auto/Manual → DB500.DBX50.0
    ///   Running          → DB500.DBX50.1
    ///   Stop             → DB500.DBX50.2
    ///   Error            → DB500.DBX50.3
    ///   Error Code       → DB500.DBW52  (Int)
    /// </summary>
    public class PlcSystemStateModel
    {
        public bool Auto      { get; set; }   // DB500.DBX50.0
        public bool Running   { get; set; }   // DB500.DBX50.1
        public bool Stop      { get; set; }   // DB500.DBX50.2
        public bool Error     { get; set; }   // DB500.DBX50.3
        public int  ErrorCode { get; set; }   // DB500.DBW52
    }
}
