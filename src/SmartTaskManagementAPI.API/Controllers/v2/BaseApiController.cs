using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Common.Models;

namespace SmartTaskManagementAPI.API.Controllers.v2
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class BaseApiController : ControllerBase
    {
        private IMediator? _mediator;
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        protected ActionResult<ApiResponse<T>> HandleResult<T>(T result, string message = "")
        {
            if (result == null)
            {
                return NotFound(ApiResponse<T>.ErrorResponse("Resource not found"));
            }

            return Ok(ApiResponse<T>.SuccessResponse(result, message));
        }

        protected ActionResult<ApiResponse<T>> HandlePaginatedResult<T>(
            PaginatedResult<T> result,
            string message = "")
        {
            if (result == null || result.Items.Count == 0)
            {
                return Ok(new PaginatedApiResponse<T>(
                    new List<T>(),
                    result?.PageNumber ?? 1,
                    result?.PageSize ?? 10,
                    result?.TotalCount ?? 0,
                    "No records found"));
            }

            return Ok(new PaginatedApiResponse<T>(
                result.Items,
                result.PageNumber,
                result.PageSize,
                result.TotalCount,
                message));
        }

        protected ActionResult<ApiResponse> HandleSuccess(string message = "Operation completed successfully")
        {
            return Ok(ApiResponse.SuccessResponse(null, message));
        }

        protected ActionResult<ApiResponse> HandleError(string message, List<string>? errors = null)
        {
            return BadRequest(ApiResponse.ErrorResponse(message, errors));
        }
    }
}
