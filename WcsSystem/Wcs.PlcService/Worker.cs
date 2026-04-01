using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
using Wcs.PlcService.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Plc1Connector _plc1;
    private readonly Plc2Connector _plc2;
    private readonly PlcBarcodeReader _barcodeReader;
    private readonly SystemState _state;
    private readonly LocationRouter _router;
    private readonly TcpClientService _tcpClient;

    private readonly CraneService _crane;
    private readonly ShuttleService _shuttle;
    private readonly SystemService _system;



    public Worker(
        ILogger<Worker> logger,
        Plc1Connector plc1,
        Plc2Connector plc2,
        PlcBarcodeReader barcodeReader,
        SystemState state,
        LocationRouter router,
        TcpClientService tcpClient,
        CraneService crane,
        ShuttleService shuttle,
        SystemService system)
    {
        _logger = logger;
        _plc1 = plc1;
        _plc2 = plc2;
        _barcodeReader = barcodeReader;
        _state = state;
        _router = router;
        _tcpClient = tcpClient;

        _crane = crane;
        _shuttle = shuttle;
        _system = system;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WCS PLC WORKER STARTED");

        await Task.Delay(500, stoppingToken);

        // TCP connect
        await SafeTcpConnect();

        // Chủ động kết nối PLC khi startup (không chờ TryRead mới connect)
        SafePlcConnect();


        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //
                // 0️⃣ Cập nhật trạng thái kết nối PLC (luôn chạy, kể cả khi chưa connect)
                //
                _state.Plc1Connected = _plc1.IsConnected;
                _state.Plc2Connected = _plc2.IsConnected;

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
                // 4️⃣ Router logic — ghi Mode_CraneShuttle TRƯỚC
                //
                _router.Execute();

                //
                // 5️⃣ Shuttle update — đọc Mode_CraneShuttle SAU khi Router đã ghi
                //
                _shuttle.Update();

                //
                // 6️⃣ Cập nhật lại sau khi các service đã TryRead (IsConnected có thể thay đổi)
                //
                _state.Plc1Connected = _plc1.IsConnected;
                _state.Plc2Connected = _plc2.IsConnected;
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

    private async Task SafeTcpConnect()
    {
        try
        {
            bool ok = await _tcpClient.PingAsync();
            _logger.LogInformation("TCP Ping → {ok}", ok ? "PONG ✓" : "No response");
            _logger.LogInformation("TCP Connected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP connect error");
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