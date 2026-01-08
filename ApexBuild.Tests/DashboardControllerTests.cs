using System.Threading.Tasks;
using ApexBuild.Api.Controllers;
using ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats;
using ApexBuild.Contracts.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;

namespace ApexBuild.Tests;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetStats_ReturnsOk_WithApiResponse()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var sample = new GetDashboardStatsResponse { ActiveProjects = 3, TeamMembers = 10 };
        mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardStatsQuery>(), default)).ReturnsAsync(sample as GetDashboardStatsResponse);

        var controller = new DashboardController(mediatorMock.Object);

        // Act
        var actionResult = await controller.GetStats();

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var ok = actionResult.Result as OkObjectResult;
        ok!.Value.Should().BeOfType<ApiResponse<GetDashboardStatsResponse>>();
        var apiResp = ok.Value as ApiResponse<GetDashboardStatsResponse>;
        apiResp!.Data.ActiveProjects.Should().Be(3);
    }
}
