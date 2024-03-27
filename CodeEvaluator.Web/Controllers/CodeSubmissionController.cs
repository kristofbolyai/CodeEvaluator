using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Data.Models;
using CodeEvaluator.Runner.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace CodeEvaluator.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CodeSubmissionController(CodeDataDbContext codeDataDbContext, CodeExecutionHandler executionHandler)
    : ControllerBase
{
    // POST: api/CodeSubmission/Submit
    [HttpPost("Submit")]
    public async Task<IActionResult> SubmitCode(CodeLanguage codeLanguage, [FromForm] IFormFile file)
    {
        CodeSubmission codeSubmission = await executionHandler.SetupContainerFolder(file.OpenReadStream(), codeLanguage);

        // Add the code submission to the database
        codeDataDbContext.CodeSubmissions.Add(codeSubmission);
        await codeDataDbContext.SaveChangesAsync();

        return Ok(codeSubmission);
    }
}