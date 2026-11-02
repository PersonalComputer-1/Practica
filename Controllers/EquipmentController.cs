using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase {
    private readonly AppDb _db;
    public EquipmentController(AppDb db) => _db = db;

    [HttpGet]
    public IActionResult Get([FromQuery] string? q, [FromQuery] int? categoryId,
                             [FromQuery] DateTime? from, [FromQuery] DateTime? to) {
        var query = _db.Equipment.Include(e => e.Category).Where(e => e.Status == "active");
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e => e.Name.Contains(q) || e.Description.Contains(q));
        if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId);

        var list = query.Select(e => new {
            e.Id, e.Name, e.Description, e.PhotoUrl, e.PricePerDay, e.TotalQty,
            Category = e.Category!.Name
        }).ToList();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public IActionResult GetOne(int id) {
        var e = _db.Equipment.Include(x => x.Category).FirstOrDefault(x => x.Id == id);
        return e == null ? NotFound() : Ok(e);
    }
}