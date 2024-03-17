using Bankai.MLApi.Controllers.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bankai.MLApi.Validators;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ModelState.IsValid)
        {
            await next();
            return;
        }

        context.Result = new BadRequestObjectResult(
            new ErrorResponse(context
                .ModelState
                .Where(x => x.Value is not null && x.Value.Errors.Any())
                .SelectMany(x => x.Value!.Errors.Select(y => (key: x.Key, message: y.ErrorMessage)))
                .Select(x => new ErrorModel(x.key, x.message))));
    }
}
