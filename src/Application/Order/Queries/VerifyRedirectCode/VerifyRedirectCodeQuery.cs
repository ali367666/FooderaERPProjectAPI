using MediatR;

namespace Application.Orders.Queries.VerifyRedirectCode;

/// <summary>Checks that a POS code belongs to someone allowed to redirect orders (Pos.RedirectUser).</summary>
public record VerifyRedirectCodeQuery(string Code) : IRequest<string>;
