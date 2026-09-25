using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ValueMonitorModel<T> : INotifyPropertyChanged
{
    private static readonly object obj = new object();
    private T _paraValue;
    public T ParaValue
    {
        get { return _paraValue; }
        set
        {
            if (!compare(value, _paraValue))
            {
                _paraValue = value;
                OnPropertyChanged();
            }
        }
    }

    private bool compare(T obj1, T obj2)
    {
        return EqualityComparer<T>.Default.Equals(obj1, obj2);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
