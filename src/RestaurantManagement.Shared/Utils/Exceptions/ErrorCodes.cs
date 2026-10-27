namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>
    /// The Shared catalog of error codes (STUB: every getter throws until T1 fills it; the comment on each
    /// line is the value and status it must answer). Fallback codes (Http*) answer framework-made errors.
    /// </summary>
    [ErrorCodeCatalog]
    public static class ErrorCodes
    {
        // "validation.failed", 400
        public static ErrorCode ValidationFailed => throw new NotImplementedException();

        // "auth.unauthorized", 401
        public static ErrorCode Unauthorized => throw new NotImplementedException();

        // "auth.invalid_token", 401
        public static ErrorCode InvalidToken => throw new NotImplementedException();

        // "access.denied", 403
        public static ErrorCode AccessDenied => throw new NotImplementedException();

        // "organization.required", 403
        public static ErrorCode OrganizationRequired => throw new NotImplementedException();

        // "organization.not_found", 404
        public static ErrorCode OrganizationNotFound => throw new NotImplementedException();

        // "location.not_found", 404
        public static ErrorCode LocationNotFound => throw new NotImplementedException();

        // "rate_limit.exceeded", 429, retry
        public static ErrorCode RateLimitExceeded => throw new NotImplementedException();

        // "server.unexpected_error", 500, retry
        public static ErrorCode ServerUnexpectedError => throw new NotImplementedException();

        // "request.in_progress", 409, retry
        public static ErrorCode RequestInProgress => throw new NotImplementedException();

        // "location.invalid_id", 400
        public static ErrorCode LocationInvalidId => throw new NotImplementedException();

        // "location.access_denied", 403
        public static ErrorCode LocationAccessDenied => throw new NotImplementedException();

        // "audit_log.invalid_date_range", 400
        public static ErrorCode AuditLogInvalidDateRange => throw new NotImplementedException();

        // "audit_log.unknown_action", 400
        public static ErrorCode AuditLogUnknownAction => throw new NotImplementedException();

        // "tenant.mismatch", 403
        public static ErrorCode TenantMismatch => throw new NotImplementedException();

        // "http.bad_request", 400
        public static ErrorCode HttpBadRequest => throw new NotImplementedException();

        // "http.unauthorized", 401
        public static ErrorCode HttpUnauthorized => throw new NotImplementedException();

        // "http.forbidden", 403
        public static ErrorCode HttpForbidden => throw new NotImplementedException();

        // "http.not_found", 404
        public static ErrorCode HttpNotFound => throw new NotImplementedException();

        // "http.method_not_allowed", 405
        public static ErrorCode HttpMethodNotAllowed => throw new NotImplementedException();

        // "http.conflict", 409
        public static ErrorCode HttpConflict => throw new NotImplementedException();

        // "http.unsupported_media_type", 415
        public static ErrorCode HttpUnsupportedMediaType => throw new NotImplementedException();

        // "http.too_many_requests", 429
        public static ErrorCode HttpTooManyRequests => throw new NotImplementedException();

        // "http.internal_server_error", 500
        public static ErrorCode HttpInternalServerError => throw new NotImplementedException();

        // STUB (T1): the fallback code of a status (400, 401, 403, 404, 405, 409, 415, 429, 500).
        public static ErrorCode Fallback(int status) => throw new NotImplementedException();
    }
}
