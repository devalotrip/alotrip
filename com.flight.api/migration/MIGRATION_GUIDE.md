# Database Migration Guide
## SQL Server → PostgreSQL

**Dự án:** com.flight.api  
**Stack mới:** .NET 8 + Clean Architecture + PostgreSQL 18  
**Stack cũ:** .NET Framework 4.6.1 + SOAP + SQL Server  

---

## Yêu cầu cài đặt

| Tool | Version | Link |
|---|---|---|
| PostgreSQL | 18+ | https://www.postgresql.org/download/ |
| Python | 3.11+ | https://www.python.org/downloads/ |
| Docker Desktop | latest | https://www.docker.com/products/docker-desktop/ |
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download/dotnet/8.0 |
| dotnet-ef tool | latest | `dotnet tool install --global dotnet-ef` |

### Cài thư viện Python
```bash
pip install pyodbc psycopg2-binary
```

### Yêu cầu kết nối
- SQL Server cũ phải accessible (qua SSH tunnel hoặc trực tiếp)
- Trong hướng dẫn này, SQL Server được kết nối qua PuTTY SSH tunnel tại `127.0.0.1:14330`

---

## Cấu trúc files migration

```
migration/
├── 02_migrate.py          # Bước 2: Copy data từ SQL Server vào staging tables
├── 04_transform_to_ef.py  # Bước 4: Transform staging → EF Core schema
└── MIGRATION_GUIDE.md     # File này
```

> **Lưu ý:** File `01_schema_postgresql.sql` và `03_transform_data.sql` là phiên bản cũ, không dùng nữa.

---

## Các bước thực hiện

### Bước 1 — Tạo database PostgreSQL

Mở CMD và chạy:

```cmd
set PGPASSWORD=<password_postgresql>
"C:\Program Files\PostgreSQL\18\bin\psql" -U postgres -h localhost -c "CREATE DATABASE flight_api;"
```

---

### Bước 2 — Tạo schema bằng EF Core migrations

```cmd
D:
cd "D:\NewProjects\alotrip\com.flight.api"
dotnet ef database update --project src\Services\Flight\Core\Flight.Infrastructure --startup-project src\Services\Flight\Api\Flight.Api
```

Lệnh này sẽ áp dụng tất cả migrations và tạo **33 tables** trong PostgreSQL.

**Kiểm tra tables đã tạo:**
```cmd
"C:\Program Files\PostgreSQL\18\bin\psql" -U postgres -h localhost -d flight_api -c "\dt"
```

---

### Bước 3 — Mở PuTTY tunnel (nếu SQL Server ở remote)

- Mở PuTTY và connect SSH vào server
- Cấu hình port forwarding: `0.0.0.0:14330` → `127.0.0.1:1433`
- Giữ PuTTY mở trong suốt quá trình migration

---

### Bước 4 — Copy data từ SQL Server vào staging tables

```cmd
python "D:\NewProjects\alotrip\com.flight.api\migration\02_migrate.py"
```

Script này sẽ:
- Kết nối SQL Server qua tunnel (`127.0.0.1:14330`)
- Copy **38 tables** (~2.3 triệu rows) vào staging tables (`stg_*`) trong PostgreSQL
- Hiển thị progress từng table

**Thời gian ước tính:** 5-10 phút

---

### Bước 5 — Transform data sang EF Core schema

```cmd
python "D:\NewProjects\alotrip\com.flight.api\migration\04_transform_to_ef.py"
```

Script này sẽ:
- Tự động TRUNCATE tất cả EF tables (có thể chạy lại nhiều lần)
- Map data từ staging tables sang EF schema
- Generate UUID mới cho `bookings`, `booking_flights`, `booking_segments`, `passengers`, `tickets`
- Duy trì FK relationships qua UUID mapping dictionary
- Hiển thị row counts cho từng table

**Thời gian ước tính:** 10-20 phút

---

### Bước 6 — Kiểm tra kết quả

```cmd
"C:\Program Files\PostgreSQL\18\bin\psql" -U postgres -h localhost -d flight_api -c "
SELECT 'bookings'         AS tbl, COUNT(*) FROM bookings         UNION ALL
SELECT 'booking_flights'  AS tbl, COUNT(*) FROM booking_flights  UNION ALL
SELECT 'passengers'       AS tbl, COUNT(*) FROM passengers       UNION ALL
SELECT 'agents'           AS tbl, COUNT(*) FROM agents           UNION ALL
SELECT 'airlines'         AS tbl, COUNT(*) FROM airlines         UNION ALL
SELECT 'geo_airports'     AS tbl, COUNT(*) FROM geo_airports
ORDER BY 1;
"
```

