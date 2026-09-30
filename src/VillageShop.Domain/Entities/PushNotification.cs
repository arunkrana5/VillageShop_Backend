using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VillageShop.Domain.Entities;

[Table("v_PushNotification")]
public class PushNotification
{
    [Key]
    public int NotificationID { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string MessageContent { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long TableID { get; set; }
    public string Status { get; set; } = "SENT";
    public string GotoURL { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public bool IsStatusRead { get; set; } = false;
    public int CreatedBy { get; set; } = 1;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsRecent { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public short isdeleted { get; set; } = 0;
    public string IPAddress { get; set; } = "127.0.0.1";
    public long? TenantId { get; set; } = 1;
}
