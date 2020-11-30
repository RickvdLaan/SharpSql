![.NET Core - ORM Unit Tests](https://github.com/Albileon/ORM/workflows/.NET%20Core%20-%20ORM%20Unit%20Tests/badge.svg?branch=master)

**Summary**

A pre-alpha O/R mapping framework for .NET Standard 2.1, licensed under the MIT license. 

## Table of contents

* [Chapter 1. Getting started](#chapter-1-getting-started)
* [Chapter 2. Configuration](#chapter-2-configuration)
* [Chapter 3. CRUD operations](#chapter-3-crud-operations)
	* [3.1 Create](#31-create)
	* [3.2 Read](#32-read)
		* [3.2.1 Basic fetch](#321-basic-fetch)
		* [3.2.2 Select](#322-select)
		* [3.2.3 Join](#323-join)
		* [3.2.4 Where](#324-where)
		* [3.2.5 OrderBy](#325-orderby)
		* [3.2.6 Many-to-many relations](#326-many-to-many-relations)
	* [3.3 Update](#33-update)
	* [3.4 Delete](#34-delete)
* [Chapter 4. Direct queries](#chapter-4-direct-queries)
* [Chapter 5. Virtual methods](#chapter-5-virtual-methods)
	* [5.1 ORMObject](#51-ormobject)
	* [5.2 ORMEntity](#52-ormentity)
	* [5.3 ORMCollection](#53-ormcollection)
* [Chapter 6. Attributes](#chapter-6-attributes)
	* [6.1 ORMColumnAttribute](#61-ormcolumnattribute)
	* [6.2 ORMPrimaryKeyAttribute](#62-ormprimarykeyattribute)
	* [6.3 ORMTableAttribute](#63-ormtableattribute)
	* [6.4 ORMUnitTestAttribute](#64-ormunittestattribute)
* [Chapter 7. Specifications](#chapter-7-specifications)
	* [7.1 Version information](#71-version-information)
	* [7.2 Supported databases](#72-supported-databases)
	* [7.3 Supported .NET versions](#73-supported-net-versions)

## Chapter 1. Getting started

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
    .AddJsonFile("appsettings.json")
    .Build();

new ORMInitialize(configuration);

```

For a more detailed step on setting up the configuration see *[ Chapter 2. Configuration](#chapter-2-configuration)*.

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

And that's it for the regular tables! With this set-up you're able to perform all CRUD (Create, Read, Update and Delete) actions on your table. See *[ Chapter 3. CRUD operations](#chapter-3-crud-operations)* for more examples regarding all the CRUD actions or check out *[ Chapter 5. Virtual methods](#chapter-5-virtual-methods)* to see what else can be expanded on.

**Step 6.**  Many-to-many relations - this requires the use of the same ```ORMTable``` attribute, but with a different constructor. In this example we'll use the previously delcared Users and User types and a collection of type Roles with entity type Role with the parameters Id as primary key and Name which will be the name of the role itself and so creating the many-to-many table UserRoles. The constructor requires the following parameters: the collection type of the current many-to-many class (in this case UserRoles), the entity type of the current many-to-many class (in this case UserRole) the first collection class (in this case Users) and the second collection class (in this case Roles).

```cs
[ORMTable(typeof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles))]
public class UserRoles : ORMCollection<UserRole>
{
    public UserRole() { }
}
```

Next we'll set-up the basic UserRole entity class and we'll add the primary keys as parameters to the constructor and call the ```base.FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>()``` to be able to fetch specific records.

```cs
public class UserRole : ORMEntity
{
    [ORMPrimaryKey]
    public int UserId { get; private set; }

    [ORMPrimaryKey]
    public int RoleId { get; private set; }

    public UserRole(int userId, int roleId)
    {
        base.FetchEntityByCombinedPrimaryKey<UserRoles, UserRole>(userId, roleId);
    
        User = new User(UserId);
        Role = new RoleEntity(RoleId);
    }
}
```
Now we have a many-to-many relation set-up with basic functionalities and accessability. For information on how many-to-many relations work within the framework and what else can be done with them see *[ 3.2.6 Many-to-many relations](#326-many-to-many-relations)*.

*[ Back to top](#table-of-contents)*

## Chapter 2. Configuration

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
    .AddJsonFile("appsettings.json")
    .Build();

new ORMInitialize(configuration);
```

After that point, your code base will be able to communicate with your database.

*[ Back to top](#table-of-contents)*

## Chapter 3. CRUD operations

```cs
Todo
```

### 3.1. Create

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 3.2. Read

```cs
Todo
```

*[ Back to top](#table-of-contents)*

#### 3.2.1 Basic fetch

When you want to fetch all the data from a specific collection class, you can easily do so with the following lines of code:

```cs
var users = new Users();
users.Fetch();
```

This will result in the following query:

```sql
SELECT * FROM [DBO].[USERS] AS [U];
```

It's also possible to specify how many records you want to fetch with the parameter ```maxNumberOfItemsToReturn``` in the ```Fetch()``` method.

```cs
var users = new Users();
users.Fetch(10);
```

This will result in the following query:

```sql
SELECT TOP (10) * FROM [DBO].[USERS] AS [U];
```

And as you may have noticed: ```SELECT *``` is being generated, this is because no columns have been specified. If you do want to get only a certain amount of columns you can do this through the ```Select()``` method, see *[ 3.2.2 Select](#322-select)*.

If you want to count the amount of rows you have fetched from the specified table you can use ```users.Collection.Count```. But if you want to know the amount of records in the database table it's quite inefficient to first fetch all the data and then count it. This could be achieved through the static ```Records()``` method on the collection class.

```cs
var records = Users.Records();
```

This will result in the following query:

```sql
SELECT COUNT(*) FROM USERS AS INT;
```

*[ Back to top](#table-of-contents)*

#### 3.2.2 Select

The collection class has a method ```Select()``` which can be used to specify which column names you want to return, let's say we want to fetch all users with only the column ```Username```.

```cs
var users = new Users();
users.Select(x => x.Username);
users.Fetch();
```

This will result in the following query:

```sql
SELECT [U].[USERNAME] FROM [DBO].[USERS] AS [U];
```

If you want to provide more than one column, you have to provide an ```object[]``` with the columns you wish to return.

```cs
var users = new Users();
users.Select(x => new object[] { x.Username, x.Password });
users.Fetch();
```

This will result in the following query:

```sql
SELECT [U].[USERNAME], [U].[PASSWORD] FROM [DBO].[USERS] AS [U];
```

*[ Back to top](#table-of-contents)*

#### 3.2.3 Join

The collection ```Left()``` or ```Inner()``` methods can be used to retrieve the information of subobjects and fill them. The other operators (Where and OrderBy can also be applied to the joined object.

```cs
var users = new Users();
users.Join(x => x.Organisation.Left());
users.Fetch();
```

This will result in the following query:

```sql
SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];
```

*[ Back to top](#table-of-contents)*

#### 3.2.4 Where

```cs
Todo
```

*[ Back to top](#table-of-contents)*

#### 3.2.5 OrderBy

We can also order our data before retrieving it through the ```OrderBy()``` method. In this method we can order on each column with the ```Ascending()``` and ```Descending()``` methods. 

```cs
var users = new Users();
users.OrderBy(x => x.Username.Descending());
users.Fetch();
```

This will result in the following query:

```sql
SELECT * FROM [DBO].[USERS] AS [U] ORDER BY [U].[USERNAME] DESC;
```

If you want to order by more than one column, you have to provide an ```object[]``` with the columns you wish to order.

```cs
var users = new Users();
users.OrderBy(x => new object[] { x.Username.Descending(), x.Organisation.Ascending() });
users.Fetch();
```

This will result in the following query:

```sql
SELECT * FROM [DBO].[USERS] AS [U] ORDER BY [U].[USERNAME] DESC, [U].[ORGANISATION] ASC;
```

*[ Back to top](#table-of-contents)*

#### 3.2.6 Many-to-many relations

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 3.3. Update

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 3.4. Delete

```cs
Todo
```

*[ Back to top](#table-of-contents)*

## Chapter 4. Direct queries

```cs
Todo
```

*[ Back to top](#table-of-contents)*

## Chapter 5. Virtual methods

```cs
Todo
```

### 5.1. ORMObject

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 5.2 ORMEntity

In [Chapter 1. Getting started](#chapter-1-getting-started) (*Step 5.*) we left off with a fairly basic entity class, let's expand on this entity class by adding two more properties to our entity: DateCreated and DateLastModified.

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

The ```ORMEntity``` class provides multiple virtual methods, so let's say we want to override the ```Save()``` method to change it's behaviour when posting data to the database by always setting the DateCreated to the current date and time, including the DateLastModified.

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

### 5.3. ORMCollection<ORMEntity>

```cs
Todo
```

*[ Back to top](#table-of-contents)*

## Chapter 6. Attributes

Within this framework we have created multiple attributes. In this chapter we'll explain how each attribute can be used and how the attributes are used within the framework itself.

### 6.1 ORMColumnAttribute

Sometimes you want to name your entity property different than the actual column name, to achieve this you can use the ```ORMColumn``` attribute. The framework will automatically assume the name of the property is the same name as the column name, when it doesn't find any matches it'll try and resolve it through the ```ORMColumn``` attribute and throws an ```NotImplementedException``` when neither was found.

```cs
[ORMColumn(ColumnName)]
public string Description { get; private set; }
```

*[ Back to top](#table-of-contents)*

### 6.2 ORMPrimaryKeyAttribute

To tell the framework what the primary or shared key of the table is, you can use the ```ORMPrimaryKey``` attribute. If there is a shared primary key, it'll map them in the same top-to-down order from the entity class, this means that any parameters regarding the primary keys which are passed on to the framework has to be passed in the exact same order.

```cs
// a single primary key:

[ORMPrimaryKey]
public int Id { get; private set; } = -1;

// a shared primary key:

[ORMPrimaryKey]
public int UserId { get; private set; }

[ORMPrimaryKey]
public int RoleId { get; private set; }
```

*[ Back to top](#table-of-contents)*

### 6.3 ORMTableAttribute

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 6.4 ORMUnitTestAttribute

The ```ORMUnitTest``` attribute is an internally used attribute. This project make use of the NUnit testing framework for all of our unit tests and the project is named "ORMNUnit", which has access to all of the internal classes, methods, properties and variables through the ```ORMUnitTest``` attribute which is used on the initialization class.

```cs
[SetUpFixture, ORMUnitTest]
internal class NUnitSetupFixture
{
    [OneTimeSetUp]
    public void Initialize()
    {
        var memoryEntityTables = new List<string>()
        {
            "MemoryEntityTables/USERS.xml",
            "MemoryEntityTables/ORGANISATIONS.xml"
        };

        var memoryCollectionTables = new List<string>()
        {
            "MemoryCollectionTables/BasicFetchUsers.xml",
            "MemoryCollectionTables/BasicFetchTopUsers.xml",
            "MemoryCollectionTables/BasicJoinInner.xml",
            "MemoryCollectionTables/BasicSelectUsers.xml",
            "MemoryCollectionTables/BasicJoinLeft.xml",
            "MemoryCollectionTables/BasicOrderBy.xml",
            "MemoryCollectionTables/BasicWhereAnd.xml",
            "MemoryCollectionTables/BasicWhereLessThanOrEqual.xml",
            "MemoryCollectionTables/BasicWhereGreaterThanOrEqual.xml",
            "MemoryCollectionTables/ComplexJoin.xml",
            "MemoryCollectionTables/ComplexWhereLike.xml"
        };

        _ = new ORMInitialize(memoryEntityTables, memoryCollectionTables);
    }
}
```

*[ Back to top](#table-of-contents)*

## Chapter 7. Specifications

All of the specifications of the ORM framework.

### 7.1 Version information

The latest version of this framework is version alpha-0.1, released on 2020-11-30.

### 7.2 Supported databases

SQL Server 2005 or higher

### 7.3 Supported .NET versions

NET Standard 2.1., .NET Core 3.1.

*[ Back to top](#table-of-contents)*
