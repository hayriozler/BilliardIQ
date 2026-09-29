namespace BilliardIQ.Mobile.Utilities;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class DbFieldNameAttribute(string fieldName) : Attribute
{
    public string FieldName => fieldName;
}