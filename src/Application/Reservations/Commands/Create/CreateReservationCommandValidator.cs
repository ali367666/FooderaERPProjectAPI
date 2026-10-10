using FluentValidation;

namespace Application.Reservations.Commands.Create;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("Filialı seçin.");

        RuleFor(x => x.Request.GuestName)
            .NotEmpty().WithMessage("Müştərinin adı mütləqdir.")
            .MaximumLength(150).WithMessage("Ad ən çox 150 simvol ola bilər.");

        RuleFor(x => x.Request.GuestPhone)
            .NotEmpty().WithMessage("Telefon nömrəsi mütləqdir.")
            .MaximumLength(30).WithMessage("Telefon ən çox 30 simvol ola bilər.");

        RuleFor(x => x.Request.GuestCount)
            .GreaterThan(0).WithMessage("Adam sayı mütləqdir (ən azı 1).");

        RuleFor(x => x.Request.ReservationDate)
            .Must(d => d != default).WithMessage("Tarix mütləqdir.");

        RuleFor(x => x.Request.ReservationTime)
            .NotEmpty().WithMessage("Saat mütləqdir.")
            .Must(t => TimeSpan.TryParse(t, out _)).WithMessage("Saat formatı yanlışdır. HH:mm formatında daxil edin.");
    }
}
