using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class CompanyLicenseDetailResponse : LicenseStatusResponse
{
    public decimal? MonthlyPrice { get; set; }
    public List<LicensePaymentResponse> Payments { get; set; } = new();
    public List<TrustedDeviceResponse> Devices { get; set; } = new();
}
