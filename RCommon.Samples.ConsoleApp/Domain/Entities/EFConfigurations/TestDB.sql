
/****** Object:  Table [dbo].[MonthlySalesSummary]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[MonthlySalesSummary]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [MonthlySalesSummary](
	[Year] [int] NOT NULL,
	[Month] [int] NOT NULL,
	[SalesPersonId] [int] NOT NULL,
	[Amount] [decimal](19, 5) NULL,
	[Currency] [nvarchar](255) NULL,
	[SalesPersonFirstName] [nvarchar](255) NULL,
	[SalesPersonLastName] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Year] ASC,
	[Month] ASC,
	[SalesPersonId] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Departments]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[Departments]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [Departments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Customers]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[Customers]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [Customers](
	[CustomerID] [int] IDENTITY(1,1) NOT NULL,
	[StreetAddress1] [nvarchar](255) NULL,
	[StreetAddress2] [nvarchar](255) NULL,
	[City] [nvarchar](255) NULL,
	[State] [nvarchar](255) NULL,
	[ZipCode] [nvarchar](255) NULL,
	[FirstName] [nvarchar](255) NULL,
	[LastName] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Products]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[Products]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [Products](
	[ProductID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NULL,
	[Description] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ProductID] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[SalesTerritory]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[SalesTerritory]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [SalesTerritory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NULL,
	[Description] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[SalesPerson]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[SalesPerson]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [SalesPerson](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](255) NULL,
	[LastName] [nvarchar](255) NULL,
	[SalesQuota] [real] NULL,
	[SalesYTD] [decimal](19, 5) NULL,
	[DepartmentId] [int] NULL,
	[TerritoryId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Orders]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[Orders]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [Orders](
	[OrderID] [int] IDENTITY(1,1) NOT NULL,
	[OrderDate] [datetime] NULL,
	[ShipDate] [datetime] NULL,
	[CustomerId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderID] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[OrderItems]    Script Date: 03/28/2010 18:18:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[OrderItems]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE [OrderItems](
	[OrderItemID] [int] IDENTITY(1,1) NOT NULL,
	[Price] [decimal](19, 5) NULL,
	[Quantity] [int] NULL,
	[Store] [nvarchar](255) NULL,
	[ProductId] [int] NULL,
	[OrderId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderItemID] ASC
) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  ForeignKey [FK_OrderItems_Product]    Script Date: 03/28/2010 18:18:35 ******/
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK_OrderItems_Product]') AND type = 'F')
ALTER TABLE [OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItems_Product] FOREIGN KEY([ProductId])
REFERENCES [Products] ([ProductID])
GO
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK_OrderItems_Product]') AND type = 'F')
ALTER TABLE [OrderItems] CHECK CONSTRAINT [FK_OrderItems_Product]
GO
/****** Object:  ForeignKey [FK_Orders_OrderItems]    Script Date: 03/28/2010 18:18:35 ******/
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK_Orders_OrderItems]') AND type = 'F')
ALTER TABLE [OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_Orders_OrderItems] FOREIGN KEY([OrderId])
REFERENCES [Orders] ([OrderID])
GO
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK_Orders_OrderItems]') AND type = 'F')
ALTER TABLE [OrderItems] CHECK CONSTRAINT [FK_Orders_OrderItems]
GO
/****** Object:  ForeignKey [FK_Customer_Orders]    Script Date: 03/28/2010 18:18:35 ******/
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK_Customer_Orders]') AND type = 'F')
ALTER TABLE [Orders]  WITH CHECK ADD  CONSTRAINT [FK_Customer_Orders] FOREIGN KEY([CustomerId])
REFERENCES [Customers] ([CustomerID])
GO
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK_Customer_Orders]') AND type = 'F')
ALTER TABLE [Orders] CHECK CONSTRAINT [FK_Customer_Orders]
GO
/****** Object:  ForeignKey [FK74214A90B23DB0A3]    Script Date: 03/28/2010 18:18:35 ******/
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK74214A90B23DB0A3]') AND type = 'F')
ALTER TABLE [SalesPerson]  WITH CHECK ADD  CONSTRAINT [FK74214A90B23DB0A3] FOREIGN KEY([TerritoryId])
REFERENCES [SalesTerritory] ([Id])
GO
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK74214A90B23DB0A3]') AND type = 'F')
ALTER TABLE [SalesPerson] CHECK CONSTRAINT [FK74214A90B23DB0A3]
GO
/****** Object:  ForeignKey [FK74214A90E25FF6]    Script Date: 03/28/2010 18:18:35 ******/
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK74214A90E25FF6]') AND type = 'F')
ALTER TABLE [SalesPerson]  WITH CHECK ADD  CONSTRAINT [FK74214A90E25FF6] FOREIGN KEY([DepartmentId])
REFERENCES [Departments] ([Id])
GO
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[FK74214A90E25FF6]') AND type = 'F')
ALTER TABLE [SalesPerson] CHECK CONSTRAINT [FK74214A90E25FF6]
GO
