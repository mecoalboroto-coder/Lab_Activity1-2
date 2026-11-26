using System.Text.Json.Serialization;

namespace EquipmentBorrowing.Api;

public sealed record ErrorBody(string Code, string Message);

public sealed record StudentDto(int Id, string StudentNumber, string FullName, string? Contact, bool IsEligible);

public sealed record EquipmentDto(int Id, string EquipmentCode, string Name, string? Description, bool IsAvailable);

public sealed record BorrowingDto(
    int Id,
    int StudentId,
    int EquipmentId,
    DateOnly BorrowedDate,
    DateOnly ExpectedReturnDate,
    DateOnly? ReturnedDate,
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StudentNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StudentName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EquipmentCode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EquipmentName);
