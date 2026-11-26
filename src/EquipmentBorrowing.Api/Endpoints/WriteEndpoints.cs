using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Api.Endpoints;

public static class WriteEndpoints
{
    public static void MapWriteEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapPost("/students", async Task<IResult> (HttpRequest request, StudentManagementService students, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<StudentRequest>(request, ct);
            if (error is not null)
                return error;

            var result = await students.RegisterAsync(body!.StudentNumber, body.FullName, body.Contact, ct);

            return result.Success
                ? Results.Created($"/api/v1/students/{result.Value!.Id}", result.Value.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("CreateStudent")
        .WithTags("Students")
        .WithSummary("Register a student. New students are eligible.")
        .Accepts<StudentRequest>("application/json")
        .Produces<StudentDto>(201)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);

        api.MapPut("/students/{id:int}", async Task<IResult> (int id, HttpRequest request, StudentManagementService students, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<StudentRequest>(request, ct);
            if (error is not null)
                return error;

            var result = await students.UpdateAsync(id, body!.StudentNumber, body.FullName, body.Contact, ct);

            return result.Success
                ? Results.Ok(result.Value!.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("UpdateStudent")
        .WithTags("Students")
        .WithSummary("Update a student's editable fields. A null contact clears it.")
        .Accepts<StudentRequest>("application/json")
        .Produces<StudentDto>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);

        api.MapMethods("/students/{id:int}/eligibility", new[] { "PATCH" }, async Task<IResult> (int id, HttpRequest request, StudentManagementService students, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<EligibilityRequest>(request, ct);
            if (error is not null)
                return error;

            if (body!.IsEligible is null)
                return ApiResults.Error(400, "invalid_request", "The field 'isEligible' is required and must be true or false.");

            var result = await students.SetEligibilityAsync(id, body.IsEligible.Value, ct);

            return result.Success
                ? Results.Ok(result.Value!.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("SetStudentEligibility")
        .WithTags("Students")
        .WithSummary("Permit or restrict a student. Repeating the current value is a no-op.")
        .Accepts<EligibilityRequest>("application/json")
        .Produces<StudentDto>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(500);

        api.MapPost("/equipment", async Task<IResult> (HttpRequest request, EquipmentManagementService equipment, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<EquipmentRequest>(request, ct);
            if (error is not null)
                return error;

            var result = await equipment.RegisterAsync(body!.EquipmentCode, body.Name, body.Description, ct);

            return result.Success
                ? Results.Created($"/api/v1/equipment/{result.Value!.Id}", result.Value.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("CreateEquipment")
        .WithTags("Equipment")
        .WithSummary("Register equipment. New items are available.")
        .Accepts<EquipmentRequest>("application/json")
        .Produces<EquipmentDto>(201)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);

        api.MapPut("/equipment/{id:int}", async Task<IResult> (int id, HttpRequest request, EquipmentManagementService equipment, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<EquipmentRequest>(request, ct);
            if (error is not null)
                return error;

            var result = await equipment.UpdateAsync(id, body!.EquipmentCode, body.Name, body.Description, ct);

            return result.Success
                ? Results.Ok(result.Value!.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("UpdateEquipment")
        .WithTags("Equipment")
        .WithSummary("Update descriptive fields. Availability and other protected fields are rejected.")
        .Accepts<EquipmentRequest>("application/json")
        .Produces<EquipmentDto>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);

        api.MapPost("/borrowings", async Task<IResult> (HttpRequest request, BorrowEquipmentService borrow, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<BorrowRequest>(request, ct);
            if (error is not null)
                return error;

            if (body!.StudentId is null || body.EquipmentId is null || body.ExpectedReturnDate is null)
                return ApiResults.Error(400, "invalid_request", "The fields 'studentId', 'equipmentId' and 'expectedReturnDate' are required.");

            var result = await borrow.BorrowAsync(body.StudentId.Value, body.EquipmentId.Value, body.ExpectedReturnDate.Value, ct);

            return result.Success
                ? Results.Created($"/api/v1/borrowings/{result.Borrowing!.Id}", result.Borrowing.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("CreateBorrowing")
        .WithTags("Borrowings")
        .WithSummary("Issue one item to a student. The server sets the checkout date.")
        .Accepts<BorrowRequest>("application/json")
        .Produces<BorrowingDto>(201)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);

        api.MapPost("/checkout-requests", async Task<IResult> (HttpRequest request, MultiCheckoutService checkout, CancellationToken ct) =>
        {
            var (body, error) = await RequestBody.ReadAsync<CheckoutRequest>(request, ct);
            if (error is not null)
                return error;

            if (body!.StudentId is null || body.EquipmentIds is null || body.ExpectedReturnDate is null)
                return ApiResults.Error(400, "invalid_request", "The fields 'studentId', 'equipmentIds' and 'expectedReturnDate' are required.");

            var result = await checkout.CheckoutAsync(body.StudentId.Value, body.EquipmentIds, body.ExpectedReturnDate.Value, ct);

            return result.Success
                ? Results.Created("/api/v1/borrowings", new CheckoutResponse(result.Borrowings.Select(b => b.ToDto()).ToList()))
                : ResultMapper.Failure(result);
        })
        .WithName("CreateCheckoutRequest")
        .WithTags("Borrowings")
        .WithSummary("Issue two or more distinct items to one student in a single transaction.")
        .Accepts<CheckoutRequest>("application/json")
        .Produces<CheckoutResponse>(201)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);

        api.MapPost("/borrowings/{id:int}/return", async Task<IResult> (int id, HttpRequest request, ReturnEquipmentService returns, CancellationToken ct) =>
        {
            var (_, error) = await RequestBody.ReadAsync<EmptyRequest>(request, ct, allowEmpty: true);
            if (error is not null)
                return error;

            var result = await returns.ReturnAsync(id, ct);

            return result.Success
                ? Results.Ok(result.Borrowing!.ToDto())
                : ResultMapper.Failure(result);
        })
        .WithName("ReturnBorrowing")
        .WithTags("Borrowings")
        .WithSummary("Return an active borrowing. The server sets the return date.")
        .Accepts<EmptyRequest>("application/json")
        .Produces<BorrowingDto>(200)
        .Produces<ErrorBody>(400)
        .Produces<ErrorBody>(404)
        .Produces<ErrorBody>(409)
        .Produces<ErrorBody>(500);
    }
}
