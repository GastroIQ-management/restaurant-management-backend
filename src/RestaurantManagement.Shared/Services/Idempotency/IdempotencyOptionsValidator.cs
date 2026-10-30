using Microsoft.Extensions.Options;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    public class IdempotencyOptionsValidator : IValidateOptions<IdempotencyOptions>
    {
        public ValidateOptionsResult Validate(string? name, IdempotencyOptions options) => throw new NotImplementedException();
    }
}
