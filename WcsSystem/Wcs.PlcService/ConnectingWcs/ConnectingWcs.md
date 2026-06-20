# Tài liệu Giao thức Kết nối & Trao đổi Dữ liệu với WCS

Tài liệu này hướng dẫn chi tiết cách hệ thống WCS (Warehouse Control System) cấp trên trao đổi dữ liệu với dịch vụ giám sát PLC hiện tại thông qua mô hình **Cổng Giao Tiếp (API Gateway)**. Toàn bộ các thao tác trao đổi dữ liệu do WMS chủ động gọi xuống WCS.

---

## 1. Lấy Trạng Thái Từ WCS (Status Outward)

WMS hoặc hệ thống giám sát UI có thể gửi yêu cầu truy vấn đến API để lấy toàn bộ trạng thái hệ thống theo thời gian thực.

- **Địa chỉ Endpoint**: `http://localhost:5000/api/status` (Hoặc địa chỉ IP chạy dịch vụ)
- **Phương thức truy vấn**: `HTTP GET`
- **Định dạng phản hồi**: `application/json`

### Cấu trúc JSON Phản hồi (Payload Schema)
Cấu trúc JSON mô tả toàn bộ bản đồ RAM của PLC sẽ trả về khi WMS gọi API `/api/status`:

```json
{
  "plc1": true,                 // Trạng thái kết nối của PLC 1 (true = Đang kết nối, false = Mất kết nối)
  "plc2": false,                // Trạng thái kết nối của PLC 2 (true = Đang kết nối, false = Mất kết nối)
  "barcode1": "1234",           // Mã vạch hiện tại đọc từ PLC 1 (hoặc "---" nếu không có)
  "barcodeOk1": false,          // Cờ báo đọc Barcode thành công từ PLC 1 (OK)
  "barcodeNg1": false,          // Cờ báo đọc Barcode thất bại từ PLC 1 (NG)
  "gateImport1": 0,             // Trạng thái cổng nhập PLC 1
  "gateExport1": 0,             // Trạng thái cổng xuất PLC 1
  "gate1": 0,                   // Trường tương thích ngược tương đương gateImport1
  "barcode2": "---",            // Mã vạch hiện tại đọc từ PLC 2
  "barcodeOk2": false,          // Cờ báo đọc Barcode thành công từ PLC 2 (OK)
  "barcodeNg2": false,          // Cờ báo đọc Barcode thất bại từ PLC 2 (NG)
  "gateImport2": 0,             // Trạng thái cổng nhập PLC 2
  "gateExport2": 0,             // Trạng thái cổng xuất PLC 2
  "gate2": 0,                   // Trường tương thích ngược tương đương gateImport2
  
  "crane1": {                   // Trạng thái cẩu trục 1 (PLC 1)
    "x": 0,                     // Tọa độ X hiện tại
    "z": 0,                     // Tọa độ Z hiện tại
    "busy": false,              // Trạng thái đang hoạt động (Bận)
    "free": true,               // Trạng thái đang rảnh (Sẵn sàng nhận lệnh)
    "error": false,             // Trạng thái báo lỗi thiết bị
    "errorCode": "0"            // Mã lỗi chi tiết từ PLC
  },
  "crane2": {                   // Trạng thái cẩu trục 2 (PLC 1)
    "x": 0,
    "z": 0,
    "busy": false,
    "free": true,
    "error": false,
    "errorCode": "0"
  },
  
  "shuttle1": {                 // Trạng thái robot trung chuyển Shuttle 1 (PLC 1)
    "x": 0,                     // Tọa độ X hiện tại
    "z": 0,                     // Tọa độ Z hiện tại
    "b": 0,                     // Tọa độ B hiện tại
    "busy": false,              // Trạng thái bận
    "free": true,               // Trạng thái rảnh
    "error": false,             // Trạng thái lỗi
    "errorCode": "0",           // Mã lỗi chi tiết từ PLC
    "pin": 0                    // Dung lượng Pin hiện tại (%)
  },
  "shuttle2": {                 // Trạng thái robot trung chuyển Shuttle 2 (PLC 1)
    "x": 0,
    "z": 0,
    "b": 0,
    "busy": false,
    "free": true,
    "error": false,
    "errorCode": "0",
    "pin": 0
  },
  
  "system1": {                  // Trạng thái hệ thống PLC 1
    "auto": false,              // Chế độ Auto (true) hoặc Manual (false)
    "running": false,           // Hệ thống đang chạy (true/false)
    "stop": true,               // Hệ thống dừng khẩn cấp (true/false)
    "error": false,             // Hệ thống đang báo lỗi (true/false)
    "errorCode": 0              // Mã lỗi hệ thống
  },
  "system2": {                  // Trạng thái hệ thống PLC 2
    "auto": false,
    "running": false,
    "stop": true,
    "error": false,
    "errorCode": 0
  },
  
  "done1": false,               // Trạng thái hoàn thành lệnh trên PLC 1 (true = Hoàn thành)
  "fail1": false,               // Trạng thái lệnh lỗi trên PLC 1 (true = Lỗi)
  "done2": false,               // Trạng thái hoàn thành lệnh trên PLC 2 (true = Hoàn thành)
  "fail2": false,               // Trạng thái lệnh lỗi trên PLC 2 (true = Lỗi)
  "directionBlock2_Plc1": 0,    // Trực quan luồng chia làn/hướng di chuyển PLC 1
  "directionBlock2_Plc2": 0     // Trực quan luồng chia làn/hướng di chuyển PLC 2
}
```

