using FluentValidation;

namespace FluxoCaixa.Lancamento.Features.CriarLancamento;

public class CriarLancamentoValidator : AbstractValidator<CriarLancamentoCommand>
{
    public CriarLancamentoValidator()
    {
        RuleFor(x => x.Comerciante)
            .NotEmpty()
            .WithMessage("Comerciante é obrigatório")
            .MaximumLength(100)
            .WithMessage("Comerciante deve ter no máximo 100 caracteres");

        RuleFor(x => x.Valor)
            .GreaterThan(0)
            .WithMessage("Valor deve ser maior que zero");

        RuleFor(x => x.Tipo)
            .IsInEnum()
            .WithMessage("Tipo deve ser Débito ou Crédito");

        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage("Data é obrigatória");

        RuleFor(x => x.Descricao)
            .MaximumLength(500)
            .WithMessage("Descrição deve ter no máximo 500 caracteres");
    }
}