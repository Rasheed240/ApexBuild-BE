using System.Net;
using System.Text.Json;
using System.Linq;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Contracts.Responses;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace ApexBuild.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private const string SwaggerPath = "/swagger";
    private const string OpenApiPath = "/openapi";
    private const string HangfirePath = "/hangfire";
    private const string ContentTypeJson = "application/json";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(SwaggerPath) ||
            context.Request.Path.StartsWithSegments(OpenApiPath) ||
            context.Request.Path.StartsWithSegments(HangfirePath))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, ex);
            }
            else
            {
                _logger.LogError(ex, "An exception occurred but response has already started");
                throw;
            }
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot handle exception properly");
            return;
        }

        var response = context.Response;
        response.ContentType = ContentTypeJson;

        var responseModel = ApiResponse.Failure<object>(exception.Message);

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationException.Errors.ToList();
                responseModel.Errors = errors
                    .GroupBy((ValidationFailure e) => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                responseModel.Message = "Validation failed";
                _logger.LogWarning(exception, "Validation error with {ErrorCount} violation(s)", errors.Count);
                break;

            case NotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                _logger.LogWarning(exception, "Resource not found");
                break;

            case BadRequestException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning(exception, "Bad request");
                break;

            case UnauthorizedException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                _logger.LogWarning(exception, "Unauthorized access attempt");
                break;

            case ForbiddenException:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                _logger.LogWarning(exception, "Forbidden access attempt");
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                _logger.LogWarning(exception, "Unauthorized access");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                responseModel.Message = "An error occurred while processing your request";
                _logger.LogError(exception, "An unexpected error occurred");
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(responseModel, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

