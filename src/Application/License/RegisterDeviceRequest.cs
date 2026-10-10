using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class RegisterDeviceRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}
