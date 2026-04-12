using MediatR;

namespace Application.MenuCategories.Commands.Delete;

public record DeleteMenuCategoryCommand(
    int Id,
    int CompanyId) : IRequest;