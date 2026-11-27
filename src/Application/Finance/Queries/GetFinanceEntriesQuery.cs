using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Finance.Commands;
using Application.Finance.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Finance.Queries;

public class GetFinanceEntriesRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int? RestaurantId { get; set; }
    public FinanceEntryType? Type { get; set; }
}

public record GetFinanceEntriesQuery(GetFinanceEntriesRequest Request) : IRequest<List<FinanceEntryResponse>>;

public class GetFinanceEntriesQueryHandler : IRequestHandler<GetFinanceEntriesQuery, List<FinanceEntryResponse>>
{
    private readonly IFinanceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetFinanceEntriesQueryHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<FinanceEntryResponse>> Handle(GetFinanceEntriesQuery query, CancellationToken cancellationToken)
    {
        var r = query.Request;
        if (r.To.Date < r.From.Date)
            throw new BadRequestException("Bitmə tarixi başlanğıc tarixindən əvvəl ola bilməz.");

        var entries = await _repository.GetEntriesAsync(
            _currentUserService.CompanyId, r.RestaurantId is > 0 ? r.RestaurantId : null,
            r.From.Date, r.To.Date.AddDays(1), r.Type, cancellationToken);
        return entries.Select(FinanceEntryMapper.Map).ToList();
    }
}
