using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Identity.Models.Languages;
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Languages
{
    public class UserLanguageService : IUserLanguageService
    {
        private readonly IdentityDbContext _db;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserLanguageService(IdentityDbContext db, IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task UpdateAsync(Guid userId, UpdateLanguageRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive || !user.IsVerified)
                throw new BusinessException(_localizer["UnauthorizedMessage"].Value, 401);

            user.PreferredLanguage = request.Language;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
