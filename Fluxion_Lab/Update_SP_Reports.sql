ALTER PROCEDURE
    [dbo].[SP_Reports]
--DECLARE
    @Flag INT = 119,
    @ClientID BIGINT = 1001,
    @PageNo INT = 1,
    @PageSize INT = 10,
    @FromDate DATE = '2024-10-13',
    @ToDate DATE = '2025-12-13',
    @SearchKey BIGINT = -1,
	@PatientID BIGINT = NULL,
	@GraphType VARCHAR(30) = NULL,
	@LabID INT =NULL,
	@TestID INT =NULL,
	@ItemType VARCHAR(20) = NULL,
	@ItemNo INT = 48
AS
SET NOCOUNT ON;

IF(@Flag = 100 and @ItemType IS NULL) -- Bill Wise
BEGIN
    SELECT * FROM 
    (
    SELECT
        [Sequence],InvoiceNo,EditNo,A.PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,CAST('Open' AS VARCHAR(100)) as [RecStatus],TotalAmount,P.MobileNo
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
		LEFT JOIN [dbo].[mtbl_PatientMaster] P ON A.ClientID = P.ClientID and A.PatientID = P.PatientID
    WHERE
       A. ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate 

    UNION ALL

    SELECT
        [Sequence],InvoiceNo,EditNo,A.PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,'Cancelled',TotalAmount,P.MobileNo
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
		LEFT JOIN [dbo].[mtbl_PatientMaster] P ON A.ClientID = P.ClientID and A.PatientID = P.PatientID
    WHERE
        A.ClientID = @ClientID and EntryDate BETWEEN @FromDate and @ToDate and DocStatus = 'R' and IsCancelled = 1
    ) as tbl
    ORDER BY
        InvoiceNo DESC
END
ELSE IF(@Flag = 100 and @ItemType IS NOT NULL) -- Bill Wise
BEGIN
	SELECT
		DISTINCT A.[Sequence],A.InvoiceNo,A.EditNo,A.PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,CAST('Open' AS VARCHAR(100)) as [RecStatus],TotalAmount
	FROM
		[dbo].[trntbl_TestEntriesHdr] A 
		JOIN [dbo].[trntbl_TestEntriesLine] B ON A.ClientID = B.ClientID and A.Sequence = B.Sequence and A.InvoiceNo = B.InvoiceNo and a.EditNo = B.EditNo 
		JOIN [dbo].[mtbl_TestMaster] T ON T.ClientID = B.ClientID and T.TestID = B.ID
	 Where
		 A. ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and B.Type = 'T' and T.ItemType = @ItemType
     Order by
		A.InvoiceNo ASC
 END
ELSE IF(@Flag = 101 and @SearchKey <> -1) -- Test Wise
BEGIN
    SELECT
        A.[Sequence],A.InvoiceNo,A.EditNo,PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,TotalAmount
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
        JOIN [dbo].[trntbl_TestEntriesLine] B on A.ClientID = B.ClientID and A.[Sequence] = B.[Sequence] and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and B.ID = @SearchKey and [Type] = 'T' 
END
ELSE IF(@Flag = 101 and @SearchKey = -1) -- Test Wise
BEGIN
    SELECT
       C.TestID,C.TestCode,C.TestName,C.Rate,ISNULL(COUNT(A.InvoiceNo),0) as TotalNumber
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
        LEFT JOIN [dbo].[trntbl_TestEntriesLine] B on A.ClientID = B.ClientID and A.[Sequence] = B.[Sequence] and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo
        LEFT JOIN [dbo].[mtbl_TestMaster] C on C.ClientID = B.ClientID and C.TestID = B.ID
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and [Type] = 'T' 
    GROUP BY
        C.TestID,C.TestCode,C.TestName,C.Rate
END
ELSE IF(@Flag = 102 and @SearchKey <> -1) -- Group Wise
BEGIN
    SELECT
       A.[Sequence],A.InvoiceNo,A.EditNo,PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,TotalAmount
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
        JOIN [dbo].[trntbl_TestEntriesLine] B on A.ClientID = B.ClientID and A.[Sequence] = B.[Sequence] and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and B.ID = @SearchKey  and [Type] = 'G' 
