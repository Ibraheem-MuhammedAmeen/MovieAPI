﻿using FluentValidation;
using MinimalAPIsMovies.DTOs;

namespace MinimalAPIsMovies.Validations
{
    public class CreateActorDTOValidator : AbstractValidator<CreateActorDTO>
    {
        public CreateActorDTOValidator()
        {
            RuleFor(x => x.Name).NotEmpty()
                .WithMessage(ValidationUtilities.NonEmptyMessage)
                .MaximumLength(150)
                    .WithMessage(ValidationUtilities.MaximumLengthMessage)

                .Must(ValidationUtilities.FirstLetterIsUppercase).WithMessage(ValidationUtilities.FirstLetterIsUpperCaseMessage);

            var minimumDate = new DateTime(1900, 1, 1);
            RuleFor(p => p.DateOfBirth).GreaterThanOrEqualTo(minimumDate)
                .WithMessage(ValidationUtilities.GreaterThanDate(minimumDate));
        }

        
    }
}
