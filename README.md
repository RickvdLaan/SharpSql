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

Initialize the ORM Framework as follows:

```cs

IConfiguration configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
  .Build();

new ORMInitialize(configuration);

```

**Step 3.** Code your database as follows:

```cs
// The Collection class.
[ORMTable(typeof(Users), typeof(User))]
public class Users : ORMCollection<User>
{
	public Users() { }
}

// The Entity class.
public class User : ORMEntity
{
  public int Id { get; private set; } = -1;

	public string Username { get; set; }

	public string Password { get; set; }

	public Organisation Organisation { get; set; }

	public User() : base(nameof(Id)) { }

	public User(int fetchByUserId) : base(nameof(Id))
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
