using System.ComponentModel;
using System.Reflection;

namespace EchoCut.Objects;

/// <summary>
/// <see cref="BindingList{T}"/> que sí admite ordenación por columna.
/// </summary>
/// <remarks>
/// <see cref="BindingList{T}"/> declara <see cref="BindingList{T}.SupportsSortingCore"/> como
/// <c>false</c>, así que una rejilla enlazada a ella no reacciona a las pulsaciones en la cabecera:
/// no es que ordene mal, es que no ordena en absoluto. Basta con implementar el mínimo del contrato
/// para que <c>DataGridView</c> dibuje la flecha de sentido y reordene sin código adicional.
/// </remarks>
/// <typeparam name="T">
/// Tipo de los elementos de la lista. El desempate por nombre requiere que tenga una propiedad
/// pública de instancia llamada <c>Name</c> de tipo <see cref="string"/>; si no la tiene, el
/// desempate simplemente no distingue entre elementos empatados.
/// </typeparam>
public sealed class SortableBindingList<T> : BindingList<T>
{
    /// <summary>
    /// Propiedad usada para desempatar. Se resuelve una sola vez: buscarla por reflexión dentro del
    /// comparador supondría hacerlo en cada una de las O(n log n) comparaciones de la ordenación.
    /// </summary>
    private static readonly PropertyInfo? TiebreakProperty =
        typeof(T).GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);

    private PropertyDescriptor? _sortProperty;
    private ListSortDirection _sortDirection;
    private bool _isSorted;

    /// <summary>Crea una lista ordenable vacía.</summary>
    public SortableBindingList()
    {
    }

    /// <summary>Crea una lista ordenable a partir de una colección existente.</summary>
    /// <param name="list">Elementos iniciales de la lista.</param>
    public SortableBindingList(IList<T> list) : base(list)
    {
    }

    /// <inheritdoc/>
    protected override bool SupportsSortingCore => true;

    /// <inheritdoc/>
    protected override bool IsSortedCore => _isSorted;

    /// <inheritdoc/>
    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    /// <inheritdoc/>
    protected override ListSortDirection SortDirectionCore => _sortDirection;

    /// <summary>Reaplica el criterio vigente, si lo hay. Útil tras repoblar la lista.</summary>
    public void ReapplySort()
    {
        if (_isSorted && _sortProperty is not null)
        {
            ApplySortCore(_sortProperty, _sortDirection);
            ResetBindings();
        }
    }

    /// <inheritdoc/>
    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        if (Items is not List<T> items)
        {
            return;
        }

        Comparison<T> comparison = (left, right) => Compare(prop, left, right, direction);
        items.Sort(comparison);

        _sortProperty = prop;
        _sortDirection = direction;
        _isSorted = true;

        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    /// <inheritdoc/>
    protected override void RemoveSortCore()
    {
        _sortProperty = null;
        _isSorted = false;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    /// <summary>Compara dos elementos por el valor de una propiedad, con nulos primero y desempate estable.</summary>
    /// <param name="prop">Propiedad por la que ordenar.</param>
    /// <param name="left">Primer elemento a comparar.</param>
    /// <param name="right">Segundo elemento a comparar.</param>
    /// <param name="direction">Sentido de la ordenación.</param>
    /// <returns>
    /// Valor negativo si <paramref name="left"/> precede a <paramref name="right"/>, positivo si es
    /// al revés, o el resultado del desempate por nombre si ambos valores son iguales.
    /// </returns>
    private static int Compare(PropertyDescriptor prop, T left, T right, ListSortDirection direction)
    {
        object? a = prop.GetValue(left);
        object? b = prop.GetValue(right);

        // Los ausentes se agrupan al principio en ambos sentidos. Una pista sin analizar no tiene un
        // silencio "menor" que otra: no tiene dato, y mezclarla entre las medidas como si valiera
        // cero haría creer que ya se comprobó y que no hay nada que recortar.
        if (a is null || b is null)
        {
            return a is null && b is null ? Tiebreak(left, right) : a is null ? -1 : 1;
        }

        int result = a is string textA && b is string textB
            ? string.Compare(textA, textB, StringComparison.CurrentCultureIgnoreCase)
            : Comparer<object>.Default.Compare(a, b);

        if (direction == ListSortDirection.Descending)
        {
            result = -result;
        }

        // El desempate va siempre en el mismo sentido: así ordenar dos veces por la misma columna
        // devuelve exactamente la misma disposición y las filas empatadas no bailan.
        return result != 0 ? result : Tiebreak(left, right);
    }

    /// <summary>Desempata dos elementos por su propiedad <c>Name</c>, siempre en sentido ascendente.</summary>
    /// <param name="left">Primer elemento a comparar.</param>
    /// <param name="right">Segundo elemento a comparar.</param>
    /// <returns>Resultado de comparar los nombres, ignorando mayúsculas según la cultura actual.</returns>
    private static int Tiebreak(T left, T right) =>
        string.Compare(NameOf(left), NameOf(right), StringComparison.CurrentCultureIgnoreCase);

    /// <summary>Obtiene el valor de la propiedad <c>Name</c> de un elemento, si existe.</summary>
    /// <param name="item">Elemento del que leer el nombre.</param>
    /// <returns>Valor de la propiedad <c>Name</c>, o cadena vacía si <typeparamref name="T"/> no la tiene.</returns>
    private static string NameOf(T item) =>
        TiebreakProperty?.GetValue(item) as string ?? string.Empty;
}
