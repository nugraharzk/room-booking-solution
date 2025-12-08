using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RoomBooking.API.Infrastructure
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            switch (exception)
            {
                case ArgumentException _:
                    problemDetails.Title = "Bad Request";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = exception.Message;
                    break;
                case KeyNotFoundException _:
                    problemDetails.Title = "Not Found";
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Detail = exception.Message;
                    break;
                case InvalidOperationException _:
                    // Often used for domain rule violations (logic conflicts)
                    problemDetails.Title = "Conflict";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = exception.Message;
                    break;
                case UnauthorizedAccessException _:
                     problemDetails.Title = "Unauthorized";
                     problemDetails.Status = StatusCodes.Status401Unauthorized;
                     problemDetails.Detail = "Access is denied.";
                     break;
                default:
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Detail = "An unexpected error occurred.";
                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
