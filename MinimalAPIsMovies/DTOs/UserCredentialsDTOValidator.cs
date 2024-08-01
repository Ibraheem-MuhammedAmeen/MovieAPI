using FluentValidation;

namespace MinimalAPIsMovies.DTOs
{
    public class UserCredentialsDTOValidator : AbstractValidator<UserCredentialsDTO>
    {
        public UserCredentialsDTOValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(Validations.ValidationUtilities.NonEmptyMessage)
                .MaximumLength(256).WithMessage(Validations.ValidationUtilities.MaximumLengthMessage)
                .EmailAddress().WithMessage(Validations.ValidationUtilities.EmailAddressMessage);

            RuleFor(x => x.Password).NotEmpty().WithMessage(Validations.ValidationUtilities.NonEmptyMessage);
        }
    }
}
