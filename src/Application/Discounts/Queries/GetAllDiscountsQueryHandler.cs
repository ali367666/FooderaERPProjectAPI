using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Discounts.Dtos;
using MediatR;

namespace Application.Discounts.Queries;

public class GetAllDiscountsQueryHandler : IRequestHandler<GetAllDiscountsQuery, List<DiscountResponse>>
{
    private readonly IDiscountRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetAllDiscountsQueryHandler(IDiscountRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<DiscountResponse>> Handle(GetAllDiscountsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetAllAsync(_currentUser.CompanyId, cancellationToken);
        return list.Select(DiscountMapper.ToResponse).OrderByDescending(x => x.Id).ToList();
    }
}
