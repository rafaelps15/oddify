using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

// Fonte única do catálogo de faixas de meta, multiplicador, frações e passos. Antes vivia só em
// memória em RegrasDeAlavancagem, duplicado entre o lado de escrita e o de leitura, agora ambos
// buscam daqui. O lado de leitura lê via Dapper, direto na tabela, como qualquer query. O lado de
// escrita lê via IFaixaDeMetaCatalogoRepository, já que um CommandHandler nunca usa Dapper. As
// três linhas são seedadas via migration e nunca são inseridas ou alteradas por nenhum Command.
// Revisar os valores com o produto exige uma nova migration, não só mudar código — ver
// RegrasDeAlavancagem para o motivo desses números serem provisórios.
public sealed class FaixaDeMetaCatalogo : Entity
{
    private FaixaDeMetaCatalogo()
    {
    }

    public FaixaDeMeta Faixa { get; private set; }

    public int Multiplicador { get; private set; }

    public int NumeroDeFracoes { get; private set; }

    public int TotalDePassos { get; private set; }

    public static FaixaDeMetaCatalogo Create(FaixaDeMeta faixa, int multiplicador, int numeroDeFracoes, int totalDePassos)
    {
        var faixaDeMetaCatalogo = new FaixaDeMetaCatalogo
        {
            Faixa = faixa,
            Multiplicador = multiplicador,
            NumeroDeFracoes = numeroDeFracoes,
            TotalDePassos = totalDePassos
        };

        return faixaDeMetaCatalogo;
    }
}
