namespace ConsolidationEngine.Config
{
    public class ConsolidationSettings
    {
        public string Server { get; set; }
        public int BatchSize { get; set; }
        public int UpsertBatchWithFallbackTimeoutSeconds { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public List<DatabasePairConfig> Databases { get; set; } = new();
        public List<TableConfig> Tables { get; set; } = new();
        public bool FaultRetryProcessorEnabled { get; set; } = true;
        
        /// <summary>
        /// Número máximo de intentos de reintento para errores con Retry = 1.
        /// Si un error supera este límite, se marca como Retry = 0 (no se reintentará más).
        /// Valor por defecto: 3 intentos.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class DatabasePairConfig
    {
        public string Origin { get; set; }
        public string Target { get; set; }
    }

    public class TableConfig
    {
        public string Name { get; set; }
        public string KeyColumn { get; set; }
        public bool SkipPrimaryKey { get; set; } = false;

        /// <summary>
        /// Filtro SQL opcional aplicado sobre la tabla origen en el JOIN del CHANGETABLE.
        /// Permite excluir filas que cumplan cierta condición (e.g., "s.FECHACIERRE < '20260301'").
        /// Se agrega como AND en el WHERE del FetchChanges. Usar alias "s" para referirse a la tabla origen.
        /// Si es null o vacío, no se aplica ningún filtro adicional.
        /// </summary>
        public string RowFilter { get; set; } = null;
    }
}
