CREATE DATABASE KvaternionDB;
USE KvaternionDB;

--Таблица Ролей (Roles)
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(50) NOT NULL
);

--Таблица Категорий товаров (Categories)
CREATE TABLE Categories (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL
);

--Таблица Складов / Объектов (Warehouses)
CREATE TABLE Warehouses (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255)
);

--Таблица Пользователей (Users) 
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(128) NOT NULL, 
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    PhotoPath NVARCHAR(255),
    RoleId INT NOT NULL REFERENCES Roles(Id),
    IsActive BIT DEFAULT 1
);

--Таблица Товаров (Products)
CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY,
    Article NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Unit NVARCHAR(10) NOT NULL, -- шт, кг, м3, т
    CategoryId INT NOT NULL REFERENCES Categories(Id)
);

--Таблица Остатков (Stocks) - Связь товара и склада
CREATE TABLE Stocks (
    Id INT PRIMARY KEY IDENTITY,
    ProductId INT NOT NULL REFERENCES Products(Id),
    WarehouseId INT NOT NULL REFERENCES Warehouses(Id),
    Quantity DECIMAL(18,2) NOT NULL DEFAULT 0
);

--Таблица Журнала операций (SupplyLogs) - История движений
CREATE TABLE SupplyLogs (
    Id INT PRIMARY KEY IDENTITY,
    ProductId INT NOT NULL REFERENCES Products(Id),
    WarehouseId INT NOT NULL REFERENCES Warehouses(Id),
    UserId INT NOT NULL REFERENCES Users(Id),
    OperationType NVARCHAR(50) NOT NULL, -- Приход, Расход, Списание
    Quantity DECIMAL(18,2) NOT NULL,
    OperationDate DATETIME DEFAULT GETDATE(),
    Comment NVARCHAR(255)
);

-----------------------------------------------------------
-- ЗАПОЛНЕНИЕ ДАННЫМИ
-----------------------------------------------------------

-- Роли
INSERT INTO Roles (Name) VALUES 
(N'Администратор'), 
(N'Старший кладовщик'), 
(N'Начальник участка'), 
(N'Бухгалтер');

-- Категории
INSERT INTO Categories (Name) VALUES 
(N'Бетон и растворы'), 
(N'Металлопрокат'), 
(N'Стеновые материалы'), 
(N'Изоляция и кровля'), 
(N'Инструменты и СИЗ'), 
(N'Сухие смеси'), 
(N'Электротовары'), 
(N'Пиломатериалы');

-- Склады
INSERT INTO Warehouses (Name, Address) VALUES 
(N'База "Центральная"', N'ул. Промышленная, 10'),
(N'Объект "ЖК Северный"', N'ул. Полярная, 22'),
(N'Объект "Сити-Центр"', N'пр. Ленина, 5');

-- Юзеры
-- 1. АДМИНИСТРАТОРЫ (Пароль: Admin1)
INSERT INTO Users (Login, PasswordHash, FullName, RoleId, Phone, Email) VALUES 
('sichev_ds', 'CvsAE42OczSOwf5B/T06j8vZAVayY7+leRug4JX0LPw=', N'Сычев Михаил Витальевич', 1, '+79001112233', 'admin@kvaternion.ru');

-- 2. КЛАДОВЩИКИ (Пароль: Sklad2)
INSERT INTO Users (Login, PasswordHash, FullName, RoleId, Phone, Email) VALUES 
('petrov_sk', '15szkEr24ZVSLIZjjs9qv1Il2XPRnbAcXUxefcobg5g=', N'Петров Игорь Семенович', 2, '+79051234567', 'sk_petrov@kvaternion.ru');

-- 3. НАЧАЛЬНИКИ УЧАСТКОВ (Пароль: Uchastok3)
INSERT INTO Users (Login, PasswordHash, FullName, RoleId, Phone, Email) VALUES 
('sidorov_nu', 'YQYIdCK8yan91ITZRp3u+8mymMVZ14BEvwLmBbYTOAM=', N'Сидоров Алексей Владимирович', 3, '+79110001122', 'sidorov_nu@kvaternion.ru'),
('volkov_nu', 'YQYIdCK8yan91ITZRp3u+8mymMVZ14BEvwLmBbYTOAM=', N'Волков Максим Юрьевич', 3, '+79119998877', 'volkov_nu@kvaternion.ru');

-- 4. БУХГАЛТЕРИЯ (Пароль: Buh4)
INSERT INTO Users (Login, PasswordHash, FullName, RoleId, Phone, Email) VALUES 
('ivanova_bh', 'oHy239cbDqhQgizSFvLfeI3Fvb5gfSZfZTFLxJTc5+0=', N'Иванова Елена Петровна', 4, '+74951234567', 'accountant_1@kvaternion.ru');

