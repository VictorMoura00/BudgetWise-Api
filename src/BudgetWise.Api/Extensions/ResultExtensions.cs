using BudgetWise.Domain.Common.Results;
using Microsoft.AspNetCore.Http;

namespace BudgetWise.Api.Extensions;

/// <summary>
/// Converte Result/Result&lt;T&gt; em respostas HTTP para Minimal APIs.
/// Equivalente ao HandleFailure de Controllers, adaptado para IResult.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToResponse<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);

        return ToProblemDetails(result.Error);
    }

    public static IResult ToResponse(this Result result, Func<IResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess();

        return ToProblemDetails(result.Error);
    }

    private static IResult ToProblemDetails(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = error.Type switch
        {
            ErrorType.Validation => "Bad Request",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            _ => "Internal Server Error"
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                { "errors", new[] { new { error.Code, error.Description } } }
            });
    }
}