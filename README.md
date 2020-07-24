![.NET Core - ORM Unit Tests](https://github.com/Albileon/ORM/workflows/.NET%20Core%20-%20ORM%20Unit%20Tests/badge.svg?branch=master)

**Summary**

A simple open source ORM Framework written in .NET Standard 2.0.

## Table of contents

* [Getting started](#getting-started)
* [Examples](#examples)
	* [Chapter 1. Configuration](#chapter-1-configuration)
	* [Chapter 2. CRUD](#chapter-2-crud)
	* [Chapter 3. Virtual methods](#chapter-3-virtual-methods)
		* [Chapter 3.1 ORMObject](#chapter-31-ormobject)
		* [Chapter 3.2 ORMEntity](#chapter-32-ormentity)
		* [Chapter 3.3 ORMCollection](#chapter-33-ormcollection)
* [Specifications](#specifications)
	* [Version information](#version-information)
	* [Supported databases](#supported-databases)
	* [Supported .NET versions](#supported-net-versions)

## Getting started

```cs
Todo
```

**Step 1.** Install the ORM Framework via the NuGet package: [ORM Framework](https://www.nuget.org/packages/Todo/)

```
PM> Install-Package Todo
```

**Step 2.**

Initialize the ORM Framework somewhere as follows:

```cs

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

new ORMInitialize(configuration);

```

For a more detailed step on setting up the configuration see *[ Chapter 1. Configuration](#chapter-1-configuration)*.

**Step 3.** The framework works with both a code first and database first approach, and coding your database is fairly straightforward - for each table in your database you'll create a collection class and give this class the same name as your table. Then place the ```ORMTable``` attribute above the class with the following parameters: the type of the current collection class, and the type of the entity class (*see Step 4.*). And as a last step, inherit from the ```ORMCollection<EntityType>``` class and your collection class is all set!

```cs
// The collection class of the database table Users.
[ORMTable(typeof(Users), typeof(User))]
public class Users : ORMCollection<User>
{
    public Users() { }
}
```

**Step 4.** As seen in the previous step (*Step 3.*) every collection class also requires an entity class, create an entity class (name can't be the same as an existing column name) and inherit from the ```ORMEntity``` class. After that - create a property for each column in the table and provide it with a getter and setter (setters are allowed to be private) and mark the primary key with the ```ORMPrimaryKey``` attribute. In this example, we have an Id as primary key (the default -1 is not mandatory), a username, password and an organisation whereas the organisation is a foreign key (join) and last, an empty constructor for the entity class.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : ORMEntity
{
    [ORMPrimaryKey]
    public int Id { get; private set; } = -1;

    public string Username { get; set; }

    public string Password { get; set; }

    public Organisation Organisation { get; set; }

    public User() {}
}
```

**Step 5.** The base class of ORMEntity provides one optional parameter for the constructor: 
```cs
protected ORMEntity(bool disableChangeTracking = false) { }
```
With this parameter you can provide whether or not you want to enable or disable ```DisableChangeTracking``` (```false``` by default). Note that disabling change tracking causes the ```IsDirty``` property to always return true, because then the framework has to assume changes were made to the object.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : ORMEntity
{
    [ORMPrimaryKey]
    public int Id { get; private set; } = -1;

    public string Username { get; set; }

    public string Password { get; set; }

    public Organisation Organisation { get; set; }

    public User() { }

    public User(int fetchByUserId, bool disableChangeTracking = false) 
        : base(disableChangeTracking)
    {
        base.FetchEntityById<Users, User>(fetchByUserId);
    }
}
```

And that's it for the regular tables! With this set-up you're able to perform all CRUD (Create, Read, Update and Delete) actions on your table. See *[ Chapter 2. CRUD](#chapter-2-crud)* for more examples regarding all the CRUD actions or check out *[ Chapter 3. Virtual methods](#virtual-method)* to see what else can be expanded on.

**Step 6.**  Many-to-many relations - this requires the use of the same ```ORMTable``` attribute, but with a different constructor. In this example we'll use the previously delcared Users and User types and a collection of type Roles with entity type Role with the parameters Id as primary key and Name which will be the name of the role itself and so creating the many-to-many table UserRoles. The constructor requires the following parameters: the collection type of the current many-to-many class (in this case UserRoles), the entity type of the current many-to-many class (in this case UserRole) the first collection class (in this case Users) and the second collection class (in this case Roles).

```cs
[ORMTable(typeof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles))]
public class UserRoles : ORMCollection<UserRole>
{
    public UserRole() { }
}
```

Next we'll set-up the basic UserRole entity class.

```cs
public class UserRole : ORMEntity
{
    [ORMPrimaryKey]
    public int UserId { get; private set; }

    [ORMPrimaryKey]
    public int RoleId { get; private set; }

    public UserRole(int userId, int roleId) { }
}
```

Now that's done, let's add a few more things to make the class more usefull, for starters - let's add the primary keys as parameters to the constructor and call the ```base.FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>()``` to be able to get specific records. After that, let's add a User and Role property with a getter and setter and initialize them by calling the previously made constructors. The framework can't automatically join these properties, due to them not being columns in the table.

```cs
public class UserRole : ORMEntity
{
    [ORMPrimaryKey]
    public int UserId { get; private set; }

    [ORMPrimaryKey]
    public int RoleId { get; private set; }

    public User User { get; private set; }

    public Role Role { get; private set; }
    
    public UserRole(int userId, int roleId)
    {
        base.FetchEntityByCombinedPrimaryKey<UserRoles, UserRole>(userId, roleId);
    
        User = new User(UserId);
        Role = new RoleEntity(RoleId);
    }
}
```
Now we have a many-to-many relation set-up with basic functionalities and accessability.

*[ Back to top](#table-of-contents)*

## Examples

```cs
Todo
```

### Chapter 1. Configuration

First create an appsettings.json file in your project folder, and set your ConnectionStrings:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost; Database=ORM; Trusted_Connection=True; MultipleActiveResultSets=true"
    }
}
```

For a more detailed guide on creating connection strings, see Microsoft's documentation: *[Creating a Connection String](https://docs.microsoft.com/en-us/sql/ado/guide/data/creating-a-connection-string?view=sql-server-ver15)*.

Next initialize the ORM Framework somewhere once as follows:

```cs

IConfiguration configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
	.Build();

new ORMInitialize(configuration);
```

After that point, your code base will be able to communicate with your database.

*[ Back to top](#table-of-contents)*

### Chapter 2. CRUD

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### Chapter 3. Virtual methods

```cs
Todo
```

#### Chapter 3.1. ORMObject

```cs
Todo
```

*[ Back to top](#table-of-contents)*

#### Chapter 3.2 ORMEntity

In [Getting started](#getting-started) *Step 5.* we left off with a fairly basic entity class, let's expand on this entity class by adding two more properties to our entity: DateCreated and DateLastModified.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : ORMEntity
{
    [ORMPrimaryKey]
    public int Id { get; private set; } = -1;

    public string Username { get; set; }

    public string Password { get; set; }

    public Organisation Organisation { get; set; }
	
    public DateTime DateCreated { get; set; }
    
    public DateTime DateLastModified { get; set; }

    public User() { }

    public User(int fetchByUserId, bool disableChangeTracking = false) 
        : base(disableChangeTracking)
    {
        base.FetchEntityById<Users, User>(fetchByUserId);
    }
}
```

The ```ORMEntity``` class provides multiple virtual methods, so let's say we want to override the Save method to change it's behaviour when posting data to the database by always setting the DateCreated to the current date and time, including the DateLastModified.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : ORMEntity
{
    [ORMPrimaryKey]
    public int Id { get; private set; } = -1;

    public string Username { get; set; }

    public string Password { get; set; }

    public Organisation Organisation { get; set; }
	
    public DateTime DateCreated { get; set; }

    public DateTime DateLastModified { get; set; }

    public User() { }

    public User(int fetchByUserId, bool disableChangeTracking = false) 
        : base(disableChangeTracking)
    {
        base.FetchEntityById<Users, User>(fetchByUserId);
    }
	
    public override void Save()
    {
        if (IsDirty)
        {
            DateLastModified = DateTime.Now;

            if (IsNew)
            {
                DateCreated = DateLastModified;
            }
        }

        base.Save();
    }
}
```

*[ Back to top](#table-of-contents)*

#### Chapter 3.3. ORMCollection<ORMEntity>

```cs
Todo
```

*[ Back to top](#table-of-contents)*

## Specifications

All of the specifications of the ORM framework.

### Version information

The latest version of this framework is version 1.0, released on [date].

### Supported databases

SQL Server 2005 or higher

### Supported .NET versions

NET Standard 2.0, .NET Standard 2.1., .NET Core 3.0, .NET Core 3.1.

*[ Back to top](#table-of-contents)*
