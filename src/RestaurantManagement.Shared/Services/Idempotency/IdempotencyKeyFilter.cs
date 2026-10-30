using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Middleware;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Checks, reserves, replays and saves the <c>Idempotency-Key</c> of a signed-in write, in one transaction with the write.</summary>
    public class IdempotencyKeyFilter : IAsyncResourceFilter
    {
        public const string HeaderName = "Idempotency-Key";

        private static readonly string[] WriteMethods = [HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var http = context.HttpContext;
            var user = http.RequestServices.GetRequiredService<ICurrentUser>();
            var registration = FindStore(context);
            if (user.UserId is not { } userId || registration is null || !http.Request.Headers.TryGetValue(HeaderName, out var header))
            {
                await next();
                return;
            }

            var key = ParseKey(header, http);
            var organizationId = user.OrganizationId;
            var requestHash = await HashRequestAsync(http);
            var dbContext = (DbContext)http.RequestServices.GetRequiredService(registration.ContextType);
            var store = new IdempotencyKeyStore(dbContext, http.RequestServices.GetRequiredService<IOptions<IdempotencyOptions>>(),
                http.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System);

            var reservation = await store.ReserveAsync(userId, organizationId, key, requestHash, http.RequestAborted);
            if (reservation.Outcome == IdempotencyOutcome.Reserved)
                await RunReservedAsync(context, next, dbContext, store, (userId, organizationId, key));
            else
                await AnswerStoredAsync(context, reservation);
        }

        private static IdempotencyStoreRegistration? FindStore(ResourceExecutingContext context)
        {
            var http = context.HttpContext;
            if (!WriteMethods.Contains(http.Request.Method, StringComparer.OrdinalIgnoreCase))
                return null;

            var metadata = http.GetEndpoint()?.Metadata;
            if (metadata is null || metadata.GetMetadata<IAuthorizeData>() is null || metadata.GetMetadata<IAllowAnonymous>() is not null
                || metadata.GetMetadata<IgnoreIdempotencyKeyAttribute>() is not null)
                return null;

            if (context.ActionDescriptor is not ControllerActionDescriptor action)
                return null;

            return http.RequestServices.GetServices<IdempotencyStoreRegistration>()
                .FirstOrDefault(r => r.ControllersAssembly == action.ControllerTypeInfo.Assembly);
        }

        private static Guid ParseKey(StringValues header, HttpContext http)
        {
            if (header.Count == 1 && Guid.TryParse(header[0], out var key) && key != Guid.Empty)
                return key;

            var localizer = http.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
            throw new BusinessException(ErrorCodes.IdempotencyKeyInvalid, localizer["IdempotencyKeyInvalid"].Value);
        }

        private static async Task<string> HashRequestAsync(HttpContext http)
        {
            http.Request.EnableBuffering();
            using var copy = new MemoryStream();
            await http.Request.Body.CopyToAsync(copy, http.RequestAborted);
            http.Request.Body.Position = 0;

            http.Request.Headers.TryGetValue(LocationAccessMiddleware.HeaderName, out var location);
            return IdempotencyRequestHash.Compute(http.Request.Method, http.Request.Path + http.Request.QueryString, location.ToString(), copy.ToArray());
        }

        private static async Task AnswerStoredAsync(ResourceExecutingContext context, IdempotencyReservation reservation)
        {
            var http = context.HttpContext;
            var localizer = http.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
            switch (reservation.Outcome)
            {
                case IdempotencyOutcome.Reused:
                    throw new BusinessException(ErrorCodes.IdempotencyKeyReused, localizer["IdempotencyKeyReused"].Value);
                case IdempotencyOutcome.InProgress:
                    throw new BusinessException(ErrorCodes.RequestInProgress, localizer["RequestInProgress"].Value);
            }

            var saved = reservation.Response!;
            http.Response.StatusCode = saved.Status;
            http.Response.ContentType = saved.ContentType;
            if (saved.Location is not null)
                http.Response.Headers.Location = saved.Location;
            await http.Response.Body.WriteAsync(saved.Body, http.RequestAborted);
            context.Result = new EmptyResult();
        }

        private static async Task RunReservedAsync(ResourceExecutingContext context, ResourceExecutionDelegate next, DbContext dbContext,
            IdempotencyKeyStore store, (Guid UserId, Guid? OrganizationId, Guid Key) owner)
        {
            var http = context.HttpContext;
            var realBody = http.Response.Body;
            var held = new MemoryStream();
            http.Response.Body = held;
            var committed = false;
            try
            {
                committed = await RunInTransactionAsync(http, next, dbContext, store, owner, held);
            }
            catch
            {
                http.Response.Headers.Remove("Location");
                throw;
            }
            finally
            {
                http.Response.Body = realBody;
                if (!committed)
                    await ReleaseAsync(store, owner);
            }

            held.Position = 0;
            await held.CopyToAsync(realBody, http.RequestAborted);
        }

        // Returns true when the answer was a success that was saved with the write and committed.
        private static async Task<bool> RunInTransactionAsync(HttpContext http, ResourceExecutionDelegate next, DbContext dbContext,
            IdempotencyKeyStore store, (Guid UserId, Guid? OrganizationId, Guid Key) owner, MemoryStream held)
        {
            await using var transaction = await JoinableTransaction.BeginOrJoinAsync(dbContext, http.RequestAborted);
            var executed = await next();
            if (executed.Exception is not null || http.Response.StatusCode is < 200 or >= 300)
            {
                await transaction.RollbackAsync(http.RequestAborted);
                return false;
            }

            var saved = new StoredResponse(http.Response.StatusCode, http.Response.ContentType, held.ToArray(), LocationOf(http));
            await store.CompleteAsync(owner.UserId, owner.OrganizationId, owner.Key, saved, http.RequestAborted);
            await transaction.CommitAsync(http.RequestAborted);
            return true;
        }

        private static string? LocationOf(HttpContext http)
            => http.Response.Headers.Location.Count > 0 ? http.Response.Headers.Location.ToString() : null;

        private static async Task ReleaseAsync(IdempotencyKeyStore store, (Guid UserId, Guid? OrganizationId, Guid Key) owner)
        {
            try
            {
                await store.ReleaseAsync(owner.UserId, owner.OrganizationId, owner.Key, CancellationToken.None);
            }
            catch (Exception)
            {
                // The original error is the one to answer; a key left behind frees itself after the in-progress timeout.
            }
        }
    }
}
