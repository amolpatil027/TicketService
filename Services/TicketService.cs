using System.Globalization;
using ClosedXML.Excel;
using TicketAgeApi.Models;

namespace TicketAgeApi.Services;

public sealed class TicketService
{
    private readonly object _sync = new();
    private readonly List<Ticket> _tickets = new();

    // The machine/server system date is evaluated for every calculation.
    public DateTime SystemDate => DateTime.Today;

    // Appends tickets from one uploaded workbook to the existing merged collection.
    // Duplicate ticket numbers are replaced by the latest imported record.
    public int Import(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("Excel file has no worksheet.");

        var headerRow = ws.FirstRowUsed()
            ?? throw new InvalidDataException("Excel file has no header row.");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var name = Normalize(cell.GetString());
            if (!string.IsNullOrWhiteSpace(name))
                headers[name] = cell.Address.ColumnNumber;
        }

        string[] required =
        [
            "Number", "Opened", "Short description", "Caller", "Location",
            "Employee", "Maxim", "Priority", "State", "Service",
            "Service offering", "Assignment group", "Assigned to"
        ];

        var missing = required.Where(x => !headers.ContainsKey(Normalize(x))).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException("Missing columns: " + string.Join(", ", missing));

        int C(string name) => headers[Normalize(name)];

        var imported = new List<Ticket>();

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var number = row.Cell(C("Number")).GetString().Trim();
            if (string.IsNullOrWhiteSpace(number))
                continue;

            var openedCell = row.Cell(C("Opened"));
            if (!TryGetDate(openedCell, out var opened))
                throw new InvalidDataException(
                    $"Invalid Opened date for ticket '{number}': {openedCell.GetString()}");

            imported.Add(new Ticket
            {
                Number = number,
                Opened = opened,
                ShortDescription = row.Cell(C("Short description")).GetString().Trim(),
                Caller = row.Cell(C("Caller")).GetString().Trim(),
                Location = row.Cell(C("Location")).GetString().Trim(),
                Employee = row.Cell(C("Employee")).GetString().Trim(),
                Maxim = row.Cell(C("Maxim")).GetString().Trim(),
                Priority = row.Cell(C("Priority")).GetString().Trim(),
                State = row.Cell(C("State")).GetString().Trim(),
                Service = row.Cell(C("Service")).GetString().Trim(),
                ServiceOffering = row.Cell(C("Service offering")).GetString().Trim(),
                AssignmentGroup = row.Cell(C("Assignment group")).GetString().Trim(),
                AssignedTo = row.Cell(C("Assigned to")).GetString().Trim()
            });
        }

        lock (_sync)
        {
            // Merge behavior:
            // - New ticket number: add
            // - Existing ticket number: replace with the newly uploaded record
            foreach (var ticket in imported)
            {
                var existingIndex = _tickets.FindIndex(
                    x => string.Equals(x.Number, ticket.Number, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                    _tickets[existingIndex] = ticket;
                else
                    _tickets.Add(ticket);
            }
        }

        return imported.Count;
    }

    public int Count
    {
        get
        {
            lock (_sync)
                return _tickets.Count;
        }
    }

    public List<Ticket> GetTickets()
    {
        lock (_sync)
            return _tickets.ToList();
    }

    public void Clear()
    {
        lock (_sync)
            _tickets.Clear();
    }

    public int GetAgeDays(Ticket ticket)
        => Math.Max(0, (SystemDate.Date - ticket.Opened.Date).Days);

    public string GetBucket(Ticket ticket)
    {
        var age = GetAgeDays(ticket);
        return age switch
        {
            <= 3 => "0-3 days",
            <= 5 => "4-5 days",
            <= 7 => "6-7 days",
            _ => "More than 7 days"
        };
    }

    public AgeSummary GetSummary()
    {
        var tickets = GetTickets();

        return new AgeSummary
        {
            SystemDate = SystemDate,
            TotalTickets = tickets.Count,
            Age0To3Days = tickets.Count(t => GetAgeDays(t) <= 3),
            Age4To5Days = tickets.Count(t => GetAgeDays(t) is >= 4 and <= 5),
            Age6To7Days = tickets.Count(t => GetAgeDays(t) is >= 6 and <= 7),
            MoreThan7Days = tickets.Count(t => GetAgeDays(t) > 7)
        };
    }

    public List<Ticket> GetOlderThan7Days()
        => GetTickets().Where(t => GetAgeDays(t) > 7).ToList();

    private static string Normalize(string value) =>
        string.Join(" ", value.Trim().Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static bool TryGetDate(IXLCell cell, out DateTime date)
    {
        if (cell.DataType == XLDataType.DateTime)
        {
            date = cell.GetDateTime();
            return true;
        }

        var value = cell.GetString().Trim();

        string[] formats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd H:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy H:mm:ss",
            "dd-MM-yyyy HH:mm:ss"
        ];

        return DateTime.TryParseExact(value, formats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out date)
               || DateTime.TryParse(value, CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out date);
    }
}
