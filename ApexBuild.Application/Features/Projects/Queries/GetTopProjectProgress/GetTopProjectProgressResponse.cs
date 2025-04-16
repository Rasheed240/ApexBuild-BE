using System.Collections.Generic;

namespace ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

public class GetTopProjectProgressResponse
{
    public List<ProjectProgressDto> Items { get; set; } = new List<ProjectProgressDto>();
}
