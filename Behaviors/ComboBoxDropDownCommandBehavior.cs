using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NMEASender.Wpf.Behaviors;

public static class ComboBoxDropDownCommandBehavior
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(ComboBoxDropDownCommandBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
        "CommandParameter",
        typeof(object),
        typeof(ComboBoxDropDownCommandBehavior),
        new PropertyMetadata(null));

    private static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
        "IsAttached",
        typeof(bool),
        typeof(ComboBoxDropDownCommandBehavior),
        new PropertyMetadata(false));

    public static void SetCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(CommandProperty, value);
    }

    public static ICommand? GetCommand(DependencyObject element)
    {
        return (ICommand?)element.GetValue(CommandProperty);
    }

    public static void SetCommandParameter(DependencyObject element, object? value)
    {
        element.SetValue(CommandParameterProperty, value);
    }

    public static object? GetCommandParameter(DependencyObject element)
    {
        return element.GetValue(CommandParameterProperty);
    }

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
        {
            return;
        }

        bool isAttached = (bool)comboBox.GetValue(IsAttachedProperty);
        if (e.NewValue is ICommand)
        {
            if (!isAttached)
            {
                comboBox.DropDownOpened += ComboBox_DropDownOpened;
                comboBox.SetValue(IsAttachedProperty, true);
            }

            return;
        }

        if (isAttached)
        {
            Detach(comboBox);
        }
    }

    private static void Detach(ComboBox comboBox)
    {
        comboBox.DropDownOpened -= ComboBox_DropDownOpened;
        comboBox.ClearValue(IsAttachedProperty);
    }

    private static void ComboBox_DropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        ICommand? command = GetCommand(comboBox);
        if (command is null)
        {
            return;
        }

        object? parameter = GetCommandParameter(comboBox) ?? comboBox.DataContext ?? comboBox;
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }
}
