using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xigenia.Web.Models;

namespace Xigenia.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient http, AuthState auth)
    {
        _http = http;
        _auth = auth;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private void ApplyAuthHeader()
    {
        if (_auth.IsAuthenticated && !string.IsNullOrWhiteSpace(_auth.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response, int requestVersion)
    {
        // Ignore 401 from an expired session: do not clear newer session.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var isStale = requestVersion != _auth.SessionVersion;
            if (!isStale)
            {
                _auth.Clear();
            }
            return ApiResult<T>.Unauthorized(stale: isStale);
        }

        // Stale successful responses are not applied by caller; return stale marker for callers that care.
        // Keep success payload but mark stale so Home can discard.
        if (requestVersion != _auth.SessionVersion)
        {
            // Still parse to consume but caller should ignore. Mark stale.
            // For stale success, don't return data to prevent repopulating private views.
            return ApiResult<T>.Stale();
        }

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return ApiResult<T>.Success(default!);
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResult<T>.Success(default!);
            }

            var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            return ApiResult<T>.Success(data!);
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        ApiError error;
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorContent, _jsonOptions);
            Dictionary<string, string[]>? errors = problem?.Errors;
            if (errors is null && errorContent.Contains("\"errors\""))
            {
                var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("errors", out var e))
                {
                    errors = new Dictionary<string, string[]>();
                    foreach (var prop in e.EnumerateObject())
                    {
                        var arr = prop.Value.EnumerateArray().Select(v => v.GetString() ?? "").ToArray();
                        errors[prop.Name] = arr;
                    }
                }
            }

            var title = problem?.Title ?? (response.StatusCode == HttpStatusCode.BadRequest ? "Validation error" : "Server error");
            var detail = problem?.Detail;
            if (string.IsNullOrWhiteSpace(detail) && errors is not null)
            {
                detail = string.Join(" ", errors.SelectMany(kv => kv.Value));
            }

            error = new ApiError(title, detail, (int)response.StatusCode, errors);
        }
        catch
        {
            error = new ApiError("Server error", errorContent, (int)response.StatusCode, null);
        }

        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && error.ValidationErrors is not null)
        {
            return ApiResult<T>.ValidationFailed(error);
        }

        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
        {
            return ApiResult<T>.ValidationFailed(error);
        }

        return ApiResult<T>.ServerFailed(error);
    }

    // Auth endpoints using Identity API: POST /api/auth/register, POST /api/auth/login
    public async Task<ApiResult<AccessTokenResponse>> LoginAsync(string email, string password)
    {
        ApplyAuthHeader();
        // Capture version before request for correlation; login typically unauthenticated so version is baseline.
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/auth/login", new { email, password }, _jsonOptions);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<AccessTokenResponse>.Uncertain("Unable to confirm sign-in. The request may have completed.", ex);
        }

        var result = await HandleResponseAsync<AccessTokenResponse>(response, version);
        if (result.IsSuccess && result.Data is not null)
        {
            _auth.SetAuthenticated(result.Data.AccessToken, email);
            // Return a fresh success with current data; version already bumped.
            return ApiResult<AccessTokenResponse>.Success(result.Data);
        }
        return result;
    }

    public async Task<ApiResult<object>> RegisterAsync(string email, string password)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/auth/register", new { email, password }, _jsonOptions);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<object>.Uncertain("Unable to confirm registration. The request may have completed.", ex);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized && version == _auth.SessionVersion)
        {
            _auth.Clear();
            return ApiResult<object>.Unauthorized();
        }

        if (response.IsSuccessStatusCode)
        {
            return ApiResult<object>.Success(new object());
        }

        return await HandleResponseAsync<object>(response, version);
    }

    public void Logout()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        _auth.Clear();
    }

    // Finance endpoints — each captures session version and returns Stale if version changed.
    public async Task<ApiResult<FinancialAccountResponse>> CreateAccountAsync(CreateFinancialAccountRequest request)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/finance/accounts", request, _jsonOptions);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<FinancialAccountResponse>.Uncertain("Unable to confirm whether the account was created. It may have succeeded. Refresh to reconcile before retrying.", ex);
        }
        return await HandleFinancialWriteAsync<FinancialAccountResponse>(response, version, "Unable to confirm whether the account was created. It may have succeeded. Refresh to reconcile before retrying.");
    }

    public async Task<ApiResult<IReadOnlyList<FinancialAccountResponse>>> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/api/finance/accounts", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<IReadOnlyList<FinancialAccountResponse>>.Uncertain("Unable to confirm accounts. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<IReadOnlyList<FinancialAccountResponse>>(response, version);
    }

    public async Task<ApiResult<MoneyMovementResponse>> CreateMovementAsync(CreateMoneyMovementRequest request)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/finance/movements", request, _jsonOptions);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<MoneyMovementResponse>.Uncertain("Unable to confirm whether the movement was recorded. It may have succeeded. Refresh to reconcile before retrying.", ex);
        }
        return await HandleFinancialWriteAsync<MoneyMovementResponse>(response, version, "Unable to confirm whether the movement was recorded. It may have succeeded. Refresh to reconcile before retrying.");
    }

    public async Task<ApiResult<IReadOnlyList<MoneyMovementResponse>>> ListMovementsAsync()
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/api/finance/movements");
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<IReadOnlyList<MoneyMovementResponse>>.Uncertain("Unable to confirm movements. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<IReadOnlyList<MoneyMovementResponse>>(response, version);
    }

    public async Task<ApiResult<FinancialExplanationResponse>> RequestFinancialExplanationAsync(FinancialExplanationRequest request)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/ai/financial-explanation", request, _jsonOptions);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            // Read-only request: no financial write occurred, so retrying is always safe.
            return ApiResult<FinancialExplanationResponse>.Uncertain("The explanation request could not be completed. It is safe to try again — explanations never change your records.", ex);
        }
        return await HandleResponseAsync<FinancialExplanationResponse>(response, version);
    }

    public async Task<ApiResult<MoneyMovementSummaryResponse>> GetSummaryAsync()
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/api/finance/movements/summary");
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<MoneyMovementSummaryResponse>.Uncertain("Unable to confirm summary. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<MoneyMovementSummaryResponse>(response, version);
    }

    private async Task<ApiResult<T>> HandleFinancialWriteAsync<T>(HttpResponseMessage response, int requestVersion, string uncertainDetail)
    {
        // 5xx has unknown commit outcome -> uncertain (DB may have committed before error).
        if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
        {
            // Preserve stale check: if already stale, return stale to avoid clearing new session.
            if (requestVersion != _auth.SessionVersion)
            {
                return ApiResult<T>.Stale();
            }
            return ApiResult<T>.Uncertain(uncertainDetail + $" Server returned {(int)response.StatusCode}.");
        }

        ApiResult<T> result;
        try
        {
            result = await HandleResponseAsync<T>(response, requestVersion);
        }
        catch (Exception ex) when (response.IsSuccessStatusCode)
        {
            // Successful status but body read/deserialize failed -> ambiguous commit.
            if (requestVersion != _auth.SessionVersion)
            {
                return ApiResult<T>.Stale();
            }
            return ApiResult<T>.Uncertain(uncertainDetail + " Response could not be read.", ex);
        }
        catch (Exception ex)
        {
            // Any other processing failure where persistence cannot be ruled out -> uncertain if success or 5xx.
            if (response.IsSuccessStatusCode || (int)response.StatusCode >= 500)
            {
                if (requestVersion != _auth.SessionVersion)
                {
                    return ApiResult<T>.Stale();
                }
                return ApiResult<T>.Uncertain(uncertainDetail, ex);
            }
            throw;
        }

        // Missing resource data on 2xx (204 No Content, empty, whitespace, JSON null) cannot confirm success.
        if (result.IsSuccess && result.Data is null)
        {
            if (requestVersion != _auth.SessionVersion)
            {
                return ApiResult<T>.Stale();
            }
            return ApiResult<T>.Uncertain(uncertainDetail + " Success response contained no usable data. Refresh to reconcile before retrying.");
        }

        if (result.IsStale)
        {
            return result;
        }

        return result;
    }

    // Showcase R1 — GET-only showcase reads (exact backend contracts, no writes)

    public async Task<ApiResult<ProfileResponse>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/api/auth/manage/info", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<ProfileResponse>.Uncertain("Unable to confirm profile. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<ProfileResponse>(response, version);
    }

    public async Task<ApiResult<IReadOnlyList<ObligationResponse>>> ListObligationsAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/api/finance/obligations", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<IReadOnlyList<ObligationResponse>>.Uncertain("Unable to confirm obligations. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<IReadOnlyList<ObligationResponse>>(response, version);
    }

    public async Task<ApiResult<IReadOnlyList<FinancialCategoryResponse>>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/api/finance/categories", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<IReadOnlyList<FinancialCategoryResponse>>.Uncertain("Unable to confirm categories. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<IReadOnlyList<FinancialCategoryResponse>>(response, version);
    }

    public async Task<ApiResult<FinancialCategoryResponse>> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync($"/api/finance/categories/{id}", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<FinancialCategoryResponse>.Uncertain("Unable to confirm category. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<FinancialCategoryResponse>(response, version);
    }

    public async Task<ApiResult<FinancialAccountResponse>> GetAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync($"/api/finance/accounts/{id}", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<FinancialAccountResponse>.Uncertain("Unable to confirm account. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<FinancialAccountResponse>(response, version);
    }

    // Showcase R2 — filtered movement explorer (GET-only, fixed pageSize, no new POST)

    public async Task<ApiResult<IReadOnlyList<MoneyMovementResponse>>> ListMovementsAsync(MovementQuery query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        var qs = query.ToQueryString(page, pageSize);
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync($"/api/finance/movements{qs}", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<IReadOnlyList<MoneyMovementResponse>>.Uncertain("Unable to confirm movements. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<IReadOnlyList<MoneyMovementResponse>>(response, version);
    }

    public async Task<ApiResult<MoneyMovementSummaryResponse>> GetFilteredSummaryAsync(MovementQuery query, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var version = _auth.SessionVersion;
        var qs = query.ToSummaryQueryString();
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync($"/api/finance/movements/summary{qs}", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return ApiResult<MoneyMovementSummaryResponse>.Uncertain("Unable to confirm summary. Refresh to retry.", ex);
        }
        return await HandleResponseAsync<MoneyMovementSummaryResponse>(response, version);
    }

    private static bool IsTransportFailure(Exception ex) =>
        ex is HttpRequestException || ex is TaskCanceledException || ex is OperationCanceledException;
}

public sealed class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsValidationError { get; init; }
    public bool IsServerError { get; init; }
    public bool IsUncertain { get; init; }
    public bool IsStale { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ApiResult<T> Unauthorized(bool stale = false) => new() { IsUnauthorized = true, IsStale = stale, Error = new ApiError("Unauthorized", "Please sign in again.", 401, null) };
    public static ApiResult<T> Stale() => new() { IsStale = true };
    public static ApiResult<T> ValidationFailed(ApiError error) => new() { IsValidationError = true, Error = error };
    public static ApiResult<T> ServerFailed(ApiError error) => new() { IsServerError = true, Error = error };
    public static ApiResult<T> Uncertain(string detail, Exception? ex = null) => new() { IsUncertain = true, Error = new ApiError("Uncertain outcome", detail, 0, null) };
}
