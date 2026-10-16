using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Enums;
using MediatR;

namespace Application.SaleReturn;

public class GetSaleReturnsQueryHandler : IRequestHandler<GetSaleReturnsQuery, List<SaleReturnResponse>>
{
    private readonly ISaleReturnRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetSaleReturnsQueryHandler(ISaleReturnRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<SaleReturnResponse>> Handle(GetSaleReturnsQuery request, CancellationToken cancellationToken)
    {
        var returns = await _repository.GetBetweenAsync(
            _currentUserService.CompanyId, request.RestaurantId, request.From, request.To, cancellationToken);
        return returns.Select(SaleReturnMapping.MapReturn).ToList();
    }
}
