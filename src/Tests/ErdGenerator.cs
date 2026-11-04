using System.Text;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Tests;

/// <summary>
/// Writes docs/store-mode/ERD.md (a Mermaid entity-relationship diagram) from the real EF model, so the diagram can never
/// drift from the code. Runs with the tests; commit the regenerated file when the model changes.
/// </summary>
public class ErdGenerator
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FooderaERPProject.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }

    private static string Type(IProperty p)
    {
        var t = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
        return t == typeof(string) ? "string" : t == typeof(DateTime) ? "datetime" : t == typeof(bool) ? "bool" : t == typeof(byte[]) ? "bytes" : t == typeof(Guid) ? "guid" : t.IsEnum ? "int" : t.Name.ToLowerInvariant();
    }

    [Fact]
    public void Store_model_diagram_is_written_and_covers_the_main_entities()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var store = db.Model.GetEntityTypes().Where(e => e.ClrType.Name.StartsWith("Store", StringComparison.Ordinal)).OrderBy(e => e.ClrType.Name).ToList();
        var names = store.Select(e => e.ClrType.Name).ToHashSet();
        var external = new[] { "Company", "Restaurant", "Warehouse" };

        var sb = new StringBuilder();
        sb.AppendLine("# Mağaza rejimi — ERD");
        sb.AppendLine();
        sb.AppendLine("Bu fayl EF modelindən avtomatik yaradılır (`src/Tests/ErdGenerator.cs`); əl ilə dəyişdirməyin. Testləri işlədəndə yenilənir.");
        sb.AppendLine();
        sb.AppendLine("Stok və maliyyə balansları hərəkət/ledger cədvəllərindən (`StoreStockMovements`, `StoreLedgerEntries`, `StoreBonusEntries`) hesablanır; `StoreStockBalances` performans üçün snapshot-dır (RowVersion ilə). Satış sətrində qiymət, endirim, ƏDV və maya snapshot kimi saxlanır. Unikal indekslər: `(CompanyId, OperationId)` təkrar əməliyyatın qarşısını alır.");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("erDiagram");

        foreach (var e in store)
        {
            sb.AppendLine($"  {e.ClrType.Name} {{");
            foreach (var p in e.GetProperties())
            {
                var key = p.IsPrimaryKey() ? " PK" : p.IsForeignKey() ? " FK" : "";
                sb.AppendLine($"    {Type(p)} {p.Name}{key}");
            }
            sb.AppendLine("  }");
        }

        var lines = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var e in store)
            foreach (var fk in e.GetForeignKeys())
            {
                var principal = fk.PrincipalEntityType.ClrType.Name;
                if (!names.Contains(principal) && !external.Contains(principal)) continue;
                var many = fk.IsUnique ? "o|" : "o{";
                lines.Add($"  {principal} ||--{many} {e.ClrType.Name} : \"{string.Join(",", fk.Properties.Select(p => p.Name))}\"");
            }
        foreach (var l in lines) sb.AppendLine(l);

        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## İndekslər");
        sb.AppendLine();
        sb.AppendLine("| Cədvəl | İndeks | Unikal |");
        sb.AppendLine("|---|---|---|");
        foreach (var e in store)
            foreach (var i in e.GetIndexes())
                sb.AppendLine($"| {e.GetTableName()} | {string.Join(", ", i.Properties.Select(p => p.Name))} | {(i.IsUnique ? "bəli" : "")} |");

        var path = Path.Combine(RepoRoot(), "docs", "store-mode", "ERD.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));

        foreach (var must in new[] { "StoreSale", "StoreSaleLine", "StoreStockMovement", "StoreLedgerEntry", "StoreTransfer", "StoreSyncOperation" })
            Assert.Contains(must, names);
        Assert.True(store.Count > 30);
    }
}
