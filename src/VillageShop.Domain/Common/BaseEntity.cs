using System;

namespace VillageShop.Domain.Common;

public abstract class BaseEntity
{
    public long ID { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public int Priority { get; set; } = 0;

    public long CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public long ModifiedBy { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public long DeletedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string EntrySource { get; set; } = string.Empty;

    public string IPAddress { get; set; } = string.Empty;
}
