// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace KekuleHtmlUi.Controls;

/// <summary>
/// Minimal numeric up/down (spinner) control: a <see cref="TextBox"/> with two
/// <see cref="RepeatButton"/>s. Bind <see cref="Value"/> (two-way by default)
/// and set <see cref="Minimum"/>/<see cref="Maximum"/> to constrain it.
/// </summary>
public partial class NumericUpDown : UserControl
{
    #region Constructor

    public NumericUpDown() => InitializeComponent();

    #endregion

    #region DependencyProperties

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(int), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(int.MinValue, OnLimitChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(int.MaxValue, OnLimitChanged));

    /// <summary>
    /// Current value. Always coerced into <c>[<see cref="Minimum"/>, <see cref="Maximum"/>]</c>.
    /// </summary>
    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    #endregion

    #region Coercion

    /// <summary>
    /// Central clamping: every change to <see cref="Value"/> (buttons, typing, binding) passes through here.
    /// </summary>
    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var control = (NumericUpDown)d;
        return Math.Clamp((int)baseValue, control.Minimum, control.Maximum);
    }

    /// <summary>
    /// Re-clamp the value when the limits change.
    /// </summary>
    private static void OnLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => d.CoerceValue(ValueProperty);

    #endregion

    #region Event handling

    private void Up_Click(object sender, RoutedEventArgs e) => Step(+1);

    private void Down_Click(object sender, RoutedEventArgs e) => Step(-1);

    /// <summary>
    /// Increment (wheel up) / decrement (wheel down) the value while hovering the control.
    /// </summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (e.Delta != 0)
        {
            Step(e.Delta > 0 ? +1 : -1);

            // don't let the wheel also scroll a surrounding container
            e.Handled = true;
        }
    }

    /// <summary>
    /// Changes <see cref="Value"/> by <paramref name="delta"/>; the coercion callback keeps it in range.
    /// </summary>
    private void Step(int delta) => SetCurrentValue(ValueProperty, Value + delta);

    /// <summary>
    /// Allow digits only while typing.
    /// </summary>
    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !IsDigitsOnly(e.Text);

    /// <summary>
    /// Reject pasting non-numeric content.
    /// </summary>
    private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(typeof(string)) is string text && IsDigitsOnly(text))
            return;

        e.CancelCommand();
    }

    private static bool IsDigitsOnly(string text) => Regex.IsMatch(text, "^[0-9]+$");

    #endregion
}