-- Товары 
INSERT INTO Products (Article, Name, Unit, CategoryId) VALUES 
('BT-300', N'Бетон М300 (В22.5)', N'м3', 1),
('ARM-12', N'Арматура стальная 12мм', N'т', 2),
('KRP-01', N'Кирпич керамический', N'шт', 3),
('GB-200', N'Газобетонный блок 200мм', N'шт', 3),
('ISO-05', N'Технониколь Роклайт', N'уп', 4),
('GLV-01', N'Перчатки рабочие', N'пар', 5),
('CMN-01', N'Цемент М500 (50 кг)', N'меш', 6),
('KLEI-01', N'Клей для газобетона', N'меш', 6),
('LMB-50', N'Доска обрезная 50х150', N'м3', 8),
('LMB-25', N'Брус сосна 100х100', N'м3', 8),
('EL-CBL', N'Кабель ВВГнг 3х2.5', N'м', 7),
('EL-SWT', N'Выключатель IP44', N'шт', 7),
('SND-01', N'Песок строительный', N'т', 1),
('SHCH-20', N'Щебень фракция 5-20', N'т', 1),
('PLST-01', N'Штукатурка (30 кг)', N'меш', 6),
('SDF-10', N'Саморез по металлу', N'тыс.шт', 5);

-- Остатки 
-- 1. ЦЕНТРАЛЬНАЯ БАЗА (WarehouseId = 1)
INSERT INTO Stocks (ProductId, WarehouseId, Quantity) VALUES 
(1, 1, 2500.00), (2, 1, 45.20), (3, 1, 15000.00), (5, 1, 300.00), 
(9, 1, 50.00), (11, 1, 800.00), (13, 1, 1200.00), (15, 1, 400.00);

-- 2. ОБЪЕКТ "ЖК СЕВЕРНЫЙ" (WarehouseId = 2)
INSERT INTO Stocks (ProductId, WarehouseId, Quantity) VALUES 
(1, 2, 85.00), (2, 2, 0.85), (4, 2, 1200.00), (8, 2, 150.00), 
(10, 2, 15.00), (15, 2, 3.00), (6, 2, 50.00);

-- 3. ОБЪЕКТ "СИТИ-ЦЕНТР" (WarehouseId = 3)
INSERT INTO Stocks (ProductId, WarehouseId, Quantity) VALUES 
(4, 3, 2500.00), (11, 3, 120.00), (12, 3, 45.00), (16, 3, 5000.0), 
(5, 3, 15.00), (14, 3, 2.10);

SET DATEFORMAT ymd;

-- Журнал операций (Приход и Расход)
INSERT INTO SupplyLogs (ProductId, WarehouseId, UserId, OperationType, Quantity, OperationDate, Comment) VALUES 
-- БАЗА "ЦЕНТРАЛЬНАЯ" (Склад 1)
(1, 1, 2, N'Приход', 3000.00, '2026-03-20', N'Поступление бетона от поставщика'),
(1, 1, 2, N'Расход', 500.00,  '2026-03-21', N'Отгрузка на сторонний объект'),
(2, 1, 2, N'Приход', 50.00,   '2026-03-22', N'Приемка арматуры (партия А-1)'),
(2, 1, 2, N'Расход', 4.80,    '2026-03-23', N'Выдача на резку'),

-- ОБЪЕКТ "ЖК СЕВЕРНЫЙ" (Склад 2)
(4, 2, 3, N'Приход', 1500.00, '2026-03-24', N'Завоз газобетона'),
(4, 2, 3, N'Расход', 300.00,  '2026-03-25', N'Расход на возведение стен 1 этажа'),
(8, 2, 3, N'Приход', 200.00,  '2026-03-24', N'Поставка клея для блоков'),
(8, 2, 3, N'Расход', 50.00,   '2026-03-25', N'Использовано при кладке'),

-- ОБЪЕКТ "СИТИ-ЦЕНТР" (Склад 3)
(11, 3, 3, N'Приход', 200.00, '2026-03-26', N'Кабель ВВГнг (бухты)'),
(11, 3, 3, N'Расход', 80.00,  '2026-03-27', N'Монтаж временного освещения'),
(12, 3, 3, N'Приход', 45.00,  '2026-03-28', N'Выключатели IP44'),
(16, 3, 3, N'Приход', 5000.00,'2026-03-29', N'Саморезы по металлу');