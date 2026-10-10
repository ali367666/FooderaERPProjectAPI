using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class DeviceRegistrationCodeResponse
{
    public string Code { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}
