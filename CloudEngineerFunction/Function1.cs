using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CloudEngineerFunction;

public class TicketFunction
{
    private readonly ILogger<TicketFunction> _logger;
    private readonly AppDbContext _context; // <-- Unser Datenbank-Kontext

    public TicketFunction(ILogger<TicketFunction> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [Function("CreateTicket")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tickets")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to create a ticket.");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        SupportTicket? ticket = null;
        try
        {
            ticket = await JsonSerializer.DeserializeAsync<SupportTicket>(req.Body, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Lesen des JSON-Bodys.");
            return new BadRequestObjectResult("Ungültiges JSON-Format.");
        }

        if (ticket == null || string.IsNullOrEmpty(ticket.Title))
        {
            return new BadRequestObjectResult("Bitte gib mindestens einen Titel für das Ticket an.");
        }

        // 1. In die Azure SQL Datenbank einfügen
        ticket.Id = Guid.NewGuid(); // Wir generieren eine ID
        await _context.Tickets.AddAsync(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket erfolgreich in Azure SQL gespeichert: {Title} (ID: {Id})", ticket.Title, ticket.Id);

        // 2. Erfolgreiche Antwort zurückgeben
        return new OkObjectResult(new
        {
            Message = "Ticket erfolgreich in der Azure SQL Datenbank gespeichert!",
            TicketId = ticket.Id,
            Data = ticket
        });
    }

    [Function("GetTickets")]
    public async Task<IActionResult> GetTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tickets")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to list tickets.");

        var tickets = await _context.Tickets.ToListAsync();

        return new OkObjectResult(tickets);
    }

    [Function("GetTicketById")]
    public async Task<IActionResult> GetTicketById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tickets/{id:guid}")] HttpRequest req,
        Guid id)
    {
        _logger.LogInformation("Lade Ticket mit ID {Id}...", id);

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return new NotFoundObjectResult(new { Message = $"Ticket mit ID {id} wurde nicht gefunden." });
        }

        return new OkObjectResult(ticket);
    }

    [Function("UpdateTicket")]
    public async Task<IActionResult> UpdateTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "tickets/{id:guid}")] HttpRequest req,
        Guid id)
    {
        _logger.LogInformation("Aktualisiere Ticket mit ID {Id}...", id);

        var existingTicket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (existingTicket == null)
        {
            return new NotFoundObjectResult(new { Message = $"Ticket mit ID {id} nicht gefunden." });
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        SupportTicket? updatedData;
        try
        {
            updatedData = JsonSerializer.Deserialize<SupportTicket>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Lesen des JSON-Bodys.");
            return new BadRequestObjectResult("Ungültiges JSON-Format.");
        }

        if (updatedData == null)
        {
            return new BadRequestObjectResult(new { Message = "Ungültige Daten im Request Body." });
        }

        existingTicket.Title = updatedData.Title;
        existingTicket.Description = updatedData.Description;
        existingTicket.User = updatedData.User;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket ID {Id} erfolgreich in Azure SQL aktualisiert.", id);

        return new OkObjectResult(existingTicket);
    }

    [Function("DeleteTicket")]
    public async Task<IActionResult> DeleteTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tickets/{id:guid}")] HttpRequest req,
        Guid id)
    {
        _logger.LogInformation("Lösche Ticket mit ID {Id}...", id);

        var ticketToDelete = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticketToDelete == null)
        {
            return new NotFoundObjectResult(new { Message = $"Ticket mit ID {id} nicht gefunden." });
        }

        _context.Tickets.Remove(ticketToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket ID {Id} erfolgreich gelöscht.", id);

        return new OkObjectResult(new { Message = $"Ticket {id} wurde erfolgreich gelöscht." });
    }
}

// Unser erweitertes Datenmodell mit ID für die Datenbank
public class SupportTicket
{
    public int IdDatabase { get; set; } // Primärschlüssel für EF Core
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? User { get; set; }
}