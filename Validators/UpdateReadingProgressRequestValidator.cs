using FluentValidation;
using MyApi.Models.DTOs;

namespace MyApi.Validators;

public class UpdateReadingProgressRequestValidator : AbstractValidator<UpdateReadingProgressRequest>
{
    public UpdateReadingProgressRequestValidator()
    {
        RuleFor(x => x.CurrentPage)
            .InclusiveBetween(0, 50000).WithMessage("Current page must be between 0 and 50,000.")
            .When(x => x.CurrentPage.HasValue);

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 3).WithMessage("Status must be 0 (WantToRead), 1 (Reading), 2 (Finished), or 3 (Dropped).")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.UserRating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.")
            .When(x => x.UserRating.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}
