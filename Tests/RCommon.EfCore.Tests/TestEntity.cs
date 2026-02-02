using RCommon.Entities;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Test entity for repository tests.
/// </summary>
public class TestEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }

    public TestEntity() : base()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
        IsActive = true;
    }

    public TestEntity(Guid id) : base(id)
    {
        CreatedDate = DateTime.UtcNow;
        IsActive = true;
    }
}
