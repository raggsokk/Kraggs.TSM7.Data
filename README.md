# Kraggs.TSM7.Data

Kraggs.TSM7.Data is an extension to Kraggs.TSM7.Utils which
adds some SQL Query capabilities.

### TODO:
* Aggregate (sum, count, etc).
* Unions (select subset from several tables).
* Limit (fetch first X rows only)
* Nullable Types.
* Handle string StartsWith and others.

### Example: Where

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

Then use for example the where extension to query data.

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
                x => x.NodeName == "TESTNODE");
        }
    }
}
```

This will generate a sql query like this: 
```sql
SELECT NODE_NAME,TOTAL_MB FROM AUDITOCC WHERE NODE_NAME = 'TESTNODE'
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



### Missing Tests:
* ~~Convert GUID~~
* ~~Convert DateTime~~
* ~~Sql queries~~
* Nullable types
* Unit Tests
* Proper Linq provider.