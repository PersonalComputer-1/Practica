using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase {
    private readonly AppDb _db;
    public BookingsController(AppDb db) => _db = db;

    private int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public IActionResult Create([FromBody] CreateBookingDto dto) {
        if (dto.StartDate >= dto.EndDate) return BadRequest("EndDate должна быть позже StartDate");

        var eq = _db.Equipment.Find(dto.EquipmentId);
        if (eq == null) return NotFound("Оборудование не найдено");

        // ПРОВЕРКА ПЕРЕСЕЧЕНИЯ: сумма забронированных единиц на пересекающиеся даты
        var booked = _db.BookingItems
            .Where(bi => bi.EquipmentId == dto.EquipmentId
                      && bi.Booking!.Status != "cancelled"
                      && bi.Booking.StartDate < dto.EndDate
                      && dto.StartDate < bi.Booking.EndDate)
            .Sum(bi => bi.Qty);

        if (booked + dto.Qty > eq.TotalQty)
            return Conflict($"Недостаточно единиц. Доступно: {eq.TotalQty - booked}");

        var days = (dto.EndDate.Date - dto.StartDate.Date).Days;
        if (days < 1) days = 1;

        var booking = new Booking {
            UserId = Uid,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Status = "pending",
            Items = new List<BookingItem> {
                new() { EquipmentId = dto.EquipmentId, Qty = dto.Qty, PricePerDay = eq.PricePerDay }
            }
        };
        booking.TotalPrice = days * eq.PricePerDay * dto.Qty;
        _db.Bookings.Add(booking);
        _db.SaveChanges();
        return Ok(new { booking.Id, booking.TotalPrice, booking.Status });
    }

    [HttpGet("my")]
    public IActionResult My() {
        var list = _db.Bookings.Include(b => b.Items).ThenInclude(i => i.Equipment)
            .Where(b => b.UserId == Uid)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new {
                b.Id, b.StartDate, b.EndDate, b.Status, b.TotalPrice,
                Items = b.Items.Select(i => new { i.Equipment!.Name, i.Qty, i.PricePerDay })
            }).ToList();
        return Ok(list);
    }

    [HttpPatch("{id}/cancel")]
    public IActionResult Cancel(int id) {
        var b = _db.Bookings.FirstOrDefault(x => x.Id == id && x.UserId == Uid);
        if (b == null) return NotFound();
        if (b.Status != "pending") return BadRequest("Отменить можно только неподтверждённую бронь");
        b.Status = "cancelled";
        _db.SaveChanges();
        return Ok();
    }

    // Менеджер: список всех
    [HttpGet("all")]
    [Authorize(Roles = "manager,admin")]
    public IActionResult All() {
        var list = _db.Bookings.Include(b => b.User).Include(b => b.Items).ThenInclude(i => i.Equipment)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, Client = b.User!.FullName, b.StartDate, b.EndDate, b.Status, b.TotalPrice })
            .ToList();
        return Ok(list);
    }

    [HttpPatch("{id}/confirm")]
    [Authorize(Roles = "manager,admin")]
    public IActionResult Confirm(int id) {
        var b = _db.Bookings.Find(id);
        if (b == null || b.Status != "pending") return BadRequest();
        b.Status = "confirmed";
        _db.SaveChanges();
        return Ok();
    }

    [HttpPatch("{id}/issue")]
    [Authorize(Roles = "manager,admin")]
    public IActionResult Issue(int id) {
        var b = _db.Bookings.Include(x => x.Items).FirstOrDefault(x => x.Id == id);
        if (b == null || b.Status != "confirmed") return BadRequest();
        b.Status = "issued";
        _db.SaveChanges();
        return Ok();
    }

    [HttpPatch("{id}/return")]
    [Authorize(Roles = "manager,admin")]
    public IActionResult Return(int id, [FromBody] ReturnDto dto) {
        var b = _db.Bookings.Include(x => x.Items).FirstOrDefault(x => x.Id == id);
        if (b == null || b.Status != "issued") return BadRequest();

        // просрочка
        if (DateTime.UtcNow.Date > b.EndDate.Date) {
            var daysLate = (DateTime.UtcNow.Date - b.EndDate.Date).Days;
            var rate = b.Items.Sum(i => i.PricePerDay * i.Qty);
            _db.Penalties.Add(new Penalty {
                BookingId = b.Id, Type = "late",
                Amount = daysLate * rate * 1.5m,
                Reason = $"Просрочка {daysLate} дн."
            });
        }
        // повреждение
        if (dto.DamageFee > 0) {
            _db.Penalties.Add(new Penalty {
                BookingId = b.Id, Type = "damage",
                Amount = dto.DamageFee, Reason = dto.DamageComment ?? "Повреждение"
            });
        }
        b.Status = "returned";
        _db.SaveChanges();
        return Ok();
    }
}

public record CreateBookingDto(int EquipmentId, int Qty, DateTime StartDate, DateTime EndDate);
public record ReturnDto(decimal DamageFee, string? DamageComment);