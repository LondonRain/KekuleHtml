using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace KekuleHtmlUi.Controls;

/// <summary>
/// <para>
/// Editable ComboBox with built-in "suggestion" filtering.
/// </para>
/// <para>
/// While the user types, the assigned <see cref="ItemsControl.ItemsSource"/> is wrapped internally in a <see cref="ListCollectionView"/>
/// and filtered via its <see cref="ListCollectionView.Filter"/>. The ItemsSource itself is not swapped out.
/// </para>
/// <para>
/// Usage is identical to a normal ComboBox:
///   &lt;local:SuggestionComboBox ItemsSource="{Binding Persons}" DisplayMemberPath="DisplayName" /&gt;
/// </para>
/// <para>
/// By default items are filtered via <see cref="object.ToString"/>. If you need a different criterion, set <see cref="FilterPredicate"/>.
/// </para>
/// </summary>
public class SuggestionComboBox : ComboBox
{
    #region Variables

    private const string EditableTextBoxPartName = "PART_EditableTextBox";
    private TextBox? _EditableTextBox;

    private ListCollectionView? _CollectionView;

    private bool _IsInternalItemsSourceUpdate;
    private bool _IsSelectionChanging;

    #endregion

    #region Constructor

    static SuggestionComboBox()
    {
        // Keep using the default ComboBox theme/template so that
        // PART_EditableTextBox etc. are present as usual.
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SuggestionComboBox),
            new FrameworkPropertyMetadata(typeof(ComboBox)));
    }

    public SuggestionComboBox()
    {
        IsEditable = true;
        IsTextSearchEnabled = false;
        StaysOpenOnEdit = true;

        // Enable virtualization.
        ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));

        // ItemsSourceProperty belongs to ItemsControl and can't be
        // overridden "virtually" - so we observe it via
        // DependencyPropertyDescriptor instead of replacing the base logic.
        DependencyPropertyDescriptor
            .FromProperty(ItemsSourceProperty, typeof(ItemsControl))
            ?.AddValueChanged(this, OnItemsSourceChanged);
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Determines whether an item should be shown for a given search text.
    /// </summary>
    public Func<object, string, bool> FilterPredicate { get; set; } = DefaultFilterPredicate;

    /// <summary>
    /// Search for every word in match (AND) but it can be anywhere in the string.
    /// </summary>
    private static bool DefaultFilterPredicate(object item, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        string? text = item?.ToString();
        if (string.IsNullOrEmpty(text))
            return false;

        return searchText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Apply the <see cref="FilterPredicate"/> on our <see cref="_CollectionView"/>.
    /// </summary>
    private void ApplyFilter(string searchText)
    {
        if (_CollectionView is null)
            return;

        using (_CollectionView.DeferRefresh())
        {
            _CollectionView.Filter = item => FilterPredicate(item, searchText);
        }

        if (!string.IsNullOrEmpty(searchText) && !IsDropDownOpen && _CollectionView.Cast<object>().Any())
        {
            // When starting typing for filtering, ppen the ComboBox.
            IsDropDownOpen = true;

            // When starting typing for filtering, make sure that the newly entered text won't be selected automatically everytime.
            _EditableTextBox?.SelectionStart = int.MaxValue;
        }
    }

    #endregion

    #region Event handling

    /// <summary>
    /// Register for TextChanged event.
    /// </summary>
    public override void OnApplyTemplate()
    {
        _EditableTextBox?.TextChanged -= EditableTextBox_TextChanged;

        base.OnApplyTemplate();

        _EditableTextBox = GetTemplateChild(EditableTextBoxPartName) as TextBox;

        _EditableTextBox?.TextChanged += EditableTextBox_TextChanged;
    }

    /// <summary>
    /// Handle changing <see cref="ItemsSource"/>. Set up <see cref="_CollectionView"/> for filtering.
    /// </summary>
    private void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        if (_IsInternalItemsSourceUpdate)
            return;

        var source = ItemsSource;

        if (source is null)
        {
            _CollectionView = null;
            return;
        }

        if (source is ListCollectionView alreadyAView)
        {
            // A ListCollectionView was already passed in - just use it as-is.
            _CollectionView = alreadyAView;
            ApplyFilter(_EditableTextBox?.Text ?? string.Empty);
            return;
        }

        // Wrap the raw list in a ListCollectionView so we can filter via Filter instead of constantly replacing ItemsSource.
        // Do not create a copy if alreay IList!
        IList list = source as IList ?? source.Cast<object>().ToList();
        _CollectionView = new ListCollectionView(list);

        _IsInternalItemsSourceUpdate = true;
        try
        {
            // Replaces the current value without destroying an existing binding.
            SetCurrentValue(ItemsSourceProperty, _CollectionView);
        }
        finally
        {
            _IsInternalItemsSourceUpdate = false;
        }

        ApplyFilter(_EditableTextBox?.Text ?? string.Empty);
    }

    /// <summary>
    /// Remember that selection is currently changing so we can differentiate that in <see cref="EditableTextBox_TextChanged(object, TextChangedEventArgs)"/>.
    /// </summary>
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        _IsSelectionChanging = true;
        base.OnSelectionChanged(e);
        _IsSelectionChanging = false;
    }

    /// <summary>
    /// Handle <see cref="ComboBox.Text"/> (containing filter string) and <see cref="Selector.SelectedItem"/>
    /// of <see cref="_EditableTextBox"/> when user types text to filter.
    /// </summary>
    private void EditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SelectedItem != null)
        {
            if (_IsSelectionChanging)
            {
                // Selection was just changed. Clear filter, so everything will be selectable in ComboBoy afain.
                ApplyFilter(string.Empty);
            }
            else
            {
                // Remove previous selection when entering new filter (but keep the new filter!).
                var filter = _EditableTextBox?.Text;
                SetCurrentValue(SelectedItemProperty, null);
                _EditableTextBox?.Text = filter;
                ApplyFilter(filter ?? string.Empty);
            }
        }
        else
        {
            // Perform default filtering when nothing is selected.
            ApplyFilter(_EditableTextBox?.Text ?? string.Empty);
        }
    }

    #endregion
}