**Kết quả mong đợi:**

| Table | Rows |
|---|---|
| bookings | ~219,994 |
| booking_flights | ~271,796 |
| passengers | ~384,591 |
| agents | ~162 |
| airlines | ~736 |
| geo_airports | ~4,108 |

---

## Cấu hình ứng dụng

File `src/Services/Flight/Api/Flight.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=flight_api;Username=postgres;Password=<your_password>",
    "Redis": "localhost:6379"
  }
}
```

File `src/Services/Flight/Core/Flight.Infrastructure/Persistence/ApplicationDbContextFactory.cs` — cập nhật connection string nếu thay đổi password:

```csharp
"Host=localhost;Port=5432;Database=flight_api;Username=postgres;Password=<your_password>"
```

---

## Chạy ứng dụng

```cmd
cd "D:\NewProjects\alotrip\com.flight.api"
dotnet run --project src\Services\Flight\Api\Flight.Api
```

---

## Xem database

### pgAdmin 4 (GUI)
- Mở **pgAdmin 4** từ Start Menu
- Kết nối với: `localhost:5432`, user `postgres`

### psql (command line)
```cmd
set PGPASSWORD=123456
"C:\Program Files\PostgreSQL\18\bin\psql" -U postgres -h localhost -d flight_api
```

### DBeaver (khuyên dùng — free, đa năng)
- Download: https://dbeaver.io/download/
- New Connection → PostgreSQL → nhập thông tin kết nối

---

## Lưu ý quan trọng

### Về ID conversion (Int → UUID)
- Các table `bookings`, `booking_flights`, `booking_segments`, `passengers`, `tickets` dùng **UUID** thay vì integer ID
- Script `04_transform_to_ef.py` tự động generate UUID mới và duy trì FK relationships qua in-memory mapping
- Mỗi lần chạy script sẽ generate UUID khác nhau (TRUNCATE + reinsert)

### Về data bị bỏ qua
- `tblTripCancellation`: không tồn tại trong SQL Server cũ
- `tblTicket`: 0 rows trong SQL Server cũ
- `commissions`: không có EF entity (quản lý riêng qua raw SQL)
- `currencies`: không có EF entity (quản lý riêng)

### Về password cũ
- Password của users và agents được copy nguyên từ SQL Server (plain text / MD5)
- Cần thông báo users đổi password sau khi go-live
- Hoặc implement logic nhận diện MD5 khi login

### Về booking_code trống
- Một số booking cũ không có `BookingCode`
- Script tự động tạo booking_code dạng `MIGR-{old_id}` cho các trường hợp này

---

## Cấu trúc thư mục dự án

```
com.flight.api/
├── migration/
│   ├── 02_migrate.py           # Script copy data từ SQL Server
│   ├── 04_transform_to_ef.py   # Script transform sang EF schema
│   └── MIGRATION_GUIDE.md      # Hướng dẫn này
└── src/
    └── Services/Flight/
        ├── Api/Flight.Api/           # API endpoints (Carter)
        └── Core/
            ├── Flight.Application/   # CQRS handlers (MediatR)
            ├── Flight.Domain/        # Domain entities & aggregates
            └── Flight.Infrastructure/
                └── Persistence/
                    └── Migrations/   # EF Core migrations
```

---

## Troubleshooting

### Lỗi "password authentication failed"
```cmd
set PGPASSWORD=<password>
```

### Lỗi "can't adapt type UUID" 
Đã được fix trong `04_transform_to_ef.py` bằng `psycopg2.extras.register_uuid()`

### Lỗi "value too long for type character varying"
Đã được fix bằng `trunc(value, max_length)` trong transform script

### Muốn chạy lại migration từ đầu
Script `04_transform_to_ef.py` tự động TRUNCATE tất cả tables trước khi insert — an toàn để chạy lại nhiều lần.

Nếu muốn chạy lại từ bước copy data (Bước 4):
```cmd
python "D:\NewProjects\alotrip\com.flight.api\migration\02_migrate.py"
python "D:\NewProjects\alotrip\com.flight.api\migration\04_transform_to_ef.py"
```

### dotnet ef không nhận
```cmd
dotnet tool install --global dotnet-ef
```
