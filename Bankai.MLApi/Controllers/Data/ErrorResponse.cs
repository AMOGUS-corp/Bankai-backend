namespace Bankai.MLApi.Controllers.Data;

public record ErrorResponse(IEnumerable<ErrorModel> Errors);

public record ErrorModel(string FieldName, string Message);