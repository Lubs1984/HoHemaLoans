using Npgsql;

var connectionString = args.Length > 0 
    ? args[0] 
    : Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ ERROR: DATABASE_URL not provided");
    Console.WriteLine("Usage: dotnet run <connection-string>");
    return 1;
}

var sql = File.ReadAllText("../../../scripts/add-user-documents-table.sql");

try
{
    Console.WriteLine("🚀 Connecting to Railway database...");
    
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    
    Console.WriteLine("✅ Connected successfully!");
    Console.WriteLine("📋 Executing migration script...");
    Console.WriteLine();
    
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
    
    Console.WriteLine();
    Console.WriteLine("✅ UserDocuments table created successfully!");
    Console.WriteLine("✅ Migration records added to __EFMigrationsHistory");
    
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR: {ex.Message}");
    return 1;
}