END
ELSE IF(@Flag = 102 and @SearchKey = -1) -- Group Wise With All
BEGIN
    SELECT
        C.GroupID,C.GroupCode,C.GroupName,C.Rate,ISNULL(COUNT(A.InvoiceNo),0) as TotalNumber
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
        LEFT JOIN [dbo].[trntbl_TestEntriesLine] B on A.ClientID = B.ClientID and A.[Sequence] = B.[Sequence] and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo
        LEFT JOIN [dbo].[mtbl_TestGroupMaster] C on C.ClientID = B.ClientID and C.GroupID = B.ID
    WHERE
        A.ClientID = @ClientID and A.DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and B.[Type] = 'G' 
    Group BY
        C.GroupID,C.GroupCode,C.GroupName,C.Rate
END
ELSE IF(@Flag = 104) -- Doctor Wise
BEGIN
    SELECT
       A.[Sequence],A.InvoiceNo,A.EditNo,PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,TotalAmount
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and A.Ref_DoctorID = @SearchKey   
END
ELSE IF(@Flag = 105) -- Lab Wise
BEGIN
    SELECT
       A.[Sequence],A.InvoiceNo,A.EditNo,PatientName,GrandTotal,BalanceDue,ResultStatus,PaymentStatus,EntryDate,DiscAmount,TotalAmount
    FROM    
        [dbo].[trntbl_TestEntriesHdr] A 
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and EntryDate BETWEEN @FromDate and @ToDate and A.Ref_LabID = @SearchKey 
END
ELSE IF(@Flag = 103) -- DropDown List
BEGIN 
    -- Test Master
     SELECT
        TestID,TestName
     FROM    
        [dbo].[mtbl_TestMaster]
     WHERE
        ClientID = @ClientID

    -- Doctor Master
    SELECT
        DoctorID,DoctorName
     FROM    
        [dbo].[mtbl_DoctorMaster]
     WHERE
        ClientID = @ClientID
     
     -- Lab Master
    SELECT
        LabID,LabName
     FROM    
        [dbo].[mtbl_LabMaster]
     WHERE
        ClientID = @ClientID

    -- Test Group Master
     SELECT
        GroupID,GroupName
     FROM    
        [dbo].[mtbl_TestGroupMaster]
     WHERE
        ClientID = @ClientID

    -- Test Item Types 
     SELECT
       Distinct ItemType
     FROM    
        [dbo].[mtbl_TestMaster]
     WHERE
        ClientID = @ClientID

END
ELSE IF (@Flag = 106) -- Purchase Report Bill Wise
BEGIN
     SELECT
       [Sequence],InvoiceNo,EditNo,SupplierName,PurchaseRefNo,InvoiceDate,NetAmount as GrandAmount,PaymentMode,TaxAmount,DiscountAmount,Total
    FROM    
        [dbo].[trntbl_PurchaseHeader] 
    WHERE
        ClientID = @ClientID and DocStatus <> 'R' and InvoiceDate BETWEEN @FromDate and @ToDate 
END
ELSE IF(@Flag = 107) -- Purchase Report Item Wise
BEGIN
     SELECT
       A.[Sequence],A.InvoiceNo,A.EditNo,SupplierName,PurchaseRefNo,InvoiceDate,NetAmount as GrandAmount,PaymentMode,A.TaxAmount,A.DiscountAmount,A.Total
    FROM    
        [dbo].[trntbl_PurchaseHeader] A
        JOIN [dbo].[trntbl_PurchaseDetails] B on A.ClientID = B.ClientID and A.[Sequence] = B.[Sequence] and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and InvoiceDate BETWEEN @FromDate and @ToDate and B.ItemNo = @SearchKey
