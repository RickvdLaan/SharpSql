CREATE DATABASE [SHARPSQL]
GO

USE [SHARPSQL]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Organisations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK__Organisa__3214EC078222B24E] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [int] NOT NULL,
	[Name] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](128) NOT NULL,
	[Password] [nvarchar](128) NOT NULL,
	[Organisation] [int] NULL,
	[DateCreated] [datetime] NULL,
	[DateLastModified] [datetime] NULL,
 CONSTRAINT [PK__Users__3214EC0788A1C5BB] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Organisations] ON 
GO
INSERT [dbo].[Organisations] ([Id], [Name]) VALUES (1, N'The Boring Company')
GO
INSERT [dbo].[Organisations] ([Id], [Name]) VALUES (2, N'SpaceX')
GO
SET IDENTITY_INSERT [dbo].[Organisations] OFF
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (1, N'Admin')
GO
INSERT [dbo].[Roles] ([Id], [Name]) VALUES (2, N'Moderator')
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (1, 1)
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (1, 2)
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (2, 1)
GO
INSERT [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (3, 2)
GO
SET IDENTITY_INSERT [dbo].[Users] ON 
GO
INSERT [dbo].[Users] ([Id], [Username], [Password], [Organisation], [DateCreated], [DateLastModified]) VALUES (1, N'Imaani', N'qwerty', 1, CAST(N'2020-07-23T16:50:38.213' AS DateTime), CAST(N'2020-07-23T16:50:38.213' AS DateTime))
GO
INSERT [dbo].[Users] ([Id], [Username], [Password], [Organisation], [DateCreated], [DateLastModified]) VALUES (2, N'Clarence', N'password', 1, CAST(N'2020-07-23T16:50:38.213' AS DateTime), CAST(N'2020-07-23T16:50:38.213' AS DateTime))
GO
INSERT [dbo].[Users] ([Id], [Username], [Password], [Organisation], [DateCreated], [DateLastModified]) VALUES (3, N'Beverley', N'abc123', 2, CAST(N'2020-07-23T16:50:38.213' AS DateTime), CAST(N'2020-07-23T16:50:38.213' AS DateTime))
GO
INSERT [dbo].[Users] ([Id], [Username], [Password], [Organisation], [DateCreated], [DateLastModified]) VALUES (4, N'Adyan', N'123456', NULL, CAST(N'2020-07-23T16:50:38.213' AS DateTime), CAST(N'2020-07-23T16:50:38.213' AS DateTime))
GO
INSERT [dbo].[Users] ([Id], [Username], [Password], [Organisation], [DateCreated], [DateLastModified]) VALUES (5, N'Chloe', N'dragon', 2, NULL, NULL)
GO
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Organisations] FOREIGN KEY([Organisation])
REFERENCES [dbo].[Organisations] ([Id])
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Organisations]
GO
