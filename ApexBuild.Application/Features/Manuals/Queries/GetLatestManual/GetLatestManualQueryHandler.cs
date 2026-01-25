using ApexBuild.Application.Common.Interfaces;
using MediatR;

namespace ApexBuild.Application.Features.Manuals.Queries.GetLatestManual;

public class GetLatestManualQueryHandler : IRequestHandler<GetLatestManualQuery, GetLatestManualResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLatestManualQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetLatestManualResponse?> Handle(GetLatestManualQuery request, CancellationToken cancellationToken)
    {
        var all = await _unitOfWork.UserManuals.GetAllAsync(cancellationToken);
        var latest = all.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        if (latest is null) return null;

        return new GetLatestManualResponse
        {
            Id            = latest.Id,
            Title         = latest.Title,
            Version       = latest.Version,
            FileUrl       = latest.FileUrl,
            FileSizeBytes = latest.FileSizeBytes,
            UploadedAt    = latest.CreatedAt,
            UploadedByName = latest.UploadedByUser != null
                ? $"{latest.UploadedByUser.FirstName} {latest.UploadedByUser.LastName}"
                : string.Empty,
        };
    }
}