END
ELSE IF(@Flag = 108) -- Purchase Report Supplier Wise
BEGIN
     SELECT
       A.[Sequence],A.InvoiceNo,A.EditNo,SupplierName,PurchaseRefNo,InvoiceDate,NetAmount as GrandAmount,PaymentMode,TaxAmount,DiscountAmount,Total
    FROM    
        [dbo].[trntbl_PurchaseHeader] A 
    WHERE
        A.ClientID = @ClientID and DocStatus <> 'R' and InvoiceDate BETWEEN @FromDate and @ToDate and A.SupplierID = @SearchKey
END
ELSE IF (@Flag = 110) -- Collection Report
BEGIN
     SELECT
		 FORMAT(A.EntryDate,'dd/MM/yyyy') AS [Date]
		,ISNULL(SUM(A.GrandTotal),0) as TotalAmount
		,ISNULL(SUM(A.DiscAmount),0) as DiscountAmount
		,ISNULL(SUM(B.CashAmount + B.BankAmount),0) as TotalReceived
		,ISNULL(SUM(A.BalanceDue),0) as BalanceDue
	 FROM
		[dbo].[trntbl_TestEntriesHdr] A
		LEFT JOIN [dbo].[trntbl_Receipt] B ON A.ClientID = B.ClientID and A.Sequence = B.InvoiceSequence and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.InvoiceEditNo
	Where
		A.ClientID = @ClientID and A.DocStatus <> 'R' and A.EntryDate between @FromDate and @ToDate
	GROUP BY
		FORMAT(A.EntryDate,'dd/MM/yyyy')
	Order by
		FORMAT(A.EntryDate,'dd/MM/yyyy') ASC
END
ELSE IF (@Flag = 111) -- Collection Report
BEGIN
	-- 1?? Receipts where DocType = 'LB'
	SELECT
		A.ReceiptNo,A.PatientName,FORMAT(A.ReceiptDate, 'dd/MM/yyyy') AS ReceiptDate,A.InvoiceNo,A.Sequence,A.InvoiceEditNo,
		A.CashAmount,A.BankAmount,ISNULL(A.WalletIn, 0) AS WalletIn,ISNULL(A.WalletOut, 0) AS WalletOut,'' DocType,'' AS TransType INTO #RC
	FROM
		[dbo].[trntbl_Receipt] A
	WHERE
		A.ClientID = @ClientID AND A.DocStatus <> 'R' AND A.ReceiptDate BETWEEN @FromDate AND @ToDate AND A.DocType = 'LB' 

	INSERT INTO #RC 
	
	SELECT
		A.ReceiptNo,A.PatientName,FORMAT(A.ReceiptDate, 'dd/MM/yyyy') AS ReceiptDate,
		A.InvoiceNo,A.Sequence,A.InvoiceEditNo
		,CASE WHEN A.CashAmount <> 0 then PayingAmout else 0 end as CashAmount
		,CASE WHEN A.BankAmount <> 0 then PayingAmout else 0 end as BankAmount,ISNULL(A.WalletIn, 0) AS WalletIn,
		ISNULL(A.WalletOut, 0) AS WalletOut,'',''
	FROM
		[dbo].[trntbl_Receipt] A
	OUTER APPLY OPENJSON(A.LineJsonData)
	WITH (
		TransType varchar(10) '$.TransType',
		PayingAmout DECIMAL(18,3) '$.PayingAmout'
	) AS J
	WHERE
		A.ClientID = @ClientID AND A.DocStatus <> 'R' AND A.ReceiptDate BETWEEN @FromDate AND @ToDate
		AND J.TransType = 'LB' 
	ORDER BY
		ReceiptDate ASC;

	SELECT 
		 A.ReceiptDate AS [Date]
		,ISNULL(COUNT(A.ReceiptNo),0) as TotalReceipts
		,ISNULL(SUM(A.CashAmount + A.BankAmount),0) as TotalCollection
		,ISNULL(SUM(A.CashAmount),0) as TotalCash
		,ISNULL(SUM(A.BankAmount),0) as TotalBank 
		,ISNULL((SELECT SUM(A.CrAmount) FROM [dbo].[trntbl_WalletTransactions] A Where A.ClientID = @ClientID and A.TransDate between @FromDate and @ToDate),0) as WalletIn
		,ISNULL((SELECT SUM(A.DrAmount) FROM [dbo].[trntbl_WalletTransactions] A Where A.ClientID = @ClientID and A.TransDate between @FromDate and @ToDate),0) as WalletOut 
	FROM
		#RC A
	GROUP BY
		ReceiptDate

	SELECT
		A.ReceiptNo,A.PatientName,A.ReceiptDate AS ReceiptDate,A.InvoiceNo,A.Sequence,A.InvoiceEditNo,A.CashAmount
		,A.BankAmount,ISNULL(WalletIn,0) WalletIn,ISNULL(WalletOut,0) WalletOut
 	FROM
		#RC A

	DROP TABLE #RC 

