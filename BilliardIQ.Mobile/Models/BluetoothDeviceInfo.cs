namespace BilliardIQ.Mobile.Models;

public class BluetoothDeviceInfo(Guid id, string? name)
{
    public Guid Id { get; } = id;
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? "Unknown device" : name;

    public override bool Equals(object? obj) => obj is BluetoothDeviceInfo other && other.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
}
