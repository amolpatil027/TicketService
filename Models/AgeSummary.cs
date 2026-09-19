namespace TicketAgeApi.Models;

public sealed class AgeSummary
{
    public DateTime SystemDate { get; init; }
    public int TotalTickets { get; init; }
    public int Age0To3Days { get; init; }
    public int Age4To5Days { get; init; }
    public int Age6To7Days { get; init; }
    public int MoreThan7Days { get; init; }
}
