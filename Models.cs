using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User {
    public int Id { get; set; }
    [Required] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [Required] public string Role { get; set; } = "client"; // client | manager | admin
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Category {
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

public class Equipment {
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    [Required] public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
    public decimal PricePerDay { get; set; }
    public int TotalQty { get; set; }
    public string Status { get; set; } = "active";
}

public class EquipmentItem {
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    [Required] public string InventoryNo { get; set; } = "";
    public string Condition { get; set; } = "good";
    public string State { get; set; } = "ok"; // ok | repair | written_off
}

public class Booking {
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "pending";
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<BookingItem> Items { get; set; } = new();
}

public class BookingItem {
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public int Qty { get; set; }
    public decimal PricePerDay { get; set; }
}

public class Penalty {
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string Type { get; set; } = "late"; // late | damage
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WarehouseLog {
    public int Id { get; set; }
    public int ItemId { get; set; }
    public EquipmentItem? Item { get; set; }
    public string OldState { get; set; } = "";
    public string NewState { get; set; } = "";
    public string Comment { get; set; } = "";
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}