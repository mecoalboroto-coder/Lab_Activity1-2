using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Api;

internal static class Mappers
{
    public static StudentDto ToDto(this Student student) =>
        new(student.Id, student.StudentNumber, student.Name, student.Contact, student.IsAllowedToBorrow);

    public static EquipmentDto ToDto(this Equipment equipment) =>
        new(equipment.Id, equipment.Identifier, equipment.Name, equipment.Description, equipment.IsAvailable);

    public static BorrowingDto ToDto(this Borrowing borrowing, Student? student = null, Equipment? equipment = null)
    {
        var s = borrowing.Student ?? student;
        var e = borrowing.Equipment ?? equipment;

        return new BorrowingDto(
            borrowing.Id,
            borrowing.StudentId,
            borrowing.EquipmentId,
            borrowing.DateBorrowed,
            borrowing.ExpectedReturnDate,
            borrowing.ReturnedAt,
            borrowing.Status.ToString(),
            s?.StudentNumber,
            s?.Name,
            e?.Identifier,
            e?.Name);
    }
}
