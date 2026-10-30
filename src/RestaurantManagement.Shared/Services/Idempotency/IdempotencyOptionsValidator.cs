using Microsoft.Extensions.Options;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    public class IdempotencyOptionsValidator : IValidateOptions<IdempotencyOptions>
    {
        public ValidateOptionsResult Validate(string? name, IdempotencyOptions options)
        {
            var failures = new List<string>();
            Require(options.RetentionDays, nameof(IdempotencyOptions.RetentionDays), failures);
            Require(options.InProgressTimeoutSeconds, nameof(IdempotencyOptions.InProgressTimeoutSeconds), failures);
            Require(options.CleanupIntervalMinutes, nameof(IdempotencyOptions.CleanupIntervalMinutes), failures);

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }

        private static void Require(int value, string setting, List<string> failures)
        {
            if (value <= 0)
                failures.Add($"{IdempotencyOptions.SectionName}:{setting} must be greater than zero.");
        }
    }
}
