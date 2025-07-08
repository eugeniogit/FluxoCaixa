using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace FluxoCaixa.Lancamento.Features.CriarLancamento;

public class CriarLancamentoRequest
{
    [Required(ErrorMessage = "Comerciante é obrigatório")]
    [StringLength(100, ErrorMessage = "Comerciante deve ter no máximo 100 caracteres")]
    public string Comerciante { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório")]
    public TipoLancamento Tipo { get; set; }

    [Required(ErrorMessage = "Data é obrigatória")]
    public DateTime Data { get; set; }

    [StringLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
    public string Descricao { get; set; } = string.Empty;

}