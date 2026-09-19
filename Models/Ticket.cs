namespace TicketAgeApi.Models;

public sealed class Ticket
{
    public string Number { get; set; } = "";
    public DateTime Opened { get; set; }
    public string ShortDescription { get; set; } = "";
    public string Caller { get; set; } = "";
    public string Location { get; set; } = "";
    public string Employee { get; set; } = "";
    public string Maxim { get; set; } = "";
    public string Priority { get; set; } = "";
    public string State { get; set; } = "";
    public string Service { get; set; } = "";
    public string ServiceOffering { get; set; } = "";
    public string AssignmentGroup { get; set; } = "";
    public string AssignedTo { get; set; } = "";
}
