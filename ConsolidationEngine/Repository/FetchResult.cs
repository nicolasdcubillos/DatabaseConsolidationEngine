using System.Data;

namespace ConsolidationEngine.Repository
{
    /// <summary>
    /// Resultado del FetchChanges: separa las filas que pasaron el RowFilter
    /// del total de cambios detectados en el rango de versiones.
    /// </summary>
    public sealed class FetchResult
    {
        /// <summary>Filas que pasaron el RowFilter (las que se procesarán).</summary>
        public DataTable ChangedRows { get; init; }

        /// <summary>
        /// Total de cambios detectados en el rango, sin aplicar RowFilter.
        /// Cuando no hay RowFilter, siempre es igual a ChangedRows.Rows.Count.
        /// </summary>
        public int TotalChangesInRange { get; init; }

        /// <summary>Cambios descartados intencionalmente por el RowFilter.</summary>
        public int FilteredOutCount => TotalChangesInRange - ChangedRows.Rows.Count;

        /// <summary>Indica si se descartaron filas por el RowFilter.</summary>
        public bool HasFilteredOut => FilteredOutCount > 0;
    }
}
