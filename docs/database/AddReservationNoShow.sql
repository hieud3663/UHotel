-- Bổ sung trạng thái khách không đến cho phiếu đặt phòng quá giờ nhận
-- Chạy script này cho database hiện tại trước khi dùng chức năng MarkNoShow.

IF COL_LENGTH('dbo.ReservationForm', 'reservationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.ReservationForm
    ADD reservationStatus NVARCHAR(20) NOT NULL
        CONSTRAINT DF_ReservationForm_reservationStatus DEFAULT 'BOOKED';
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ReservationForm_reservationStatus'
      AND parent_object_id = OBJECT_ID('dbo.ReservationForm')
)
BEGIN
    ALTER TABLE dbo.ReservationForm
    ADD CONSTRAINT CK_ReservationForm_reservationStatus
        CHECK (reservationStatus IN ('BOOKED', 'NO_SHOW'));
END;
GO

CREATE OR ALTER PROCEDURE sp_MarkReservationNoShow
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Phiếu đặt phòng không tồn tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID AND isActivate = 'DEACTIVATE')
        BEGIN
            RAISERROR(N'Phiếu đặt phòng đã bị hủy hoặc đã được xử lý.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Phiếu đặt phòng đã check-in, không thể đánh dấu khách không đến.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF NOT EXISTS (SELECT 1 FROM Employee WHERE employeeID = @employeeID AND isActivate = 'ACTIVATE')
        BEGIN
            RAISERROR(N'Nhân viên xử lý không tồn tại hoặc không hoạt động.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        DECLARE @roomID NVARCHAR(15);
        DECLARE @checkInDate DATETIME;

        SELECT
            @roomID = roomID,
            @checkInDate = checkInDate
        FROM ReservationForm
        WHERE reservationFormID = @reservationFormID;

        IF @checkInDate > GETDATE()
        BEGIN
            RAISERROR(N'Chưa đến giờ nhận phòng, không thể đánh dấu khách không đến.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        UPDATE ReservationForm
        SET reservationStatus = 'NO_SHOW',
            isActivate = 'DEACTIVATE'
        WHERE reservationFormID = @reservationFormID;

        -- Giải phóng phòng, nhưng giữ RESERVED nếu có đặt phòng kế tiếp trong vòng 5 giờ.
        UPDATE Room
        SET roomStatus = CASE
            WHEN EXISTS (
                SELECT 1
                FROM ReservationForm nextRf
                LEFT JOIN HistoryCheckin nextHci ON nextRf.reservationFormID = nextHci.reservationFormID
                WHERE nextRf.roomID = @roomID
                  AND nextRf.reservationFormID <> @reservationFormID
                  AND nextRf.isActivate = 'ACTIVATE'
                  AND nextHci.historyCheckinID IS NULL
                  AND nextRf.checkInDate > GETDATE()
                  AND nextRf.checkInDate <= DATEADD(HOUR, 5, GETDATE())
            ) THEN 'RESERVED'
            ELSE 'AVAILABLE'
        END
        WHERE roomID = @roomID
          AND roomStatus IN ('AVAILABLE', 'RESERVED');

        SELECT
            @reservationFormID AS ReservationFormID,
            @roomID AS RoomID,
            'NO_SHOW' AS ReservationStatus,
            (SELECT roomStatus FROM Room WHERE roomID = @roomID) AS RoomStatus,
            N'Đã đánh dấu khách không đến và giải phóng phòng nếu phù hợp.' AS Message;

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO
