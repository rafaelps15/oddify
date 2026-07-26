using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Oddify.ArchitectureTests;

public sealed class CrossModuleBoundaryTests
{
    private sealed record Modulo(string Nome, Assembly[] Assemblies);

    private static Modulo Fixtures => new(
        "Fixtures",
        [
            typeof(Oddify.Modules.Fixtures.Domain.Partidas.Partida).Assembly,
            Oddify.Modules.Fixtures.Application.AssemblyReference.Assembly,
            typeof(Oddify.Modules.Fixtures.Infrastructure.FixturesModule).Assembly,
            Oddify.Modules.Fixtures.Presentation.AssemblyReference.Assembly,
        ]);

    private static Modulo Analise => new(
        "Analise",
        [
            typeof(Oddify.Modules.Analise.Domain.Analises.AnaliseDePartida).Assembly,
            Oddify.Modules.Analise.Application.AssemblyReference.Assembly,
            typeof(Oddify.Modules.Analise.Infrastructure.AnaliseModule).Assembly,
            Oddify.Modules.Analise.Presentation.AssemblyReference.Assembly,
        ]);

    private static Modulo Apostas => new(
        "Apostas",
        [
            typeof(Oddify.Modules.Apostas.Domain.Bancas.Banca).Assembly,
            Oddify.Modules.Apostas.Application.AssemblyReference.Assembly,
            typeof(Oddify.Modules.Apostas.Infrastructure.ApostasModule).Assembly,
            Oddify.Modules.Apostas.Presentation.AssemblyReference.Assembly,
        ]);

    private static Modulo Users => new(
        "Users",
        [
            typeof(Oddify.Modules.Users.Domain.Users.User).Assembly,
            Oddify.Modules.Users.Application.AssemblyReference.Assembly,
            typeof(Oddify.Modules.Users.Infrastructure.UsersModule).Assembly,
            Oddify.Modules.Users.Presentation.AssemblyReference.Assembly,
        ]);

    public static TheoryData<string, Assembly, string, string> ParesDeModulos()
    {
        Modulo[] modulos = [Fixtures, Analise, Apostas, Users];
        var data = new TheoryData<string, Assembly, string, string>();

        foreach (Modulo modulo in modulos)
        {
            foreach (Assembly assembly in modulo.Assemblies)
            {
                foreach (Modulo outroModulo in modulos)
                {
                    if (outroModulo.Nome == modulo.Nome)
                    {
                        continue;
                    }

                    data.Add(
                        $"{modulo.Nome}:{assembly.GetName().Name}",
                        assembly,
                        $"Oddify.Modules.{outroModulo.Nome}.Domain",
                        $"Oddify.Modules.{outroModulo.Nome}.Application");
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ParesDeModulos))]
    public void Module_assembly_should_not_depend_on_another_modules_domain_or_application(
        string descricao, Assembly assembly, string domainNamespaceDoOutroModulo, string applicationNamespaceDoOutroModulo)
    {
        TestResult result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(domainNamespaceDoOutroModulo, applicationNamespaceDoOutroModulo)
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"{descricao} não deve depender de {domainNamespaceDoOutroModulo}/{applicationNamespaceDoOutroModulo} diretamente — só de PublicApi ou IntegrationEvents");
    }

    [Fact]
    public void Infrastructure_assemblies_should_not_depend_on_another_modules_infrastructure()
    {
        (Assembly Assembly, string Nome)[] infraestruturas =
        [
            (typeof(Oddify.Modules.Fixtures.Infrastructure.FixturesModule).Assembly, "Fixtures"),
            (typeof(Oddify.Modules.Analise.Infrastructure.AnaliseModule).Assembly, "Analise"),
            (typeof(Oddify.Modules.Apostas.Infrastructure.ApostasModule).Assembly, "Apostas"),
            (typeof(Oddify.Modules.Users.Infrastructure.UsersModule).Assembly, "Users"),
        ];

        foreach ((Assembly assembly, string nome) in infraestruturas)
        {
            foreach ((_, string outroNome) in infraestruturas)
            {
                if (nome == outroNome)
                {
                    continue;
                }

                TestResult result = Types.InAssembly(assembly)
                    .Should()
                    .NotHaveDependencyOn($"Oddify.Modules.{outroNome}.Infrastructure")
                    .GetResult();

                result.IsSuccessful.Should().BeTrue($"{nome}.Infrastructure não deve depender de {outroNome}.Infrastructure");
            }
        }
    }
}
