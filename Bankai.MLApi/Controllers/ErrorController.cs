using Microsoft.AspNetCore.Diagnostics;

namespace Bankai.MLApi.Controllers;

public class ErrorController : Controller
{
    [Route("error")]
    protected IActionResult HandleErrorDevelopment(
        [FromServices] IHostEnvironment hostEnvironment)
    {
        var exceptionHandlerFeature =
            HttpContext.Features.Get<IExceptionHandlerFeature>();

        return exceptionHandlerFeature is not null
            ? Problem(
                detail: exceptionHandlerFeature.Error.StackTrace,
                title: exceptionHandlerFeature.Error.Message)
            : Empty;
    }
}