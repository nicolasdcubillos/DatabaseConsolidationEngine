DECLARE @from BIGINT = 0;  -- watermark anterior
DECLARE @to   BIGINT = CHANGE_TRACKING_CURRENT_VERSION();

-- Validar que no se haya purgado el CT (requeriría reinicialización):
DECLARE @min BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('dbo.MVTOAUXNIF'));
IF @from < @min
BEGIN
  RAISERROR('El watermark es menor que MIN_VALID_VERSION. Requiere carga completa.', 16, 1);
  RETURN;
END

-- Cambios (payload actual + operación)
SELECT
    ct.SYS_CHANGE_VERSION,
    ct.SYS_CHANGE_OPERATION,      -- I/U/D
    s.*
FROM CHANGETABLE(CHANGES dbo.MVTOAUXNIF, @from) AS ct
LEFT JOIN dbo.MVTOAUXNIF AS s
  ON s.IDMVTOAUX = ct.IDMVTOAUX;


SELECT * FROM dbo.ETL_Watermark WHERE SourceServer=@server AND SourceDB=@db AND TableName=@table