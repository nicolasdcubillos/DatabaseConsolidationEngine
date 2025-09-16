USE CONSOLIDADA;


SELECT 
    tr.name AS TriggerName,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    OBJECT_NAME(tr.parent_id) AS TableName,
    s.name AS SchemaName
FROM sys.triggers tr
JOIN sys.tables t ON tr.parent_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name = 'MVTONIIF'   -- <-- cambia por tu tabla



SELECT 
    tr.name AS TriggerName,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    OBJECT_NAME(tr.parent_id) AS TableName,
    s.name AS SchemaName
FROM sys.triggers tr
JOIN sys.tables t ON tr.parent_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name = 'MVTOAUXNIF'   -- <-- cambia por tu tabla

