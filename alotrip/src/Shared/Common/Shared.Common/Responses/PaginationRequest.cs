namespace Shared.Common.Responses;

public sealed record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}
