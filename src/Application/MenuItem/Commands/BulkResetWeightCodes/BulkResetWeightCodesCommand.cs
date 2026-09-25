using MediatR;

namespace Application.MenuItems.Commands.BulkResetWeightCodes;

/// <summary>Regenerates the weight-code sticker for every weight-sold (Kg/Gram) menu item in the
/// company at once, instead of resetting them one at a time.</summary>
public record BulkResetWeightCodesCommand : IRequest<int>;
