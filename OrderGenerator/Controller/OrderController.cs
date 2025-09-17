using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Models;

namespace OrderGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private static int _orderCounter = 1;
        private readonly Dictionary<string, decimal> _exposure = new();
        private const decimal ExposureLimit = 100_000_000m; // Limite de exposição

        [HttpPost]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
        {
            try
            {
                //Validar símbolo
                var validSymbols = new[] { "PETR4", "VALE3", "VIIA4" };
                if (!validSymbols.Contains(request.Symbol))
                    return BadRequest("Símbolo inválido. Os símbolos válidos são: PETR4, VALE3, VIIA4.");

                //Validar lado
                if (request.Side != "Compra" && request.Side != "Venda")
                    return BadRequest("Lado inválido. Selecione Compra ou Venda.");

                //Validar quantidade
                if (request.Quantity <= 0 || request.Quantity > 100000)
                    return BadRequest("Quantidade deve serestar entre 1 e 99.999.");

                //Validar preço
                if (request.Price <= 0 || request.Price >= 10000)
                    return BadRequest("Preço deve ser estar entre 0,01 e 1000,00.");

                var currentExposure = _exposure.GetValueOrDefault(request.Symbol, 0m);
                var orderValue = request.Price * request.Quantity;

                var newExposure = request.Side == "Compra"
                    ? currentExposure + orderValue
                    : currentExposure - orderValue;

                var orderId = $"ORD-{_orderCounter++:D6}";

                if (Math.Abs(newExposure) <= 100_000_000m)
                {
                    _exposure[request.Symbol] = newExposure;
                    return Ok(new
                    {
                        orderId = orderId,
                        accepted = true,
                        message = $"Ordem aceita. Exposição {request.Symbol}: R$ {newExposure:N2}",
                        currentExposure = newExposure
                    });
                }
                else
                {
                    return Ok(new
                    {
                        orderId = orderId,
                        accepted = false,
                        message = $"Ordem rejeitada. Limite excedido",
                        currentExposure = newExposure
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Erro: {ex.Message}" });
            }

        }
    }
}