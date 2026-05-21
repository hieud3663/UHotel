-- Kiểm tra và xóa cơ sở dữ liệu nếu tồn tại
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'HotelManagement')
BEGIN
    DROP DATABASE HotelManagement;
END
GO

-- Tạo cơ sở dữ liệu mới
CREATE DATABASE HotelManagement;
GO

-- Sử dụng cơ sở dữ liệu HotelManagement
USE HotelManagement;
GO

-- ===================================================================================
-- 1. TẠO BẢNG
-- ===================================================================================

-- Tạo bảng User
DROP TABLE IF EXISTS [User];

CREATE TABLE [User] (
    userID NVARCHAR(15) NOT NULL PRIMARY KEY,
    employeeID NVARCHAR(15) NOT NULL,
    username NVARCHAR(50) NOT NULL UNIQUE,
    passwordHash NVARCHAR(255) NOT NULL,
    role NVARCHAR(10) NOT NULL CHECK (role IN ('ADMIN', 'MANAGER', 'EMPLOYEE', 'CLEANER', 'TECHNICIAN')),
    isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE')),
    FOREIGN KEY (employeeID) REFERENCES Employee(employeeID)
);

-- Tạo bảng Employee
CREATE TABLE Employee (
    employeeID NVARCHAR(15) NOT NULL PRIMARY KEY,
    fullName NVARCHAR(50) NOT NULL,
    phoneNumber NVARCHAR(10) NOT NULL UNIQUE,
    email NVARCHAR(50) NOT NULL,
    address NVARCHAR(100),
    gender NVARCHAR(6) NOT NULL 
	CHECK (gender IN ('MALE', 'FEMALE')),
    idCardNumber NVARCHAR(12) NOT NULL,
    dob DATE NOT NULL,
    position NVARCHAR(20) NOT NULL 
	CHECK (position IN ('RECEPTIONIST', 'MANAGER', 'CLEANER', 'SECURITY', 'ACCOUNTANT', 'TECHNICIAN')),
	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' 
	CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE')),
    --Kiểm tra tuổi >= 18 và <= 65
    CHECK (DATEDIFF(YEAR, dob, GETDATE()) BETWEEN 18 AND 65)
);
GO


-- Tạo bảng ServiceCategory
CREATE TABLE ServiceCategory (
    serviceCategoryID NVARCHAR(15) NOT NULL PRIMARY KEY,
    serviceCategoryName NVARCHAR(50) NOT NULL,
	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE'))
);
GO

-- Tạo bảng HotelService
CREATE TABLE HotelService (
    hotelServiceId NVARCHAR(15) NOT NULL PRIMARY KEY,
    serviceName NVARCHAR(50) NOT NULL,
    description NVARCHAR(255) NOT NULL,
    servicePrice MONEY NOT NULL,
    serviceCategoryID NVARCHAR(15) NULL,

    CONSTRAINT FK_HotelService_ServiceCategory
        FOREIGN KEY (serviceCategoryID)
        REFERENCES ServiceCategory(serviceCategoryID)
        ON DELETE SET NULL
		ON UPDATE CASCADE,

	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE')),

    CHECK (servicePrice >= 0),
);
GO

-- Tạo bảng RoomCategory
CREATE TABLE RoomCategory (
    roomCategoryID NVARCHAR(15) NOT NULL PRIMARY KEY,
    roomCategoryName NVARCHAR(50) NOT NULL,
    numberOfBed INT NOT NULL,
	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE')),
    CHECK (numberOfBed >= 1 AND numberOfBed <= 10),
);
GO

-- Tạo bảng Pricing
CREATE TABLE Pricing (
    pricingID NVARCHAR(15) NOT NULL PRIMARY KEY,
    priceUnit NVARCHAR(15) NOT NULL CHECK (priceUnit IN ('DAY', 'HOUR')),
    price MONEY NOT NULL,
    roomCategoryID NVARCHAR(15) NOT NULL,

    CHECK (price >= 0),

    CONSTRAINT FK_Pricing_RoomCategory FOREIGN KEY (roomCategoryID)
        REFERENCES RoomCategory(roomCategoryID)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT UQ_roomCategoryID_priceUnit UNIQUE (roomCategoryID, priceUnit)
);
GO

-- Tạo bảng Room
CREATE TABLE Room (
    roomID NVARCHAR(15) NOT NULL PRIMARY KEY,
    roomStatus NVARCHAR(20) NOT NULL CHECK (roomStatus IN ('AVAILABLE', 'ON_USE', 'UNAVAILABLE', 'OVERDUE', 'RESERVED', 'DIRTY', 'CLEANING', 'MAINTENANCE', 'OUT_OF_SERVICE')),
    dateOfCreation DATETIME NOT NULL,
    roomCategoryID NVARCHAR(15) NOT NULL,
    FOREIGN KEY (roomCategoryID) REFERENCES RoomCategory(roomCategoryID),
	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE'))
);
GO


-- Tạo bảng Customer
CREATE TABLE Customer (
    customerID NVARCHAR(15) NOT NULL PRIMARY KEY,
    fullName NVARCHAR(50) NOT NULL,
    phoneNumber NVARCHAR(10) NOT NULL UNIQUE,
    email NVARCHAR(50),
    address NVARCHAR(100),
    gender NVARCHAR(6) NOT NULL CHECK (gender IN ('MALE', 'FEMALE')),
    idCardNumber NVARCHAR(12) NOT NULL UNIQUE,
    dob DATE NOT NULL,
	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE')),
    --Kiểm tra tuổi >= 18
    CHECK (DATEDIFF(YEAR, dob, GETDATE()) >= 18),
);
GO

-- Tạo bảng ReservationForm
CREATE TABLE ReservationForm (
    reservationFormID NVARCHAR(15) NOT NULL PRIMARY KEY,
    reservationDate DATETIME NOT NULL,
    checkInDate DATETIME NOT NULL,
    checkOutDate DATETIME NOT NULL,
    employeeID NVARCHAR(15),
    roomID NVARCHAR(15),
    customerID NVARCHAR(15),
	roomBookingDeposit FLOAT NOT NULL,
    FOREIGN KEY (employeeID) REFERENCES Employee(employeeID),
    FOREIGN KEY (roomID) REFERENCES Room(roomID),
    FOREIGN KEY (customerID) REFERENCES Customer(customerID),
	isActivate NVARCHAR(10) NOT NULL DEFAULT 'ACTIVATE' CHECK (isActivate IN ('ACTIVATE', 'DEACTIVATE')),
    CHECK (reservationDate <= checkInDate),
    CHECK (checkInDate <= checkOutDate), 
    CHECK (roomBookingDeposit >= 0),
);
GO

ALTER TABLE ReservationForm
ADD priceUnit NVARCHAR(15) NOT NULL DEFAULT 'DAY' CHECK (priceUnit IN ('DAY', 'HOUR')),
    unitPrice MONEY NOT NULL DEFAULT 0,
    reservationStatus NVARCHAR(20) NOT NULL DEFAULT 'BOOKED' CHECK (reservationStatus IN ('BOOKED', 'NO_SHOW'));

GO
-- Tạo bảng RoomChangeHistory
CREATE TABLE RoomChangeHistory (
    roomChangeHistoryID NVARCHAR(15) NOT NULL PRIMARY KEY,
    dateChanged DATETIME NOT NULL,
    roomID NVARCHAR(15) NOT NULL,
    reservationFormID NVARCHAR(15) NOT NULL,
    employeeID NVARCHAR(15),
    FOREIGN KEY (roomID) REFERENCES Room(roomID),
    FOREIGN KEY (reservationFormID) REFERENCES ReservationForm(reservationFormID),
    FOREIGN KEY (employeeID) REFERENCES Employee(employeeID)
		ON DELETE SET NULL
        ON UPDATE CASCADE,

);
GO

-- Tạo bảng RoomUsageService
CREATE TABLE RoomUsageService (
    roomUsageServiceId NVARCHAR(15) NOT NULL PRIMARY KEY,
    quantity INT NOT NULL,
    unitPrice DECIMAL(18, 2) NOT NULL,
    totalPrice AS (quantity * unitPrice) PERSISTED,
    dateAdded DATETIME NOT NULL,
    hotelServiceId NVARCHAR(15) NOT NULL,
    reservationFormID NVARCHAR(15) NOT NULL,
    employeeID NVARCHAR(15),

    FOREIGN KEY (hotelServiceId) REFERENCES HotelService(hotelServiceId),
    FOREIGN KEY (reservationFormID) REFERENCES ReservationForm(reservationFormID),
    FOREIGN KEY (employeeID) REFERENCES Employee(employeeID)
		ON DELETE SET NULL
        ON UPDATE CASCADE,
    
    CHECK (unitPrice >= 0),
    CHECK (quantity >= 1),
);
GO

-- Tạo bảng HistoryCheckin
CREATE TABLE HistoryCheckin (
    historyCheckInID NVARCHAR(15) NOT NULL PRIMARY KEY,
    checkInDate DATETIME NOT NULL,
    reservationFormID NVARCHAR(15) NOT NULL UNIQUE,
    employeeID NVARCHAR(15),
    FOREIGN KEY (reservationFormID) REFERENCES ReservationForm(reservationFormID),
    FOREIGN KEY (employeeID) REFERENCES Employee(employeeID)
		ON DELETE SET NULL
        ON UPDATE CASCADE
);
GO

-- Tạo bảng HistoryCheckOut
CREATE TABLE HistoryCheckOut (
    historyCheckOutID NVARCHAR(15) NOT NULL PRIMARY KEY,
    checkOutDate DATETIME NOT NULL,
    reservationFormID NVARCHAR(15) NOT NULL UNIQUE,
    employeeID NVARCHAR(15),
    FOREIGN KEY (reservationFormID) REFERENCES ReservationForm(reservationFormID),
    FOREIGN KEY (employeeID) REFERENCES Employee(employeeID)
		ON DELETE SET NULL
        ON UPDATE CASCADE
);
GO

-- Tạo bảng RoomTask (công việc bảo trì/dọn phòng)
CREATE TABLE RoomTask (
    roomTaskID NVARCHAR(15) NOT NULL PRIMARY KEY,
    roomID NVARCHAR(15) NOT NULL,
    taskType NVARCHAR(20) NOT NULL CHECK (taskType IN ('CLEANING', 'MAINTENANCE')),
    title NVARCHAR(200) NOT NULL,
    description NVARCHAR(MAX) NULL,
    priority NVARCHAR(20) NOT NULL DEFAULT 'NORMAL' CHECK (priority IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')),
    status NVARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'ASSIGNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')),
    assignedEmployeeID NVARCHAR(15) NULL,
    createdByEmployeeID NVARCHAR(15) NOT NULL,
    createdAt DATETIME NOT NULL DEFAULT GETDATE(),
    dueAt DATETIME NULL,
    slaMinutes INT NULL CHECK (slaMinutes IS NULL OR slaMinutes > 0),
    startedAt DATETIME NULL,
    completedAt DATETIME NULL,
    cancelledAt DATETIME NULL,
    cancelReason NVARCHAR(500) NULL,
    note NVARCHAR(MAX) NULL,

    CONSTRAINT FK_RoomTask_Room FOREIGN KEY (roomID) REFERENCES Room(roomID),
    CONSTRAINT FK_RoomTask_AssignedEmployee FOREIGN KEY (assignedEmployeeID) REFERENCES Employee(employeeID) ON DELETE SET NULL,
    CONSTRAINT FK_RoomTask_CreatedByEmployee FOREIGN KEY (createdByEmployeeID) REFERENCES Employee(employeeID),
    CONSTRAINT CHK_RoomTask_TimeFlow CHECK (
        (startedAt IS NULL OR startedAt >= createdAt) AND
        (completedAt IS NULL OR completedAt >= createdAt) AND
        (cancelledAt IS NULL OR cancelledAt >= createdAt)
    )
);
GO

-- Tạo bảng RoomTaskHistory (lịch sử xử lý công việc bảo trì/dọn phòng)
CREATE TABLE RoomTaskHistory (
    roomTaskHistoryID NVARCHAR(15) NOT NULL PRIMARY KEY,
    roomTaskID NVARCHAR(15) NOT NULL,
    oldStatus NVARCHAR(20) NULL CHECK (oldStatus IN ('PENDING', 'ASSIGNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED', NULL)),
    newStatus NVARCHAR(20) NOT NULL CHECK (newStatus IN ('PENDING', 'ASSIGNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')),
    changedByEmployeeID NVARCHAR(15) NOT NULL,
    changedAt DATETIME NOT NULL DEFAULT GETDATE(),
    note NVARCHAR(MAX) NULL,

    CONSTRAINT FK_RoomTaskHistory_RoomTask FOREIGN KEY (roomTaskID) REFERENCES RoomTask(roomTaskID) ON DELETE CASCADE,
    CONSTRAINT FK_RoomTaskHistory_Employee FOREIGN KEY (changedByEmployeeID) REFERENCES Employee(employeeID)
);
GO

-- Tạo bảng Invoice
CREATE TABLE Invoice (
    invoiceID NVARCHAR(15) NOT NULL PRIMARY KEY,
    invoiceDate DATETIME NOT NULL,
    roomCharge DECIMAL(18, 2) NOT NULL,
    servicesCharge DECIMAL(18, 2) NOT NULL,
    totalDue AS (roomCharge + servicesCharge) PERSISTED,
    netDue AS ((roomCharge + servicesCharge) * 1.1) PERSISTED, -- Thuế 10%
    reservationFormID NVARCHAR(15) NOT NULL,
    FOREIGN KEY (reservationFormID) REFERENCES ReservationForm(reservationFormID),
    CHECK (totalDue >= 0 AND totalDue = roomCharge + servicesCharge), 
    CHECK (netDue >= 0),
    CHECK (roomCharge >= 0),
    CHECK (servicesCharge >= 0),
);

GO
ALTER TABLE Invoice
ADD roomBookingDeposit DECIMAL(18, 2);
ALTER TABLE Invoice
ADD taxRate FLOAT NOT NULL DEFAULT 0.1 CHECK (taxRate >= 0 AND taxRate <= 1),
    totalAmount AS (
        CASE
            WHEN ((roomCharge + servicesCharge) * (1 + taxRate) - roomBookingDeposit) > 0
                THEN ((roomCharge + servicesCharge) * (1 + taxRate) - roomBookingDeposit)
            ELSE 0
        END
    ) PERSISTED;
GO

-- Thêm các cột mới cho luồng thanh toán mới
ALTER TABLE Invoice
ADD isPaid BIT NOT NULL DEFAULT 0,              -- 0 = Chưa thanh toán, 1 = Đã thanh toán
    paymentDate DATETIME NULL,                  -- Ngày thanh toán thực tế
    paymentMethod NVARCHAR(20) NULL CHECK (paymentMethod IN ('CASH', 'CARD', 'TRANSFER', NULL)),
    checkoutType NVARCHAR(20) NULL CHECK (checkoutType IN ('CHECKOUT_THEN_PAY', 'PAY_THEN_CHECKOUT', NULL));
