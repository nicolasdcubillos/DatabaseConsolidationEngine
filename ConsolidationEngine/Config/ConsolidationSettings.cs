﻿namespace ConsolidationEngine.Config
{
    public class ConsolidationSettings
    {
        public string Server { get; set; }
        public int BatchSize { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public List<DatabasePairConfig> Databases { get; set; } = new();
        public List<TableConfig> Tables { get; set; } = new();
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
    }
}
