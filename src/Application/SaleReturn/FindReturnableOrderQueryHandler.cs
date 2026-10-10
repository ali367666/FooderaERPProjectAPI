using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Enums;
using MediatR;

namespace Application.SaleReturn;

public class FindReturnableOrderQueryHandler : IRequestHandler<FindReturnableOrderQuery, ReturnableOrderResponse>
{
    private readonly ISaleReturnRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public FindReturnableOrderQueryHandler(ISaleReturnRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<ReturnableOrderResponse> Handle(FindReturnableOrderQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Qəbz barkodunu oxudun.");

        var companyId = _currentUserService.CompanyId;
        var order = await _repository.FindPaidOrderForReturnAsync(companyId, request.Code, cancellationToken);
        if (order is null)
            throw new NotFoundException("Bu barkodla ödənilmiş satış tapılmadı.");

        var returns = await _repository.GetByOrderIdAsync(companyId, order.Id, cancellationToken);
        return SaleReturnMapping.MapOrder(order, returns);
    }
}
