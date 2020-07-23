![.NET Core - ORM Unit Tests](https://github.com/Albileon/ORM/workflows/.NET%20Core%20-%20ORM%20Unit%20Tests/badge.svg?branch=master)

**Summary**

Todo

## Table of contents

* [Getting started](#getting-started)
* [Examples](#examples)

## Getting started

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

**Step 3.** The framework works with both a code first and database first approach, and coding your database is fairly straightforward - for each table in your database you'll create a collection class and give this class the same name as your table. Then place the ORMTableAttribute above the class with the following parameters: the type of the current collection class, and the type of the entity class (*see Step 4.*). And as a last step, inherit from the ```ORMCollection<EntityType>``` class and your collection class is all set!

```cs
// The collection class of the database table Users.
[ORMTable(typeof(Users), typeof(User))]
public class Users : ORMCollection<User>
{
	public Users() { }
}
```

**Step 4.** As seen in the previous step (*Step 3.*) every collection class also requires an entity class, create an entity class (name is not important) and inherit from the ORMEntity. After that - create a property for each column in the table and provide it with a getter and setter (setters are allowed to be private). In this example, we have an Id as primary key (the default -1 is not mandatory), a username, password and an organisation whereas the organisation is a foreign key (join) and last, an empty constructor for the entity class.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : ORMEntity
{
	public int Id { get; private set; } = -1;

	public string Username { get; set; }

	public string Password { get; set; }

	public Organisation Organisation { get; set; }

	public User() {}
}
```

**Step 5.** The base class of ORMEntity provides two different (optional) parameters for the constructor: 
```cs
protected ORMEntity(string primaryKeyName = default, bool disableChangeTracking = false) { }
```
The first one is the name of the primary key and the second one is whether or not you want to disable change tracking. If you provide the base with the name of the primary key, you don't have to implement your own fetch enttiy by id. And with the second parameter, you can provide whether or not you want to be able to enable or disable change tracking (enabled by default). Note that disabling change tracking causes the IsDirty property to always return true, since the framework has to assume changes were made to the object.

That's it for the regular tables, with this setup you're able to perform all CRUD (Create, Read, Update and Delete) actions on your table. See *[ Chapter 2. CRUD](#chapter-2-crud)* for examples on all the CRUD actions.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : ORMEntity
{
	public int Id { get; private set; } = -1;

	public string Username { get; set; }

	public string Password { get; set; }

	public Organisation Organisation { get; set; }

	public User() { }

	public User(int fetchByUserId) : base(nameof(Id))
        {
            base.FetchEntityById<Users, User>(fetchByUserId);
        }

        public User(int fetchByUserId, bool disableChangeTracking) : base(nameof(Id), disableChangeTracking)
        {
            base.FetchEntityById<Users, User>(fetchByUserId);
        }
}
```

**Step 6.**  Many-to-many relations - this requires the use of the same ORMTableAttribute, but with a different constructor. In this example we'll use the previously delcared Users and User types with an collection of type Roles with entity type Role, creating the many-to-many table UserRole. The constructor requires the following parameters: the type of the current many-to-many class (in this case UserRole), the first collection class (in this case Users) and the second collection class (in this case Roles).

```cs
[ORMTable(typeof(UserRole), typeof(Users), typeof(Roles))]
public class UserRole
{
	public UserRole() { }
}
```

## Examples

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

### Chapter 2. CRUD

Fetching data examples:

```cs

// Todo

```
