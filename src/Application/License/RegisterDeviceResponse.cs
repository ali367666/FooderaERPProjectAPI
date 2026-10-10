using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class RegisterDeviceResponse
{
    public string DeviceKey { get; set; } = default!;
    public string DeviceName { get; set; } = default!;
    public int CompanyId { get; set; }
}
