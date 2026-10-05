using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using DevVault.Application.Common.Interfaces;
using DevVault.Domain.Entities;
using DevVault.Infrastructure.Persistence;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DevVault.ArchitectureTests;

/// <summary>
/// The README's "dependencies point strictly inward" rule, enforced instead of trusted.
/// </summary>
public class LayerTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Snippet).Assembly,
            typeof(ISnippetRepository).Assembly,
            typeof(AppDbContext).Assembly,
            typeof(Program).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> Domain =
        Types().That().ResideInAssembly(typeof(Snippet).Assembly).As("Domain");

    private static readonly IObjectProvider<IType> Application =
        Types().That().ResideInAssembly(typeof(ISnippetRepository).Assembly).As("Application");

    private static readonly IObjectProvider<IType> Infrastructure =
        Types().That().ResideInAssembly(typeof(AppDbContext).Assembly).As("Infrastructure");

    private static readonly IObjectProvider<IType> Api =
        Types().That().ResideInAssembly(typeof(Program).Assembly).As("API");

    private static readonly string[] FrameworkAssemblyPrefixes =
        ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore"];

    [Fact]
    public void Domain_DependsOnNoOtherLayer() =>
        Types().That().Are(Domain).Should().NotDependOnAny(Application)
            .AndShould().NotDependOnAny(Infrastructure)
            .AndShould().NotDependOnAny(Api)
            .Check(Architecture);

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi() =>
        Types().That().Are(Application).Should().NotDependOnAny(Infrastructure)
            .AndShould().NotDependOnAny(Api)
            .Check(Architecture);

    [Fact]
    public void Infrastructure_DoesNotDependOnApi() =>
        Types().That().Are(Infrastructure).Should().NotDependOnAny(Api)
            .Check(Architecture);

    // Checked on the compiled assembly references: ArchUnitNET only sees framework types whose
    // assemblies are loaded into the Architecture, so a namespace rule here would pass vacuously.
    [Theory]
    [InlineData(typeof(Snippet))]
    [InlineData(typeof(ISnippetRepository))]
    public void DomainAndApplication_ReferenceNoEfCoreOrAspNetCore(System.Type layerMarker)
    {
        var offending = layerMarker.Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => FrameworkAssemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        Assert.True(offending.Count == 0,
            $"{layerMarker.Assembly.GetName().Name} references {string.Join(", ", offending)}");
    }
}
