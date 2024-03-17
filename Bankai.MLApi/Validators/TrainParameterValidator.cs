using Bankai.MLApi.Services.Training.Data;
using FluentValidation;

namespace Bankai.MLApi.Validators;

public class TrainParameterValidator : AbstractValidator<TrainParameter>
{
    public TrainParameterValidator()
    {
        When(x => x.Name.Equals("TestFraction", StringComparison.InvariantCultureIgnoreCase), () =>
        {
            RuleFor(x => x.Value)
            .NotEmpty();

            When(x => float.TryParse(x.Value, InvariantCulture, out _), () =>
            {
                RuleFor(x => float.Parse(x.Value, InvariantCulture))
                    .ExclusiveBetween(0, 1)
                    .WithMessage("TestFraction must be between 0 and 1!");
            }).Otherwise(() =>
            {
                RuleFor(x => x.Value)
                .Empty()
                .WithMessage("TestFraction value must be float!");
            });
        });

        When(x => x.Name.Equals("NumberOfFolds", StringComparison.InvariantCultureIgnoreCase), () =>
        {
            RuleFor(x => x.Value)
            .NotEmpty();

            When(x => int.TryParse(x.Value, out _), () =>
            {
                RuleFor(x => int.Parse(x.Value))
                    .GreaterThan(1)
                    .WithMessage("NumberOfFolds must be greater than 1!");
            }).Otherwise(() =>
            {
                RuleFor(x => x.Value)
                .Empty()
                .WithMessage("NumberOfFolds must be integer!");
            });
        });

        When(x => x.Name.Equals("Top", StringComparison.InvariantCultureIgnoreCase), () =>
        {
            RuleFor(x => x.Value).NotEmpty();
        });
    }
}
