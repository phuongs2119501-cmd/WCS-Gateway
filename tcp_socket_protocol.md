# Tài liệu Giao thức TCP Socket (WCS TCP Client)

Đây là tài liệu đặc tả cấu trúc dữ liệu gửi và nhận thông qua kết nối **TCP Socket** trong hệ thống WCS (Cụ thể được triển khai tại file `TcpClientService.cs`).

## 1. Thông tin kết nối Socket
- **IP Target (Server):** `127.0.0.1` (Localhost)
- **Port:** `9000`
- **Encoding:** `UTF-8`

## 2. Các Lệnh (Commands) Hỗ Trợ

Giao thức được thiết kế theo dạng Command-Response (Gửi lệnh - Chờ phản hồi). Dưới đây là 2 lệnh chính mà Client gửi đi:

### 2.1. Lệnh kiểm tra kết nối (PING)
- **Dữ liệu Client gửi đi:** chuỗi `PING`
- **Dữ liệu Server trả về:** chuỗi `PONG`
- **Mục đích:** Dùng để kiểm tra xem Server TCP (Port 9000) có đang sống và sẵn sàng nhận lệnh hay không.

### 2.2. Lệnh lấy trạng thái hệ thống (GET_STATUS)
- **Dữ liệu Client gửi đi:** chuỗi `GET_STATUS`
- **Dữ liệu Server trả về:** MỘT chuỗi JSON chứa toàn bộ dữ liệu trạng thái (WcsStatusDto).

## 3. Cấu trúc JSON nhận về từ lệnh `GET_STATUS`

Khi gọi lệnh `GET_STATUS`, trả về sẽ là chuỗi JSON. Cấu trúc chi tiết của chuỗi JSON đó bao gồm các trường sau:

```json
{
  "plc1": true,              // Trạng thái kết nối của PLC 1 (true/false)
  "plc2": true,              // Trạng thái kết nối của PLC 2 (true/false)
  "barcode1": "123456",      // Chuỗi mã vạch đọc từ PLC 1
  "barcode2": "789012",      // Chuỗi mã vạch đọc từ PLC 2
  
  "crane1": {
    "x": 100,                // Tọa độ X của Crane 1
    "z": 200,                // Tọa độ Z của Crane 1
    "busy": false,           // Trạng thái bận
    "free": true,            // Trạng thái rảnh
    "error": false,          // Trạng thái lỗi
    "errorCode": "0"         // Mã lỗi (nếu có)
  },
  "crane2": {
    "x": 150,
    "z": 250,
    "busy": true,
    "free": false,
    "error": false,
    "errorCode": "0"
  },

  "shuttle1": {
    "x": 50,                 // Tọa độ X của Shuttle 1
    "z": 60,                 // Tọa độ Z của Shuttle 1
    "b": 10,                 // Thông số B của Shuttle 1
    "busy": false,
    "free": true,
    "error": false,
    "errorCode": "0",
    "pin": 95                // Phần trăm Pin (tuỳ biến nếu có)
  },
  "shuttle2": {
    "x": 80,
    "z": 90,
    "b": 20,
    "busy": false,
    "free": true,
    "error": false,
    "errorCode": "0",
    "pin": 80
  }
}
```

### ✅ Lưu ý khi parse dữ liệu (Phía C#):
- Dữ liệu JSON map trực tiếp vào model `WcsStatusDto`.
- Các model `CraneModel` và `ShuttleModel` có thể dính giá trị `null` nếu Server hiện tại không cập nhật.
- C# sử dụng thư viện System.Text.Json đọc với tuỳ chọn `PropertyNameCaseInsensitive = true`, nghĩa là chữ hoa/chữ thường trong JSON key (ví dụ: `plc1` hay `Plc1`) đều sẽ được tự động nhận diện chính xác.
