using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using TicketAgeApi.Models;
using TicketAgeApi.Services;

namespace TicketAgeApi.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly TicketService _service;

    public TicketsController(TicketService service) => _service = service;

    /// <summary>
    /// Upload one or more XLSX files. All files are merged into one in-memory ticket collection.
    /// Duplicate Number values are replaced by the record from the later uploaded file.
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] List<IFormFile> files)
    {
        if (files is null || files.Count == 0)
            return BadRequest("Upload at least one .xlsx file.");

        var results = new List<object>();
        var importedRows = 0;

        foreach (var file in files)
        {
            if (file.Length == 0)
                return BadRequest($"File '{file.FileName}' is empty.");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest($"File '{file.FileName}' is not an .xlsx file.");

            try
            {
                await using var stream = file.OpenReadStream();
                var count = _service.Import(stream);
                importedRows += count;

                results.Add(new
                {
                    fileName = file.FileName,
                    importedRows = count,
                    status = "Success"
                });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    fileName = file.FileName,
                    error = ex.Message
                });
            }
        }

        return Ok(new
        {
            systemDate = _service.SystemDate,
            filesProcessed = files.Count,
            importedRows,
            mergedUniqueTickets = _service.Count,
            files = results
        });
    }

    /// <summary>
    /// Returns age counts from the merged ticket collection.
    /// </summary>
    [HttpGet("age-summary")]
    public ActionResult<AgeSummary> AgeSummary()
        => Ok(_service.GetSummary());

    /// <summary>
    /// Downloads ONE merged Excel file containing all merged tickets older than 7 days.
    /// </summary>
    [HttpGet("older-than-7/download")]
    public IActionResult DownloadOlderThan7()
    {
        var tickets = _service.GetOlderThan7Days();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tickets > 7 Days");

        string[] headers =
        [
            "Number", "Opened", "Short description", "Caller", "Location",
            "Employee", "Maxim", "Priority", "State", "Service",
            "Service offering", "Assignment group", "Assigned to"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        for (int r = 0; r < tickets.Count; r++)
        {
            var t = tickets[r];

            ws.Cell(r + 2, 1).Value = t.Number;
            ws.Cell(r + 2, 2).Value = t.Opened;
            ws.Cell(r + 2, 3).Value = t.ShortDescription;
            ws.Cell(r + 2, 4).Value = t.Caller;
            ws.Cell(r + 2, 5).Value = t.Location;
            ws.Cell(r + 2, 6).Value = t.Employee;
            ws.Cell(r + 2, 7).Value = t.Maxim;
            ws.Cell(r + 2, 8).Value = t.Priority;
            ws.Cell(r + 2, 9).Value = t.State;
            ws.Cell(r + 2, 10).Value = t.Service;
            ws.Cell(r + 2, 11).Value = t.ServiceOffering;
            ws.Cell(r + 2, 12).Value = t.AssignmentGroup;
            ws.Cell(r + 2, 13).Value = t.AssignedTo;
        }

        ws.Column(2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();

        using var output = new MemoryStream();
        workbook.SaveAs(output);

        return File(
            output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"merged-tickets-more-than-7-days-{_service.SystemDate:yyyy-MM-dd}.xlsx");
    }

    /// <summary>
    /// Returns all merged tickets with calculated age and bucket.
    /// </summary>
    [HttpGet]
    public IActionResult GetTickets()
    {
        return Ok(_service.GetTickets().Select(t => new
        {
            t.Number,
            t.Opened,
            t.ShortDescription,
            AgeDays = _service.GetAgeDays(t),
            AgeBucket = _service.GetBucket(t)
        }));
    }

    /// <summary>
    /// Clears all imported tickets from the in-memory collection.
    /// </summary>
    [HttpDelete("clear")]
    public IActionResult Clear()
    {
        _service.Clear();
        return Ok(new { message = "Merged ticket collection cleared." });
    }
}
