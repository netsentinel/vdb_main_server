using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace main_server_api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class LinksController : ControllerBase
{
}
