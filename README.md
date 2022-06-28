![ORM Unit Tests](https://img.shields.io/github/workflow/status/RickvdLaan/ORM/ORM%20Unit%20Tests?label=unit%20tests&logo=GitHub)
[![License](https://img.shields.io/github/license/RickvdLaan/ORM?color=dark-green)](LICENSE)
![Top Language](https://img.shields.io/github/languages/top/RickvdLaan/ORM)
![Code Size](https://img.shields.io/github/languages/code-size/RickvdLaan/ORM)
![Repo Size](https://img.shields.io/github/repo-size/RickvdLaan/ORM)

**Summary**

A beta O/R mapping framework for .NET 6.0, licensed under the MIT license.

This project is pure a hobby project - with certain goals and limitations for the developers. The limitations we've set ourselves are as follows:
- Only use official documentation for the used software, programming languages and libraries;
- No Google, stackoverflow, et cetera.

Even though the last rule might seem a bit strange, since in a real world scenario you'd make use of this resource. We did set this rule in place for a reason; mainly to challenge ourselves and to tackle and solve the problems together we find along the ways. Rather than quickly finding the solution to our problems and learn nothing.

One time during the project we've made an exception. We ran into a problem with linq expression trees and our unit tests (our [question](https://stackoverflow.com/questions/66017666/c-sharp-net-standard-expression-tree-value-not-parsed-as-expected)) and we're unable to understand the problem. After asking for an explaination on stackoverflow  someone helped us understand the underlying problem by explaining 'closing' variables in c# and by providing us with this [SharpLab IO](https://sharplab.io/#v2:D4AQTAjAsAUCAMACEEB0AZAlgOwI6oFEAPABwCcBTAZyswHtsqBuWWbAQwFtqT2BjCogDyAJQCyAOQCu2TABdEAb1gBIEAGZkYYeIDCdADYGKfOfWwAVanKpLVazSAAsiYuWq0GAfQBi7TAYAFACUdjAqEQBu7GSIUlQUZBzciAC8iABEctYZLOERAOoAFokUgURpAHyIFanp8YnJFMF5EQC+rPkayC5ulDTmXgDKUnwCNCFhESrFpeVVNWnpWTkt9h0w9t3OiLOUgX0e5ogAPMgArKeIdABGAFYmcgA0iDd0hoiV1RSk/Z7YoWU+Q2Kg2bSAA==) link to see what the compiler does under the hood. Which made it possible for us to solve our problem. 

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
	* [5.1 SharpSqlObject](#51-sharpsqlobject)
	* [5.2 SharpSqlEntity](#52-sharpsqlentity)
	* [5.3 SharpSqlCollection](#53-ormcollection)
* [Chapter 6. Attributes](#chapter-6-attributes)
	* [6.1 SharpSqlColumnAttribute](#61-sharpsqlcolumnattribute)
	* [6.2 SharpSqlPrimaryKeyAttribute](#62-sharpsqlprimarykeyattribute)
	* [6.3 SharpSqlTableAttribute](#63-sharpsqltableattribute)
	* [6.4 SharpSqlUnitTestAttribute](#64-sharpsqlunittestattribute)
* [Chapter 7. Specifications](#chapter-7-specifications)
	* [7.1 Version information](#71-version-information)
	* [7.2 Supported databases](#72-supported-databases)
	* [7.3 Supported .NET versions](#73-supported-net-versions)

## Chapter 1. Getting started

This chapter quickly guides you through how to install SharpSql and on how to set it up in your project.

**Step 1.** Install SharpSql via the NuGet package: [SharpSql](https://www.nuget.org/packages/SharpSql/)

```
PM> Install-Package SharpSql
```

**Step 2.** Create a connection

Once you've installed the NuGet package you can start initializing the framework in your source code.

First create an appsettings.json file in your project folder, and set your ConnectionStrings:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost; Database=SharpSqlDatabase; Trusted_Connection=True; MultipleActiveResultSets=true"
    }
}
```

For a more detailed guide on creating connection strings, see Microsoft's documentation: *[Creating a Connection String](https://docs.microsoft.com/en-us/sql/ado/guide/data/creating-a-connection-string?view=sql-server-ver15)*.

Next you can create a variable named ```configuration``` (as shown below) which uses the appsettings.json file which will be needed later.

```cs
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
```

**Step 3.** Initialize SharpSql

Next you can  initialize SharpSql with the following line of code to let SharpSql connect to the database.

```cs
_ = new SharpSqlInitializer(configuration);

```

Even though the example above would be sufficient, the initializer does accept multiple parameters as shown here:
```cs
public SharpSqlInitializer(IConfiguration configuration = null, 
                    	  bool loadAllReferencedAssemblies = false, 
		    	  bool allowAnonymousTypes = false, 
		    	  string schemaAlias = "DBO")
```

The first parameter ```IConfiguration``` is used for the connection string, so the framework knows what database to connect to. The second parameter ```loadAllReferencesAssemblies``` can forcefully load all assemblies on start-up before SharpSql.dll is initialized. The third parameter ```allowAnonymousTypes``` enables the use of anonymous types in the expression trees, and the finaly parameter ```schemaAlias``` allows an override for existing databases aliases.

And that's it!  SharpSql is now fully initialized and now you're ready to set-up your entities to start loading and writing data.

**Step 4.** The framework works with both a code first and database first approach, and coding your database is fairly straightforward - for each table in your database you'll create a collection class and give this class the same name as your table. Then place the ```SharpSqlTable``` attribute above the class with the following parameters: the type of the current collection class, and the type of the entity class (*see Step 4.*). And as a last step, inherit from the ```SharpSqlCollection<EntityType>``` class and your collection class is all set!

```cs
// The collection class of the database table Users.
[SharpSqlTable(typeof(Users), typeof(User))]
public class Users : SharpSqlCollection<User>
{
    public Users() { }
}
```

**Step 4.** As seen in the previous step (*Step 3.*) every collection class also requires an entity class, create an entity class (name can't be the same as an existing column name) and inherit from the ```SharpSqlEntity``` class. After that - create a property for each column in the table and provide it with a getter and setter (setters are allowed to be private) and mark the primary key with the ```SharpSqlPrimaryKey``` attribute. In this example, we have an Id as primary key (the default -1 is not mandatory), a username, password and an organisation whereas the organisation is a foreign key (join) and last, an empty constructor for the entity class.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : SharpSqlEntity
{
    [SharpSqlPrimaryKey]
    public int Id { get; private set; } = -1;

    public string Username { get; set; }

    public string Password { get; set; }

    public Organisation Organisation { get; set; }

    public User() {}
}
```

**Step 5.** The base class of SharpSqlEntity provides one optional parameter for the constructor: 
```cs
protected SharpSqlEntity(bool disableChangeTracking = false) { }
```
With this parameter you can provide whether or not you want to enable or disable ```DisableChangeTracking``` (```false``` by default). Note that disabling change tracking causes the ```IsDirty``` property to always return true, because then the framework has to assume changes were made to the object.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : SharpSqlEntity
{
    [SharpSqlPrimaryKey]
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

**Step 6.**  Many-to-many relations - this requires the use of the same ```SharpSqlTable``` attribute, but with a different constructor. In this example we'll use the previously delcared Users and User types and a collection of type Roles with entity type Role with the parameters Id as primary key and Name which will be the name of the role itself and so creating the many-to-many table UserRoles. The constructor requires the following parameters: the collection type of the current many-to-many class (in this case UserRoles), the entity type of the current many-to-many class (in this case UserRole) the first collection class (in this case Users) and the second collection class (in this case Roles).

```cs
[SharpSqlTable(typeof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles))]
public class UserRoles : SharpSqlCollection<UserRole>
{
    public UserRole() { }
}
```

Next we'll set-up the basic UserRole entity class and we'll add the primary keys as parameters to the constructor and call the ```base.FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>()``` to be able to fetch specific records.

```cs
public class UserRole : SharpSqlEntity
{
    [SharpSqlPrimaryKey]
    public int UserId { get; private set; }

    [SharpSqlPrimaryKey]
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

When you want to join between two tables you can use the ```Join()``` method on your collection class to retrieve the information of your sub-object(s), when no join is provided the sub-object will remain ```null```. The type of join can be specified by using either the ```Left()``` or ```Inner()``` method on the user entity (lambda expression). The left join will be used by default if none are specified.

```cs
var users = new Users();
users.Join(user => user.Organisation.Left());
users.Fetch();
```

This will result in the following query:

```sql
SELECT * FROM [DBO].[USERS] AS [U] LEFT JOIN [DBO].[ORGANISATIONS] AS [O] ON [U].[ORGANISATION] = [O].[ID];
```

```
Todo - advanced cases
```

*[ Back to top](#table-of-contents)*

#### 3.2.4 Where

When you want to filter records, you can use the ```Where()``` method and use the comparison operators (see *[ SQL Comparison Operators](#sql-comparison-operators)*) on any of the entities fields. In the example below we filter on the Users Id with the equals operator.

```cs
var users = new Users();
users.Where(x => x.Id == 1);
users.Fetch();
```

This will result in the following query:

```sql
SELECT * FROM [DBO].[USERS] AS [U] WHERE ([U].[ID] = @PARAM1);
```

##### SQL Comparison Operators
```sql
= 	-- Equal to 	
> 	-- Greater than 	
< 	-- Less than 	
>= 	-- Greater than or equal to 	
<= 	-- Less than or equal to 	
<> 	-- Not equal to
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

Currently the ```Delete()``` method throws a ```NotImplementedException()```. In alpha 0.2 the delete method will be available, but drop tables will never be implemented (but can still be achieved through a direct query).

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

### 5.1. SharpSqlObject

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 5.2 SharpSqlEntity

In [Chapter 1. Getting started](#chapter-1-getting-started) (*Step 5.*) we left off with a fairly basic entity class, let's expand on this entity class by adding two more properties to our entity: DateCreated and DateLastModified.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : SharpSqlEntity
{
    [SharpSqlPrimaryKey]
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

The ```SharpSqlEntity``` class provides multiple virtual methods, so let's say we want to override the ```Save()``` method to change it's behaviour when posting data to the database by always setting the DateCreated to the current date and time, including the DateLastModified.

```cs
// The entity class User which represents a single (new) row in the collection Users.
public class User : SharpSqlEntity
{
    [SharpSqlPrimaryKey]
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

### 5.3. SharpSqlCollection<SharpSqlEntity>

```cs
Todo
```

*[ Back to top](#table-of-contents)*

## Chapter 6. Attributes

Within this framework we have created multiple attributes. In this chapter we'll explain how each attribute can be used and how the attributes are used within the framework itself.

### 6.1 SharpSqlColumnAttribute

Sometimes you want to name your entity property different than the actual column name, to achieve this you can use the ```SharpSqlColumn``` attribute. The framework will automatically assume the name of the property is the same name as the column name, when it doesn't find any matches it'll try and resolve it through the ```SharpSqlColumn``` attribute and throws an ```NotImplementedException``` when neither was found.

```cs
[SharpSqlColumn(ColumnName)]
public string Description { get; private set; }
```

*[ Back to top](#table-of-contents)*

### 6.2 SharpSqlPrimaryKeyAttribute

To tell the framework what the primary or shared key of the table is, you can use the ```SharpSqlPrimaryKey``` attribute. If there is a shared primary key, it'll map them in the same top-to-down order from the entity class, this means that any parameters regarding the primary keys which are passed on to the framework has to be passed in the exact same order.

```cs
// a single primary key:

[SharpSqlPrimaryKey]
public int Id { get; private set; } = -1;

// a shared primary key:

[SharpSqlPrimaryKey]
public int UserId { get; private set; }

[SharpSqlPrimaryKey]
public int RoleId { get; private set; }
```

*[ Back to top](#table-of-contents)*

### 6.3 SharpSqlTableAttribute

```cs
Todo
```

*[ Back to top](#table-of-contents)*

### 6.4 SharpSqlUnitTestAttribute

The ```SharpSqlUnitTest``` attribute is an internally used attribute. This project make use of the NUnit testing framework for all of our unit tests and the project is named "SharpSqlNUnit", which has access to all of the internal classes, methods, properties and variables through the ```SharpSqlUnitTest``` attribute which is used on the initialization class.

```cs
[SetUpFixture, SharpSqlUnitTest]
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

        _ = new SharpSqlInitialize(memoryEntityTables, memoryCollectionTables);
    }
}
```

*[ Back to top](#table-of-contents)*

## Chapter 7. Specifications

All of the specifications of SharpSql.

### 7.1 Version information

The latest version of this framework is version beta-0.3, released on 2022-03-01.

### 7.2 Supported databases

SQL Server 2005 or higher

### 7.3 Supported .NET versions

NET Standard 2.2., .NET 6.0+.
	
*[ Back to top](#table-of-contents)*