---

## 2. Nhận Lệnh Từ WMS Gửi Xuống (Command Inward)

Để WMS có thể điều khiển và ra lệnh cho phần cứng, hệ thống đã cung cấp một API Endpoint duy nhất. Tất cả các dữ liệu mà WMS muốn ghi xuống WCS sẽ được gộp chung vào một cấu trúc JSON đồng nhất.

- **Địa chỉ Endpoint**: `http://localhost:5000/api/wms/send-command` (Hoặc IP chạy dịch vụ)
- **Phương thức**: `HTTP POST`
- **Định dạng dữ liệu**: `application/json`

### Cấu trúc JSON Request (WmsCommandModel)

WMS chỉ cần truyền các trường liên quan đến nhóm lệnh muốn thực thi, các trường không dùng đến có thể bỏ trống hoặc truyền `null`.

```json
{
  "commandGroup": "MoveTask",    // Phân loại nhóm lệnh: "MoveTask", "SystemControl", "BarcodeResult"
  
  // 1. NHÓM LỆNH GẮP/THẢ HÀNG (MOVE TASK)
  "commandType": 1,              // Loại lệnh: 1 = Nhập (Import), 2 = Xuất (Export), 3 = Chuyển (Transfer)
  "targetPlc": 1,                // Chỉ định cứng PLC thực thi (1 hoặc 2)
  
  "xin": 10,                     // Tọa độ X nguồn
  "zin": 5,                      // Tọa độ Z nguồn
  "bin": 1,                      // Tọa độ B nguồn
  
  "xout": 20,                    // Tọa độ X đích
  "zout": 5,                     // Tọa độ Z đích
  "bout": 1,                     // Tọa độ B đích

  // 2. NHÓM LỆNH ĐIỀU KHIỂN HỆ THỐNG (SYSTEM CONTROL)
  "requestAutoRun": true,        // Yêu cầu chuyển hệ thống sang Auto và Chạy
  "requestStop": false,          // Yêu cầu dừng hệ thống khẩn cấp
  "resetError": false,           // Yêu cầu xóa lỗi (Reset Faults)

  // 3. NHÓM PHẢN HỒI MÃ VẠCH (BARCODE RESULT)
  "barcodeOk": true,             // Xác nhận mã vạch đúng, cho phép đi tiếp
  "barcodeNg": false             // Xác nhận mã vạch sai, loại bỏ hàng
}
```

