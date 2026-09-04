using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace ArchitectureTests;

/// <summary>
/// Phase 8 item 8: enforce the clean-architecture dependency rules and keep tenant HTTP
/// concerns out of Domain and Application. These guard the multi-tenancy foundation: a
/// handler that reaches for Infrastructure or WebUI could bypass <c>ICurrentTenant</c>.
/// </summary>
public sealed class ArchitectureDependencyTests
{
    private static readonly Assembly Domain = typeof(Domain.Tenants.Tenant).Assembly;
    private static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly WebUi = typeof(WebUI.Program).Assembly;

    [Fact]
    public void Domain_DoesNotReferenceAspNetCoreOrHttp()
    {
        TestResult result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.AspNetCore", "System.Net.Http", "WebUI", "Infrastructure", "Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failures(result));
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrWebUi()
    {
        TestResult result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny("Infrastructure", "WebUI")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failures(result));
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceWebUi()
    {
        TestResult result = Types.InAssembly(Infrastructure)
            .Should()
            .NotHaveDependencyOn("WebUI")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failures(result));
    }

    [Fact]
    public void WebUi_EndpointsDependOnApplicationLayer()
    {
        // Minimal API endpoints (WebUI.Endpoints) must dispatch through Application handlers.
        TestResult result = Types.InAssembly(WebUi)
            .That()
            .ResideInNamespace("WebUI.Endpoints")
            .And()
            .HaveNameEndingWith("Endpoints")
            .Should()
            .HaveDependencyOn("Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failures(result));
    }

    private static string Failures(TestResult result) =>
        string.Join(Environment.NewLine, result.FailingTypeNames ?? []);
}
