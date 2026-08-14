using DataCleaner.Application;

namespace DataCleaner.Application.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ApplicationAssembly_DoesNotReferenceInfrastructureOrWpf()
    {
        var references = typeof(DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("DataCleaner.Infrastructure", references);
        Assert.DoesNotContain("PresentationFramework", references);
    }
}