END
ELSE IF(@Flag =112)
BEGIN
    SELECT
        A.Item_No,B.Item_Name,A.BatchCode,A.ExpiryDate,A.PurchaseDate,A.Onhand,A.PurchaseRefNo
    FROM
        [dbo].[mtbl_ItemsBatches] A
        LEFT JOIN [dbo].[mtbl_ItemMaster] B ON A.ClientID = B.ClientID and A.Item_No = B.Item_No
    WHERE
        A.ClientID = @ClientID  
END
ELSE IF(@Flag =113)
BEGIN
    SELECT * FROM (SELECT
		TransactionID,PatientName,TransDate,DrAmount as Amount,TransPayRefNo,'OUT' as TransType
	FROM
		[dbo].[trntbl_WalletTransactions]
	Where
		ClientID = @ClientID  and PatientID = @PatientID and DrAmount <> 0

	UNION ALL

	SELECT
		TransactionID,PatientName,TransDate,CrAmount,TransPayRefNo,'IN' as TransType
	FROM
		[dbo].[trntbl_WalletTransactions]
	Where
		ClientID = @ClientID  and PatientID = @PatientID and CrAmount <> 0) as tbl Order by TransDate DESC
END
ELSE IF(@Flag =114)
BEGIN
	-- This Month Sales & Last Month Sales
	SELECT
		SUM(CASE 
				WHEN FORMAT(EntryDate, 'yyyyMM') = FORMAT(GETDATE(), 'yyyyMM') THEN GrandTotal
				ELSE 0 
			END) AS ThisMonthSales,
		SUM(CASE 
				WHEN FORMAT(EntryDate, 'yyyyMM') = FORMAT(DATEADD(MONTH, -1, GETDATE()), 'yyyyMM') THEN GrandTotal
				ELSE 0 
			END) AS LastMonthSales
	FROM
		trntbl_TestEntriesHdr
	WHERE
		EntryDate >= DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) and DocStatus <> 'R'

   -- This Month Purchase & Last Month Purchase
   SELECT
    ISNULL(SUM(CASE 
            WHEN FORMAT(InvoiceDate, 'yyyyMM') = FORMAT(GETDATE(), 'yyyyMM') THEN NetAmount
            ELSE 0 
        END),0) AS ThisMonthPurchase,
    ISNULL(SUM(CASE 
            WHEN FORMAT(InvoiceDate, 'yyyyMM') = FORMAT(DATEADD(MONTH, -1, GETDATE()), 'yyyyMM') THEN NetAmount
            ELSE 0 
        END),0) AS LastMonthPurchase
	FROM
		trntbl_PurchaseHeader
	WHERE
		InvoiceDate >= DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) and DocStatus <> 'R' 

  -- This Month Patients & Last Month Patients
  SELECT
    ISNULL(COUNT(CASE 
            WHEN FORMAT(Created_at, 'yyyyMM') = FORMAT(GETDATE(), 'yyyyMM') THEN PatientID
            ELSE NULL
        END), 0) AS ThisMonthPatient,
    ISNULL(COUNT(CASE 
            WHEN FORMAT(Created_at, 'yyyyMM') = FORMAT(DATEADD(MONTH, -1, GETDATE()), 'yyyyMM') THEN PatientID
            ELSE NULL
        END), 0) AS LastMonthPatient
	FROM
		mtbl_PatientMaster
	WHERE
		Created_at >= DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));

	-- Today Sales & Yesterday Sales
	SELECT
		SUM(CASE 
				WHEN CAST(EntryDate AS DATE) = CAST(GETDATE() AS DATE) THEN GrandTotal
				ELSE 0 
			END) AS TodaySales,
		SUM(CASE 
				WHEN CAST(EntryDate AS DATE) = CAST(DATEADD(DAY, -1, GETDATE()) AS DATE) THEN GrandTotal
				ELSE 0 
			END) AS YesterdaySales
	FROM
		trntbl_TestEntriesHdr
	WHERE
		EntryDate >= CAST(DATEADD(DAY, -1, GETDATE()) AS DATE) and DocStatus <> 'R'
