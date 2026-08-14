using System.ComponentModel;
using System.Runtime.CompilerServices;
using DataCleaner.Domain.Profiling;
using DataCleaner.Domain.Validation;

namespace DataCleaner.App;

public sealed class ColumnProfileViewModel : INotifyPropertyChanged
{
    private readonly Action _mappingChanged;
    private string _targetField;
    private bool _isIgnored;
    private bool _validateType = true;
    private bool _isRequired;
    private bool _validateEmail;
    private bool _isUnique;
    private decimal? _minimumAllowed;
    private decimal? _maximumAllowed;
    private string _allowedValues = string.Empty;
    private ValidationSeverity _severity = ValidationSeverity.Error;

    public ColumnProfileViewModel(int columnIndex, ColumnProfile profile, Action mappingChanged)
    {
        ColumnIndex = columnIndex;
        Profile = profile;
        _mappingChanged = mappingChanged;
        _targetField = profile.ColumnName;
        _validateEmail = profile.SemanticType == DataCleaner.Domain.Data.SemanticType.Email;
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

    public IReadOnlyList<ValidationSeverity> AvailableSeverities { get; } =
        Enum.GetValues<ValidationSeverity>();

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

    public bool ValidateType
    {
        get => _validateType;
        set => SetConfigurationField(ref _validateType, value);
    }

    public bool IsRequired
    {
        get => _isRequired;
        set => SetConfigurationField(ref _isRequired, value);
    }

    public bool ValidateEmail
    {
        get => _validateEmail;
        set => SetConfigurationField(ref _validateEmail, value);
    }

    public bool IsUnique
    {
        get => _isUnique;
        set => SetConfigurationField(ref _isUnique, value);
    }

    public decimal? MinimumAllowed
    {
        get => _minimumAllowed;
        set => SetConfigurationField(ref _minimumAllowed, value);
    }

    public decimal? MaximumAllowed
    {
        get => _maximumAllowed;
        set => SetConfigurationField(ref _maximumAllowed, value);
    }

    public string AllowedValues
    {
        get => _allowedValues;
        set => SetConfigurationField(ref _allowedValues, value ?? string.Empty);
    }

    public ValidationSeverity Severity
    {
        get => _severity;
        set => SetConfigurationField(ref _severity, value);
    }

    public void ResetValidationConfiguration()
    {
        ValidateType = false;
        IsRequired = false;
        ValidateEmail = false;
        IsUnique = false;
        MinimumAllowed = null;
        MaximumAllowed = null;
        AllowedValues = string.Empty;
        Severity = ValidationSeverity.Error;
    }

    private void SetConfigurationField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetField(ref field, value, propertyName))
        {
            _mappingChanged();
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
