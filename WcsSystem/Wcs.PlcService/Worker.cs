using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
using Wcs.PlcService.Services;
using Wcs.PlcService.DataMappingPlc;
using Wcs.PlcService.ConnectingWcs;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Plc1Connector _plc1;
    private readonly Plc2Connector _plc2;
    private readonly PlcBarcodeReader _barcodeReader;
    private readonly SystemState _state;


    private readonly CraneService _crane;
    private readonly ShuttleService _shuttle;
    private readonly SystemService _system;



    public Worker(
        ILogger<Worker> logger,
        Plc1Connector plc1,
        Plc2Connector plc2,
        PlcBarcodeReader barcodeReader,
        SystemState state,

        CraneService crane,
        ShuttleService shuttle,
        SystemService system)
    {
        _logger = logger;
        _plc1 = plc1;
        _plc2 = plc2;
        _barcodeReader = barcodeReader;
        _state = state;


        _crane = crane;
        _shuttle = shuttle;
        _system = system;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WCS PLC WORKER STARTED");

        await Task.Delay(500, stoppingToken);

        // Chủ động kết nối PLC khi startup (không block thread chạy vòng quét)
        _ = Task.Run(() => SafePlcConnect(), stoppingToken);




        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //
                // 0️⃣ Cập nhật trạng thái kết nối PLC (luôn chạy, kể cả khi chưa connect)
                //
                _state.DataPlc1.Connected = _plc1.IsConnected;
                _state.DataPlc2.Connected = _plc2.IsConnected;

                // Ghi bit báo WCS (PC) sống / kết nối thành công xuống PLC liên tục để làm Heartbeat
                if (_plc1.IsConnected) _plc1.TryWriteBool(DataPlc1.WCS_HEARTBEAT, true);
                if (_plc2.IsConnected) _plc2.TryWriteBool(DataPlc2.WCS_HEARTBEAT, true);

                //
                // 1️⃣ System update
                //
                _system.Update();

                //
                // 2️⃣ Crane update
                //
                _crane.Update();

                //
                // 3️⃣ Barcode read
                //
                ReadBarcode();

                //
                // 5️⃣ Shuttle update
                //
                _shuttle.Update();

                //
                // 6️⃣ Cập nhật lại sau khi các service đã TryRead (IsConnected có thể thay đổi)
                //
                _state.DataPlc1.Connected = _plc1.IsConnected;
                _state.DataPlc2.Connected = _plc2.IsConnected;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
            }

            await Task.Delay(50, stoppingToken);
        }
    }

    private void ReadBarcode()
    {
        try
        {
            // Đọc độc lập từng PLC — kết quả lưu vào _state.Barcode1 / Barcode2
            _barcodeReader.ReadAll();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barcode read error");
        }
    }



    private void SafePlcConnect()
    {
        try
        {
            _plc1.Connect();
            _logger.LogInformation("PLC1 connect attempted → IsConnected={v}", _plc1.IsConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PLC1 connect error");
        }

        try
        {
            _plc2.Connect();
            _logger.LogInformation("PLC2 connect attempted → IsConnected={v}", _plc2.IsConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PLC2 connect error");
        }
    }
}