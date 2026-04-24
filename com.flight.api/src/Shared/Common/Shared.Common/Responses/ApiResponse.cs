namespace Shared.Common.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    int StatusCode = 200);

public sealed record ApiCreatedResponse<T>(T Data)
{
    public bool Success    { get; } = true;
    public int StatusCode  { get; } = 201;
    public string Message  { get; } = "Created successfully.";
}

public sealed record ApiPagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
