# Tẩu Thoát Turbo

Game lái xe trốn cảnh sát góc nhìn từ trên cao (lấy cảm hứng từ Pako), làm bằng **Unity 6.5 (C#, URP)**.

Sinh viên thực hiện: **Lê Quang Lâm** – Lớp **TH29.14** – Trường Đại học Kinh doanh và Công nghệ Hà Nội.

## Cách chạy
1. Mở thư mục dự án bằng Unity Hub (Unity 6000.5.9f1 hoặc mới hơn).
2. Mở `Assets/Scenes/SampleScene` (hoặc scene bất kỳ) và bấm **Play**.
   Game tự dựng thành phố, xe, cảnh sát và giao diện, không cần kéo thả gì.

## Điều khiển
| | Bàn phím | Chuột |
|---|---|---|
| Lái | ← / A, → / D | Di chuột tới đâu xe chạy tới đó |
| Phanh trượt | Space (hoặc giữ cả hai phím) | – |
| Lùi | ↓ / S | Đưa chuột ra ngay sau xe |
| Tạm dừng | P / Esc | P / Esc |

Chọn kiểu điều khiển ở mục **Điều khiển** trong menu.

## Tính năng
- 3 chế độ: Tài xế tẩu thoát, Sinh tồn, Chiếm vùng
- Cảnh sát AI: xe tuần tra, SUV, thiết giáp SWAT, càng chơi càng đông và nhanh
- Vật phẩm: xu, khiên, nitro, cờ lê sửa xe
- Gara 8 xe, 10 màu sơn; lưu xu và kỷ lục
- 4 bản đồ sinh ngẫu nhiên: Thành phố, Đêm neon, Sa mạc, Tuyết

## Mã nguồn
Toàn bộ code nằm trong `Assets/TauThoat/Scripts`:
- `TTGame.cs` – luật chơi, menu, AI cảnh sát, camera, HUD, âm thanh, lưu dữ liệu
- `TTCar.cs` – vật lý xe (tự ga, drift, phanh, lùi, va chạm)
- `TTCore.cs` – dữ liệu xe/bản đồ, sinh thành phố, dựng mô hình xe, âm thanh tổng hợp
