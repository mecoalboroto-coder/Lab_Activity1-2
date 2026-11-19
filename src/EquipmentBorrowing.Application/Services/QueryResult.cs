namespace EquipmentBorrowing.Application.Services;

public enum QueryState
{
    Loaded,
    Missing,
    Failed
}

public sealed record QueryResult<T>(QueryState State, IReadOnlyList<T> Items, string? Message)
{
    public static QueryResult<T> Loaded(IReadOnlyList<T> items) => new(QueryState.Loaded, items, null);

    public static QueryResult<T> Missing(string message) => new(QueryState.Missing, Array.Empty<T>(), message);

    public static QueryResult<T> Failed(string message) => new(QueryState.Failed, Array.Empty<T>(), message);
}
