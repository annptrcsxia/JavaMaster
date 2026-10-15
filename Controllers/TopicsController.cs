using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TopicsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Topic>>> GetAll() =>
        await _db.Topics.Where(t => t.IsActive).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Topic>> Create(Topic topic)
    {
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync();
        return Ok(topic);
    }
}