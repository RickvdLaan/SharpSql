create table Users (
    Id int not null primary key,
    Username nvarchar(128) not null,
    Password nvarchar(128) not null,
	Organisation int
)
go

create table Roles (
   Id int not null primary key,
   Role nvarchar(50)
)
go

create table UserRoles (
    UserId int not null,
    RoleId int not null,
    primary key(UserId, RoleId)
)
go

create table Organisations (
    Id int not null primary key,
    Name nvarchar(128) not null
)
go

alter table UserRoles with check add constraint FK_UserRoles_Roles 
foreign key (RoleID) references Roles (Id)

alter table UserRoles with check add constraint FK_UserRoles_Users 
foreign key (UserID) references Users (Id)

alter table Users with check add constraint FK_Users_Organisations
foreign key (Organisation) references Organisations (Id)

INSERT INTO Organisations (
	Id,
	Name
)Values
(1,'Rocket');

INSERT INTO Users (
	Id,
	Username,
    Password,
	Organisation
)Values
(1,'James','password',1),
(2,'Jessie','pass',1),
(3,'Meowth','pwd',1);