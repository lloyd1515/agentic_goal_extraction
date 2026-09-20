using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace GoalExtraction.UnitTests;

public class ArchitectureTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "GoalExtraction.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new InvalidOperationException("GoalExtraction.sln not found");
    }

    [Fact]
    public void DomainProject_ShouldHaveNoExternalDependenciesOrProjectReferences()
    {
        var domainCsprojPath = Path.Combine(SolutionRoot, "src", "GoalExtraction.Domain", "GoalExtraction.Domain.csproj");
        File.Exists(domainCsprojPath).Should().BeTrue("Domain csproj must exist");

        var doc = XDocument.Load(domainCsprojPath);
        var projectReferences = doc.Descendants("ProjectReference").Select(x => x.Attribute("Include")?.Value).ToList();
        var packageReferences = doc.Descendants("PackageReference").Select(x => x.Attribute("Include")?.Value).ToList();

        projectReferences.Should().BeEmpty("Domain layer must have zero ProjectReferences");
        packageReferences.Should().BeEmpty("Domain layer must have zero third-party PackageReferences");
    }

    [Fact]
    public void DomainAssembly_ShouldNotReferenceApplicationInfrastructureOrApi()
    {
        var domainAssembly = Assembly.Load("GoalExtraction.Domain");
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        referencedAssemblies.Should().NotContain(name => name != null && name.StartsWith("GoalExtraction.Application"),
            "Domain must not reference Application");
        referencedAssemblies.Should().NotContain(name => name != null && name.StartsWith("GoalExtraction.Infrastructure"),
            "Domain must not reference Infrastructure");
        referencedAssemblies.Should().NotContain(name => name != null && name.StartsWith("GoalExtraction.Api"),
            "Domain must not reference Api");
    }

    [Fact]
    public void ApplicationProject_ShouldOnlyReferenceDomain_AndNeverInfrastructureOrApi()
    {
        var appCsprojPath = Path.Combine(SolutionRoot, "src", "GoalExtraction.Application", "GoalExtraction.Application.csproj");
        File.Exists(appCsprojPath).Should().BeTrue("Application csproj must exist");

        var doc = XDocument.Load(appCsprojPath);
        var projectReferences = doc.Descendants("ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension((x.Attribute("Include")?.Value ?? "").Replace('\\', '/')))
            .ToList();

        projectReferences.Should().Contain("GoalExtraction.Domain", "Application must reference Domain");
        projectReferences.Should().NotContain("GoalExtraction.Infrastructure", "Application must not reference Infrastructure");
        projectReferences.Should().NotContain("GoalExtraction.Api", "Application must not reference Api");
    }

    [Fact]
    public void ApplicationAssembly_ShouldNotReferenceInfrastructureOrApi()
    {
        var appAssembly = Assembly.Load("GoalExtraction.Application");
        var referencedAssemblies = appAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        referencedAssemblies.Should().NotContain(name => name != null && name.StartsWith("GoalExtraction.Infrastructure"),
            "Application must not reference Infrastructure");
        referencedAssemblies.Should().NotContain(name => name != null && name.StartsWith("GoalExtraction.Api"),
            "Application must not reference Api");
        referencedAssemblies.Should().NotContain(name => name != null && name.Contains("EntityFrameworkCore"),
            "Application must not reference EntityFrameworkCore");
    }

    [Fact]
    public void InfrastructureProject_ShouldNotReferenceApi()
    {
        var infraCsprojPath = Path.Combine(SolutionRoot, "src", "GoalExtraction.Infrastructure", "GoalExtraction.Infrastructure.csproj");
        File.Exists(infraCsprojPath).Should().BeTrue("Infrastructure csproj must exist");

        var doc = XDocument.Load(infraCsprojPath);
        var projectReferences = doc.Descendants("ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension((x.Attribute("Include")?.Value ?? "").Replace('\\', '/')))
            .ToList();

        projectReferences.Should().Contain("GoalExtraction.Domain", "Infrastructure must reference Domain");
        projectReferences.Should().Contain("GoalExtraction.Application", "Infrastructure must reference Application");
        projectReferences.Should().NotContain("GoalExtraction.Api", "Infrastructure must not reference Api");
    }

    [Fact]
    public void IReadOnlyGoalQueryRepository_ShouldHaveZeroMutatingMethods_AdheringToIsp()
    {
        var interfaceType = typeof(GoalExtraction.Domain.Interfaces.IReadOnlyGoalQueryRepository);
        var methods = interfaceType.GetMethods();

        methods.Should().NotBeEmpty("Interface must define query methods");

        var mutatingPrefixes = new[] { "Insert", "Add", "Update", "Delete", "Remove", "Save", "Create", "Set", "Write", "Put", "Post", "Mutate" };

        foreach (var method in methods)
        {
            foreach (var prefix in mutatingPrefixes)
            {
                method.Name.Should().NotStartWith(prefix, $"Read-only repository interface must not contain mutating method: {method.Name}");
            }

            typeof(Task).IsAssignableFrom(method.ReturnType).Should().BeTrue("All repository methods should be asynchronous returning Task");
            method.ReturnType.IsGenericType.Should().BeTrue("Read-only queries must return a query result (Task<T>) and not a void-like Task");
        }
    }

    [Fact]
    public void IGoalBatchWriteRepository_ShouldOnlyHaveBatchWriteMethods_AdheringToIsp()
    {
        var interfaceType = typeof(GoalExtraction.Domain.Interfaces.IGoalBatchWriteRepository);
        var methods = interfaceType.GetMethods();

        methods.Should().HaveCount(1);
        methods[0].Name.Should().Be("SaveGoalsBatchAsync");
    }

    [Fact]
    public void ExtractGoalsCommandHandler_MustNeverInjectOrReferenceWriteRepositories()
    {
        var handlerType = typeof(GoalExtraction.Application.Commands.ExtractGoals.ExtractGoalsCommandHandler);
        var ctors = handlerType.GetConstructors();
        ctors.Should().NotBeEmpty();

        foreach (var ctor in ctors)
        {
            var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

            parameterTypes.Should().NotContain(t => typeof(GoalExtraction.Domain.Interfaces.IGoalBatchWriteRepository).IsAssignableFrom(t),
                "ExtractGoalsCommandHandler must never inject IGoalBatchWriteRepository");

            parameterTypes.Should().NotContain(t => t.Name.Contains("Write") || t.Name.Contains("DbContext"),
                "ExtractGoalsCommandHandler must never inject any write repository or DbContext");
        }

        var fields = handlerType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            typeof(GoalExtraction.Domain.Interfaces.IGoalBatchWriteRepository).IsAssignableFrom(field.FieldType)
                .Should().BeFalse("ExtractGoalsCommandHandler must not have any write repository fields");
            field.FieldType.Name.Should().NotContain("Write", "ExtractGoalsCommandHandler must not have write repository fields");
        }
    }

    [Fact]
    public void SaveGoalsBatchCommandHandler_MustNeverInjectOrReferenceAiClientsOrAgents()
    {
        var handlerType = typeof(GoalExtraction.Application.Commands.SaveGoalsBatch.SaveGoalsBatchCommandHandler);
        var ctors = handlerType.GetConstructors();
        ctors.Should().NotBeEmpty();

        foreach (var ctor in ctors)
        {
            var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

            parameterTypes.Should().NotContain(t => typeof(GoalExtraction.Application.AI.IGoalExtractionAgent).IsAssignableFrom(t),
                "SaveGoalsBatchCommandHandler must never inject IGoalExtractionAgent");

            parameterTypes.Should().NotContain(t => typeof(GoalExtraction.Application.AI.IGeminiOpenAiClient).IsAssignableFrom(t),
                "SaveGoalsBatchCommandHandler must never inject IGeminiOpenAiClient");

            parameterTypes.Should().NotContain(t => t.Name.Contains("Agent") || t.Name.Contains("Gemini") || t.Name.Contains("AiClient"),
                "SaveGoalsBatchCommandHandler must never inject AI clients or agents");
        }

        var fields = handlerType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            typeof(GoalExtraction.Application.AI.IGoalExtractionAgent).IsAssignableFrom(field.FieldType)
                .Should().BeFalse("SaveGoalsBatchCommandHandler must not have IGoalExtractionAgent fields");
            typeof(GoalExtraction.Application.AI.IGeminiOpenAiClient).IsAssignableFrom(field.FieldType)
                .Should().BeFalse("SaveGoalsBatchCommandHandler must not have IGeminiOpenAiClient fields");
        }
    }
}