GO

ALTER TABLE Invoice
ADD amountPaid DECIMAL(18,2) NOT NULL DEFAULT 0;

GO

-- Thêm cột invoiceID vào HistoryCheckOut
ALTER TABLE HistoryCheckOut
ADD invoiceID NVARCHAR(15) NULL,
CONSTRAINT FK_HistoryCheckOut_Invoice FOREIGN KEY (invoiceID) REFERENCES Invoice(invoiceID) ON DELETE NO ACTION;
GO

-- Tạo bảng ConfirmationReceipt (Phiếu xác nhận đặt phòng/nhận phòng)
CREATE TABLE ConfirmationReceipt (
    receiptID NVARCHAR(15) NOT NULL PRIMARY KEY,
    receiptType NVARCHAR(15) NOT NULL CHECK (receiptType IN ('RESERVATION', 'CHECKIN')),
    issueDate DATETIME NOT NULL DEFAULT GETDATE(),
    reservationFormID NVARCHAR(15) NULL,
    invoiceID NVARCHAR(15) NULL,
    customerName NVARCHAR(50) NOT NULL,
    customerPhone NVARCHAR(10) NOT NULL,
    customerEmail NVARCHAR(50) NULL,
    roomID NVARCHAR(15) NOT NULL,
    roomCategoryName NVARCHAR(50) NOT NULL,
    checkInDate DATETIME NOT NULL,
    checkOutDate DATETIME NULL,
    priceUnit NVARCHAR(15) NULL,
    unitPrice MONEY NULL,
    deposit MONEY NULL,
    totalAmount MONEY NULL,
    employeeName NVARCHAR(50) NULL,
    notes NVARCHAR(500) NULL,
    qrCode NVARCHAR(200) NULL,
    
    CONSTRAINT FK_ConfirmationReceipt_ReservationForm 
        FOREIGN KEY (reservationFormID) REFERENCES ReservationForm(reservationFormID)
        ON DELETE NO ACTION,
    CONSTRAINT FK_ConfirmationReceipt_Invoice 
        FOREIGN KEY (invoiceID) REFERENCES Invoice(invoiceID)
        ON DELETE NO ACTION,
    
    -- Phiếu RESERVATION phải có reservationFormID
    CONSTRAINT CHK_Receipt_ReservationType 
        CHECK (
            (receiptType = 'RESERVATION' AND reservationFormID IS NOT NULL) OR
            (receiptType = 'CHECKIN')
        )
);
GO


------------------
-- Thêm ràng buộc 
-----------------
-- Đảm bảo tên khách hàng và nhân viên không chứa ký tự đặc biệt
ALTER TABLE Customer
ADD CONSTRAINT CHK_Customer_FullName 
CHECK (fullName NOT LIKE '%[0-9!@#$%^&*()_+={}[\]|\\:;"<>,.?/~`]%');
GO

ALTER TABLE Employee
ADD CONSTRAINT CHK_Employee_FullName 
CHECK (fullName NOT LIKE '%[0-9!@#$%^&*()_+={}[\]|\\:;"<>,.?/~`]%');
GO

-- Thêm ràng buộc validate sdt,  email trong bảng Employee
ALTER TABLE Employee
ADD CONSTRAINT CHK_Employee_PhoneNumber 
CHECK (LEN(phoneNumber) = 10 AND ISNUMERIC(phoneNumber) = 1 AND LEFT(phoneNumber, 1) = '0');
GO

ALTER TABLE Employee
ADD CONSTRAINT CHK_Employee_Email 
CHECK (email LIKE '%@%.%' AND CHARINDEX('@', email) > 1);
GO

-- Thêm ràng buộc validate sdt, email trong bảng Customer
ALTER TABLE Customer
ADD CONSTRAINT CHK_Customer_PhoneNumber 
CHECK (LEN(phoneNumber) = 10 AND ISNUMERIC(phoneNumber) = 1 AND LEFT(phoneNumber, 1) = '0');
GO

ALTER TABLE Customer
ADD CONSTRAINT CHK_Customer_Email 
CHECK (email IS NULL OR (email LIKE '%@%.%' AND CHARINDEX('@', email) > 1));
GO

-- Thêm ràng buộc validate idCardNumber trong bảng Employee
ALTER TABLE Employee
ADD CONSTRAINT CHK_Employee_IDCardNumber 
CHECK (ISNUMERIC(idCardNumber) = 1 AND LEN(idCardNumber) = 12);
GO

-- Thêm ràng buộc validate idCardNumber trong bảng Customer
ALTER TABLE Customer
ADD CONSTRAINT CHK_Customer_IDCardNumber 
CHECK (ISNUMERIC(idCardNumber) = 1 AND LEN(idCardNumber) = 12);
GO

-- Đảm bảo dateOfCreation không vượt quá ngày hiện tại
ALTER TABLE Room
ADD CONSTRAINT CHK_Room_DateOfCreation 
CHECK (dateOfCreation <= GETDATE());
GO

-- Đảm bảo thời gian check-out không quá sớm sau check-in (ít nhất 1 giờ)
ALTER TABLE ReservationForm
ADD CONSTRAINT CHK_ReservationForm_MinStayDuration 
CHECK (DATEDIFF(HOUR, checkInDate, checkOutDate) >= 1);
GO

--Update check dob --cho employee  Thêm kiểm tra năm sinh (tuổi từ 18-65)
ALTER TABLE Employee
ADD CONSTRAINT CHK_Employee_Age
CHECK (DATEDIFF(YEAR, dob, GETDATE()) BETWEEN 18 AND 65);
GO

-- test 
UPDATE Employee
SET dob = '1955-01-01'
WHERE employeeID = 'EMP-000001';
GO
-- ===================================================================================
-- 2. TRIGGER - PROCEDURE - FUNCTION
-- ===================================================================================

