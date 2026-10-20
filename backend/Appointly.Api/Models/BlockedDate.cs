namespace Appointly.Api.Models;

public sealed class BlockedDate
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
}
