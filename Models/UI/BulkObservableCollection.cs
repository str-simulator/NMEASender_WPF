using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NMEASender.Wpf.Models.UI;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IReadOnlyList<T> items, int maxCount)
    {
        if (items.Count == 0)
            return;

        int excess = Count + items.Count - maxCount;
        for (int i = 0; i < excess; i++)
            Items.RemoveAt(0);
        foreach (T item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