--------------------------------
-- FUNCION TẠO ID TỰ ĐỘNG
--------------------------------
CREATE OR ALTER FUNCTION dbo.fn_GenerateID
(
    @prefix NVARCHAR(10),
    @tableName NVARCHAR(128),
    @idColumnName NVARCHAR(128),
    @padLength INT = 6
)
RETURNS NVARCHAR(50)
AS
BEGIN
    DECLARE @result NVARCHAR(50);
    DECLARE @maxID INT;
    
    -- Sử dụng CASE để xử lý từng bảng cụ thể, lấy ID lớn nhất
    SELECT @maxID = CASE
        WHEN @tableName = 'Employee' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(employeeID, LEN(@prefix) + 1, @padLength) AS INT))    
                    FROM Employee WHERE employeeID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'Customer' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(customerID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM Customer WHERE customerID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'Room' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(roomID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM Room WHERE roomID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'RoomCategory' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(roomCategoryID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM RoomCategory WHERE roomCategoryID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'ReservationForm' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(reservationFormID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM ReservationForm WHERE reservationFormID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'HistoryCheckin' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(historyCheckInID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM HistoryCheckin WHERE historyCheckInID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'HistoryCheckOut' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(historyCheckOutID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM HistoryCheckOut WHERE historyCheckOutID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'RoomChangeHistory' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(roomChangeHistoryID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM RoomChangeHistory WHERE roomChangeHistoryID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'RoomUsageService' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(roomUsageServiceId, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM RoomUsageService WHERE roomUsageServiceId LIKE @prefix + '%'), 0)
        WHEN @tableName = 'Invoice' THEN 
            ISNULL((SELECT MAX(CAST(SUBSTRING(invoiceID, LEN(@prefix) + 1, @padLength) AS INT)) 
                    FROM Invoice WHERE invoiceID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'ServiceCategory' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(serviceCategoryID, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM ServiceCategory WHERE serviceCategoryID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'HotelService' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(hotelServiceId, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM HotelService WHERE hotelServiceId LIKE @prefix + '%'), 0)
        WHEN @tableName = 'Pricing' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(pricingID, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM Pricing WHERE pricingID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'User' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(userID, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM [User] WHERE userID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'ConfirmationReceipt' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(receiptID, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM ConfirmationReceipt WHERE receiptID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'RoomTask' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(roomTaskID, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM RoomTask WHERE roomTaskID LIKE @prefix + '%'), 0)
        WHEN @tableName = 'RoomTaskHistory' THEN
            ISNULL((SELECT MAX(CAST(SUBSTRING(roomTaskHistoryID, LEN(@prefix) + 1, @padLength) AS INT))
                    FROM RoomTaskHistory WHERE roomTaskHistoryID LIKE @prefix + '%'), 0)
        ELSE 0
    END;
    
    -- Tạo ID mới dựa trên ID lớn nhất + 1
    SET @result = @prefix + RIGHT(REPLICATE('0', @padLength) + CAST(@maxID + 1 AS NVARCHAR(50)), @padLength);
    
    RETURN @result;
END;
GO

--=================
--== Trigger kiểm tra thông tin nhân viên trùng lặp
CREATE OR ALTER TRIGGER TR_Employee_CheckDuplicate
ON Employee
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra trùng SĐT
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Employee e ON i.phoneNumber = e.phoneNumber
    )
    BEGIN
        RAISERROR(N'Số điện thoại đã tồn tại.', 16, 1);
        RETURN;
    END

    -- Kiểm tra trùng CCCD
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Employee e ON i.idCardNumber = e.idCardNumber
    )
    BEGIN
        RAISERROR(N'Số CCCD đã tồn tại.', 16, 1);
        RETURN;
    END

    -- Kiểm tra trùng Email (nếu muốn)
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Employee e ON i.email = e.email
    )
    BEGIN
        RAISERROR(N'Email đã tồn tại.', 16, 1);
        RETURN;
    END

    -- Nếu hợp lệ, thực hiện INSERT
    INSERT INTO Employee (employeeID, fullName, phoneNumber, email, address, gender, idCardNumber, dob, position, isActivate)
    SELECT employeeID, fullName, phoneNumber, email, address, gender, idCardNumber, dob, position, isActivate
    FROM inserted;
END;
GO

--==============================
--== Trigger kiểm tra thông tin nhân viên trùng lặp khi UPDATE
CREATE OR ALTER TRIGGER TR_Employee_CheckDuplicate_Update
ON Employee
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra trùng SĐT (trừ chính bản ghi đang update)
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Employee e ON i.phoneNumber = e.phoneNumber
        WHERE e.employeeID <> i.employeeID
    )
    BEGIN
        RAISERROR(N'Số điện thoại đã tồn tại.', 16, 1);
        RETURN;
    END

    -- Kiểm tra trùng CCCD
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Employee e ON i.idCardNumber = e.idCardNumber
        WHERE e.employeeID <> i.employeeID
    )
    BEGIN
        RAISERROR(N'Số CCCD đã tồn tại.', 16, 1);
        RETURN;
    END

    -- Kiểm tra trùng Email (nếu muốn)
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Employee e ON i.email = e.email
        WHERE e.employeeID <> i.employeeID
    )
    BEGIN
        RAISERROR(N'Email đã tồn tại.', 16, 1);
        RETURN;
    END

    -- Nếu hợp lệ, thực hiện UPDATE
    UPDATE Employee
    SET fullName = i.fullName,
        phoneNumber = i.phoneNumber,
        email = i.email,
        address = i.address,
        gender = i.gender,
        idCardNumber = i.idCardNumber,
        dob = i.dob,
        position = i.position,
        isActivate = i.isActivate
    FROM inserted i
    WHERE Employee.employeeID = i.employeeID;
END;
GO

--==sp thêm employee
CREATE OR ALTER PROCEDURE sp_InsertEmployee
    @employeeID NVARCHAR(15),
    @fullName NVARCHAR(50),
    @phoneNumber NVARCHAR(10),
    @email NVARCHAR(50),
    @address NVARCHAR(100),
    @gender NVARCHAR(6),
    @idCardNumber NVARCHAR(12),
    @dob DATE,
    @position NVARCHAR(20)
AS
BEGIN
    INSERT INTO Employee (employeeID, fullName, phoneNumber, email, address, gender, idCardNumber, dob, position, isActivate)
    VALUES (@employeeID, @fullName, @phoneNumber, @email, @address, @gender, @idCardNumber, @dob, @position, 'ACTIVATE');
END;
GO

--== sp sửa employee
CREATE OR ALTER PROCEDURE sp_UpdateEmployee
    @employeeID NVARCHAR(15),
    @fullName NVARCHAR(50),
    @phoneNumber NVARCHAR(10),
    @email NVARCHAR(50),
    @address NVARCHAR(100),
    @gender NVARCHAR(6),
    @idCardNumber NVARCHAR(12),
    @dob DATE,
    @position NVARCHAR(20),
    @isActivate NVARCHAR(10)
AS
BEGIN
    UPDATE Employee
    SET fullName = @fullName,
        phoneNumber = @phoneNumber,
        email = @email,
        address = @address,
        gender = @gender,
        idCardNumber = @idCardNumber,
        dob = @dob,
        position = @position,
        isActivate = @isActivate
    WHERE employeeID = @employeeID;
END;
GO

-- SELECT dbo.fn_GenerateID('EMP-', 'Employee', 'employeeID', 6);
-- GO

CREATE OR ALTER PROCEDURE sp_CreateReservation
    @checkInDate DATETIME,
    @checkOutDate DATETIME,
    @roomID NVARCHAR(15),
    @customerID NVARCHAR(15),
    @employeeID NVARCHAR(15),
    @roomBookingDeposit FLOAT,
    @priceUnit NVARCHAR(15),
    @unitPrice MONEY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra các giá trị đầu vào
        IF @checkInDate IS NULL OR @checkOutDate IS NULL OR @roomID IS NULL OR @customerID IS NULL
        BEGIN
            RAISERROR('Thông tin đặt phòng không được để trống.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra thời gian đặt phòng hợp lệ
        IF @checkInDate <= GETDATE()
        BEGIN
            RAISERROR('Thời gian check-in phải sau thời điểm hiện tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        IF @checkOutDate <= @checkInDate
        BEGIN
            RAISERROR('Thời gian check-out phải sau thời gian check-in.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra phòng có tồn tại và không ở trạng thái khóa đặt phòng.
        -- Cho phép đặt lịch tương lai khi phòng đang ON_USE/RESERVED nếu không trùng lịch.
        IF NOT EXISTS (
            SELECT 1
            FROM Room
            WHERE roomID = @roomID
              AND isActivate = 'ACTIVATE'
              AND roomStatus NOT IN ('UNAVAILABLE', 'OVERDUE', 'MAINTENANCE', 'OUT_OF_SERVICE')
        )
        BEGIN
            RAISERROR('Phòng không tồn tại hoặc không khả dụng.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra khách hàng có tồn tại
        IF NOT EXISTS (SELECT 1 FROM Customer WHERE customerID = @customerID AND isActivate = 'ACTIVATE')
        BEGIN
            RAISERROR('Khách hàng không tồn tại hoặc không hoạt động.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra nhân viên có tồn tại
        IF NOT EXISTS (SELECT 1 FROM Employee WHERE employeeID = @employeeID AND isActivate = 'ACTIVATE')
        BEGIN
            RAISERROR('Nhân viên không tồn tại hoặc không hoạt động.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra tiền đặt cọc hợp lệ
        IF @roomBookingDeposit < 0
        BEGIN
            RAISERROR('Tiền đặt cọc không thể âm.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra hình thức thuê hợp lệ
        IF @priceUnit NOT IN ('DAY', 'HOUR')
        BEGIN
            RAISERROR('Hình thức thuê phải là DAY hoặc HOUR.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đơn giá hợp lệ
        IF @unitPrice <= 0
        BEGIN
            RAISERROR('Đơn giá phải lớn hơn 0.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra trùng lịch đặt phòng
        IF EXISTS (
            SELECT 1
            FROM ReservationForm rf
            OUTER APPLY (
                SELECT MAX(ho.checkOutDate) AS checkOutDateActual
                FROM HistoryCheckOut ho
                WHERE ho.reservationFormID = rf.reservationFormID
            ) ho
            WHERE rf.roomID = @roomID
            AND rf.isActivate = 'ACTIVATE'
            AND (
                    (@checkInDate < ISNULL(ho.checkOutDateActual, rf.checkOutDate))
                    AND
                    (@checkOutDate > rf.checkInDate)
            )
        )
        BEGIN
            RAISERROR(N'Phòng đã được đặt trong khoảng thời gian này.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Tạo mã đặt phòng mới
        DECLARE @reservationFormID NVARCHAR(15) = dbo.fn_GenerateID('RF-', 'ReservationForm', 'reservationFormID', 6);
        
        -- Thêm phiếu đặt phòng mới
        INSERT INTO ReservationForm (
            reservationFormID, reservationDate, checkInDate, checkOutDate,
            employeeID, roomID, customerID, roomBookingDeposit, priceUnit, unitPrice
        )
        VALUES (
            @reservationFormID, GETDATE(), @checkInDate, @checkOutDate,
            @employeeID, @roomID, @customerID, @roomBookingDeposit, @priceUnit, @unitPrice
        );

        -- KHÔNG CẬP NHẬT Room.status = 'RESERVED' NGAY NỮA
        -- Trigger TR_Room_AutoReserve sẽ tự động cập nhật khi còn 5 giờ đến check-in
        
        -- Trả về thông tin đặt phòng
        SELECT 
            rf.reservationFormID,
            rf.reservationDate,
            rf.checkInDate,
            rf.checkOutDate,
            r.roomID,
            rc.roomCategoryName,
            c.fullName AS CustomerName,
            e.fullName AS EmployeeName,
            rf.roomBookingDeposit,
            DATEDIFF(DAY, rf.checkInDate, rf.checkOutDate) AS DaysBooked
        FROM 
            ReservationForm rf
            JOIN Room r ON rf.roomID = r.roomID
            JOIN RoomCategory rc ON r.roomCategoryID = rc.roomCategoryID
            JOIN Customer c ON rf.customerID = c.customerID
            JOIN Employee e ON rf.employeeID = e.employeeID
        WHERE 
            rf.reservationFormID = @reservationFormID;
            
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Hiển thị thông tin lỗi
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO

-- Cập nhật lịch/phòng cho phiếu đặt phòng từ màn hình calendar.
-- Chỉ cho phép điều chỉnh phiếu còn hoạt động, chưa check-in/check-out và thời gian mới không trùng lịch.
-- Khi đổi sang loại phòng khác, procedure nhận snapshot giá đã được server xác nhận để tránh checkout sai đơn giá.
CREATE OR ALTER PROCEDURE sp_UpdateReservationSchedule
    @reservationFormID NVARCHAR(15),
    @roomID NVARCHAR(15),
    @checkInDate DATETIME,
    @checkOutDate DATETIME,
    @employeeID NVARCHAR(15),
    @priceUnit NVARCHAR(15),
    @unitPrice MONEY,
    @roomBookingDeposit FLOAT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @reservationFormID IS NULL OR @roomID IS NULL OR @checkInDate IS NULL OR @checkOutDate IS NULL OR @employeeID IS NULL
           OR @priceUnit IS NULL OR @unitPrice IS NULL OR @roomBookingDeposit IS NULL
        BEGIN
            RAISERROR(N'Thông tin cập nhật lịch đặt phòng không được để trống.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF @checkInDate <= GETDATE()
        BEGIN
            RAISERROR(N'Thời gian nhận phòng mới phải sau thời điểm hiện tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF @checkOutDate <= @checkInDate
        BEGIN
            RAISERROR(N'Thời gian trả phòng mới phải sau thời gian nhận phòng.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF @priceUnit NOT IN ('DAY', 'HOUR')
        BEGIN
            RAISERROR(N'Đơn vị giá không hợp lệ. Chỉ chấp nhận DAY hoặc HOUR.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF @unitPrice <= 0
        BEGIN
            RAISERROR(N'Đơn giá phòng phải lớn hơn 0.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF @roomBookingDeposit < 0
        BEGIN
            RAISERROR(N'Tiền đặt cọc không được âm.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF NOT EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID AND isActivate = 'ACTIVATE')
        BEGIN
            RAISERROR(N'Phiếu đặt phòng không tồn tại hoặc đã bị hủy.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
           OR EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Không thể điều chỉnh phiếu đã check-in hoặc check-out.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        DECLARE @newRoomCategoryID NVARCHAR(15);
        SELECT @newRoomCategoryID = roomCategoryID
        FROM Room
        WHERE roomID = @roomID
          AND isActivate = 'ACTIVATE'
          AND roomStatus NOT IN ('UNAVAILABLE', 'OVERDUE', 'MAINTENANCE', 'OUT_OF_SERVICE');

        IF @newRoomCategoryID IS NULL
        BEGIN
            RAISERROR(N'Phòng không tồn tại hoặc đang ở trạng thái không thể đặt.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM Pricing
            WHERE roomCategoryID = @newRoomCategoryID
              AND priceUnit = @priceUnit
              AND price = @unitPrice
              AND price > 0
        )
        BEGIN
            RAISERROR(N'Đơn giá xác nhận không khớp với bảng giá hiện tại của loại phòng mới.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF NOT EXISTS (SELECT 1 FROM Employee WHERE employeeID = @employeeID AND isActivate = 'ACTIVATE')
        BEGIN
            RAISERROR(N'Nhân viên không tồn tại hoặc không hoạt động.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (
            SELECT 1
            FROM ReservationForm rf
            OUTER APPLY (
                SELECT MAX(ho.checkOutDate) AS checkOutDateActual
                FROM HistoryCheckOut ho
                WHERE ho.reservationFormID = rf.reservationFormID
            ) ho
            WHERE rf.roomID = @roomID
              AND rf.reservationFormID <> @reservationFormID
              AND rf.isActivate = 'ACTIVATE'
              AND @checkInDate < ISNULL(ho.checkOutDateActual, rf.checkOutDate)
              AND @checkOutDate > rf.checkInDate
        )
        BEGIN
            RAISERROR(N'Phòng đã được đặt trong khoảng thời gian này.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (
            SELECT 1
            FROM RoomTask rt
            WHERE rt.roomID = @roomID
              AND rt.taskType = 'MAINTENANCE'
              AND rt.status IN ('PENDING', 'ASSIGNED', 'IN_PROGRESS')
        )
        BEGIN
            RAISERROR(N'Phòng đang có yêu cầu bảo trì mở, không thể điều chỉnh lịch đặt phòng.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        UPDATE ReservationForm
        SET roomID = @roomID,
            checkInDate = @checkInDate,
            checkOutDate = @checkOutDate,
            employeeID = @employeeID,
            priceUnit = @priceUnit,
            unitPrice = @unitPrice,
            roomBookingDeposit = @roomBookingDeposit
        WHERE reservationFormID = @reservationFormID;

        DECLARE @roomChangeHistoryID NVARCHAR(15) = dbo.fn_GenerateID('RCH-', 'RoomChangeHistory', 'roomChangeHistoryID', 6);

        INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
        VALUES (@roomChangeHistoryID, GETDATE(), @roomID, @reservationFormID, @employeeID);

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

-- Tạo procedure đơn giản để đặt phòng nhanh với thông tin cơ bảncó
CREATE OR ALTER PROCEDURE sp_QuickReservation
    @checkInDate DATETIME,
    @daysStay INT,
    @roomID NVARCHAR(15),
    @customerID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    -- Tính ngày check-out dựa trên số ngày ở
    DECLARE @checkOutDate DATETIME = DATEADD(DAY, @daysStay, @checkInDate);
    
    -- Tính tiền đặt cọc (VD: 30% giá phòng theo ngày)
    DECLARE @roomBookingDeposit FLOAT = 0;
    DECLARE @roomCategoryID NVARCHAR(15);
    
    SELECT @roomCategoryID = roomCategoryID FROM Room WHERE roomID = @roomID;
    
    SELECT @roomBookingDeposit = price * 0.3 * @daysStay
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'DAY';
    
    -- Gọi procedure đặt phòng chính
    EXEC sp_CreateReservation 
        @checkInDate = @checkInDate,
        @checkOutDate = @checkOutDate,
        @roomID = @roomID,
        @customerID = @customerID,
        @employeeID = @employeeID,
        @roomBookingDeposit = @roomBookingDeposit;
END;
GO

-------------------------------------
-- STORED PROCEDURE TẠO PHIẾU XÁC NHẬN ĐẶT PHÒNG/NHẬN PHÒNG
-------------------------------------
CREATE OR ALTER PROCEDURE sp_CreateConfirmationReceipt
    @receiptType NVARCHAR(15), -- 'RESERVATION' hoặc 'CHECKIN'
    @reservationFormID NVARCHAR(15),
    @invoiceID NVARCHAR(15) = NULL,
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate input
        IF @receiptType NOT IN ('RESERVATION', 'CHECKIN')
        BEGIN
            RAISERROR('Receipt type phải là RESERVATION hoặc CHECKIN', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Lấy thông tin từ ReservationForm
        DECLARE @customerID NVARCHAR(15);
        DECLARE @roomID NVARCHAR(15);
        DECLARE @checkInDate DATETIME;
        DECLARE @checkOutDate DATETIME;
        DECLARE @priceUnit NVARCHAR(15);
        DECLARE @unitPrice MONEY;
        DECLARE @deposit MONEY;
        
        SELECT 
            @customerID = rf.customerID,
            @roomID = rf.roomID,
            @checkInDate = rf.checkInDate,
            @checkOutDate = rf.checkOutDate,
            @priceUnit = rf.priceUnit,
            @unitPrice = rf.unitPrice,
            @deposit = rf.roomBookingDeposit
        FROM ReservationForm rf
        WHERE rf.reservationFormID = @reservationFormID;
        
        IF @customerID IS NULL
        BEGIN
            RAISERROR('Không tìm thấy reservation form', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Lấy thông tin khách hàng
        DECLARE @customerName NVARCHAR(50);
        DECLARE @customerPhone NVARCHAR(10);
        DECLARE @customerEmail NVARCHAR(50);
        
        SELECT 
            @customerName = c.fullName,
            @customerPhone = c.phoneNumber,
            @customerEmail = c.email
        FROM Customer c
        WHERE c.customerID = @customerID;
        
        -- Lấy thông tin phòng
        DECLARE @roomCategoryName NVARCHAR(50);
        
        SELECT 
            @roomCategoryName = rc.roomCategoryName
        FROM Room r
        JOIN RoomCategory rc ON r.roomCategoryID = rc.roomCategoryID
        WHERE r.roomID = @roomID;
        
        -- Lấy tên nhân viên
        DECLARE @employeeName NVARCHAR(50);
        SELECT @employeeName = fullName FROM Employee WHERE employeeID = @employeeID;
        
        -- Lấy tổng tiền nếu là CHECK-IN và có invoice
        DECLARE @totalAmount MONEY = NULL;
        IF @receiptType = 'CHECKIN' AND @invoiceID IS NOT NULL
        BEGIN
            SELECT @totalAmount = totalAmount FROM Invoice WHERE invoiceID = @invoiceID;
        END
        
        -- Tạo mã phiếu xác nhận
        DECLARE @receiptID NVARCHAR(15) = dbo.fn_GenerateID('CR-', 'ConfirmationReceipt', 'receiptID', 6);
        
        -- Tạo QR code (có thể là URL hoặc JSON)
        DECLARE @qrCode NVARCHAR(200) = 'RECEIPT:' + @receiptID + '|ROOM:' + @roomID + '|DATE:' + CONVERT(NVARCHAR, GETDATE(), 120);
        
        -- Insert phiếu xác nhận
        INSERT INTO ConfirmationReceipt (
            receiptID, receiptType, issueDate,
            reservationFormID, invoiceID,
            customerName, customerPhone, customerEmail,
            roomID, roomCategoryName,
            checkInDate, checkOutDate,
            priceUnit, unitPrice, deposit, totalAmount,
            employeeName, qrCode
        )
        VALUES (
            @receiptID, @receiptType, GETDATE(),
            @reservationFormID, @invoiceID,
            @customerName, @customerPhone, @customerEmail,
            @roomID, @roomCategoryName,
            @checkInDate, @checkOutDate,
            @priceUnit, @unitPrice, @deposit, @totalAmount,
            @employeeName, @qrCode
        );
        
        -- Trả về thông tin phiếu
        SELECT 
            receiptID, receiptType, issueDate, reservationFormID,
            customerName, 
            ISNULL(invoiceID, '') AS invoiceID,  -- Fix: Đảm bảo invoiceID luôn có giá trị
            customerPhone, customerEmail,
            roomID, roomCategoryName,
            checkInDate, checkOutDate,
            priceUnit, unitPrice, deposit, totalAmount,
            employeeName, qrCode, notes
        FROM ConfirmationReceipt
        WHERE receiptID = @receiptID;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-------------------------------------
-- TRIGGER TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI PHÒNG KHI SẮP ĐẾN GIỜ CHECK-IN
-------------------------------------

-- Stored procedure để cập nhật trạng thái phòng RESERVED
-- Gọi định kỳ (mỗi 30 phút) hoặc khi cần kiểm tra
CREATE OR ALTER PROCEDURE sp_UpdateRoomStatusToReserved
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Cập nhật tất cả phòng có reservation còn 5 giờ nữa đến check-in
    UPDATE Room
    SET roomStatus = 'RESERVED'
    WHERE roomID IN (
        SELECT DISTINCT rf.roomID
        FROM ReservationForm rf
        LEFT JOIN HistoryCheckin hc ON rf.reservationFormID = hc.reservationFormID
        WHERE rf.isActivate = 'ACTIVATE'
        AND hc.historyCheckinID IS NULL  -- Chưa check-in
        AND rf.checkInDate <= DATEADD(HOUR, 5, GETDATE())  -- Còn <= 5 giờ
        AND rf.checkInDate > GETDATE()  -- Chưa quá giờ check-in
    )
    AND roomStatus = 'AVAILABLE';  -- Chỉ update phòng đang AVAILABLE
    
    -- Trả về số lượng phòng đã cập nhật
    SELECT @@ROWCOUNT AS RoomsUpdated;
END;
GO

-- View để theo dõi phòng sắp được reserved
CREATE OR ALTER VIEW vw_RoomsNearCheckIn
AS
SELECT 
    r.roomID,
    r.roomStatus,
    rf.reservationFormID,
    rf.checkInDate,
    DATEDIFF(MINUTE, GETDATE(), rf.checkInDate) AS MinutesUntilCheckIn,
    c.fullName AS CustomerName,
    c.phoneNumber AS CustomerPhone
FROM Room r
JOIN ReservationForm rf ON r.roomID = rf.roomID
JOIN Customer c ON rf.customerID = c.customerID
LEFT JOIN HistoryCheckin hc ON rf.reservationFormID = hc.reservationFormID
WHERE rf.isActivate = 'ACTIVATE'
AND hc.historyCheckinID IS NULL  -- Chưa check-in
AND rf.checkInDate > GETDATE()  -- Chưa quá giờ
AND rf.checkInDate <= DATEADD(HOUR, 6, GETDATE());  -- Trong vòng 6 giờ tới
GO

-------------------------------------
-- trigger quản lý hóa đơn
-------------------------------------
CREATE OR ALTER TRIGGER TR_Invoice_ManageInsert
ON Invoice
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- ================================================================
    -- HỖ TRỢ LUỒNG THANH TOÁN MỚI:
    -- 1. CHECKOUT_THEN_PAY: Checkout trước → Tạo invoice (isPaid=0) → Thanh toán sau
    -- 2. PAY_THEN_CHECKOUT: Thanh toán trước (isPaid=1) → Checkout sau
    -- ================================================================

    DECLARE @reservationFormID NVARCHAR(15),
            @priceUnit NVARCHAR(15),
            @unitPrice MONEY,
            @roomCategoryID NVARCHAR(15),
            @roomBookingDeposit MONEY,
            @checkInDateExpected DATETIME,
            @checkInDateActual DATETIME,
            @checkOutDateExpected DATETIME,
            @checkOutDateActual DATETIME,
            @dayPrice MONEY,
            @hourPrice MONEY,
            @roomCharge DECIMAL(18,2),
            @earlyCheckinFee DECIMAL(18,2) = 0,
            @lateCheckoutFee DECIMAL(18,2) = 0,
            @checkoutType NVARCHAR(20),
            @isPaid BIT,
            @amountPaid DECIMAL(18,2) = 0;

    SELECT 
        @reservationFormID = i.reservationFormID,
        @priceUnit = rf.priceUnit,
        @unitPrice = rf.unitPrice,
        @roomBookingDeposit = rf.roomBookingDeposit,
        @roomCategoryID = r.roomCategoryID,
        @checkInDateExpected = rf.checkInDate,
        @checkOutDateExpected = rf.checkOutDate,
        @checkoutType = i.checkoutType,
        @isPaid = ISNULL(i.isPaid, 0),
        @amountPaid = ISNULL(i.amountPaid, 0)
    FROM inserted i
    JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
    JOIN Room r ON rf.roomID = r.roomID;

    SELECT @checkInDateActual = checkInDate
    FROM HistoryCheckin
    WHERE reservationFormID = @reservationFormID;

    SELECT @checkOutDateActual = checkOutDate
    FROM HistoryCheckOut
    WHERE reservationFormID = @reservationFormID;

    SELECT @dayPrice = price 
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'DAY';
    
    SELECT @hourPrice = price 
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'HOUR';
    
    IF @dayPrice IS NULL OR @dayPrice = 0 SET @dayPrice = @unitPrice;
    IF @hourPrice IS NULL OR @hourPrice = 0 SET @hourPrice = @unitPrice;

    -- ================================================================================
    -- LOGIC MỚI - ĐƠN GIẢN HÓA:
    -- Tính tiền trực tiếp từ check-in THỰC TẾ → checkout THỰC TẾ
    -- KHÔNG CÒN phí check-in sớm hay checkout muộn riêng biệt
    -- 
    -- CHECKOUT_THEN_PAY: Tính từ actualCheckIn → actualCheckOut
    -- PAY_THEN_CHECKOUT: 
    --   - Lần đầu: Tính từ actualCheckIn → expectedCheckOut
    --   - Sau checkout: Tính lại từ actualCheckIn → actualCheckOut
    -- ================================================================================
    DECLARE @bookingMinutes INT;
    DECLARE @timeUnits INT;
    DECLARE @effectiveCheckIn DATETIME = ISNULL(@checkInDateActual, @checkInDateExpected);
    DECLARE @effectiveCheckOut DATETIME;
    
    -- Xác định thời gian checkout dựa trên loại thanh toán
    IF @checkoutType = 'PAY_THEN_CHECKOUT' AND @checkOutDateActual IS NULL
    BEGIN
        -- Thanh toán trước: Tính theo checkout DỰ KIẾN
        SET @effectiveCheckOut = @checkOutDateExpected;
    END
    ELSE
    BEGIN
        -- Checkout rồi thanh toán HOẶC đã có checkout thực tế: Tính theo THỰC TẾ
        SET @effectiveCheckOut = ISNULL(@checkOutDateActual, @checkOutDateExpected);
    END
    
    -- Tính số phút THỰC TẾ khách ở (từ actual check-in → checkout)
    SET @bookingMinutes = DATEDIFF(MINUTE, @effectiveCheckIn, @effectiveCheckOut);
    
    -- Tính tiền phòng trực tiếp (KHÔNG CÒN PHÍ PHỤ THU)
    IF @priceUnit = 'DAY'
    BEGIN
        SET @timeUnits = CASE WHEN @bookingMinutes <= 0 THEN 1 ELSE CEILING(@bookingMinutes / 1440.0) END;
        SET @roomCharge = @timeUnits * @unitPrice;
    END
    ELSE IF @priceUnit = 'HOUR'
    BEGIN
        SET @timeUnits = CASE WHEN @bookingMinutes <= 0 THEN 1 ELSE CEILING(@bookingMinutes / 60.0) END;
        SET @roomCharge = @timeUnits * @unitPrice;
    END

    -- KHÔNG CÒN TÍNH PHÍ SỚM/MUỘN - ĐÃ BAO GỒM TRONG ROOMCHARGE
    SET @earlyCheckinFee = 0;
    SET @lateCheckoutFee = 0;

    -- TÍNH PHÍ DỊCH VỤ
    DECLARE @servicesCharge DECIMAL(18,2);
    SELECT @servicesCharge = ISNULL(SUM(quantity * unitPrice), 0)
    FROM RoomUsageService
    WHERE reservationFormID = @reservationFormID;

    INSERT INTO Invoice (
        invoiceID, invoiceDate, roomCharge, servicesCharge, roomBookingDeposit, reservationFormID, paymentDate, paymentMethod, checkoutType, isPaid, amountPaid
    )
    SELECT 
        COALESCE(i.invoiceID, dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6)),
        COALESCE(i.invoiceDate, GETDATE()),
        ROUND(@roomCharge, 0),
        ROUND(@servicesCharge, 0),
        ROUND(@roomBookingDeposit, 0),
        i.reservationFormID,
        CASE WHEN @isPaid = 1 THEN COALESCE(i.paymentDate, GETDATE()) ELSE NULL END,
        CASE WHEN @isPaid = 1 THEN COALESCE(i.paymentMethod, 'CASH') ELSE NULL END,
        i.checkoutType,
        @isPaid,
        @amountPaid
    FROM inserted i;
END;
GO

-- =====================================================================
-- TRIGGER TR_Invoice_ManageUpdate
-- =====================================================================
CREATE OR ALTER TRIGGER TR_Invoice_ManageUpdate
ON Invoice
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- ================================================================
    -- HỖ TRỢ LUỒNG THANH TOÁN MỚI:
    -- 1. CHECKOUT_THEN_PAY: Checkout trước → Tạo invoice (isPaid=0) → Thanh toán sau
    -- 2. PAY_THEN_CHECKOUT: Thanh toán trước (isPaid=1) → Checkout sau
    -- ================================================================

    DECLARE @reservationFormID NVARCHAR(15),
            @priceUnit NVARCHAR(15),
            @unitPrice MONEY,
            @roomCategoryID NVARCHAR(15),
            @roomBookingDeposit MONEY,
            @checkInDateExpected DATETIME,
            @checkInDateActual DATETIME,
            @checkOutDateExpected DATETIME,
            @checkOutDateActual DATETIME,
            @dayPrice MONEY,
            @hourPrice MONEY,
            @roomCharge DECIMAL(18,2),
            @earlyCheckinFee DECIMAL(18,2) = 0,
            @lateCheckoutFee DECIMAL(18,2) = 0,
            @checkoutType NVARCHAR(20),
            @isPaid BIT,
            @amountPaid DECIMAL(18,2);

    SELECT 
        @reservationFormID = i.reservationFormID,
        @priceUnit = rf.priceUnit,
        @unitPrice = rf.unitPrice,
        @roomCharge = i.roomCharge,
        @roomBookingDeposit = rf.roomBookingDeposit,
        @roomCategoryID = r.roomCategoryID,
        @checkInDateExpected = rf.checkInDate,
        @checkOutDateExpected = rf.checkOutDate,
        @checkoutType = i.checkoutType,
        @isPaid = ISNULL(i.isPaid, 0),
        @amountPaid = ISNULL(i.amountPaid, 0)
    FROM inserted i
    JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
    JOIN Room r ON rf.roomID = r.roomID;

    SELECT @checkInDateActual = checkInDate
    FROM HistoryCheckin
    WHERE reservationFormID = @reservationFormID;

    SELECT @checkOutDateActual = checkOutDate
    FROM HistoryCheckOut
    WHERE reservationFormID = @reservationFormID;

    SELECT @dayPrice = price 
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'DAY';
    
    SELECT @hourPrice = price 
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'HOUR';
    
    IF @dayPrice IS NULL OR @dayPrice = 0 SET @dayPrice = @unitPrice;
    IF @hourPrice IS NULL OR @hourPrice = 0 SET @hourPrice = @unitPrice;

    -- ================================================================================
    -- LOGIC MỚI - ĐƠN GIẢN HÓA:
    -- CHECKOUT_THEN_PAY: Tính từ checkInActual → checkOutActual
    -- PAY_THEN_CHECKOUT: 
    --   - Lần đầu: Tính từ checkInActual → checkOutExpected
    --   - Sau checkout: Tính lại từ checkInActual → checkOutActual
    -- ================================================================================
    -- ===========================[Thích thì bỏ cmt để tính lại, nhma thôi]====================================================
    -- DECLARE @bookingMinutes INT;
    -- DECLARE @timeUnits INT;
    -- DECLARE @effectiveCheckIn DATETIME = ISNULL(@checkInDateActual, @checkInDateExpected);
    -- DECLARE @effectiveCheckOut DATETIME;
    
    -- -- Xác định thời gian checkout dựa trên loại thanh toán
    -- IF @checkoutType = 'PAY_THEN_CHECKOUT' AND @checkOutDateActual IS NULL
    -- BEGIN
    --     -- Thanh toán trước: Tính theo checkout DỰ KIẾN
    --     SET @effectiveCheckOut = @checkOutDateExpected;
    -- END
    -- ELSE
    -- BEGIN
    --     -- Checkout rồi thanh toán HOẶC đã có checkout thực tế: Tính theo THỰC TẾ
    --     SET @effectiveCheckOut = ISNULL(@checkOutDateActual, @checkOutDateExpected);
    -- END
    
    -- -- Tính số phút thực tế khách ở
    -- SET @bookingMinutes = DATEDIFF(MINUTE, @effectiveCheckIn, @effectiveCheckOut);
    
    -- IF @priceUnit = 'DAY'
    -- BEGIN
    --     SET @timeUnits = CASE WHEN @bookingMinutes <= 0 THEN 1 ELSE CEILING(@bookingMinutes / 1440.0) END;
    --     SET @roomCharge = @timeUnits * @unitPrice;
    -- END
    -- ELSE IF @priceUnit = 'HOUR'
    -- BEGIN
    --     SET @timeUnits = CASE WHEN @bookingMinutes <= 0 THEN 1 ELSE CEILING(@bookingMinutes / 60.0) END;
    --     SET @roomCharge = @timeUnits * @unitPrice;
    -- END

    -- -- Tính tiền còn lại nếu khách check-out muộn
    -- IF @checkOutDateActual > @checkOutDateExpected
    -- BEGIN
    --     DECLARE @extraMinutes INT = DATEDIFF(MINUTE, @checkOutDateExpected, @checkOutDateActual);
    --     DECLARE @extraCharge DECIMAL(18,2);
    
    --     IF @priceUnit = 'DAY'
    --     BEGIN
    --         SET @extraCharge = CEILING(@extraMinutes / 1440.0) * @unitPrice;
    --     END
    --     ELSE IF @priceUnit = 'HOUR'
    --     BEGIN
    --         SET @extraCharge = CEILING(@extraMinutes / 60.0) * @unitPrice;
    --     END
    
    --     -- Cộng thêm tiền vào roomCharge
    --     SET @roomCharge = @roomCharge + @extraCharge;
    -- END

    -- TÍNH PHÍ DỊCH VỤ
    DECLARE @servicesCharge DECIMAL(18,2);
    SELECT @servicesCharge = ISNULL(SUM(quantity * unitPrice), 0)
    FROM RoomUsageService
    WHERE reservationFormID = @reservationFormID;
    --====[ Thích thì bỏ cmt để update nó tính lại ]

    -- UPDATE 
    UPDATE Invoice
    SET invoiceDate = COALESCE(i.invoiceDate, GETDATE()),
        roomCharge = ROUND(@roomCharge, 0),
        servicesCharge = ROUND(@servicesCharge, 0),
        roomBookingDeposit = ROUND(@roomBookingDeposit, 0),
        isPaid = @isPaid,
        paymentDate = CASE WHEN @isPaid = 1 THEN COALESCE(i.paymentDate, GETDATE()) ELSE NULL END,
        paymentMethod = CASE WHEN @isPaid = 1 THEN COALESCE(i.paymentMethod, 'CASH') ELSE NULL END,
        checkoutType = i.checkoutType,
        amountPaid = @amountPaid
    FROM Invoice inv
    JOIN inserted i ON inv.invoiceID = i.invoiceID;
END;
GO


--------------------------------------------------------
-- TRIGGER KIỂM TRA THỜI GIAN CHECK-IN
--------------------------------------------------------
CREATE OR ALTER TRIGGER TR_HistoryCheckin_CheckTime
ON HistoryCheckin
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Kiểm tra thời gian check-in có sớm hơn thời gian đã đăng ký không
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
        WHERE i.checkInDate < rf.checkInDate
    )
    BEGIN
        RAISERROR(N'Chưa đến giờ check-in. Vui lòng đợi đến thời gian đã đăng ký.', 16, 1);
        RETURN;
    END
    
    -- Nếu hợp lệ, thực hiện INSERT
    INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
    SELECT historyCheckInID, checkInDate, reservationFormID, employeeID
    FROM inserted;
END;
GO

DISABLE TRIGGER TR_HistoryCheckin_CheckTime ON HistoryCheckin;
GO
--------------------------------------------------------
-- Trigger để cập nhật trạng thái phòng khi có checkin
-------------------------------------------------------
CREATE OR ALTER TRIGGER TR_UpdateRoomStatus_OnCheckin
ON HistoryCheckin
AFTER INSERT
AS
BEGIN
    UPDATE Room
    SET roomStatus = 'ON_USE'
    FROM inserted i
    JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
    WHERE Room.roomID = rf.roomID;
END;
GO

--Trigger để cập nhật trạng thái phòng khi có checkout
CREATE OR ALTER TRIGGER TR_UpdateRoomStatus_OnCheckOut
ON HistoryCheckOut
AFTER INSERT
AS
BEGIN
    UPDATE Room
    SET roomStatus = CASE
        WHEN EXISTS (
            SELECT 1
            FROM ReservationForm nextRf
            LEFT JOIN HistoryCheckin nextHci ON nextRf.reservationFormID = nextHci.reservationFormID
            WHERE nextRf.roomID = Room.roomID
              AND nextRf.isActivate = 'ACTIVATE'
              AND nextHci.historyCheckinID IS NULL
              AND nextRf.checkInDate > GETDATE()
              AND nextRf.checkInDate <= DATEADD(HOUR, 5, GETDATE())
        ) THEN 'RESERVED'
        ELSE 'AVAILABLE'
    END
    FROM inserted i
    JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
    WHERE Room.roomID = rf.roomID;
END;
GO


--trigger để kiểm tra trạng thái phòng khi thêm đặt phòng trong ReservationForm
CREATE OR ALTER TRIGGER TR_ReservationForm_RoomStatusCheck
ON ReservationForm
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Kiểm tra trạng thái phòng
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Room r ON i.roomID = r.roomID
        WHERE r.roomStatus IN ('UNAVAILABLE', 'OVERDUE', 'MAINTENANCE', 'OUT_OF_SERVICE') 
              OR r.isActivate = 'DEACTIVATE'
    )
    BEGIN
        RAISERROR ('Phòng không khả dụng để đặt.', 16, 1);
        RETURN;
    END
    
    -- Kiểm tra trùng lịch đặt phòng
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ReservationForm rf ON i.roomID = rf.roomID
        OUTER APPLY (
            SELECT MAX(ho.checkOutDate) AS checkOutDateActual
            FROM HistoryCheckOut ho
            WHERE ho.reservationFormID = rf.reservationFormID
        ) ho
        WHERE rf.isActivate = 'ACTIVATE'
          AND rf.reservationFormID <> i.reservationFormID 
          AND (
              (i.checkInDate < ISNULL(ho.checkOutDateActual, rf.checkOutDate))
              AND
              (i.checkOutDate > rf.checkInDate)
          )
    )
    BEGIN
        RAISERROR ('Phòng đã được đặt trong khoảng thời gian này.', 16, 1);
        RETURN;
    END
    

    
    INSERT INTO ReservationForm(
        reservationFormID, reservationDate, checkInDate, checkOutDate,
        employeeID, roomID, customerID, roomBookingDeposit, priceUnit, unitPrice, isActivate
    )
    SELECT 
        reservationFormID, reservationDate, checkInDate, checkOutDate,
        employeeID, roomID, customerID, roomBookingDeposit, priceUnit, unitPrice, isActivate
    FROM inserted;

END;
GO

-- -- Trigger kiểm tra trạng thái đặt phòng khi thêm dịch vụ
CREATE OR ALTER TRIGGER TR_RoomUsageService_CheckReservationStatus
ON RoomUsageService
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Kiểm tra trường hợp đặt phòng đã bị hủy
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
        WHERE rf.isActivate = 'DEACTIVATE'
    )
    BEGIN
        DECLARE @InvalidReservations TABLE (reservationFormID NVARCHAR(15));
        
        INSERT INTO @InvalidReservations (reservationFormID)
        SELECT DISTINCT i.reservationFormID
        FROM inserted i
        JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
        WHERE rf.isActivate = 'DEACTIVATE';
        
        DECLARE @ErrorMsg NVARCHAR(MAX) = N'Không thể thêm dịch vụ cho (các) đặt phòng đã hủy sau:';
        
        SELECT @ErrorMsg = @ErrorMsg + CHAR(13) + '- ' + reservationFormID
        FROM @InvalidReservations;
        
        RAISERROR(@ErrorMsg, 16, 1);
        RETURN;
    END
    
    -- Kiểm tra trường hợp đã check-out
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HistoryCheckOut ho ON i.reservationFormID = ho.reservationFormID
    )
    BEGIN
        DECLARE @CheckedOutReservations TABLE (reservationFormID NVARCHAR(15));
        
        INSERT INTO @CheckedOutReservations (reservationFormID)
        SELECT DISTINCT i.reservationFormID
        FROM inserted i
        JOIN HistoryCheckOut ho ON i.reservationFormID = ho.reservationFormID;
        
        DECLARE @CheckOutErrorMsg NVARCHAR(MAX) = N'Không thể thêm dịch vụ cho (các) đặt phòng đã check-out sau:';
        
        SELECT @CheckOutErrorMsg = @CheckOutErrorMsg + CHAR(13) + '- ' + reservationFormID
        FROM @CheckedOutReservations;
        
        RAISERROR(@CheckOutErrorMsg, 16, 1);
        RETURN;
    END
    
    -- Nếu tất cả đều hợp lệ, tiến hành thêm dịch vụ
    INSERT INTO RoomUsageService (
        roomUsageServiceId, quantity, unitPrice, dateAdded,
        hotelServiceId, reservationFormID, employeeID
    )
    SELECT
        i.roomUsageServiceId, i.quantity, i.unitPrice, i.dateAdded,
        i.hotelServiceId, i.reservationFormID, i.employeeID
    FROM inserted i;
END;
GO

--trigger để kiểm tra check-in trước khi check-out
CREATE TRIGGER TR_HistoryCheckOut_CheckInCheck
ON HistoryCheckOut
AFTER INSERT
AS
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HistoryCheckin hc ON i.reservationFormID = hc.reservationFormID
    )
    BEGIN
        RAISERROR ('Đặt phòng phải được check-in trước khi check-out.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Tạo trigger để giới hạn mỗi roomCategoryID chỉ có 2 bản ghi trong bảng Pricing
CREATE TRIGGER trg_LimitPricingForRoomCategory
ON Pricing
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT roomCategoryID
        FROM Pricing
        GROUP BY roomCategoryID
        HAVING COUNT(*) > 2
    )
    BEGIN
        RAISERROR(
            'Mỗi loại phòng chỉ được phép có 2 bản ghi trong Pricing (1 DAY và 1 HOUR)',
            16,
            1
        );
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Tạo procedure check-in
CREATE OR ALTER PROCEDURE sp_CheckinRoom
    @reservationFormID NVARCHAR(15),       -- Mã phiếu đặt phòng
    @historyCheckInID NVARCHAR(15),        -- Mã phiếu check-in
    @employeeID NVARCHAR(15)               -- Mã nhân viên thực hiện check-in
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu đặt phòng có tồn tại không
        IF NOT EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phiếu đặt phòng không tồn tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra phiếu đặt phòng có đang hoạt động không
        IF EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID AND isActivate = 'DEACTIVATE')
        BEGIN
            RAISERROR('Phiếu đặt phòng đã bị hủy.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra phòng đã được check-in chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phòng đã được check-in trước đó.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Lấy thông tin phòng và thời gian
        DECLARE @roomID NVARCHAR(15);
        DECLARE @checkInDate DATETIME;
        DECLARE @checkOutDate DATETIME;
        DECLARE @actualCheckInDate DATETIME = GETDATE(); -- Thời điểm thực hiện check-in
        
        SELECT 
            @roomID = rf.roomID,
            @checkInDate = rf.checkInDate,
            @checkOutDate = rf.checkOutDate
        FROM 
            ReservationForm rf
        WHERE 
            rf.reservationFormID = @reservationFormID;

        IF @actualCheckInDate > @checkOutDate
        BEGIN
            RAISERROR(N'Phiếu đặt phòng đã quá thời gian trả phòng, không thể check-in.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
            
        -- Kiểm tra xem phòng có sẵn sàng không
        -- Chặn check-in nếu cùng phòng đang có khách khác chưa checkout.
        IF EXISTS (
            SELECT 1
            FROM HistoryCheckin hc
            JOIN ReservationForm activeRf ON hc.reservationFormID = activeRf.reservationFormID
            LEFT JOIN HistoryCheckOut hco ON hco.reservationFormID = hc.reservationFormID
            WHERE activeRf.roomID = @roomID
              AND hc.reservationFormID <> @reservationFormID
              AND hco.historyCheckOutID IS NULL
        )
        BEGIN
            RAISERROR(N'Phòng đang có khách chưa checkout. Vui lòng checkout khách trước hoặc đổi phòng.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        DECLARE @roomStatus NVARCHAR(20);
        
        SELECT @roomStatus = roomStatus 
        FROM Room 
        WHERE roomID = @roomID;
        
        IF @roomStatus NOT IN ('AVAILABLE', 'RESERVED')
        BEGIN
            RAISERROR('Phòng không khả dụng để check-in (trạng thái: %s).', 16, 1, @roomStatus);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Thêm bản ghi check-in
        INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
        VALUES (@historyCheckInID, @actualCheckInDate, @reservationFormID, @employeeID);
        
        -- Tạo ID mới cho lịch sử thay đổi phòng bằng hàm fn_GenerateID thay vì sequence
        DECLARE @roomChangeHistoryID NVARCHAR(15) = dbo.fn_GenerateID('RCH-', 'RoomChangeHistory', 'roomChangeHistoryID', 6);
        
        INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
        VALUES (@roomChangeHistoryID, @actualCheckInDate, @roomID, @reservationFormID, @employeeID);
        
        -- Trạng thái phòng sẽ tự động cập nhật thành ON_USE nhờ trigger TR_UpdateRoomStatus_OnCheckin
        
        -- Trả về kết quả thành công
        SELECT 
            @reservationFormID AS ReservationFormID,
            @historyCheckInID AS HistoryCheckInID,
            @actualCheckInDate AS CheckInDate,
            @roomID AS RoomID,
            CASE 
                WHEN @actualCheckInDate > @checkInDate THEN N'Khách hàng check-in muộn'
                WHEN @actualCheckInDate < @checkInDate THEN N'Khách hàng check-in sớm'
                ELSE N'Khách hàng check-in đúng giờ'
            END AS CheckinStatus;
            
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Hiển thị thông tin lỗi
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO


-- Tạo procedure đơn giản hơn để check-in chỉ cần mã đặt phòng và mã nhân viên
CREATE OR ALTER PROCEDURE sp_QuickCheckin
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    -- Tạo mã check-in mới sử dụng hàm tạo ID
    DECLARE @historyCheckInID NVARCHAR(15) = dbo.fn_GenerateID('HCI-', 'HistoryCheckin', 'historyCheckInID', 6);
    
    -- Gọi procedure chính để thực hiện check-in
    EXEC sp_CheckinRoom 
        @reservationFormID = @reservationFormID,
        @historyCheckInID = @historyCheckInID,
        @employeeID = @employeeID;
END;
GO

-- Đánh dấu khách không đến và giải phóng phòng nếu không còn đặt phòng gần kế tiếp
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


-- Tạo procedure trả phòng
CREATE OR ALTER PROCEDURE sp_CheckoutRoom
    @reservationFormID NVARCHAR(15),
    @historyCheckOutID NVARCHAR(15),
    @employeeID NVARCHAR(15),
    @invoiceID NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- BƯỚC 1: Kiểm tra phiếu đặt phòng
        IF NOT EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phiếu đặt phòng không tồn tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (SELECT 1 FROM ReservationForm WHERE reservationFormID = @reservationFormID AND isActivate = 'DEACTIVATE')
        BEGIN
            RAISERROR('Phiếu đặt phòng đã bị hủy.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF NOT EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phòng chưa được check-in nên không thể trả phòng.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phòng đã được check-out trước đó.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        -- BƯỚC 2: Lấy thông tin cơ bản
        DECLARE @roomID NVARCHAR(15);
        DECLARE @checkOutDateExpected DATETIME;
        DECLARE @checkOutDateActual DATETIME = GETDATE();

        SELECT 
            @roomID = rf.roomID,
            @checkOutDateExpected = rf.checkOutDate
        FROM ReservationForm rf
        WHERE rf.reservationFormID = @reservationFormID;

        -- BƯỚC 3: Thêm bản ghi check-out
        INSERT INTO HistoryCheckOut (historyCheckOutID, checkOutDate, reservationFormID, employeeID)
        VALUES (@historyCheckOutID, @checkOutDateActual, @reservationFormID, @employeeID);

        -- BƯỚC 4: Tạo/Cập nhật Invoice (TRIGGER SẼ TỰ ĐỘNG TÍNH PHÍ)
        IF EXISTS (SELECT 1 FROM Invoice WHERE reservationFormID = @reservationFormID)
        BEGIN
            -- Cập nhật Invoice → Trigger TR_Invoice_ManageUpdate sẽ tính lại phí
            UPDATE Invoice
            SET invoiceDate = @checkOutDateActual
            WHERE reservationFormID = @reservationFormID;
        END
        ELSE
        BEGIN
            -- Tạo Invoice mới → Trigger TR_Invoice_ManageInsert sẽ tính phí
            DECLARE @newInvoiceID NVARCHAR(15) = ISNULL(@invoiceID, dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6));
            
            INSERT INTO Invoice (invoiceID, invoiceDate, roomCharge, servicesCharge, roomBookingDeposit, reservationFormID)
            VALUES (@newInvoiceID, @checkOutDateActual, 0, 0, 0, @reservationFormID);
            -- Lưu ý: roomCharge, servicesCharge sẽ được trigger tính lại
        END

        -- BƯỚC 5: Trả về kết quả (SAU KHI TRIGGER ĐÃ TÍNH PHÍ)
        SELECT 
            i.invoiceID,
            @reservationFormID AS ReservationFormID,
            @historyCheckOutID AS HistoryCheckOutID,
            @checkOutDateActual AS CheckOutDate,
            @roomID AS RoomID,
            i.roomCharge AS RoomCharge,
            i.servicesCharge AS ServicesCharge,
            i.totalDue AS TotalDue,
            i.netDue AS NetDue,
            i.totalAmount AS TotalAmount,
            i.roomBookingDeposit AS Deposit,
            CASE 
                WHEN @checkOutDateActual > @checkOutDateExpected THEN N'Khách hàng trả phòng muộn'
                WHEN @checkOutDateActual < @checkOutDateExpected THEN N'Khách hàng trả phòng sớm'
                ELSE N'Khách hàng trả phòng đúng hạn'
            END AS CheckoutStatus
        FROM Invoice i
        WHERE i.reservationFormID = @reservationFormID;

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

---------------------------------------
-- TẠO RPOCEDURE CHECKOUT 
---------------------------------------
CREATE OR ALTER PROCEDURE sp_QuickCheckout
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    -- Tạo mã check-out mới
    DECLARE @historyCheckOutID NVARCHAR(15) = dbo.fn_GenerateID('HCO-', 'HistoryCheckOut', 'historyCheckOutID', 6);
    
    -- Gọi procedure chính để thực hiện checkout
    EXEC sp_CheckoutRoom 
        @reservationFormID = @reservationFormID,
        @historyCheckOutID = @historyCheckOutID,
        @employeeID = @employeeID;
END;
GO

-- ================================================================
-- Stored Procedure: sp_AddRoomService
-- Mục đích: Thêm dịch vụ vào phòng (bypass trigger conflict với EF Core)
--          Nâng cấp: Kiểm tra dịch vụ đã tồn tại → UPDATE quantity thay vì INSERT mới
-- Tham số:
--   @reservationFormID: Mã phiếu đặt phòng
--   @hotelServiceId: Mã dịch vụ khách sạn
--   @quantity: Số lượng dịch vụ (sẽ được CỘNG THÊM nếu dịch vụ đã tồn tại)
--   @employeeID: Mã nhân viên thêm dịch vụ
-- Trả về: SELECT thông tin dịch vụ vừa thêm/cập nhật
-- ================================================================
CREATE OR ALTER PROCEDURE sp_AddRoomService
    @reservationFormID NVARCHAR(15),
    @hotelServiceId NVARCHAR(15),
    @quantity INT,
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu đặt phòng tồn tại và đang hoạt động
        IF NOT EXISTS (SELECT 1 FROM ReservationForm 
                      WHERE reservationFormID = @reservationFormID 
                      AND isActivate = 'ACTIVATE')
        BEGIN
            RAISERROR(N'Phiếu đặt phòng không tồn tại hoặc đã bị hủy.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- Kiểm tra đã check-out chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Không thể thêm dịch vụ cho phòng đã check-out.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- Kiểm tra dịch vụ tồn tại
        DECLARE @unitPrice MONEY;
        SELECT @unitPrice = servicePrice 
        FROM HotelService 
        WHERE hotelServiceId = @hotelServiceId AND isActivate = 'ACTIVATE';
        
        IF @unitPrice IS NULL
        BEGIN
            RAISERROR(N'Dịch vụ không tồn tại hoặc đã bị vô hiệu hóa.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- ===== KIỂM TRA DỊCH VỤ ĐÃ TỒN TẠI CHƯA =====
        DECLARE @existingRoomUsageServiceId NVARCHAR(15);
        DECLARE @existingQuantity INT;
        
        SELECT 
            @existingRoomUsageServiceId = roomUsageServiceId,
            @existingQuantity = quantity
        FROM RoomUsageService
        WHERE reservationFormID = @reservationFormID 
          AND hotelServiceId = @hotelServiceId;
        
        IF @existingRoomUsageServiceId IS NOT NULL
        BEGIN
            -- ===== DỊCH VỤ ĐÃ TỒN TẠI → UPDATE QUANTITY =====
            UPDATE RoomUsageService
            SET quantity = quantity + @quantity,  -- Cộng thêm số lượng mới
                dateAdded = GETDATE(),           -- Cập nhật thời gian
                employeeID = @employeeID         -- Cập nhật nhân viên
            WHERE roomUsageServiceId = @existingRoomUsageServiceId;
            
            COMMIT TRANSACTION;
            
            -- Trả về thông tin dịch vụ đã cập nhật
            SELECT 
                rus.roomUsageServiceId AS RoomUsageServiceId,
                rus.quantity AS Quantity,
                rus.unitPrice AS UnitPrice,
                rus.dateAdded AS DateAdded,
                rus.hotelServiceId AS HotelServiceId,
                hs.serviceName AS ServiceName,
                rus.reservationFormID AS ReservationFormID,
                rus.employeeID AS EmployeeID,
                (rus.quantity * rus.unitPrice) AS TotalPrice,
                'UPDATED' AS Action,
                @quantity AS QuantityAdded,
                @existingQuantity AS PreviousQuantity
            FROM RoomUsageService rus
            JOIN HotelService hs ON rus.hotelServiceId = hs.hotelServiceId
            WHERE rus.roomUsageServiceId = @existingRoomUsageServiceId;
        END
        ELSE
        BEGIN
            -- ===== DỊCH VỤ CHƯA TỒN TẠI → INSERT MỚI =====
            DECLARE @roomUsageServiceId NVARCHAR(15) = dbo.fn_GenerateID('RUS-', 'RoomUsageService', 'roomUsageServiceId', 6);
            
            INSERT INTO RoomUsageService (
                roomUsageServiceId, 
                quantity, 
                unitPrice, 
                dateAdded,
                hotelServiceId, 
                reservationFormID, 
                employeeID
            )
            VALUES (
                @roomUsageServiceId,
                @quantity,
                @unitPrice,
                GETDATE(),
                @hotelServiceId,
                @reservationFormID,
                @employeeID
            );
            
            COMMIT TRANSACTION;
            
            -- Trả về thông tin dịch vụ vừa thêm
            SELECT 
                rus.roomUsageServiceId AS RoomUsageServiceId,
                rus.quantity AS Quantity,
                rus.unitPrice AS UnitPrice,
                rus.dateAdded AS DateAdded,
                rus.hotelServiceId AS HotelServiceId,
                hs.serviceName AS ServiceName,
                rus.reservationFormID AS ReservationFormID,
                rus.employeeID AS EmployeeID,
                (rus.quantity * rus.unitPrice) AS TotalPrice,
                'INSERTED' AS Action,
                @quantity AS QuantityAdded,
                0 AS PreviousQuantity
            FROM RoomUsageService rus
            JOIN HotelService hs ON rus.hotelServiceId = hs.hotelServiceId
            WHERE rus.roomUsageServiceId = @roomUsageServiceId;
        END
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- ================================================================
-- Stored Procedure: sp_DeleteRoomService
-- Mục đích: Xóa dịch vụ khỏi phòng (cho phép xóa từng dịch vụ đã thêm)
-- Tham số:
--   @roomUsageServiceId: Mã sử dụng dịch vụ cần xóa
-- Trả về: Thông tin dịch vụ đã xóa
-- ================================================================
CREATE OR ALTER PROCEDURE sp_DeleteRoomService
    @roomUsageServiceId NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra dịch vụ tồn tại
        IF NOT EXISTS (SELECT 1 FROM RoomUsageService WHERE roomUsageServiceId = @roomUsageServiceId)
        BEGIN
            RAISERROR(N'Dịch vụ không tồn tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- Lấy thông tin trước khi xóa
        DECLARE @reservationFormID NVARCHAR(15);
        SELECT @reservationFormID = reservationFormID 
        FROM RoomUsageService 
        WHERE roomUsageServiceId = @roomUsageServiceId;
        
        -- Kiểm tra đã check-out chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Không thể xóa dịch vụ cho phòng đã check-out.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- Lưu thông tin để trả về
        DECLARE @DeletedServiceInfo TABLE (
            RoomUsageServiceId NVARCHAR(15),
            ServiceName NVARCHAR(200),
            Quantity INT,
            UnitPrice MONEY,
            TotalPrice MONEY,
            ReservationFormID NVARCHAR(15),
            DateAdded DATETIME
        );
        
        INSERT INTO @DeletedServiceInfo
        SELECT 
            rus.roomUsageServiceId,
            hs.serviceName,
            rus.quantity,
            rus.unitPrice,
            (rus.quantity * rus.unitPrice),
            rus.reservationFormID,
            rus.dateAdded
        FROM RoomUsageService rus
        JOIN HotelService hs ON rus.hotelServiceId = hs.hotelServiceId
        WHERE rus.roomUsageServiceId = @roomUsageServiceId;
        
        -- Xóa dịch vụ
        DELETE FROM RoomUsageService
        WHERE roomUsageServiceId = @roomUsageServiceId;
        
        COMMIT TRANSACTION;
        
        -- Trả về thông tin dịch vụ đã xóa
        SELECT * FROM @DeletedServiceInfo;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO


--===================================================================================
-- 3. THÊM DỮ LIỆU
--===================================================================================


-- Thêm dữ liệu vào bảng Employee
INSERT INTO Employee (employeeID, fullName, phoneNumber, email, address, gender, idCardNumber, dob, position)
VALUES
    ('EMP-000000', N'ADMIN', '0123456789', 'quanlykhachsan@gmail.com', 'KHÔNG CÓ', 'MALE', '001099012346', '2005-01-17', 'MANAGER'),
    ('EMP-000001', N'Đặng Ngọc Hiếu', '0912345678', 'hieud@gmail.com', N'123 Ho Chi Minh', 'MALE', '001099012345', '2005-01-17', 'MANAGER'),
    ('EMP-000002', N'Nguyễn Văn A', '0912345679', 'nguyenvana@gmail.com', N'456 Ho Chi Minh', 'MALE', '001099012346', '2005-01-17', 'MANAGER'),
    ('EMP-000003', N'Phạm Thị C', '0912345680', 'phamthic@gmail.com', N'123 Ho Chi Minh', 'FEMALE', '001099012347', '2000-03-25', 'RECEPTIONIST'),
    ('EMP-000004', N'Trần Văn C', '0912345681', 'tranvanc@gmail.com', N'234 Ho Chi Minh', 'MALE', '001099012348', '1999-05-30', 'CLEANER'),
    ('EMP-000005', N'Phạm Thị D', '0912345682', 'phamthid@gmail.com', N'567 Ho Chi Minh', 'FEMALE', '001099012349', '1998-08-15', 'TECHNICIAN')
GO


-- Thêm dữ liệu vào bảng ServiceCategory
INSERT INTO ServiceCategory (serviceCategoryID, serviceCategoryName)
VALUES
    ('SC-000001', N'Giải trí'),
    ('SC-000002', N'Ăn uống'),
    ('SC-000003', N'Chăm sóc và sức khỏe'),
    ('SC-000004', N'Vận chuyển');
GO

-- Thêm dữ liệu vào bảng HotelService
INSERT INTO HotelService (hotelServiceId, serviceName, description, servicePrice, serviceCategoryID)
VALUES
    ('HS-000001', N'Hồ bơi', N'Sử dụng hồ bơi ngoài trời cho khách nghỉ', 50.00, 'SC-000001'),
    ('HS-000002', N'Bữa sáng tự chọn', N'Bữa sáng buffet với đa dạng món ăn', 30.00, 'SC-000002'),
    ('HS-000003', N'Thức uống tại phòng', N'Phục vụ thức uống tại phòng', 20.00, 'SC-000002'),
    ('HS-000004', N'Dịch vụ Spa', N'Massage toàn thân và liệu trình chăm sóc da', 120.00, 'SC-000003'),
    ('HS-000005', N'Phòng Gym', N'Trung tâm thể hình với trang thiết bị hiện đại', 700000, 'SC-000001'),
    ('HS-00006', N'Trò chơi điện tử', N'Khu vực giải trí với các trò chơi điện tử', 500000, 'SC-000001'),
    ('HS-00007', N'Buffet tối', N'Thực đơn buffet với đa dạng món ăn', 2000000, 'SC-000002'),
    ('HS-00008', N'Dịch vụ Cà phê', N'Cà phê và đồ uống nóng phục vụ cả ngày', 300000, 'SC-000002'),
    ('HS-00009', N'Xe đưa đón sân bay', N'Dịch vụ đưa đón từ sân bay về khách sạn', 1200000, 'SC-000004'),
    ('HS-000010', N'Thuê xe đạp', N'Thuê xe đạp tham quan quanh thành phố', 400000, 'SC-000004'),
    ('HS-000011', N'Thuê xe điện', N'Thuê xe điện cho các chuyến đi ngắn', 600000, 'SC-000004');
GO

-- Thêm dữ liệu vào bảng RoomCategory
INSERT INTO RoomCategory (roomCategoryID, roomCategoryName, numberOfBed)
VALUES
    ('RC-000001', N'Phòng Thường Giường Đơn', 1),
    ('RC-000002', N'Phòng Thường Giường Đôi', 2),
    ('RC-000003', N'Phòng VIP Giường Đơn', 1),
    ('RC-000004', N'Phòng VIP Giường Đôi', 2);
GO

-- Thêm dữ liệu vào bảng Pricing
INSERT INTO Pricing (pricingID, priceUnit, price, roomCategoryID)
VALUES
    ('P-000001', N'HOUR', 150000.00, 'RC-000001'),
    ('P-000002', N'DAY', 800000.00, 'RC-000001'),
    ('P-000003', N'HOUR', 200000.00, 'RC-000002'),
    ('P-000004', N'DAY', 850000.00, 'RC-000002'),
    ('P-000005', N'HOUR', 300000.00, 'RC-000003'),
    ('P-000006', N'DAY', 1600000.00, 'RC-000003'),
    ('P-000007', N'HOUR', 400000.00, 'RC-000004'),
    ('P-000008', N'DAY', 1800000.00, 'RC-000004');
GO

-- Thêm dữ liệu vào bảng Room với mã phòng mới
INSERT INTO Room (roomID, roomStatus, dateOfCreation, roomCategoryID)
VALUES
    ('T1101', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000001'),
    ('V2102', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000004'),
    ('T1203', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000001'),
    ('V2304', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000004'),
    ('T1105', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000001'),
    ('V2206', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000004'),
    ('T1307', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000001'),
    ('V2408', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000004'),
    ('T1109', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000001'),
    ('V2210', N'AVAILABLE', '2025-02-28 10:00:00', 'RC-000004');
GO


-- Thêm dữ liệu vào bảng Customer
INSERT INTO ReservationForm(reservationFormID, reservationDate, checkInDate, checkOutDate, employeeID, roomID, customerID, roomBookingDeposit)
VALUES
    ('RF-000001', '2025-03-25 13:35:00', '2025-03-26 09:00:00', '2025-03-28 10:30:00', 'EMP-000002', 'V2102', 'CUS-000001', 510000),
    ('RF-000002', '2025-03-27 12:40:00', '2025-03-28 08:45:00', '2025-03-30 12:30:00', 'EMP-000003', 'V2206', 'CUS-000006', 510000),
    ('RF-000003', '2025-03-23 11:25:00', '2025-03-24 07:50:00', '2025-03-26 17:20:00', 'EMP-000004', 'V2304', 'CUS-000009', 1080000),
    ('RF-000004', '2025-03-30 16:10:00', '2025-03-31 12:10:00', '2025-04-02 14:40:00', 'EMP-000005', 'V2408', 'CUS-000004', 1080000),
    ('RF-000005', '2025-04-01 09:00:00', '2025-04-02 10:45:00', '2025-04-04 11:15:00', 'EMP-000001', 'T1101', 'CUS-000003', 480000),
    ('RF-000006', '2025-04-05 08:45:00', '2025-04-06 14:20:00', '2025-04-08 10:00:00', 'EMP-000002', 'T1109', 'CUS-000008', 480000),
    ('RF-000007', '2025-04-08 07:30:00', '2025-04-09 13:10:00', '2025-04-11 12:25:00', 'EMP-000003', 'T1105', 'CUS-000006', 720000),
    ('RF-000008', '2025-04-10 10:25:00', '2025-04-11 09:40:00', '2025-04-13 14:35:00', 'EMP-000004', 'T1203', 'CUS-000007', 720000),
    ('RF-000009', '2025-04-12 15:30:00', '2025-04-13 12:50:00', '2025-04-15 10:30:00', 'EMP-000005', 'T1307', 'CUS-000004', 1440000),
    ('RF-000010', '2025-04-15 11:15:00', '2025-04-16 09:15:00', '2025-04-18 16:00:00', 'EMP-000001', 'V2210', 'CUS-000008', 1440000);
GO

-------------------------------------------
-- Thêm dữ liệu checkIn và roomChangeHistory
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000010', '2025-03-26 09:30:00', 'RF-000001', 'EMP-000002');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000010', '2025-03-26 09:30:00', 'V2102', 'RF-000001', 'EMP-000002');

-- Check-in cho RF-000002
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000011', '2025-03-28 09:15:00', 'RF-000002', 'EMP-000003');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000011', '2025-03-28 09:15:00', 'V2206', 'RF-000002', 'EMP-000003');

-- Check-in cho RF-000003
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000012', '2025-03-24 08:30:00', 'RF-000003', 'EMP-000004');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000012', '2025-03-24 08:30:00', 'V2304', 'RF-000003', 'EMP-000004');

-- Check-in cho RF-000004
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000013', '2025-03-31 12:45:00', 'RF-000004', 'EMP-000005');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000013', '2025-03-31 12:45:00', 'V2408', 'RF-000004', 'EMP-000005');

-- Check-in cho RF-000005
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000014', '2025-04-02 10:30:00', 'RF-000005', 'EMP-000001');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000014', '2025-04-02 10:30:00', 'T1101', 'RF-000005', 'EMP-000001');


-- Check-in cho RF-000006
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000015', '2025-04-06 14:35:00', 'RF-000006', 'EMP-000002');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000015', '2025-04-06 14:35:00', 'T1109', 'RF-000006', 'EMP-000002');

-- Check-in cho RF-000007
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000016', '2025-04-09 13:25:00', 'RF-000007', 'EMP-000003');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000016', '2025-04-09 13:25:00', 'T1105', 'RF-000007', 'EMP-000003');

-- Check-in cho RF-000008
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000017', '2025-04-11 10:10:00', 'RF-000008', 'EMP-000004');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000017', '2025-04-11 10:10:00', 'T1203', 'RF-000008', 'EMP-000004');

-- Check-in cho RF-000009
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000018', '2025-04-13 13:15:00', 'RF-000009', 'EMP-000005');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000018', '2025-04-13 13:15:00', 'T1307', 'RF-000009', 'EMP-000005');

-- Check-in cho RF-000010
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-000019', '2025-04-16 09:45:00', 'RF-000010', 'EMP-000001');

INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
VALUES ('RCH-000019', '2025-04-16 09:45:00', 'V2210', 'RF-000010', 'EMP-000001');
-----------------------------------------------------------------------------------
----------------------------------------------------------------------------------
--Cập nhật trạng thái phòng thành ON_USE cho các phòng đã check-in
UPDATE Room
SET roomStatus = 'ON_USE'
WHERE roomID IN (
    SELECT r.roomID
    FROM Room r
    JOIN ReservationForm rf ON r.roomID = rf.roomID
    JOIN HistoryCheckin hc ON rf.reservationFormID = hc.reservationFormID
    LEFT JOIN HistoryCheckOut ho ON rf.reservationFormID = ho.reservationFormID
    WHERE ho.historyCheckOutID IS NULL
);


------------------------
--Thực hiện check-out và tạo hóa đơn thông qua stored procedure
-- Check-out cho RF-000001
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000001',
    @historyCheckOutID = 'HCO-000001',
    @employeeID = 'EMP-000002',
    @invoiceID = 'INV-000001';

-- Check-out cho RF-000002
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000002',
    @historyCheckOutID = 'HCO-000002',
    @employeeID = 'EMP-000003',
    @invoiceID = 'INV-000002';

-- Check-out cho RF-000003
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000003',
    @historyCheckOutID = 'HCO-000003',
    @employeeID = 'EMP-000004',
    @invoiceID = 'INV-000003';

-- Check-out cho RF-000004
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000004',
    @historyCheckOutID = 'HCO-000004',
    @employeeID = 'EMP-000005',
    @invoiceID = 'INV-000004';

-- Check-out cho RF-000005
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000005',
    @historyCheckOutID = 'HCO-000005',
    @employeeID = 'EMP-000001',
    @invoiceID = 'INV-000005';

-- Check-out cho RF-000006
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000006',
    @historyCheckOutID = 'HCO-000006',
    @employeeID = 'EMP-000002',
    @invoiceID = 'INV-000006';

-- Check-out cho RF-000007
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000007',
    @historyCheckOutID = 'HCO-000007',
    @employeeID = 'EMP-000003',
    @invoiceID = 'INV-000007';

-- Check-out cho RF-000008
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000008',
    @historyCheckOutID = 'HCO-000008',
    @employeeID = 'EMP-000004',
    @invoiceID = 'INV-000008';

-- Check-out cho RF-000009
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000009',
    @historyCheckOutID = 'HCO-000009',
    @employeeID = 'EMP-000005',
    @invoiceID = 'INV-000009';

-- Check-out cho RF-000010
EXEC sp_CheckoutRoom 
    @reservationFormID = 'RF-000010',
    @historyCheckOutID = 'HCO-000010',
    @employeeID = 'EMP-000001',
    @invoiceID = 'INV-000010';

GO
----------------------------------------------------------------------------------

--=======================================================================
-- STORED PROCEDURES THAM KHẢO CHO MODULE BẢO TRÌ & DỌN PHÒNG
--=======================================================================
-- Ứng dụng MVC hiện tại xử lý module Bảo trì & Dọn phòng bằng Entity Framework Core trực tiếp.
-- Các stored procedure RoomTask bên dưới được giữ lại để tham khảo/đối chiếu nghiệp vụ và không phải luồng chính đang được controller gọi.

-------------------------------------
-- SP 1: TẠO CÔNG VIỆC PHÒNG (THAM KHẢO)
-------------------------------------
CREATE OR ALTER PROCEDURE sp_CreateRoomTask
    @roomID NVARCHAR(15),
    @taskType NVARCHAR(20),
    @title NVARCHAR(200),
    @description NVARCHAR(MAX) = NULL,
    @priority NVARCHAR(20) = 'NORMAL',
    @createdByEmployeeID NVARCHAR(15),
    @roomTaskID NVARCHAR(15) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Room WHERE roomID = @roomID AND isActivate = 'ACTIVATE')
        THROW 50001, N'Phòng không hợp lệ hoặc đã ngừng hoạt động.', 1;

    IF @taskType NOT IN ('CLEANING', 'MAINTENANCE')
        THROW 50002, N'Loại công việc không hợp lệ.', 1;

    IF @priority NOT IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')
        THROW 50003, N'Mức độ ưu tiên không hợp lệ.', 1;

    IF EXISTS (
        SELECT 1 FROM RoomTask
        WHERE roomID = @roomID
          AND taskType = @taskType
          AND status IN ('PENDING', 'ASSIGNED', 'IN_PROGRESS')
    )
        THROW 50004, N'Phòng đã có công việc cùng loại đang mở.', 1;

    SET @roomTaskID = dbo.fn_GenerateID('RT-', 'RoomTask', 'roomTaskID', 6);

    INSERT INTO RoomTask(roomTaskID, roomID, taskType, title, description, priority, status, createdByEmployeeID, createdAt)
    VALUES (@roomTaskID, @roomID, @taskType, @title, @description, @priority, 'PENDING', @createdByEmployeeID, DATEADD(HOUR, 7, SYSUTCDATETIME()));

    INSERT INTO RoomTaskHistory(roomTaskHistoryID, roomTaskID, oldStatus, newStatus, changedByEmployeeID, changedAt, note)
    VALUES (dbo.fn_GenerateID('RTH-', 'RoomTaskHistory', 'roomTaskHistoryID', 6), @roomTaskID, NULL, 'PENDING', @createdByEmployeeID, DATEADD(HOUR, 7, SYSUTCDATETIME()), N'Tạo công việc');

    UPDATE Room
    SET roomStatus = CASE WHEN @taskType = 'CLEANING' THEN 'DIRTY' ELSE 'MAINTENANCE' END
    WHERE roomID = @roomID;
END;
GO

CREATE OR ALTER PROCEDURE sp_AssignRoomTask
    @roomTaskID NVARCHAR(15),
    @assignedEmployeeID NVARCHAR(15),
    @changedByEmployeeID NVARCHAR(15),
    @note NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM RoomTask WHERE roomTaskID = @roomTaskID AND status NOT IN ('COMPLETED', 'CANCELLED'))
        THROW 50005, N'Công việc không tồn tại hoặc đã kết thúc.', 1;

    IF NOT EXISTS (SELECT 1 FROM Employee WHERE employeeID = @assignedEmployeeID AND isActivate = 'ACTIVATE')
        THROW 50006, N'Nhân viên được phân công không hợp lệ.', 1;

    DECLARE @oldStatus NVARCHAR(20);
    SELECT @oldStatus = status FROM RoomTask WHERE roomTaskID = @roomTaskID;

    UPDATE RoomTask
    SET assignedEmployeeID = @assignedEmployeeID,
        status = 'ASSIGNED',
        note = @note
    WHERE roomTaskID = @roomTaskID;

    INSERT INTO RoomTaskHistory(roomTaskHistoryID, roomTaskID, oldStatus, newStatus, changedByEmployeeID, changedAt, note)
    VALUES (dbo.fn_GenerateID('RTH-', 'RoomTaskHistory', 'roomTaskHistoryID', 6), @roomTaskID, @oldStatus, 'ASSIGNED', @changedByEmployeeID, DATEADD(HOUR, 7, SYSUTCDATETIME()), @note);
END;
GO

CREATE OR ALTER PROCEDURE sp_UpdateRoomTaskStatus
    @roomTaskID NVARCHAR(15),
    @newStatus NVARCHAR(20),
    @changedByEmployeeID NVARCHAR(15),
    @note NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @newStatus NOT IN ('IN_PROGRESS', 'COMPLETED')
        THROW 50007, N'Trạng thái cập nhật không hợp lệ.', 1;

    DECLARE @oldStatus NVARCHAR(20), @taskType NVARCHAR(20), @roomID NVARCHAR(15);
    SELECT @oldStatus = status, @taskType = taskType, @roomID = roomID
    FROM RoomTask
    WHERE roomTaskID = @roomTaskID AND status NOT IN ('COMPLETED', 'CANCELLED');

    IF @oldStatus IS NULL
        THROW 50008, N'Công việc không tồn tại hoặc đã kết thúc.', 1;

    UPDATE RoomTask
    SET status = @newStatus,
        startedAt = CASE WHEN @newStatus = 'IN_PROGRESS' AND startedAt IS NULL THEN DATEADD(HOUR, 7, SYSUTCDATETIME()) ELSE startedAt END,
        completedAt = CASE WHEN @newStatus = 'COMPLETED' THEN DATEADD(HOUR, 7, SYSUTCDATETIME()) ELSE completedAt END,
        note = @note
    WHERE roomTaskID = @roomTaskID;

    UPDATE Room
    SET roomStatus = CASE
        WHEN @newStatus = 'IN_PROGRESS' AND @taskType = 'CLEANING' THEN 'CLEANING'
        WHEN @newStatus = 'IN_PROGRESS' AND @taskType = 'MAINTENANCE' THEN 'MAINTENANCE'
        WHEN @newStatus = 'COMPLETED' THEN 'AVAILABLE'
        ELSE roomStatus
    END
    WHERE roomID = @roomID;

    INSERT INTO RoomTaskHistory(roomTaskHistoryID, roomTaskID, oldStatus, newStatus, changedByEmployeeID, changedAt, note)
    VALUES (dbo.fn_GenerateID('RTH-', 'RoomTaskHistory', 'roomTaskHistoryID', 6), @roomTaskID, @oldStatus, @newStatus, @changedByEmployeeID, DATEADD(HOUR, 7, SYSUTCDATETIME()), @note);
END;
GO

CREATE OR ALTER PROCEDURE sp_CancelRoomTask
    @roomTaskID NVARCHAR(15),
    @changedByEmployeeID NVARCHAR(15),
    @cancelReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @oldStatus NVARCHAR(20), @roomID NVARCHAR(15);
    SELECT @oldStatus = status, @roomID = roomID
    FROM RoomTask
    WHERE roomTaskID = @roomTaskID AND status <> 'COMPLETED';

    IF @oldStatus IS NULL
        THROW 50009, N'Công việc không tồn tại hoặc đã hoàn thành.', 1;

    UPDATE RoomTask
    SET status = 'CANCELLED',
        cancelledAt = DATEADD(HOUR, 7, SYSUTCDATETIME()),
        cancelReason = @cancelReason
    WHERE roomTaskID = @roomTaskID;

    IF NOT EXISTS (
        SELECT 1 FROM RoomTask
        WHERE roomID = @roomID
          AND roomTaskID <> @roomTaskID
          AND status IN ('PENDING', 'ASSIGNED', 'IN_PROGRESS')
    )
    BEGIN
        UPDATE Room SET roomStatus = 'AVAILABLE' WHERE roomID = @roomID;
    END

    INSERT INTO RoomTaskHistory(roomTaskHistoryID, roomTaskID, oldStatus, newStatus, changedByEmployeeID, changedAt, note)
    VALUES (dbo.fn_GenerateID('RTH-', 'RoomTaskHistory', 'roomTaskHistoryID', 6), @roomTaskID, @oldStatus, 'CANCELLED', @changedByEmployeeID, DATEADD(HOUR, 7, SYSUTCDATETIME()), @cancelReason);
END;
GO

CREATE OR ALTER PROCEDURE sp_CreateInvoice_CheckoutThenPay
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        
        -- Kiểm tra đã check-in chưa
        IF NOT EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Phiếu đặt phòng chưa check-in', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã checkout chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Phiếu đặt phòng đã checkout', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã có invoice chưa
        IF EXISTS (SELECT 1 FROM Invoice WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Hóa đơn đã tồn tại', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Lấy thông tin
        DECLARE @roomID NVARCHAR(15);
        SELECT @roomID = roomID FROM ReservationForm WHERE reservationFormID = @reservationFormID;
        
        -- Tạo HistoryCheckOut với thời gian HIỆN TẠI
        DECLARE @checkOutID NVARCHAR(15) = dbo.fn_GenerateID('HCO-', 'HistoryCheckOut', 'historyCheckOutID', 6);
        
        INSERT INTO HistoryCheckOut (historyCheckOutID, checkOutDate, reservationFormID, employeeID)
        VALUES (@checkOutID, GETDATE(), @reservationFormID, @employeeID);
        
        -- Tạo Invoice (Trigger sẽ tự động tính toán dựa trên checkOutActual)
        DECLARE @invoiceID NVARCHAR(15) = dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6);
        
        INSERT INTO Invoice (
            invoiceID, invoiceDate, roomCharge, servicesCharge, reservationFormID, 
            isPaid, checkoutType
        )
        VALUES (
            @invoiceID, GETDATE(), 0, 0, @reservationFormID, 
            0, 'CHECKOUT_THEN_PAY'
        );
        
        -- Cập nhật invoiceID vào HistoryCheckOut
        UPDATE HistoryCheckOut SET invoiceID = @invoiceID WHERE historyCheckOutID = @checkOutID;
        
        
        -- Trả về thông tin invoice
        SELECT 
            inv.invoiceID,
            inv.roomCharge,
            inv.servicesCharge,
            inv.totalAmount,
            inv.isPaid,
            inv.checkoutType,
            @checkOutID AS checkOutID,
            'CHECKOUT_THEN_PAY' AS status
        FROM Invoice inv
        WHERE inv.invoiceID = @invoiceID;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-------------------------------------
-- SP 2: THANH TOÁN TRƯỚC (Pay Then Checkout)
-------------------------------------
CREATE OR ALTER PROCEDURE sp_CreateInvoice_PayThenCheckout
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15),
    @paymentMethod NVARCHAR(20) = 'CASH'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra đã check-in chưa
        IF NOT EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Phiếu đặt phòng chưa check-in', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã có invoice chưa
        IF EXISTS (SELECT 1 FROM Invoice WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Hóa đơn đã tồn tại', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Tạo Invoice với checkOutDate = Expected (chưa checkout thực tế)
        -- Trigger sẽ tính dựa trên checkInActual -> checkOutExpected
        DECLARE @invoiceID NVARCHAR(15) = dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6);
        
        INSERT INTO Invoice (
            invoiceID, invoiceDate, roomCharge, servicesCharge, 
            reservationFormID, isPaid, paymentDate, paymentMethod, checkoutType, amountPaid
        )
        VALUES (
            @invoiceID, GETDATE(), 0, 0, 
            @reservationFormID, 1, GETDATE(), @paymentMethod, 'PAY_THEN_CHECKOUT', 0
        );
        
        -- Phòng vẫn ON_USE, khách tiếp tục ở
        
        -- Trả về thông tin invoice
        SELECT 
            inv.invoiceID,
            inv.roomCharge,
            inv.servicesCharge,
            inv.totalAmount,
            inv.isPaid,
            inv.paymentDate,
            inv.paymentMethod,
            inv.checkoutType,
            'PAY_THEN_CHECKOUT' AS status
        FROM Invoice inv
        WHERE inv.invoiceID = @invoiceID;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-------------------------------------
-- SP 3: XÁC NHẬN THANH TOÁN (cho Checkout Then Pay)
-------------------------------------
CREATE OR ALTER PROCEDURE sp_ConfirmPayment
    @invoiceID NVARCHAR(15),
    @paymentMethod NVARCHAR(20) = 'CASH',
    @employeeID NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra invoice tồn tại
        IF NOT EXISTS (SELECT 1 FROM Invoice WHERE invoiceID = @invoiceID)
        BEGIN
            RAISERROR(N'Không tìm thấy hóa đơn', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã thanh toán chưa
        DECLARE @isPaid BIT,
                @totalAmount Decimal(18,2),
                @amountToCollect Decimal(18,2)

        SELECT @isPaid = isPaid, @totalAmount = totalAmount FROM Invoice WHERE invoiceID = @invoiceID;
        SET @amountToCollect = CASE WHEN @totalAmount > 0 THEN @totalAmount ELSE 0 END;

        IF @isPaid = 1
        BEGIN
            RAISERROR(N'Hóa đơn đã được thanh toán', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- xem thử phòng đã check-out chưa
        IF EXISTS (
            SELECT 1 
            FROM HistoryCheckOut hco
            JOIN Invoice inv ON hco.invoiceID = inv.invoiceID
            WHERE inv.invoiceID = @invoiceID
        )
        BEGIN
            SET @isPaid = 1
        END

        -- Cập nhật trạng thái thanh toán
        UPDATE Invoice
        SET isPaid = @isPaid,
            paymentDate = GETDATE(),
            paymentMethod = @paymentMethod,
            amountPaid = @amountToCollect
        WHERE invoiceID = @invoiceID;
        
        -- Lấy roomID để giải phóng phòng
        DECLARE @roomID NVARCHAR(15);
        SELECT @roomID = rf.roomID
        FROM Invoice inv
        JOIN ReservationForm rf ON inv.reservationFormID = rf.reservationFormID
        WHERE inv.invoiceID = @invoiceID;
        
        -- Giải phóng phòng, nhưng giữ RESERVED nếu có đặt phòng kế tiếp trong 5 giờ.
        UPDATE Room
        SET roomStatus = CASE
            WHEN EXISTS (
                SELECT 1
                FROM ReservationForm nextRf
                LEFT JOIN HistoryCheckin nextHci ON nextRf.reservationFormID = nextHci.reservationFormID
                WHERE nextRf.roomID = @roomID
                  AND nextRf.isActivate = 'ACTIVATE'
                  AND nextHci.historyCheckinID IS NULL
                  AND nextRf.checkInDate > GETDATE()
                  AND nextRf.checkInDate <= DATEADD(HOUR, 5, GETDATE())
            ) THEN 'RESERVED'
            ELSE 'AVAILABLE'
        END
        WHERE roomID = @roomID;
        
        -- Trả về thông tin (sử dụng biến để đảm bảo giá trị đúng)
        DECLARE @currentDate DATETIME = GETDATE();
        DECLARE @totalAmt DECIMAL(18,2);
        
        SELECT @totalAmt = totalAmount FROM Invoice WHERE invoiceID = @invoiceID;
        
        SELECT 
            @invoiceID AS invoiceID,
            CAST(1 AS BIT) AS isPaid,
            @currentDate AS paymentDate,
            @paymentMethod AS paymentMethod,
            @totalAmt AS totalAmount,
            'PAYMENT_CONFIRMED' AS status;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-------------------------------------
-- SP 4: CHECKOUT THỰC TẾ SAU KHI ĐÃ THANH TOÁN TRƯỚC
-------------------------------------
CREATE OR ALTER PROCEDURE sp_ActualCheckout_AfterPrepayment
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra đã thanh toán chưa
        DECLARE @invoiceID NVARCHAR(15);
        DECLARE @isPaid BIT;
        
        SELECT @invoiceID = invoiceID, @isPaid = isPaid
        FROM Invoice
        WHERE reservationFormID = @reservationFormID;
        
        IF @invoiceID IS NULL OR @isPaid = 0
        BEGIN
            RAISERROR(N'Phải thanh toán trước khi checkout', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã checkout chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Đã checkout rồi', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Lấy thông tin
        DECLARE @roomID NVARCHAR(15);
        DECLARE @checkOutExpected DATETIME;
        DECLARE @checkOutActual DATETIME = GETDATE();
        
        SELECT 
            @roomID = rf.roomID,
            @checkOutExpected = rf.checkOutDate
        FROM ReservationForm rf
        WHERE rf.reservationFormID = @reservationFormID;
        
        -- Tạo HistoryCheckOut
        DECLARE @checkOutID NVARCHAR(15) = dbo.fn_GenerateID('HCO-', 'HistoryCheckOut', 'historyCheckOutID', 6);
        
        INSERT INTO HistoryCheckOut (historyCheckOutID, checkOutDate, reservationFormID, employeeID, invoiceID)
        VALUES (@checkOutID, @checkOutActual, @reservationFormID, @employeeID, @invoiceID);
        
        -- Kiểm tra checkout muộn
        DECLARE @additionalCharge DECIMAL(18,2) = 0;
        DECLARE @checkoutStatus NVARCHAR(20) = 'ON_TIME';
        DECLARE @originalCharge DECIMAL(18,2);
        
        -- Lưu roomCharge ban đầu (trước khi checkout thực tế)
        SELECT @originalCharge = roomCharge FROM Invoice WHERE invoiceID = @invoiceID;
        
        IF @checkOutActual > @checkOutExpected
        BEGIN
            SET @checkoutStatus = 'LATE_CHECKOUT';
            
            -- Trigger TR_Invoice_ManageUpdate sẽ tự động tính lại roomCharge dựa trên checkout thực tế
            UPDATE Invoice 
            SET invoiceDate = GETDATE(),  -- Force trigger update
                isPaid = 0 --Khách chưa thanh toán hết
            WHERE invoiceID = @invoiceID;
            
            -- Tính phí phát sinh = roomCharge mới - roomCharge cũ
            SELECT @additionalCharge = roomCharge - @originalCharge
            FROM Invoice
            WHERE invoiceID = @invoiceID;
            
            IF @additionalCharge < 0 SET @additionalCharge = 0;
        END
        
        -- Giải phóng phòng, nhưng giữ RESERVED nếu có đặt phòng kế tiếp trong 5 giờ.
        UPDATE Room
        SET roomStatus = CASE
            WHEN EXISTS (
                SELECT 1
                FROM ReservationForm nextRf
                LEFT JOIN HistoryCheckin nextHci ON nextRf.reservationFormID = nextHci.reservationFormID
                WHERE nextRf.roomID = @roomID
                  AND nextRf.isActivate = 'ACTIVATE'
                  AND nextHci.historyCheckinID IS NULL
                  AND nextRf.checkInDate > GETDATE()
                  AND nextRf.checkInDate <= DATEADD(HOUR, 5, GETDATE())
            ) THEN 'RESERVED'
            ELSE 'AVAILABLE'
        END
        WHERE roomID = @roomID;
        
        -- Trả về thông tin
        SELECT 
            @checkOutID AS checkOutID,
            @checkOutActual AS checkOutDate,
            @checkOutExpected AS checkOutExpected,
            @additionalCharge AS additionalCharge,
            @checkoutStatus AS checkoutStatus,
            @invoiceID AS invoiceID;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

USE HotelManagement;
GO


-- ================================================================
-- SP: Chuyển đổi giá giờ sang giá ngày nếu ở quá 6 giờ
-- ================================================================
CREATE OR ALTER PROCEDURE sp_ConvertHourlyToDaily
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Lấy thông tin đặt phòng
        DECLARE @priceUnit NVARCHAR(15);
        DECLARE @roomCategoryID NVARCHAR(15);
        DECLARE @checkInDateExpected DATETIME;
        DECLARE @checkInDateActual DATETIME;
        DECLARE @hourPrice MONEY;
        DECLARE @dayPrice MONEY;
        
        SELECT 
            @priceUnit = rf.priceUnit,
            @roomCategoryID = r.roomCategoryID,
            @checkInDateExpected = rf.checkInDate
        FROM ReservationForm rf
        JOIN Room r ON rf.roomID = r.roomID
        WHERE rf.reservationFormID = @reservationFormID;
        
        -- Kiểm tra có phải giá giờ không
        IF @priceUnit <> 'HOUR'
        BEGIN
            RAISERROR(N'Phòng không thuê theo giờ', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã check-in chưa
        SELECT @checkInDateActual = checkInDate
        FROM HistoryCheckin
        WHERE reservationFormID = @reservationFormID;
        
        IF @checkInDateActual IS NULL
        BEGIN
            RAISERROR(N'Phòng chưa check-in', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra thời gian ở > 6 giờ
        DECLARE @hoursStayed INT = DATEDIFF(HOUR, @checkInDateActual, GETDATE());
        
        IF @hoursStayed <= 6
        BEGIN
            RAISERROR(N'Thời gian ở chưa đủ 6 giờ để chuyển đổi', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Lấy giá ngày
        SELECT @dayPrice = price
        FROM Pricing
        WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'DAY';
        
        IF @dayPrice IS NULL
        BEGIN
            RAISERROR(N'Không tìm thấy giá theo ngày', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Cập nhật ReservationForm
        UPDATE ReservationForm
        SET priceUnit = 'DAY',
            unitPrice = @dayPrice
        WHERE reservationFormID = @reservationFormID;
        
        -- Log lại thay đổi vào RoomChangeHistory
        DECLARE @roomID NVARCHAR(15);
        SELECT @roomID = roomID FROM ReservationForm WHERE reservationFormID = @reservationFormID;
        
        DECLARE @changeHistoryID NVARCHAR(15) = dbo.fn_GenerateID('RCH-', 'RoomChangeHistory', 'roomChangeHistoryID', 6);
        
        INSERT INTO RoomChangeHistory (roomChangeHistoryID, dateChanged, roomID, reservationFormID, employeeID)
        VALUES (@changeHistoryID, GETDATE(), @roomID, @reservationFormID, @employeeID);
        
        -- Trả về kết quả
        SELECT 
            @reservationFormID AS ReservationFormID,
            'HOUR' AS OldPriceUnit,
            @hourPrice AS OldUnitPrice,
            'DAY' AS NewPriceUnit,
            @dayPrice AS NewUnitPrice,
            @hoursStayed AS HoursStayed,
            'SUCCESS' AS Status;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- Test procedure
-- EXEC sp_ConvertHourlyToDaily @reservationFormID = 'RF-000001', @employeeID = 'EMP-000001';

--=======================================================================
-- CÁC CÂU TRUY VẤN SQL
--=======================================================================

-- Kiểm tra phiếu đặt phòng RF-000112 (phiếu chưa được check-in)
SELECT 
    reservationFormID, 
    roomID, 
    customerID, 
    checkInDate, 
    checkOutDate 
FROM 
    ReservationForm 
WHERE 
    reservationFormID = 'RF-000005';

-- Thực hiện check-in cho phiếu đặt phòng với nhân viên EMP-000005
EXEC sp_QuickCheckin 
    @reservationFormID = 'RF-000005', 
    @employeeID = 'EMP-000001';

-- Kiểm tra kết quả sau khi check-in
SELECT 
    hc.historyCheckInID,
    hc.checkInDate,
    rf.reservationFormID,
    rf.roomID,
    c.fullName AS CustomerName,
    r.roomStatus
FROM 
    HistoryCheckin hc
    JOIN ReservationForm rf ON hc.reservationFormID = rf.reservationFormID
    JOIN Customer c ON rf.customerID = c.customerID
    JOIN Room r ON rf.roomID = r.roomID
WHERE 
    rf.reservationFormID = 'RF-000112';
