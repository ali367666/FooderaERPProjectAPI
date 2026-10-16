using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class UpdateCompanyLicenseRequest
{
    public string DeploymentType { get; set; } = "Cloud";
    public bool RemoteAccessEnabled { get; set; }
    public DateTime? RemoteAccessExpiresAtUtc { get; set; }
    public bool OfflineModeEnabled { get; set; }
    public decimal? MonthlyPrice { get; set; }
}
