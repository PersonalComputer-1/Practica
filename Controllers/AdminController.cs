using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase {
    private readonly AppDb _db;
    public AdminController(AppDb db) => _db = db;

    [HttpPost("equipment")]
    public IActionResult AddEq([FromBody] Equipment e) { _db.Equipment.Add(e); _db.SaveChanges(); return Ok(e); }

    [HttpPut("equipment/{id}")]
    public IActionResult UpdEq(int id, [FromBody] Equipment e) {
        var x = _db.Equipment.Find(id); if (x == null) return NotFound();
        x.Name = e.Name; x.PricePerDay = e.PricePerDay; x.TotalQty = e.TotalQty; x.Description = e.Description;
        _db.SaveChanges(); return Ok(x);
    }

    [HttpGet("stats")]
    public IActionResult Stats() {
        var totalBookings = _db.Bookings.Count();
        var revenue = _db.Bookings.Where(b => b.Status == "returned").Sum(b => b.TotalPrice);
        var overdue = _db.Bookings.Count(b => b.Status == "issued" && b.EndDate < DateTime.UtcNow);
        var byStatus = _db.Bookings.GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToList();
        return Ok(new { totalBookings, revenue, overdue, byStatus });
    }
}