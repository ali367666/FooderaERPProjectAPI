using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class ExtendLicenseRequest
{
    public int Months { get; set; } = 1;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
