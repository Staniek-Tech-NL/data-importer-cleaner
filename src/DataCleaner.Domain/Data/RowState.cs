namespace DataCleaner.Domain.Data;

[Flags]
public enum RowState
{
    None = 0,
    Valid = 1 << 0,
    Info = 1 << 1,
    Warning = 1 << 2,
    Invalid = 1 << 3,
    Modified = 1 << 4,
    Duplicate = 1 << 5,
    Rejected = 1 << 6
}
