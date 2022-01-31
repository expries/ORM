# Setup

Run 'docker compose up -d' to start the postgres database.

If you are using your own database, it is recommended to 
empty it beforehand (even though the ORM tries to delete any conflicting tables).

# Configuration

When starting the database with docker compose, the SampleApp Console Application should work out of the box.

When using a custom connection string, configure the Database Context like this (see Program.cs):

```
DbContext.Configure(options =>
{
	options.UseStateTrackingCache();
	options.UsePostgres(<your_connection_string>);
});
```

# Extra features:

 + LINQ implementation. The following methods are implemented:
   + OrderBy
   + OrderByDescending
   + Average
   + Max
   + Min
   + Select
   + Count
   + Any
   + All
   + Where
   + FirstOrDefault
   + Sum
   + StartsWith
   + EndsWith
 
 + EnsureCreated: The database tables are automatically created when using DbContext.EnsureCreated()
 
