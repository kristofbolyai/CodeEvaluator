using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Data.Models;
using CodeEvaluator.Runner.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace CodeEvaluator.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CodeSubmissionController(CodeDataDbContext codeDataDbContext, CodeQueueHandler queueHandler)
    : ControllerBase
{
    // POST: api/CodeSubmission/Submit
    [HttpPost("Submit")]
    public async Task<IActionResult> SubmitCode(CodeLanguage codeLanguage, [FromForm] IFormFile file)
    {
        // Save the code to disk, add it to the database, and return the code submission
        CodeSubmission codeSubmission = await queueHandler.SaveCodeToDisk(codeDataDbContext, file.OpenReadStream(), codeLanguage);
        return Ok(codeSubmission);
    }
}