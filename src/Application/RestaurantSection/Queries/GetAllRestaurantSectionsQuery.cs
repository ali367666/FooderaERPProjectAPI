using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Queries;

public record GetAllRestaurantSectionsQuery(int RestaurantId) : IRequest<List<RestaurantSectionResponse>>;

public class GetAllRestaurantSectionsQueryHandler : IRequestHandler<GetAllRestaurantSectionsQuery, List<RestaurantSectionResponse>>
{
    private readonly IRestaurantSectionRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllRestaurantSectionsQueryHandler(IRestaurantSectionRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<RestaurantSectionResponse>> Handle(GetAllRestaurantSectionsQuery request, CancellationToken cancellationToken)
    {
        var sections = await _repository.GetAllByRestaurantAsync(_currentUserService.CompanyId, request.RestaurantId, cancellationToken);
        return sections.Select(x => new RestaurantSectionResponse
        {
            Id = x.Id,
            RestaurantId = x.RestaurantId,
            Name = x.Name,
            IsActive = x.IsActive,
            Type = x.Type
        }).ToList();
    }
}
