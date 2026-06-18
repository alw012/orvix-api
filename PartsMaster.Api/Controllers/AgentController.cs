using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;
using PartsMaster.Api.Models;
using PartsMaster.Api.Services;

namespace PartsMaster.Api.Controllers
{
    /// <summary>
    /// نقاط اتصال وكيل ORVIX (ORVIX Agent).
    /// - activate: تفعيل الجهاز بحساب المحل (username/password) وإرجاع JWT.
    /// - inventory: استقبال المخزون (رقم القطعة + الكمية فقط) وتحديث جدول Parts.
    /// ملاحظة أمان: لا يُحدّث الوكيل أي بيانات مالية (Price / CompanyPrice).
    /// </summary>
    [ApiController]
    [Route("api/agent")]
    public class AgentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AgentController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // ============================================================
        //  POST /api/agent/activate
        //  body: { "username": "...", "password": "...", "machineName": "...", "fingerprint": "..." }
        //  result: { "token": "...", "deviceId": "DEV-000001", "storeId": 1, "storeName": "..." }
        // ============================================================
        [HttpPost("activate")]
        public async Task<IActionResult> Activate(AgentActivationDto dto)
        {
            // 1) التحقق من حساب المحل (نفس آلية تسجيل الدخول)
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == dto.Username && x.IsActive);

            if (user == null)
                return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة" });

            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isValid)
                return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة" });

            // 2) إيجاد المحل المرتبط بهذا المستخدم
            var store = await _context.Stores
                .Include(s => s.Devices)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);

            if (store == null)
                return BadRequest(new { message = "لا يوجد محل مرتبط بهذا الحساب" });

            // 3) تسجيل/تحديث الجهاز
            //    نبحث ببصمة الجهاز لتفادي التكرار عند إعادة التفعيل على نفس الجهاز.
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.StoreId == store.Id && d.HardwareFingerprint == dto.Fingerprint);

            if (device == null)
            {
                // التحقق من الحد الأقصى للأجهزة فقط عند إضافة جهاز جديد
                if (store.Devices.Count >= store.MaxDevices)
                    return BadRequest(new { message = $"وصل المحل للحد الأقصى للأجهزة ({store.MaxDevices})" });

                var count = await _context.Devices.CountAsync();
                device = new Device
                {
                    DeviceId = $"DEV-{(count + 1):D6}",
                    Name = string.IsNullOrWhiteSpace(dto.MachineName) ? "ORVIX Agent" : dto.MachineName,
                    HardwareFingerprint = dto.Fingerprint ?? "",
                    StoreId = store.Id,
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Devices.Add(device);
            }
            else
            {
                device.IsOnline = true;
                device.LastSeen = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.MachineName))
                    device.Name = dto.MachineName;
            }

            await _context.SaveChangesAsync();

            // 4) إصدار JWT (نفس خdمة التوكن المستخدمة في تسجيل الدخول)
            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                deviceId = device.DeviceId,
                storeId = store.Id,
                storeName = store.Name
            });
        }

        // ============================================================
        //  POST /api/agent/inventory   (يتطلب JWT)
        //  body: { "deviceId": "...", "syncedAt": "...", "itemCount": 3,
        //          "items": [ { "partNumber": "...", "quantity": 5 }, ... ] }
        // ============================================================
        [Authorize]
        [HttpPost("inventory")]
        public async Task<IActionResult> ReceiveInventory(AgentInventoryDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "لا توجد عناصر مخزون" });

            // إيجاد المحل من الجهاز المُرسِل
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);

            if (device == null)
                return NotFound(new { message = "الجهاز غير مسجّل" });

            int storeId = device.StoreId;

            // تحميل القطع الحالية للمحل (مفهرسة برقم القطعة لسرعة المطابقة)
            var existing = await _context.Parts
                .Where(p => p.StoreId == storeId)
                .ToDictionaryAsync(p => p.PartNumber, p => p);

            int added = 0, updated = 0;

            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.PartNumber)) continue;

                if (existing.TryGetValue(item.PartNumber, out var part))
                {
                    // تحdيث الكمية فقط — لا نلمس Price ولا CompanyPrice (أمان المخزون)
                    part.Quantity = (int)item.Quantity;
                    updated++;
                }
                else
                {
                    // قطعة جديدة: رقم + كمية فق-ط، الأسعار تبقى صفر يحددها المحل لاحقاً
                    _context.Parts.Add(new Part
                    {
                        PartNumber = item.PartNumber,
                        Quantity = (int)item.Quantity,
                        Type = "",
                        Price = 0,
                        CompanyPrice = 0,
                        StoreId = storeId
                    });
                    added++;
                }
            }

            // تحديث حالة الجهاز
            device.IsOnline = true;
            device.LastSeen = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تمت مزامنة المخزون بنجاح",
                received = dto.Items.Count,
                added,
                updated
            });
        }
    }

    // ===== DTOs خاصة بالوكيل =====

    public class AgentActivationDto
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? MachineName { get; set; }
        public string? Fingerprint { get; set; }
    }

    public class AgentInventoryDto
    {
        public string DeviceId { get; set; } = "";
        public string? SyncedAt { get; set; }
        public int ItemCount { get; set; }
        public List<AgentInventoryItem> Items { get; set; } = new();
    }

    public class AgentInventoryItem
    {
        public string PartNumber { get; set; } = "";
        public decimal Quantity { get; set; }
    }
}
