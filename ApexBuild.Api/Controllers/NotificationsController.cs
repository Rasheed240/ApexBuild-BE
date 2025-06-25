using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Notifications.Queries.GetNotifications;
using ApexBuild.Application.Features.Notifications.Queries.GetUnreadNotifications;
using ApexBuild.Application.Features.Notifications.Queries.GetUnreadCount;
using ApexBuild.Application.Features.Notifications.Commands.MarkAsRead;
using ApexBuild.Application.Features.Notifications.Commands.MarkAllAsRead;
using ApexBuild.Application.Features.Notifications.Commands.DeleteNotification;
using ApexBuild.Contracts.Responses;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get notifications with pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<GetNotificationsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetNotificationsResponse>>> GetNotifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isRead = null,
            [FromQuery] NotificationType? type = null)
        {
            var query = new GetNotificationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                IsRead = isRead,
                Type = type
            };

            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Notifications retrieved successfully"));
        }

        /// <summary>
        /// Get unread notifications
        /// </summary>
        [HttpGet("unread")]
        [ProducesResponseType(typeof(ApiResponse<GetUnreadNotificationsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetUnreadNotificationsResponse>>> GetUnreadNotifications()
        {
            var query = new GetUnreadNotificationsQuery();
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Unread notifications retrieved successfully"));
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread/count")]
        [ProducesResponseType(typeof(ApiResponse<GetUnreadCountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<GetUnreadCountResponse>>> GetUnreadCount()
        {
            var query = new GetUnreadCountQuery();
            var response = await _mediator.Send(query);
            return Ok(ApiResponse.Success(response, "Unread count retrieved successfully"));
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{notificationId}/read")]
        [ProducesResponseType(typeof(ApiResponse<MarkAsReadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<MarkAsReadResponse>>> MarkAsRead(Guid notificationId)
        {
            var command = new MarkAsReadCommand { NotificationId = notificationId };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPut("read-all")]
        [ProducesResponseType(typeof(ApiResponse<MarkAllAsReadResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<MarkAllAsReadResponse>>> MarkAllAsRead()
        {
            var command = new MarkAllAsReadCommand();
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }

        /// <summary>
        /// Delete a notification (soft delete)
        /// </summary>
        [HttpDelete("{notificationId}")]
        [ProducesResponseType(typeof(ApiResponse<DeleteNotificationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<DeleteNotificationResponse>>> DeleteNotification(Guid notificationId)
        {
            var command = new DeleteNotificationCommand { NotificationId = notificationId };
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.Message));
        }
    }
}

