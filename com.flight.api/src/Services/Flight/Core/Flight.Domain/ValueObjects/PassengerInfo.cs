using Flight.Domain.Exceptions;

namespace Flight.Domain.ValueObjects;

/// <summary>
/// Thông tin hành khách (họ tên + loại). Map từ tblPassenger cũ.
/// </summary>
public sealed record PassengerInfo
{
    public string FirstName    { get; }
    public string LastName     { get; }
    public string? MiddleName  { get; }
    public string Gender       { get; }
    public DateOnly? BirthDate { get; }

    private PassengerInfo(string firstName, string lastName, string? middleName,
        string gender, DateOnly? birthDate)
    {
        FirstName  = firstName;
        LastName   = lastName;
        MiddleName = middleName;
        Gender     = gender;
        BirthDate  = birthDate;
    }

    public static PassengerInfo Of(string firstName, string lastName,
        string gender, string? middleName = null, DateOnly? birthDate = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))  throw new DomainException("Last name is required.");
        if (gender is not ("M" or "F"))           throw new DomainException("Gender must be 'M' or 'F'.");

        return new PassengerInfo(firstName.Trim(), lastName.Trim(), middleName?.Trim(), gender, birthDate);
    }

    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {MiddleName} {FirstName}";
}
