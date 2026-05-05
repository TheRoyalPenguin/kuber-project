using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace MyApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<NotesController> _logger;

    public NotesController(IDistributedCache cache, ILogger<NotesController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public class NoteDto
    {
        public string Content { get; set; } = string.Empty;
    }

    // ПОЗИТИВНЫЙ И НЕГАТИВНЫЙ GET
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNote(string id)
    {
        var cachedNote = await _cache.GetStringAsync(id);

        if (!string.IsNullOrEmpty(cachedNote))
        {
            // Позитивный лог (нашли в кэше)
            _logger.LogInformation("Cache hit for note {NoteId}", id);
            return Ok(new { Id = id, Content = cachedNote, Source = "Redis Cache" });
        }

        // Негативный лог (не нашли) - на такие мы МОЖЕМ настроить алерты (как пример, хотя 404 это не всегда алерт)
        _logger.LogWarning("Note {NoteId} not found in cache", id);
        return NotFound(new { Error = "Note not found" });
    }

    // ПОЗИТИВНЫЙ И НЕГАТИВНЫЙ POST
    [HttpPost("{id}")]
    public async Task<IActionResult> CreateNote(string id, [FromBody] NoteDto request)
    {
        // Негативный кейс: пустой контент. Генерируем Error для ElastAlert!
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            _logger.LogError("Failed to create note {NoteId}: Content is empty", id);
            return BadRequest(new { Error = "Content cannot be empty" });
        }

        // Позитивный кейс: сохраняем в Redis
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)); // Кэшируем на 10 минут

        await _cache.SetStringAsync(id, request.Content, options);

        _logger.LogInformation("Successfully created note {NoteId}", id);
        return Ok(new { Id = id, Message = "Note created and cached" });
    }
}