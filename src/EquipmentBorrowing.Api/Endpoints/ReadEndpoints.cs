using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Api.Endpoints;

public static class ReadEndpoints
{
    private const int MaxSearchLength = 200;

    public static void MapReadEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapGet("/equipment", async Task<IResult> (string? available, IEquipmentRepository equipment, CancellationToken ct) =>
        {
            bool? wantAvailable = null;
            if (available is not null)
            {
                if (!bool.TryParse(available, out var parsed))
                    return ApiResults.Error(400, "invalid_query", "Query parameter 'available' must be true or false.");

                wantAvailable = parsed;
            }

            var items = wantAvailable == true
                ? await equipment.GetAvailableAsync(ct)
                : await equipment.GetAllAsync(ct);

            var selected = wantAvailable == false ? items.Where(e => !e.IsAvailable) : items;
            return Results.Ok(selected.Select(e => e.ToDto()).ToList());
        })
        .WithName("ListEquipment")
        .WithTags("Equipment")
        .WithSummary("List equipment. available=true returns only available items; available=false only unavailable items.")
        .Produces<List<EquipmentDto>>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(500);

        api.MapGet("/equipment/{id:int}", async Task<IResult> (int id, IEquipmentRepository equipment, CancellationToken ct) =>
        {
            var item = await equipment.GetByIdAsync(id, ct);
            return item is null
                ? ApiResults.NotFound($"Equipment {id} was not found.")
                : Results.Ok(item.ToDto());
        })
        .WithName("GetEquipment")
        .WithTags("Equipment")
        .WithSummary("Get one equipment item.")
        .Produces<EquipmentDto>(200)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(500);

        api.MapGet("/students", async Task<IResult> (string? search, IStudentRepository students, CancellationToken ct) =>
        {
            var term = search?.Trim();
            if (term is not null && term.Length > MaxSearchLength)
                return ApiResults.Error(400, "invalid_query", $"Query parameter 'search' must be at most {MaxSearchLength} characters.");

            var all = await students.GetAllAsync(ct);

            var selected = string.IsNullOrEmpty(term)
                ? all
                : all.Where(s =>
                    s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || s.StudentNumber.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

            return Results.Ok(selected.Select(s => s.ToDto()).ToList());
        })
        .WithName("ListStudents")
        .WithTags("Students")
        .WithSummary("List students, optionally filtered by a partial, case-insensitive name or student number.")
        .Produces<List<StudentDto>>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(500);

        api.MapGet("/students/{id:int}", async Task<IResult> (int id, IStudentRepository students, CancellationToken ct) =>
        {
            var student = await students.GetByIdAsync(id, ct);
            return student is null
                ? ApiResults.NotFound($"Student {id} was not found.")
                : Results.Ok(student.ToDto());
        })
        .WithName("GetStudent")
        .WithTags("Students")
        .WithSummary("Get one student.")
        .Produces<StudentDto>(200)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(500);

        api.MapGet("/borrowings/{id:int}", async Task<IResult> (
            int id,
            IBorrowingRepository borrowings,
            IStudentRepository students,
            IEquipmentRepository equipment,
            CancellationToken ct) =>
        {
            var borrowing = await borrowings.GetByIdAsync(id, ct);
            if (borrowing is null)
                return ApiResults.NotFound($"Borrowing {id} was not found.");

            var student = await students.GetByIdAsync(borrowing.StudentId, ct);
            var item = await equipment.GetByIdAsync(borrowing.EquipmentId, ct);
            return Results.Ok(borrowing.ToDto(student, item));
        })
        .WithName("GetBorrowing")
        .WithTags("Borrowings")
        .WithSummary("Get one borrowing.")
        .Produces<BorrowingDto>(200)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(500);

        api.MapGet("/borrowings", async Task<IResult> (
            string? search,
            string? status,
            string? sort,
            string? direction,
            BorrowingHistoryService history,
            CancellationToken ct) =>
        {
            var term = search?.Trim();
            if (term is not null && term.Length > MaxSearchLength)
                return ApiResults.Error(400, "invalid_query", $"Query parameter 'search' must be at most {MaxSearchLength} characters.");

            if (!TryParseStatus(status, out var statusFilter))
                return ApiResults.Error(400, "invalid_query", "Query parameter 'status' must be Active, Returned or All.");

            if (!TryParseSort(sort, out var sortField))
                return ApiResults.Error(400, "invalid_query", "Query parameter 'sort' must be borrowedDate or expectedReturnDate.");

            if (!TryParseDirection(direction, out var descending))
                return ApiResults.Error(400, "invalid_query", "Query parameter 'direction' must be asc or desc.");

            var result = await history.SearchAsync(new BorrowingSearchCriteria(term, statusFilter, sortField, descending), ct);

            return result.State == QueryState.Loaded
                ? Results.Ok(result.Items.Select(b => b.ToDto()).ToList())
                : ApiResults.ServerError();
        })
        .WithName("ListBorrowings")
        .WithTags("Borrowings")
        .WithSummary("Search, filter and sort borrowings. Defaults: status=All, sort=borrowedDate, direction=asc.")
        .Produces<List<BorrowingDto>>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(500);

        api.MapGet("/students/{id:int}/borrowings", async Task<IResult> (int id, BorrowingHistoryService history, CancellationToken ct) =>
        {
            var result = await history.ForStudentAsync(id, ct);

            return result.State switch
            {
                QueryState.Loaded => Results.Ok(result.Items.Select(b => b.ToDto()).ToList()),
                QueryState.Missing => ApiResults.NotFound($"Student {id} was not found."),
                _ => ApiResults.ServerError()
            };
        })
        .WithName("ListStudentBorrowings")
        .WithTags("Students")
        .WithSummary("Active and returned borrowings of one student.")
        .Produces<List<BorrowingDto>>(200)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(500);

        api.MapGet("/equipment/{id:int}/borrowings", async Task<IResult> (int id, BorrowingHistoryService history, CancellationToken ct) =>
        {
            var result = await history.ForEquipmentAsync(id, ct);

            return result.State switch
            {
                QueryState.Loaded => Results.Ok(result.Items.Select(b => b.ToDto()).ToList()),
                QueryState.Missing => ApiResults.NotFound($"Equipment {id} was not found."),
                _ => ApiResults.ServerError()
            };
        })
        .WithName("ListEquipmentBorrowings")
        .WithTags("Equipment")
        .WithSummary("Active and returned borrowings of one equipment item.")
        .Produces<List<BorrowingDto>>(200)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(500);
    }

    private static bool TryParseStatus(string? value, out BorrowingStatusFilter filter)
    {
        filter = BorrowingStatusFilter.All;

        if (value is null)
            return true;

        if (value.Equals("All", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            filter = BorrowingStatusFilter.Active;
            return true;
        }

        if (value.Equals("Returned", StringComparison.OrdinalIgnoreCase))
        {
            filter = BorrowingStatusFilter.Returned;
            return true;
        }

        return false;
    }

    private static bool TryParseSort(string? value, out BorrowingSortField field)
    {
        field = BorrowingSortField.CheckoutDate;

        if (value is null)
            return true;

        if (value.Equals("borrowedDate", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("expectedReturnDate", StringComparison.OrdinalIgnoreCase))
        {
            field = BorrowingSortField.ExpectedReturnDate;
            return true;
        }

        return false;
    }

    private static bool TryParseDirection(string? value, out bool descending)
    {
        descending = false;

        if (value is null)
            return true;

        if (value.Equals("asc", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            descending = true;
            return true;
        }

        return false;
    }
}
