# Kraggs.TSM7.Data

Kraggs.TSM7.Data is an extension to Kraggs.TSM7.Utils which
adds some SQL Query capabilities.

### TODO:
* Aggregate (sum, count, etc).
* Unions (select subset from several tables).
* Limit (fetch first X rows only)
* Nullable Types.
* Handle string StartsWith and others.

### Example:

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


### Missing Tests:
* Convert GUID
* Convert DateTime
* Nullable types
* Sql queries
* 