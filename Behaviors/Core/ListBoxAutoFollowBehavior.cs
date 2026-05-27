using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NMEASender.Wpf.Behaviors.Core;

public static class ListBoxAutoFollowBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ListBoxAutoFollowBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty ControllerProperty = DependencyProperty.RegisterAttached(
        "Controller",
        typeof(Controller),
        typeof(ListBoxAutoFollowBehavior),
        new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            if (GetController(listBox) is not null)
            {
                return;
            }

            Controller controller = new Controller(listBox);
            SetController(listBox, controller);
            controller.Attach();
            return;
        }

        Controller? existingController = GetController(listBox);
        if (existingController is null)
        {
            return;
        }

        existingController.Detach();
        listBox.ClearValue(ControllerProperty);
    }

    private static void SetController(DependencyObject element, Controller value)
    {
        element.SetValue(ControllerProperty, value);
    }

    private static Controller? GetController(DependencyObject element)
    {
        return (Controller?)element.GetValue(ControllerProperty);
    }

    private sealed class Controller
    {
        private readonly ListBox _listBox;
        private readonly DependencyPropertyDescriptor? _itemsSourceDescriptor;
        private INotifyCollectionChanged? _observedCollection;
        private ScrollViewer? _scrollViewer;
        private bool _followLogTail = true;
        private bool _scrollPending;

        public Controller(ListBox listBox)
        {
            _listBox = listBox;
            _itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
                ItemsControl.ItemsSourceProperty,
                typeof(ListBox));
        }

        public void Attach()
        {
            _listBox.Loaded += ListBox_Loaded;
            _listBox.Unloaded += ListBox_Unloaded;
            _listBox.PreviewMouseLeftButtonDown += ListBox_PreviewMouseLeftButtonDown;
            _itemsSourceDescriptor?.AddValueChanged(_listBox, ItemsSourceChanged);
            AttachToCollection(_listBox.ItemsSource);
        }

        public void Detach()
        {
            _listBox.Loaded -= ListBox_Loaded;
            _listBox.Unloaded -= ListBox_Unloaded;
            _listBox.PreviewMouseLeftButtonDown -= ListBox_PreviewMouseLeftButtonDown;
            _itemsSourceDescriptor?.RemoveValueChanged(_listBox, ItemsSourceChanged);
            DetachFromCollection();
            DetachFromScrollViewer();
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindVisualChild<ScrollViewer>(_listBox);
            if (_scrollViewer is not null)
            {
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }

        private void ListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachFromScrollViewer();
        }

        private void DetachFromScrollViewer()
        {
            if (_scrollViewer is null)
            {
                return;
            }

            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            _scrollViewer = null;
        }

        private void ItemsSourceChanged(object? sender, EventArgs e)
        {
            AttachToCollection(_listBox.ItemsSource);
        }

        private void AttachToCollection(object? itemsSource)
        {
            DetachFromCollection();
            _observedCollection = itemsSource as INotifyCollectionChanged;
            if (_observedCollection is not null)
            {
                _observedCollection.CollectionChanged += Logs_CollectionChanged;
            }
        }

        private void DetachFromCollection()
        {
            if (_observedCollection is null)
            {
                return;
            }

            _observedCollection.CollectionChanged -= Logs_CollectionChanged;
            _observedCollection = null;
        }

        private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add && e.Action != NotifyCollectionChangedAction.Reset)
            {
                return;
            }

            if (!_followLogTail)
            {
                return;
            }

            RequestScrollToLatest();
        }

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            ListBoxItem? clickedItem = FindVisualAncestor<ListBoxItem>(source);
            if (clickedItem is null)
            {
                return;
            }

            int clickedIndex = _listBox.ItemContainerGenerator.IndexFromContainer(clickedItem);
            int lastIndex = _listBox.Items.Count - 1;
            _followLogTail = clickedIndex >= lastIndex;

            if (_followLogTail)
            {
                RequestScrollToLatest();
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            bool isAtBottom = IsAtBottom(_scrollViewer);
            if (e.ExtentHeightChange == 0)
            {
                _followLogTail = isAtBottom;
                return;
            }

            if (_followLogTail && isAtBottom)
            {
                RequestScrollToLatest();
            }
        }

        private void RequestScrollToLatest()
        {
            if (_scrollPending)
            {
                return;
            }

            _scrollPending = true;
            _listBox.Dispatcher.BeginInvoke(() =>
            {
                _scrollPending = false;
                if (!_followLogTail)
                {
                    return;
                }

                if (_listBox.Items.Count == 0)
                {
                    return;
                }

                _scrollViewer ??= FindVisualChild<ScrollViewer>(_listBox);
                if (_scrollViewer is not null)
                {
                    _scrollViewer.ScrollToEnd();
                    return;
                }

                _listBox.UpdateLayout();
                _listBox.ScrollIntoView(_listBox.Items[_listBox.Items.Count - 1]);
            }, DispatcherPriority.ContextIdle);
        }

        private static bool IsAtBottom(ScrollViewer? viewer)
        {
            if (viewer is null)
            {
                return true;
            }

            const double threshold = 1.0;
            return viewer.VerticalOffset + viewer.ViewportHeight >= viewer.ExtentHeight - threshold;
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                if (child is T matched)
                {
                    return matched;
                }

                T? descendant = FindVisualChild<T>(child);
                if (descendant is not null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static T? FindVisualAncestor<T>(DependencyObject? child)
            where T : DependencyObject
        {
            while (child is not null)
            {
                if (child is T typed)
                {
                    return typed;
                }

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }
    }
}
