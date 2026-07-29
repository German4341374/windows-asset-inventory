namespace WindowsAssetInventory.Services;

public sealed class DomainConflictException(string message) : Exception(message);
