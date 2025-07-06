using System.ComponentModel.DataAnnotations;

namespace FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;

public class ConsolidarPeriodoRequest
{
    [Required(ErrorMessage = "Data início é obrigatória")]
    public DateTime DataInicio { get; set; }

    [Required(ErrorMessage = "Data fim é obrigatória")]
    public DateTime DataFim { get; set; }

    [StringLength(100, ErrorMessage = "Nome do comerciante deve ter no máximo 100 caracteres")]
    public string? Comerciante { get; set; }
}