END
ELSE IF(@Flag =115) -- Sales Graph Summary
BEGIN
		-- Get today's date info
	DECLARE @Today DATE = CAST(GETDATE() AS DATE);
	DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);

	DECLARE @StartOfWeek DATE = DATEADD(DAY, 1 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS DATE));
	DECLARE @StartOfLastWeek DATE = DATEADD(WEEK, -1, @StartOfWeek);

	DECLARE @StartOfMonth DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
	DECLARE @StartOfLastMonth DATE = DATEADD(MONTH, -1, @StartOfMonth);

	DECLARE @StartOfYear DATE = DATEFROMPARTS(YEAR(GETDATE()), 1, 1);
	DECLARE @StartOfLastYear DATE = DATEFROMPARTS(YEAR(GETDATE()) - 1, 1, 1);


	IF @GraphType = 'daily'
	BEGIN
		;WITH Hours AS (
			SELECT 0 AS HourNumber
			UNION ALL SELECT 1
			UNION ALL SELECT 2
			UNION ALL SELECT 3
			UNION ALL SELECT 4
			UNION ALL SELECT 5
			UNION ALL SELECT 6
			UNION ALL SELECT 7
			UNION ALL SELECT 8
			UNION ALL SELECT 9
			UNION ALL SELECT 10
			UNION ALL SELECT 11
			UNION ALL SELECT 12
			UNION ALL SELECT 13
			UNION ALL SELECT 14
			UNION ALL SELECT 15
			UNION ALL SELECT 16
			UNION ALL SELECT 17
			UNION ALL SELECT 18
			UNION ALL SELECT 19
			UNION ALL SELECT 20
			UNION ALL SELECT 21
			UNION ALL SELECT 22
			UNION ALL SELECT 23
		),
		SalesData AS (
			SELECT 
				DATEPART(HOUR, CAST(EntryDate AS DATETIME)) AS HourNumber,
				SUM(CASE WHEN CAST(EntryDate AS DATE) = @Today THEN GrandTotal ELSE 0 END) AS TodaySales,
				SUM(CASE WHEN CAST(EntryDate AS DATE) = @Yesterday THEN GrandTotal ELSE 0 END) AS YesterdaySales
			FROM 
				trntbl_TestEntriesHdr
			WHERE 
				CAST(EntryDate AS DATE) IN (@Today, @Yesterday) and DocStatus <> 'R'
			GROUP BY 
				DATEPART(HOUR, CAST(EntryDate AS DATETIME))
		)
		SELECT 
			FORMAT(DATEADD(HOUR, H.HourNumber, 0), 'h tt') AS TimeSlot,
			ISNULL(S.TodaySales, 0) AS TodaySales,
			ISNULL(S.YesterdaySales, 0) AS YesterdaySales
		FROM 
			Hours H
		LEFT JOIN 
			SalesData S ON H.HourNumber = S.HourNumber
		ORDER BY 
			H.HourNumber;
	END
	ELSE IF @GraphType = 'weekly'
	BEGIN
		;WITH WeekDays AS (
			SELECT 1 AS WeekDayNumber, 'Sunday' AS WeekDayName
			UNION ALL SELECT 2, 'Monday'
			UNION ALL SELECT 3, 'Tuesday'
			UNION ALL SELECT 4, 'Wednesday'
			UNION ALL SELECT 5, 'Thursday'
			UNION ALL SELECT 6, 'Friday'
			UNION ALL SELECT 7, 'Saturday'
		),
		SalesData AS (
			SELECT 
				DATEPART(WEEKDAY, EntryDate) AS WeekDayNumber,
				SUM(CASE WHEN EntryDate >= @StartOfWeek THEN GrandTotal ELSE 0 END) AS ThisWeekSales,
				SUM(CASE WHEN EntryDate >= @StartOfLastWeek AND EntryDate < @StartOfWeek THEN GrandTotal ELSE 0 END) AS LastWeekSales
			FROM 
				trntbl_TestEntriesHdr
			WHERE 
				EntryDate >= @StartOfLastWeek and DocStatus <> 'R'
			GROUP BY 
				DATEPART(WEEKDAY, EntryDate)
		)
		SELECT 
			W.WeekDayName,
			ISNULL(S.ThisWeekSales, 0) AS ThisWeekSales,
			ISNULL(S.LastWeekSales, 0) AS LastWeekSales
		FROM 
			WeekDays W
		LEFT JOIN 
			SalesData S ON W.WeekDayNumber = S.WeekDayNumber
		ORDER BY 
			W.WeekDayNumber;
	END 
	ELSE IF @GraphType = 'monthly'
	BEGIN
		;WITH DaysOfMonth AS (
			SELECT 1 AS DayOfMonth
			UNION ALL SELECT 2
			UNION ALL SELECT 3
			UNION ALL SELECT 4
			UNION ALL SELECT 5
			UNION ALL SELECT 6
			UNION ALL SELECT 7
			UNION ALL SELECT 8
			UNION ALL SELECT 9
			UNION ALL SELECT 10
			UNION ALL SELECT 11
			UNION ALL SELECT 12
			UNION ALL SELECT 13
			UNION ALL SELECT 14
			UNION ALL SELECT 15
			UNION ALL SELECT 16
			UNION ALL SELECT 17
			UNION ALL SELECT 18
			UNION ALL SELECT 19
			UNION ALL SELECT 20
			UNION ALL SELECT 21
			UNION ALL SELECT 22
			UNION ALL SELECT 23
			UNION ALL SELECT 24
			UNION ALL SELECT 25
			UNION ALL SELECT 26
			UNION ALL SELECT 27
			UNION ALL SELECT 28
			UNION ALL SELECT 29
			UNION ALL SELECT 30
			UNION ALL SELECT 31
		),
		SalesData AS (
			SELECT 
				DAY(EntryDate) AS DayOfMonth,
				SUM(CASE WHEN EntryDate >= @StartOfMonth THEN GrandTotal ELSE 0 END) AS ThisMonthSales,
				SUM(CASE WHEN EntryDate >= @StartOfLastMonth AND EntryDate < @StartOfMonth THEN GrandTotal ELSE 0 END) AS LastMonthSales
			FROM 
				trntbl_TestEntriesHdr
			WHERE 
				EntryDate >= @StartOfLastMonth and DocStatus <> 'R'
			GROUP BY 
				DAY(EntryDate)
		)
		SELECT 
			D.DayOfMonth,
			ISNULL(S.ThisMonthSales, 0) AS ThisMonthSales,
			ISNULL(S.LastMonthSales, 0) AS LastMonthSales
		FROM 
			DaysOfMonth D
		LEFT JOIN 
			SalesData S ON D.DayOfMonth = S.DayOfMonth
		WHERE
			D.DayOfMonth <= DAY(EOMONTH(GETDATE()))  -- Only till this month's last date
		ORDER BY 
			D.DayOfMonth;
	END
	ELSE IF @GraphType = 'yearly'
	BEGIN
		;WITH Months AS (
			SELECT 1 AS MonthNumber, 'January' AS MonthName
			UNION ALL SELECT 2, 'February'
			UNION ALL SELECT 3, 'March'
			UNION ALL SELECT 4, 'April'
			UNION ALL SELECT 5, 'May'
			UNION ALL SELECT 6, 'June'
			UNION ALL SELECT 7, 'July'
			UNION ALL SELECT 8, 'August'
			UNION ALL SELECT 9, 'September'
			UNION ALL SELECT 10, 'October'
			UNION ALL SELECT 11, 'November'
			UNION ALL SELECT 12, 'December'
		),
		SalesData AS (
			SELECT 
				DATEPART(MONTH, EntryDate) AS MonthNumber,
				SUM(CASE WHEN EntryDate >= @StartOfYear THEN GrandTotal ELSE 0 END) AS ThisYearSales,
				SUM(CASE WHEN EntryDate >= @StartOfLastYear AND EntryDate < @StartOfYear THEN GrandTotal ELSE 0 END) AS LastYearSales
			FROM 
				trntbl_TestEntriesHdr
			WHERE 
				EntryDate >= @StartOfLastYear and DocStatus <> 'R'
			GROUP BY 
				DATEPART(MONTH, EntryDate)
		)
		SELECT 
			M.MonthName,
			ISNULL(S.ThisYearSales, 0) AS ThisYearSales,
			ISNULL(S.LastYearSales, 0) AS LastYearSales
		FROM 
			Months M
		LEFT JOIN 
			SalesData S ON M.MonthNumber = S.MonthNumber
		ORDER BY 
			M.MonthNumber;
	END
	END
	ELSE IF(@Flag = 116)
	BEGIN
		SELECT
			 [Sequence],[ReceiptNo],[PatientID],[PatientName],[ReceiptDate],[InvoiceNo],[CashAmount],
			[BankAmount],[PaymentMode],[DocType],[LineJsonData]
		FROM
			[dbo].[trntbl_Receipt]
		WHERE
			ClientID = @ClientID AND DocStatus <> 'R'AND ReceiptDate BETWEEN @FromDate AND @ToDate
		ORDER BY
			ReceiptNo DESC
	END
	ELSE IF (@Flag = 118)  
	BEGIN
		SELECT   
			 T.Sequence,T.InvoiceNo,T.EditNo,T.ID,T.Name,T.Type,T.LabID,B.LabName,ISNULL(O.Amount,0) as Amount
			,T.CollectionDateTime,T.ContactNo,T.ContactPersonName	
		FROM 
			[dbo].[trntbl_OutSourceTests] T 
			LEFT JOIN mtbl_LabMaster B ON T.ClientID =B.ClientID and T.LabID = B.LabID
			LEFT JOIN mtbl_OutSourceTestsMapping O ON T.ClientID = O.ClientID and T.LabID = O.LabID and T.ID = O.ID and T.Type = O.Type
 		WHERE 
			T.ClientID = @ClientID AND CAST(T.CollectionDateTime AS DATE) BETWEEN @FromDate AND @ToDate 
			and (@LabID = 0 OR T.LabID = @LabID) and ( @TestID = 0 OR T.ID = @TestID)
	END


	/*============================================= Reception Reports Section ===================================================== */

	ELSE IF(@Flag=117) -- OP Report
	BEGIN
		SELECT
			A.*,B.Department,FORMAT(A.Created_at,'dd-MM-yyyy hh:mm tt') EntDate
		FROM
			[trntbl_OPBilling] A
			LEFT JOIN [dbo].[mtbl_DoctorMaster] B ON A.ClientID = B.ClientID and A.Ref_DoctorID = B.DoctorID 
		Where
			A.ClientID = @ClientID and EntryDate between @FromDate and @ToDate and A.DocStatus <> 'R'
	END

	/****************************************** Stock Ageing Report ***********************************************************************/
	ELSE IF(@Flag = 119)
	BEGIN
    --------------------------------------------------------------------------------
    -- 1) Opening stock (all movements BEFORE @FromDate)
    --------------------------------------------------------------------------------
    DECLARE @OpeningStock DECIMAL(18,6);

    SELECT @OpeningStock = ISNULL(
        SUM(
            CASE 
                WHEN TranType = 'PI' THEN ABS(Qty)      -- treat purchases as IN (abs)
                WHEN TranType = 'TE' THEN -ABS(Qty)     -- treat test entries as OUT (abs, negative)
                ELSE 0
            END
        ), 0)
    FROM dbo.trntbl_InventoryTracking
    WHERE ClientID = @ClientID
      AND Item_No = @ItemNo
      AND CAST(TrasactionDate AS DATE) < CAST(@FromDate AS DATE)
      AND DocStatus <> 'R';

    --------------------------------------------------------------------------------
    -- 2) Grab transactions in the period into a temp table with canonical columns
    --------------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Txns') IS NOT NULL DROP TABLE #Txns;

    SELECT
        [Sequence],
        InvoiceNo,
        EditNo,
        Item_No,
        Item_Name,
        BatchCode,
        Qty,
        TranType,
        TrasactionDate,
        CASE WHEN TranType = 'PI' THEN ABS(Qty) ELSE 0 END AS MovementIn,
        CASE WHEN TranType = 'TE' THEN ABS(Qty) ELSE 0 END AS MovementOut,
        (CASE WHEN TranType = 'PI' THEN ABS(Qty) ELSE 0 END)
          - (CASE WHEN TranType = 'TE' THEN ABS(Qty) ELSE 0 END) AS SignedQty
    INTO #Txns
    FROM dbo.trntbl_InventoryTracking
    WHERE ClientID = @ClientID
      AND Item_No = @ItemNo
      AND CAST(TrasactionDate AS DATE) BETWEEN CAST(@FromDate AS DATE) AND CAST(@ToDate AS DATE)
      AND DocStatus <> 'R';

    --------------------------------------------------------------------------------
    -- 3) Summary totals using MovementIn/MovementOut/SignedQty (consistent)
    --------------------------------------------------------------------------------
    SELECT
        @OpeningStock AS OpeningStock,
        ISNULL(SUM(MovementIn),0)  AS TotalIn,
        ISNULL(SUM(MovementOut),0) AS TotalOut,
        ISNULL(SUM(SignedQty),0)   AS NetMovement,
        (@OpeningStock + ISNULL(SUM(SignedQty),0)) AS ClosingStock
    FROM #Txns;

    --------------------------------------------------------------------------------
    -- 4) Ledger rows with opening balance as the first entry
    --------------------------------------------------------------------------------
    ;WITH Ledger AS
    (
        SELECT
            T.[Sequence],
            T.InvoiceNo,
            T.EditNo,
            T.Item_No,
            T.Item_Name,
            T.BatchCode,
            T.TrasactionDate,
            CASE WHEN T.TranType = 'PI' THEN 'Purchase'
                 WHEN T.TranType = 'TE' THEN 'TestEntry'
                 ELSE T.TranType END AS TransType,
            T.Qty,
            @OpeningStock + SUM(T.SignedQty) OVER (
                ORDER BY CAST(T.TrasactionDate AS DATETIME), T.[Sequence], T.InvoiceNo, T.EditNo
                ROWS UNBOUNDED PRECEDING
            ) AS RunningTotalQty
        FROM #Txns T
    )
    SELECT 
        CAST(NULL AS BIGINT) AS [Sequence],
        CAST(NULL AS BIGINT) AS InvoiceNo,
        CAST(NULL AS INT) AS EditNo,
        @ItemNo AS Item_No,
        (SELECT TOP 1 Item_Name FROM #Txns) AS Item_Name,
        (SELECT TOP 1 BatchCode FROM #Txns) AS BatchCode,
        DATEADD(SECOND, -1, CAST(@FromDate AS DATETIME)) AS TrasactionDate,
        'OpeningStock' AS TransType,
        0.000 AS Qty,
        @OpeningStock AS RunningTotalQty
    UNION ALL
    SELECT 
        [Sequence], InvoiceNo, EditNo, Item_No, Item_Name, BatchCode,
        TrasactionDate, TransType, Qty, SignedQty, RunningTotalQty
    FROM Ledger
    ORDER BY TrasactionDate, [Sequence], InvoiceNo, EditNo;

    DROP TABLE #Txns;
END