using System.ComponentModel.DataAnnotations;

namespace WindowsAssetInventory.Contracts;

public sealed record UserDto(
    int Id,
    string FullName,
    string Email,
    string Department,
    int AssignedAssetCount);

public sealed class UserWriteRequest
{
    [Required, StringLength(140, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string Department { get; init; }
}
