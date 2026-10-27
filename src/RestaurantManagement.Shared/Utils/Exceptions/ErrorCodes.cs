namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>
    /// The Shared catalog of error codes (the comment on each line is the value and status it answers). Fallback codes (Http*) answer framework-made errors.
    /// </summary>
    [ErrorCodeCatalog]
    public static class ErrorCodes
    {
        // "validation.failed", 400
        public static ErrorCode ValidationFailed { get; } = new("validation.failed", 400);

        // "auth.unauthorized", 401
        public static ErrorCode Unauthorized { get; } = new("auth.unauthorized", 401);

        // "auth.invalid_token", 401
        public static ErrorCode InvalidToken { get; } = new("auth.invalid_token", 401);

        // "access.denied", 403
        public static ErrorCode AccessDenied { get; } = new("access.denied", 403);

        // "organization.required", 403
        public static ErrorCode OrganizationRequired { get; } = new("organization.required", 403);

        // "organization.not_found", 404
        public static ErrorCode OrganizationNotFound { get; } = new("organization.not_found", 404);

        // "location.not_found", 404
        public static ErrorCode LocationNotFound { get; } = new("location.not_found", 404);

        // "rate_limit.exceeded", 429, retry
        public static ErrorCode RateLimitExceeded { get; } = new("rate_limit.exceeded", 429, true);

        // "server.unexpected_error", 500, retry
        public static ErrorCode ServerUnexpectedError { get; } = new("server.unexpected_error", 500, true);

        // "request.in_progress", 409, retry
        public static ErrorCode RequestInProgress { get; } = new("request.in_progress", 409, true);

        // "app_version.required", 400
        public static ErrorCode AppVersionRequired { get; } = new("app_version.required", 400);

        // "app_version.invalid", 400
        public static ErrorCode AppVersionInvalid { get; } = new("app_version.invalid", 400);

        // "location.invalid_id", 400
        public static ErrorCode LocationInvalidId { get; } = new("location.invalid_id", 400);

        // "location.access_denied", 403
        public static ErrorCode LocationAccessDenied { get; } = new("location.access_denied", 403);

        // "audit_log.invalid_date_range", 400
        public static ErrorCode AuditLogInvalidDateRange { get; } = new("audit_log.invalid_date_range", 400);

        // "audit_log.unknown_action", 400
        public static ErrorCode AuditLogUnknownAction { get; } = new("audit_log.unknown_action", 400);

        // "tenant.mismatch", 403
        public static ErrorCode TenantMismatch { get; } = new("tenant.mismatch", 403);

        // "http.bad_request", 400
        public static ErrorCode HttpBadRequest { get; } = new("http.bad_request", 400);

        // "http.unauthorized", 401
        public static ErrorCode HttpUnauthorized { get; } = new("http.unauthorized", 401);

        // "http.forbidden", 403
        public static ErrorCode HttpForbidden { get; } = new("http.forbidden", 403);

        // "http.not_found", 404
        public static ErrorCode HttpNotFound { get; } = new("http.not_found", 404);

        // "http.method_not_allowed", 405
        public static ErrorCode HttpMethodNotAllowed { get; } = new("http.method_not_allowed", 405);

        // "http.conflict", 409
        public static ErrorCode HttpConflict { get; } = new("http.conflict", 409);

        // "http.unsupported_media_type", 415
        public static ErrorCode HttpUnsupportedMediaType { get; } = new("http.unsupported_media_type", 415);

        // "http.too_many_requests", 429
        public static ErrorCode HttpTooManyRequests { get; } = new("http.too_many_requests", 429);

        // "http.internal_server_error", 500
        public static ErrorCode HttpInternalServerError { get; } = new("http.internal_server_error", 500);

        // The code that answers a framework-made error or a code-less refusal of this status.
        public static ErrorCode Fallback(int status) => status switch
        {
            400 => HttpBadRequest,
            401 => HttpUnauthorized,
            403 => HttpForbidden,
            404 => HttpNotFound,
            405 => HttpMethodNotAllowed,
            409 => HttpConflict,
            415 => HttpUnsupportedMediaType,
            429 => HttpTooManyRequests,
            500 => HttpInternalServerError,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "No fallback code for this status."),
        };
    }
}
