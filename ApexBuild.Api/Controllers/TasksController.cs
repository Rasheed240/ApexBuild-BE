using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Tasks.Commands.CreateTask;
using ApexBuild.Application.Features.Tasks.Commands.UpdateTask;
using ApexBuild.Application.Features.Tasks.Commands.DeleteTask;
using ApexBuild.Application.Features.Tasks.Commands.SubmitTaskUpdate;
using ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateBySupervisor;
using ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByAdmin;
using ApexBuild.Application.Features.Tasks.Commands.MarkTaskComplete;
using ApexBuild.Application.Features.Tasks.Commands.MarkTaskDone;
using ApexBuild.Application.Features.Tasks.Commands.AddTaskComment;
using ApexBuild.Application.Features.Tasks.Queries.GetTaskById;
using ApexBuild.Application.Features.Tasks.Queries.ListTasks;
using ApexBuild.Application.Features.Tasks.Queries.GetMyTasks;
using ApexBuild.Application.Features.Tasks.Queries.GetTaskComments;
using ApexBuild.Contracts.Responses;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateTaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CreateTaskResponse>>> CreateTask([FromBody] CreateTaskCommand command)
    {
        var response = await _mediator.Send(command);
        return CreatedAtAction(
            nameof(GetTaskById),
            new { taskId = response.TaskId },
            ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Get a task by ID
    /// </summary>
    [HttpGet("{taskId}")]
    [ProducesResponseType(typeof(ApiResponse<GetTaskByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GetTaskByIdResponse>>> GetTaskById(Guid taskId)
    {
        var query = new GetTaskByIdQuery { TaskId = taskId };
        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "Task retrieved successfully"));
    }

    /// <summary>
    /// Update a task
    /// </summary>
    [HttpPut("{taskId}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UpdateTaskResponse>>> UpdateTask(
        Guid taskId,
        [FromBody] UpdateTaskCommand command)
    {
        var commandWithId = command with { TaskId = taskId };
        var response = await _mediator.Send(commandWithId);
        return Ok(ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Delete a task (soft delete)
    /// </summary>
    [HttpDelete("{taskId}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<DeleteTaskResponse>>> DeleteTask(Guid taskId)
    {
        var command = new DeleteTaskCommand { TaskId = taskId };
        var response = await _mediator.Send(command);
        return Ok(ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// List tasks with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ListTasksResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ListTasksResponse>>> ListTasks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] int? priority = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? assignedToUserId = null,
        [FromQuery] Guid? parentTaskId = null,
        [FromQuery] string? searchTerm = null)
    {
        var query = new ListTasksQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            Priority = priority,
            DepartmentId = departmentId,
            AssignedToUserId = assignedToUserId,
            ParentTaskId = parentTaskId,
            SearchTerm = searchTerm
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "Tasks retrieved successfully"));
    }

    /// <summary>
    /// Submit a daily report (task update) for a task
    /// </summary>
    [HttpPost("{taskId}/updates")]
    [ProducesResponseType(typeof(ApiResponse<SubmitTaskUpdateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SubmitTaskUpdateResponse>>> SubmitTaskUpdate(
        Guid taskId,
        [FromBody] SubmitTaskUpdateCommand command)
    {
        var commandWithTaskId = command with { TaskId = taskId };
        var response = await _mediator.Send(commandWithTaskId);
        return CreatedAtAction(
            nameof(GetTaskById),
            new { taskId = taskId },
            ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Approve or reject a task update by supervisor
    /// </summary>
    [HttpPost("updates/{updateId}/approve-supervisor")]
    [ProducesResponseType(typeof(ApiResponse<ApproveTaskUpdateBySupervisorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ApproveTaskUpdateBySupervisorResponse>>> ApproveTaskUpdateBySupervisor(
        Guid updateId,
        [FromBody] ApproveTaskUpdateBySupervisorCommand command)
    {
        var commandWithUpdateId = command with { UpdateId = updateId };
        var response = await _mediator.Send(commandWithUpdateId);
        return Ok(ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Approve or reject a task update by admin
    /// </summary>
    [HttpPost("updates/{updateId}/approve-admin")]
    [ProducesResponseType(typeof(ApiResponse<ApproveTaskUpdateByAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ApproveTaskUpdateByAdminResponse>>> ApproveTaskUpdateByAdmin(
        Guid updateId,
        [FromBody] ApproveTaskUpdateByAdminCommand command)
    {
        var commandWithUpdateId = command with { UpdateId = updateId };
        var response = await _mediator.Send(commandWithUpdateId);
        return Ok(ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Mark a task as complete
    /// </summary>
    [HttpPost("{taskId}/complete")]
    [ProducesResponseType(typeof(ApiResponse<MarkTaskCompleteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<MarkTaskCompleteResponse>>> MarkTaskComplete(Guid taskId)
    {
        var command = new MarkTaskCompleteCommand { TaskId = taskId };
        var response = await _mediator.Send(command);
        return Ok(ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Add a comment to a task
    /// </summary>
    [HttpPost("{taskId}/comments")]
    [ProducesResponseType(typeof(ApiResponse<AddTaskCommentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AddTaskCommentResponse>>> AddTaskComment(
        Guid taskId,
        [FromBody] AddTaskCommentCommand command)
    {
        var commandWithTaskId = command with { TaskId = taskId };
        var response = await _mediator.Send(commandWithTaskId);
        return CreatedAtAction(
            nameof(GetTaskById),
            new { taskId = taskId },
            ApiResponse.Success(response, response.Message));
    }

    /// <summary>
    /// Get all comments for a task
    /// </summary>
    [HttpGet("{taskId}/comments")]
    [ProducesResponseType(typeof(ApiResponse<GetTaskCommentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GetTaskCommentsResponse>>> GetTaskComments(
        Guid taskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetTaskCommentsQuery
        {
            TaskId = taskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "Comments retrieved successfully"));
    }

    /// <summary>
    /// Get all subtasks for a task
    /// </summary>
    [HttpGet("{taskId}/subtasks")]
    [ProducesResponseType(typeof(ApiResponse<ListTasksResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ListTasksResponse>>> GetSubtasks(Guid taskId)
    {
        var query = new ListTasksQuery
        {
            ParentTaskId = taskId,
            PageNumber = 1,
            PageSize = 100
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "Subtasks retrieved successfully"));
    }

    /// <summary>
    /// Get my tasks filtered by organization
    /// </summary>
    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(ApiResponse<GetMyTasksResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetMyTasksResponse>>> GetMyTasks(
        [FromQuery] Guid? organizationId = null,
        [FromQuery] Guid? projectId = null,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] int? priority = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetMyTasksQuery
        {
            OrganizationId = organizationId,
            ProjectId = projectId,
            Status = status,
            Priority = priority,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "My tasks retrieved successfully"));
    }

    /// <summary>
    /// Mark a task as done (for assignees)
    /// </summary>
    [HttpPost("{taskId}/done")]
    [ProducesResponseType(typeof(ApiResponse<MarkTaskDoneResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<MarkTaskDoneResponse>>> MarkTaskDone(
        Guid taskId,
        [FromBody] MarkTaskDoneCommand command)
    {
        var commandWithTaskId = command with { TaskId = taskId };
        var response = await _mediator.Send(commandWithTaskId);
        return Ok(ApiResponse.Success(response, response.Message));
    }
}

