using MediatR;

namespace ApexBuild.Application.Features.Notifications.Commands.MarkAllAsRead;

public record MarkAllAsReadCommand : IRequest<MarkAllAsReadResponse>
{
}

