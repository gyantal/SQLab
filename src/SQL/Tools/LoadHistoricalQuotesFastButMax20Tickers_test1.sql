USE [HedgeQuant]
GO


BEGIN TRY       -- avoid continue if error occurs
DECLARE @id0 INT, @idsq0 INT = (SELECT ID FROM Stock WHERE Ticker='SVXY!Light0.5x.SQ');
DECLARE @T0 VARCHAR(20), @Tsq0 VARCHAR(20) = (SELECT Ticker FROM Stock WHERE ID=@idsq0);
DECLARE @Traw VARCHAR(20) = LEFT(@Tsq0,LEN(@Tsq0)-3); 
DECLARE @ExclamPos INT = CHARINDEX('!',@Traw,0);	-- it is 0 if '!' is not found
IF (@ExclamPos <> 0)
	SET @Traw = SUBSTRING(@Traw, 0, @ExclamPos);

-- both 'SVXY.SQ' and 'SVXY!Light0.5x.SQ' tickers can occur, but Traw will be 'SVXY'
SELECT @T0=Ticker, @id0=ID FROM Stock WHERE Ticker=@Traw;
IF (RIGHT(@Tsq0,3) <> '.SQ' OR @idsq0 IS NULL OR @id0 IS NULL)
    -- The specified ticker '@Tsq0' is invalid (valid values are returned by SELECT ...).
    RAISERROR(14234,16,0,'SVXY!Light0.5x.SQ','SELECT Ticker FROM Stock WHERE RIGHT(Ticker,3)=''.SQ''');
DECLARE @msg0 VARCHAR(MAX);
IF (EXISTS (SELECT * FROM StockSplitDividend WHERE StockID=@idsq0)) BEGIN
    SET @msg0 = @Tsq0+' has nonzero StockSplitDividend records, which is not supported';
    THROW 50000, @msg0, 0;
END;
DECLARE @Tbegin0 DATE = (SELECT TOP 1 [Date] FROM StockQuote WHERE StockID=@id0 ORDER BY [Date]);
DECLARE @TsqEnd0 DATE = (SELECT TOP 1 [Date] FROM StockQuote WHERE StockID=@idsq0 ORDER BY [Date] DESC);

-- if 'SVXY!Light0.5x.SQ', we allow overlap and use the .SQ prices in the overlap; if 'SVXY.SQ', we don't allow overlap for safety
IF (@ExclamPos = 0 AND @Tbegin0 < @TsqEnd0) BEGIN
    SET @msg0 = @T0+' has quotes before the last quote of '+@Tsq0;
    THROW 50000, @msg0, 1;
END;
--DECLARE @adjsq0 FLOAT = (SELECT f FROM dbo.GetAdjustmentFactorAt2(@id0,@Tbegin0)); -- was used by Robert
DECLARE @adjsq0 FLOAT = (SELECT f FROM dbo.GetAdjustmentFactorAt2(@id0,@TsqEnd0));  -- maybe 1 day off, but around switching dates, there shouldn't be split
END TRY BEGIN CATCH THROW END CATCH;

(
SELECT Ticker,[Date],[Open],High,Low,[Close],Volume
FROM (SELECT /*TopN*/ tt.* FROM (
    SELECT Ticker=@Tsq0, StockID=@idsq0, [Date]=CAST([Date] AS DATE), Volume,
        [Open] =CAST( OpenPrice*@adjsq0 AS DECIMAL(19,4)), High=CAST(HighPrice*@adjsq0 AS DECIMAL(19,4)),
        [Close]=CAST(ClosePrice*@adjsq0 AS DECIMAL(19,4)), Low =CAST( LowPrice*@adjsq0 AS DECIMAL(19,4))
    FROM StockQuote sq0 WHERE StockID=@idsq0
    UNION ALL
    SELECT Ticker=@T0, StockID=@id0, [Date]=CAST([Date] AS DATE), Volume,
        [Open] =CAST( OpenPrice*adj.f AS DECIMAL(19,4)), High=CAST( HighPrice*adj.f AS DECIMAL(19,4)),
        [Close]=CAST(ClosePrice*adj.f AS DECIMAL(19,4)), Low =CAST(  LowPrice*adj.f AS DECIMAL(19,4))
    FROM StockQuote sq1
    CROSS APPLY dbo.GetAdjustmentFactorAt2(sq1.StockID,sq1.Date) adj
    WHERE sq1.StockID=@id0 AND sq1.Date > @TsqEnd0
) AS tt WHERE 1=1 /*AND_DateRange*/ /*TopN_orderby*/) AS t
) ORDER BY [Date]

