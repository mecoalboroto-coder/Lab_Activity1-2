namespace EquipmentBorrowing.Api;

public sealed record StudentRequest(string? StudentNumber, string? FullName, string? Contact);

public sealed record EquipmentRequest(string? EquipmentCode, string? Name, string? Description);

public sealed record EligibilityRequest(bool? IsEligible);

public sealed record BorrowRequest(int? StudentId, int? EquipmentId, DateOnly? ExpectedReturnDate);

public sealed record CheckoutRequest(int? StudentId, List<int>? EquipmentIds, DateOnly? ExpectedReturnDate);

public sealed record EmptyRequest();

public sealed record CheckoutResponse(List<BorrowingDto> Borrowings);
