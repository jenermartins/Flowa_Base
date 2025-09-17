using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Models;
using OrderAccumulator;

namespace OrderGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderProcessor _processor = new();
        private static int _orderCounter = 1;

        [HttpPost]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
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
            if (request.Price <= 0 || request.Price >= 10000 || request.Price % 0.01m != 0)
                return BadRequest("Preço deve ser estar entre 0,01 e 999,99.");

            //Processar ordem
            var order = new Order
            {
                Symbol = request.Symbol,
                Side = request.Side,
                Quantity = request.Quantity,
                Price = request.Price,
                OrderId = $"ORD-{_orderCounter++:D6}"
            };

            var result = _processor.ProcessOrder(order);

            var response = new OrderResponse
            {
                OrderId = order.OrderId,
                Accepted = result.Accepted,
                Message = result.Message,
                CurrentExposure = result.NewExposure
            };

            return Ok(response);

        }
    }
}