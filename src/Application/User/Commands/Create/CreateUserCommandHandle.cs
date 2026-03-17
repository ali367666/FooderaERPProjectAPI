using Application.Common.Interfaces;
using Application.User.Dtos.Responce;
using MediatR;

namespace Application.User.Commands.Create;

public class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IIdentityService _identityService;

    public CreateUserCommandHandler(
        ICompanyRepository companyRepository,
        IIdentityService identityService)
    {
        _companyRepository = companyRepository;
        _identityService = identityService;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.dto;

        // ✅ Company yoxla
        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
            throw new Exception("Company tapılmadı.");

        // ✅ Email yoxla
        var emailExists = await _identityService.EmailExistsAsync(dto.Email);
        if (emailExists)
            throw new Exception("Bu email artıq mövcuddur.");

        // ✅ Username yoxla
        var userNameExists = await _identityService.UserNameExistsAsync(dto.UserName);
        if (userNameExists)
            throw new Exception("Bu username artıq mövcuddur.");

        // ✅ User yarat
        var createResult = await _identityService.CreateUserAsync(
            dto.FullName,
            dto.UserName,
            dto.Email,
            dto.Password,
            dto.WorkplaceType,
            dto.CompanyId,
            null // 👈 hələ Restaurant yoxdur
        );

        if (!createResult.Succeeded)
            throw new Exception(string.Join(", ", createResult.Errors));

        // ✅ Role ver
        var roleAdded = await _identityService.AddToRoleAsync(createResult.UserId, "User");
        if (!roleAdded)
            throw new Exception("User role-a əlavə olunmadı.");

        // ✅ Response
        return new CreateUserResponse
        {
            Id = createResult.UserId,
            FullName = dto.FullName,
            UserName = dto.UserName,
            Email = dto.Email
        };
    }
}