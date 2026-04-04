# Hệ thống điều khiển kho hàng WCS kết nối qua TcpSocket (WCS Project)

Đây là hệ thống kết nối PLC cho kho tự động (WCS), được phát triển bằng ASP.NET Core 8 Web API và chạy ngầm như một Background Service.

## 1. Cấu trúc thư mục

- `WcsSystem/`
  Chứa toàn bộ mã nguồn Backend C# (Giao tiếp PLC S7, API trạng thái, Background Services điều phối logic Crane/Shuttle/Barcode).
  Dự án đã được loại bỏ hoàn toàn View MVC thừa thãi nên cấu trúc cực kỳ tinh gọn và chỉ làm những nhiệm vụ thuần túy của API & Background Worker.

- `TestTools/`
  Nơi chứa công cụ UI độc lập (`wcs_monitor.html`) dùng để kiểm thử hệ thống. Giao diện này sẽ gọi ngầm REST API `http://localhost:5000/api/status` mỗi 1 giây để đổ dữ liệu ra màn hình HTML.

- `TcpSocket/`
  Chứa một socket thử nghiệm liên quan (nếu vẫn cần giữ).

## 2. Cách chạy Backend System

1. Mở Terminal / PowerShell hoặc phần mềm Visual Studio, trỏ vào thư mục `WcsSystem/Wcs.PlcService/`.
2. Chạy lệnh:
   ```bash
   dotnet run
   ```
   Hoặc chạy file `WcsSystem.sln` trên Visual Studio và nhấn F5.
3. Backend lúc này sẽ khởi động ở địa chỉ `http://localhost:5000` hoặc `http://0.0.0.0:5000`. Cửa sổ log terminal sẽ hiện log trạng thái kết nối PLC.

## 3. Cách chạy công cụ Test (Monitor UI)

1. Mở trực tiếp file `TestTools/wcs_monitor.html` bằng bất kỳ trình duyệt nào (Chrome, Edge,...). Ngay lập tức UI sẽ hiển thị giao diện Dark mode và hiện các thông số thực từ PLC về thông qua việc kết nối API `http://localhost:5000`.
2. Từ lúc này, bạn không bị bó buộc với ASP.NET Core MVC (không cần reload/build lại layout). Thay đổi HTML và reload web trên trình duyệt sẽ có tác dụng tức thì, giúp tiết kiệm thời gian test.

