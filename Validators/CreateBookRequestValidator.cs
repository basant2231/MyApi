using FluentValidation;
using MyApi.Models.DTOs;

namespace MyApi.Validators;

public class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Isbn)
            .Matches(@"^[\d\-]{10,17}$").WithMessage("ISBN must be a valid ISBN-10 or ISBN-13.")
            .When(x => !string.IsNullOrEmpty(x.Isbn));

        RuleFor(x => x.PageCount)
            .InclusiveBetween(1, 50000).WithMessage("Page count must be between 1 and 50,000.")
            .When(x => x.PageCount.HasValue);

        RuleFor(x => x.Genre)
            .MaximumLength(50).WithMessage("Genre must not exceed 50 characters.")
            .When(x => x.Genre is not null);

        RuleFor(x => x.PublishedDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Published date cannot be in the future.")
            .When(x => x.PublishedDate.HasValue);
    }
}

