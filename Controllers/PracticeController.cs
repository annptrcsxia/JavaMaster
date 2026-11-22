using JavaMaster.Api.Data;
using JavaMaster.Api.Entities;
using JavaMaster.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JavaMaster.Api.Controllers;

[ApiController]
[Route("api/practice")]
public class PracticeController : ControllerBase
{
    private readonly JavaRunnerService _runner;
    private readonly FriendlyErrorService _errors;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _me;

    public PracticeController(JavaRunnerService runner, FriendlyErrorService errors,
                              AppDbContext db, ICurrentUserService me)
    {
        _runner = runner;
        _errors = errors;
        _db = db;
        _me = me;
    }

    [HttpPost("run")]
    public async Task<ActionResult<PracticeResponse>> Run([FromBody] RunRequest req)
    {
        if (req.ItemId is < 1 or > PracticeChecks.ItemCount || string.IsNullOrWhiteSpace(req.Code) || req.Code.Length > 5000)
            return BadRequest(PracticeResponse.Fail("bad_request", "Write some code first.", ""));

        var run = await _runner.CompileAndRunAsync(req.Code);
        PracticeResponse result;
        var passed = false;

        switch (run.Stage)
        {
            case RunStage.Busy:
                result = PracticeResponse.Fail("busy", "Many learners are practicing right now.", "Try again in a moment."); break;
            case RunStage.CompileTimedOut:
                result = PracticeResponse.Fail("timeout", "Checking took too long.", "Try again."); break;
            case RunStage.RunTimedOut:
                result = PracticeResponse.Fail("timeout", "Your program took too long.", "Check for a loop that never ends."); break;
            case RunStage.CompileError:
                result = await _errors.FromCompileErrorAsync(run.StdErr, req.Code); break;
            case RunStage.RuntimeError:
                result = FriendlyErrorService.FromRuntimeError(run.StdErr); break;
            default:
            {
                var wrong = PracticeChecks.Check(req.ItemId, req.Code, run.StdOut);
                passed = wrong == null;
                result = wrong ?? PracticeResponse.Success(run.StdOut);
                break;
            }
        }

        // The server remembers every item that passed. The browser cannot claim a pass.
        if (passed) await RecordPassAsync(req);
        return result;
    }

    private async Task RecordPassAsync(RunRequest req)
    {
        var user = await _me.GetAsync();
        var (node, unlocked, _) = await LearnRules.LocateAsync(_db, user.Id, req.NodeId);
        if (node == null || !unlocked || node.NodeType != NodeType.Practice) return;

        var pass = await _db.PracticePasses.FindAsync(user.Id, req.NodeId, req.ItemId);
        if (pass == null)
            _db.PracticePasses.Add(new PracticePass { UserId = user.Id, LearnNodeId = req.NodeId, ItemId = req.ItemId });
        else
            pass.PassedAt = DateTime.UtcNow;      // a replay has to pass again
        await _db.SaveChangesAsync();
    }
}