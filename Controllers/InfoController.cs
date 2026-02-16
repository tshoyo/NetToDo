using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace NetToDo.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public InfoController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            App = "NetToDo API",
            Version = "1.0.0",
            OpenApiSchema = "/openapi/v1.json"
        });
    }
}
