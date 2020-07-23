Super awesome, world class ORM, just kidding, it's probably awful.

* Main: ![.NET Core - ORM Unit Tests](https://github.com/Albileon/ORM/workflows/.NET%20Core%20-%20ORM%20Unit%20Tests/badge.svg?branch=master)

**Summary**

Todo

## Content

* [Getting started](#getting-started)

## Getting started

**Step 1.** Install the ORM Framework via the NuGet package: [ORM Framework](https://www.nuget.org/packages/Todo/)

```
PM> Install-Package Todo
```

**Step 2.**

Initialize the ORM Framework once somewhere as follows:

```cs

IConfiguration configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
  .Build();

new ORMInitialize(configuration);

```

**Step 3.** Coding your database is fairly simple - for each table in your database you'll create a collection class and give this class the same name as your table. Then place the ORMTableAttribute above the class with the following parameters: the type of the current collection class, and the type of the entity class (see Step 4.). And last, inherit from the ORMCollection<EntityType> class and your collection class is done!

```cs
// The collection class of the database table Users.
[ORMTable(typeof(Users), typeof(User))]
public class Users : ORMCollection<User>
{
	public Users() { }
}
```

**Step 4.** As seen in the previous step (Step 3.) every collection class also requires an entity class, create an entity class (name is not important) and inherit from the ORMEntity. After that - create a property for each column in the table and provide it with a getter and setter (setters are allowed to be private). In this example, we have an Id as primary key (the default -1 is not mandatory), a username, password and an organisation whereas the organisation is a foreign key (join) and last, an empty constructor for the entity class.

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

**Examples**

A configuration example:

```cs

IConfiguration configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
  .Build();

  new ORMInitialize(configuration);
```

Fetching data examples:

```cs

// Todo

```
