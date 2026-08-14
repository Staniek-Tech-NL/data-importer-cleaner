using System.ComponentModel;
using System.Runtime.CompilerServices;
using DataCleaner.Domain.Profiling;

namespace DataCleaner.App;

public sealed class ColumnProfileViewModel : INotifyPropertyChanged
{
    private readonly Action _mappingChanged;
    private string _targetField;
    private bool _isIgnored;

    public ColumnProfileViewModel(int columnIndex, ColumnProfile profile, Action mappingChanged)
    {
        ColumnIndex = columnIndex;
        Profile = profile;
        _mappingChanged = mappingChanged;
        _targetField = profile.ColumnName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int ColumnIndex { get; }

    public ColumnProfile Profile { get; }

    public string SourceColumn => Profile.ColumnName;

    public string TechnicalType => Profile.DataType.ToString();

    public string SemanticType => Profile.SemanticType.ToString();

    public int EmptyCount => Profile.EmptyCount;

    public int UniqueCount => Profile.UniqueCount;

    public int DuplicateCount => Profile.DuplicateCount;

    public int InvalidCount => Profile.InvalidCount;

    public decimal? Minimum => Profile.Minimum;

    public decimal? Maximum => Profile.Maximum;

    public decimal? Average => Profile.Average;

    public string TargetField
    {
        get => _targetField;
        set
        {
            var normalizedValue = value ?? string.Empty;
            if (SetField(ref _targetField, normalizedValue))
            {
                _mappingChanged();
            }
        }
    }

    public bool IsIgnored
    {
        get => _isIgnored;
        set
        {
            if (SetField(ref _isIgnored, value))
            {
                _mappingChanged();
            }
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