### Cơ Chế Hoạt Động Của API Này
1. WMS chủ động gửi gói JSON trên vào API `/api/wms/send-command` ngay khi có lệnh mới (Real-time).
2. Gateway sẽ **"hứng"** gói dữ liệu này, đưa vào hàng đợi (Queue) hoặc chuyển cho bộ não điều phối (Task Router) để xử lý logic.
3. WCS tự động kiểm tra điều kiện an toàn (ví dụ: cẩu đang rảnh, không kẹt xe) rồi mới chủ động chuyển lệnh này thành tín hiệu điều khiển trực tiếp xuống thanh ghi `DB500` của PLC.

---

## 3. Cấu trúc Mã nguồn Thư mục ConnectingWcs

Để hệ thống rành mạch và dễ bảo trì, toàn bộ "Tầng giao tiếp" (Integration Layer) giữa WMS và WCS đã được quy hoạch gọn gàng vào thư mục `ConnectingWcs`. Thư mục này chứa 3 Class nòng cốt đảm nhiệm các hướng dữ liệu khác nhau:

### 1. `WmsCommandModel.cs` (Khuôn mẫu Dữ liệu)
- **Chức năng:** Là đối tượng DTO (Data Transfer Object) định nghĩa "Hợp đồng giao tiếp" (Contract) duy nhất để nhận lệnh từ WMS.
- **Cách hoạt động:** Khi WMS gửi HTTP Request dạng JSON, ASP.NET Core sẽ tự động map các thuộc tính (tọa độ `Xin, Xout`, lệnh hệ thống `RequestAutoRun`, mã vạch `BarcodeOk`...) vào class này. Việc gộp chung mọi khả năng ra lệnh vào 1 file giúp quản lý cấu trúc dữ liệu tập trung, không bị phân mảnh.

### 2. `ReceiveDataAPI.cs` (Cửa Nhận Lệnh - Hướng Inward)
- **Chức năng:** Mở ra Web API Endpoint (`POST /api/wms/send-command`), đóng vai trò như "Cổng Tiếp Tân" đón nhận yêu cầu từ WMS.
- **Cách hoạt động:** Đón nhận gói dữ liệu `WmsCommandModel`. Thay vì ghi trực tiếp xuống PLC (gây rủi ro va chạm), Controller này sẽ đóng vai trò kiểm duyệt và đẩy gói lệnh sang `ProcessingDataReceive.cs` (hoặc Hàng đợi Queue) để xử lý logic.

### 3. `SendDataAPI.cs` (Cửa Gửi Trạng Thái - Hướng Outward)
- **Chức năng:** Là Web API Endpoint (`GET /api/status`), đóng vai trò cung cấp dữ liệu Real-time cho WMS.
- **Cách hoạt động:** Nằm yên chờ đợi. Khi WMS chủ động gửi yêu cầu GET tới, nó sẽ gom toàn bộ bản đồ RAM cục bộ (`SystemState` - chứa trạng thái Cẩu, Xe, Mã vạch) thành một chuỗi JSON siêu nhẹ và GỬI TRẢ (Respond) về cho WMS. Do chạy theo cơ chế "WMS chủ động tới lấy" (Pull), hệ thống WCS không cần phải tốn tài nguyên chạy vòng lặp gửi dữ liệu liên tục.

### 4. `ProcessingDataReceive.cs` (Bộ Não Điều Phối)
- **Chức năng:** Là nơi chứa logic (Business Logic) để xử lý dữ liệu ngay sau khi `ReceiveDataAPI` hứng được lệnh từ WMS.
- **Cách hoạt động:** Hoạt động như một "Task Router". Nó phân tích chuỗi JSON, kiểm tra các điều kiện an toàn của phần cứng (PLC rảnh không, tọa độ đúng không...), sau đó mới quyết định chuyển hóa lệnh này thành tín hiệu điều khiển trực tiếp chọc xuống thanh ghi của PLC.
