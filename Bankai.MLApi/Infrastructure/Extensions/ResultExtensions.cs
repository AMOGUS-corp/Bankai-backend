namespace Bankai.MLApi.Infrastructure.Extensions;

public static class ResultExtensions
{
    public static K MatchSync<T, K>(
        this Result<T> result,
        Func<T, K> onSuccess,
        Func<string, K> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }

    public static K FinallySync<K>(
        this UnitResult<string> result,
        Func<K> onSuccess,
        Func<string, K> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result.Error);
    }
    
    public static IActionResult ToActionResult<T>(
      this Result<T> result,
      int successStatusCode = 200,
      int failStatusCode = 400)
    {
      return result.MatchSync((Func<T, IActionResult>) (x => new ObjectResult(x)
      {
        StatusCode = successStatusCode
      }), (Func<string, IActionResult>) (e => new ObjectResult(e)
      {
        StatusCode = failStatusCode
      }));
    }

    public static async Task<IActionResult> ToActionResult<T>(
      this Task<Result<T>> result,
      int successStatusCode = 200,
      int failStatusCode = 400)
    {
      return (await result).MatchSync((Func<T, IActionResult>) (x => new ObjectResult(x)
      {
        StatusCode = successStatusCode
      }), (Func<string, IActionResult>) (e => new ObjectResult(e)
      {
        StatusCode = failStatusCode
      }));
    }

    public static IActionResult ToActionResult(
      this UnitResult<string> result,
      int successStatusCode = 200,
      int failStatusCode = 400)
    {
      return FinallySync(result, (Func<IActionResult>) (() => new StatusCodeResult(successStatusCode)), (Func<string, IActionResult>) (e => new ObjectResult(e)
      {
        StatusCode = failStatusCode
      }));
    }

    public static async Task<IActionResult> ToActionResult(
      this Task<UnitResult<string>> result,
      int successStatusCode = 200,
      int failStatusCode = 400)
    {
      return FinallySync(await result, (Func<IActionResult>) (() => new StatusCodeResult(successStatusCode)), (Func<string, IActionResult>) (e => new ObjectResult(e)
      {
        StatusCode = failStatusCode
      }));
    }

    public static IActionResult ToView<TModel, TController>(
      this Result<TModel> result,
      TController controller)
      where TController : Controller
    {
      return result.MatchSync((Func<TModel, IActionResult>) (x => controller.View(x)), controller.BadRequest);
    }

    public static async Task<IActionResult> ToView<TModel, TController>(
      this Task<Result<TModel>> result,
      TController controller)
      where TController : Controller
    {
      return (await result).MatchSync((Func<TModel, IActionResult>) (x => controller.View(x)), controller.BadRequest);
    }
}