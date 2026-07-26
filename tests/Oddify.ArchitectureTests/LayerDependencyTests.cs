using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Oddify.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string FixturesApplication = "Oddify.Modules.Fixtures.Application";
    private const string FixturesInfrastructure = "Oddify.Modules.Fixtures.Infrastructure";
    private const string FixturesPresentation = "Oddify.Modules.Fixtures.Presentation";

    private const string AnaliseApplication = "Oddify.Modules.Analise.Application";
    private const string AnaliseInfrastructure = "Oddify.Modules.Analise.Infrastructure";
    private const string AnalisePresentation = "Oddify.Modules.Analise.Presentation";

    private const string ApostasApplication = "Oddify.Modules.Apostas.Application";
    private const string ApostasInfrastructure = "Oddify.Modules.Apostas.Infrastructure";
    private const string ApostasPresentation = "Oddify.Modules.Apostas.Presentation";

    [Fact]
    public void Fixtures_Domain_should_not_depend_on_other_layers()
    {
        TestResult result = Types.InAssembly(typeof(Oddify.Modules.Fixtures.Domain.Partidas.Partida).Assembly)
            .Should()
            .NotHaveDependencyOnAny(FixturesApplication, FixturesInfrastructure, FixturesPresentation)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Analise_Domain_should_not_depend_on_other_layers()
    {
        TestResult result = Types.InAssembly(typeof(Oddify.Modules.Analise.Domain.Analises.AnaliseDePartida).Assembly)
            .Should()
            .NotHaveDependencyOnAny(AnaliseApplication, AnaliseInfrastructure, AnalisePresentation)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Apostas_Domain_should_not_depend_on_other_layers()
    {
        TestResult result = Types.InAssembly(typeof(Oddify.Modules.Apostas.Domain.Bancas.Banca).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApostasApplication, ApostasInfrastructure, ApostasPresentation)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Fixtures_Application_should_not_depend_on_infrastructure_or_presentation()
    {
        TestResult result = Types.InAssembly(Oddify.Modules.Fixtures.Application.AssemblyReference.Assembly)
            .Should()
            .NotHaveDependencyOnAny(FixturesInfrastructure, FixturesPresentation)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Analise_Application_should_not_depend_on_infrastructure_or_presentation()
    {
        TestResult result = Types.InAssembly(Oddify.Modules.Analise.Application.AssemblyReference.Assembly)
            .Should()
            .NotHaveDependencyOnAny(AnaliseInfrastructure, AnalisePresentation)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Apostas_Application_should_not_depend_on_infrastructure_or_presentation()
    {
        TestResult result = Types.InAssembly(Oddify.Modules.Apostas.Application.AssemblyReference.Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApostasInfrastructure, ApostasPresentation)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_should_be_sealed_and_not_public_across_all_modules()
    {
        Assembly[] assemblies =
        [
            Oddify.Modules.Fixtures.Application.AssemblyReference.Assembly,
            Oddify.Modules.Analise.Application.AssemblyReference.Assembly,
            Oddify.Modules.Apostas.Application.AssemblyReference.Assembly,
        ];

        foreach (Assembly assembly in assemblies)
        {
            TestResult resultCommandHandlersSemRetorno = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(Oddify.Common.Application.Messaging.ICommandHandler<>))
                .Should()
                .BeSealed().And().NotBePublic()
                .GetResult();

            resultCommandHandlersSemRetorno.IsSuccessful.Should().BeTrue();

            TestResult resultCommandHandlersComRetorno = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(Oddify.Common.Application.Messaging.ICommandHandler<,>))
                .Should()
                .BeSealed().And().NotBePublic()
                .GetResult();

            resultCommandHandlersComRetorno.IsSuccessful.Should().BeTrue();

            TestResult resultQueryHandlers = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(Oddify.Common.Application.Messaging.IQueryHandler<,>))
                .Should()
                .BeSealed().And().NotBePublic()
                .GetResult();

            resultQueryHandlers.IsSuccessful.Should().BeTrue();
        }
    }
}
