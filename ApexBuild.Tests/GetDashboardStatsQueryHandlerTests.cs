using System.Threading;
using System.Threading.Tasks;
using ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using Moq;
using Xunit;
using FluentAssertions;

namespace ApexBuild.Tests;

public class GetDashboardStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCounts_FromUnitOfWork()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        unitOfWorkMock.Setup(u => u.Projects.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Domain.Entities.Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        unitOfWorkMock.Setup(u => u.Users.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Domain.Entities.User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        unitOfWorkMock.Setup(u => u.Tasks.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Domain.Entities.ProjectTask, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var handler = new GetDashboardStatsQueryHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ActiveProjects.Should().Be(5);
        result.TeamMembers.Should().Be(20);
        result.TotalTasks.Should().Be(100);
    }
}
