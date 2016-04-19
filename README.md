# Kraggs.TSM7.Data

Kraggs.TSM7.Data is an extension to Kraggs.TSM7.Utils which
adds some SQL Query capabilities.



## Kraggs.TSM7.Attributes

The library is build around creating C# POCOs with attributes describing the TSM DB2 Tables.
These POCO classes can then be used by 2 separate code paths.

Below are some examples for creating these POCO classes.

First create a class to hold the returned data.

```csharp
namespace Example
{
    public class NodeUsage
    {
        public string NodeName {get;set;}
        public long TotalMB {get;set;}
    }
}
```

Then add the TSM attributes to the describe which sql mapping.

```csharp
using Kraggs.TSM7.Data;


namespace Example
{
    [TSMTable("AUDITOCC")]
    public class NodeUsage
    {
        [TSMColumn("NODE_NAME")]
        public string NodeName {get;set;}
        [TSMColumn("TOTAL_MB")]
        public long TotalMB {get;set;}
    }
}
```

## TSMConnection

The TSMConnection class is the new way to interact with a TSM Server.
It dynamically quires the TSM Server for version, platform and TSM Table layout. This information is cached in memory for the next quiery on the same TSM table.
It then uses this information to dynamically build matching SELECT statements compatible with your Poco classes.
Since it queries the TSM Server for schema information the order of the properties on the POCO does not matter.
This code path should in theory work all the way back to TSM 5.5 servers. (not testet thou).


You also dont have to use the attributes to map tsm table columns to the poco properties but that require at least case insensitive name matching.
The exception to some of this is the SelectAS<T> function, but that is explained in more detail later.

### Example: SelectAll

Using example Poco 'NodeUsage' from above:

```csharp
using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dsmadmc = new clsDsmAdmc(...);

			var conn = new TSMConnection(dsmadmc);

			// select all rows.
            var nodeUsage = conn.SelectAll<NodeUsage>();

        }
    }
}
```


### Example: Where

Using example Poco 'NodeUsage' from above:

```csharp
using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dsmadmc = new clsDsmAdmc(...);

			var conn = new TSMConnection(dsmadmc);

            var nodeUsage = conn.Where<NodeUsage>(
				"WHERE Node_Name like 'TEST%'");                
        }
    }
}
```


### Example: SelectAS

SelectAS is a bit different. It still uses Version information from TSMConnection but it bypasses the TSM Schema (at least for now).

It instead matches on the Sql AS alias command.
Its primary purpose is then you need to create complex Sql statements, for example quering several tsm tables at the same time.

Using example Poco 'NodeUsage' from above:

```csharp
using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dsmadmc = new clsDsmAdmc(...);

			var conn = new TSMConnection(dsmadmc);

            var nodeUsage = conn.SelectAS<NodeUsage>(
				"SELECT Node_Name as NodeName, TOTAL_MB as TotalMB FROM AuditOcc WHERE Node_Name like 'TEST%'");
        }
    }
}
```

This AS mathing is not as rebust as the above Where and SelectAll commands, but has a lot more query power.


## DsmAdmcExtensions

This class contains extensions for the clsDsmAdmc utility class from Kraggs.TSM7.Utils project.
It is also the first code path created for this library.



### Example: Where

Using example Poco 'NodeUsage' from above:

```csharp
using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dsmadmc = new clsDsmAdmc(...);

            var nodeUsage = dsmadmc.Where<NodeUsage>(
				"WHERE Node_Name like 'TEST%'");                
        }
    }
}
```

This will internally generate a sql query like this:
```sql
SELECT NODE_NAME,TOTAL_MB FROM AUDITOCC WHERE NODE_NAME LIKE 'TEST%'
```

The dsmadmc will be called with this query and return a csvlist of values.
After a successfull call, it will decode csv and convert to Generic Type
and return the data to caller.


### Example: Select (unsafe)

In order to handle all cases, you can use any sql (and hope conversion works).

First create the result class.
Note that no attributes are required for this.

```csharp
namespace Example
{
    public class NodeStatistics
    {
        public int NodeCount {get;set;}
        public double SumNodeMB {get;set;}
    }
}
```

Then use something like this to get the result.
But YOU are responsible for not things blowing up...
Thats why the argument is named "UnsafeSql".


```csharp
using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dsmadmc = new clsDsmAdmc(...);

            var nodeStats = dsmadmc.Select<NodeStatistics>(
                "select count(*), sum(TOTAL_MB) from AuditOcc");

        }
    }
}
```

Values will be converted in the same order as the specified on the class.

### Example: Select (TSMQueryAttribute)

Instead of having SQL Queries mixed in with functial code, you can use TSMQueryAttribute.
TSMQueryAttribute enables to tag a type with a default SQL Query to run.
This way you can keep Type and SQL code designed for the Type in the same place.

```csharp
namespace Example
{
    [TSMQuery("select count(*), sum(TOTAL_MB) from AuditOcc")]
    public class NodeStatistics
    {
        public int NodeCount {get;set;}
        public double SumNodeMB {get;set;}
    }
}
```

And the code to use this type query.

```csharp
using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dsmadmc = new clsDsmAdmc(...);

            var nodeStats = dsmadmc.Select<NodeStatistics>();

        }
    }
}
```




### TODO:
* TSMQuery attribute. Update its usage (it replaces SelectAll Sql Generation.)
* ~~DB2 DateTime conversion~~
* ~~.Net GUID Conversion~~
* ~~Nullable Types~~
* ~~Enum Support~~


### Missing Tests:
* ~~Sql queries~~
* Unit Tests
