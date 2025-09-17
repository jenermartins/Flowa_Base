using System;
using System.Collections.Concurrent;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using FIX44 = QuickFix.FIX44;
using QFixMessage = QuickFix.Message;


namespace OrderAccumulator
{
    public class OrderAccumulatorApp : IApplication
    {
        private readonly ConcurrentDictionary<string, decimal> _exposureBySymbol = new();
        private const decimal ExposureLimit = 100_000_000m; // Limite de exposição

        //Métodos da interface IApplication
        public void ToAdmin(QFixMessage message, SessionID sessionID) { }
        public void FromAdmin(QFixMessage message, SessionID sessionID) { }
        public void ToApp(QFixMessage message, SessionID sessionID) { }
        public void FromApp(QFixMessage message, SessionID sessionID)
        {
            //Processar mensagem de ordem
            if (message is FIX44.NewOrderSingle order)
            {
                ProcessOrder(order, sessionID);
            }
        }
        public void OnCreate(SessionID sessionID)
        {
            Console.WriteLine($"Sessão FIX criada: {sessionID}");
        }
        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine($"Sessão FIX logada: {sessionID}");
        }
        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine($"Sessão FIX deslogada: {sessionID}");
        }

        private void ProcessOrder(FIX44.NewOrderSingle order, SessionID sessionID)
        {
            try
            {
                var symbol = order.Symbol.Value;
                var side = order.Side.Value;
                var quantity = order.OrderQty.Value;
                var price = order.Price.Value;

                Console.WriteLine($"Recebida ordem: {order.ClOrdID.Value}, Símbolo: {symbol}, Lado: {side}, Quantidade: {quantity}, Preço: {price}");

                var currentExposure = _exposureBySymbol.GetValueOrDefault(symbol, 0m);
                decimal orderValue = quantity * price;
                decimal newExposure = side == Side.BUY
                    ? currentExposure + orderValue
                    : currentExposure - orderValue;

                var executionReport = new FIX44.ExecutionReport(
                    new OrderID(Guid.NewGuid().ToString()),
                    new ExecID(Guid.NewGuid().ToString()),
                    new ExecType(ExecType.NEW),
                    new OrdStatus(OrdStatus.NEW),
                    order.Symbol,
                    order.Side,
                    new LeavesQty(0),
                    new CumQty(quantity),
                    new AvgPx(price)
                );

                executionReport.Set(order.ClOrdID);
                executionReport.Set(order.OrderQty);
                executionReport.Set(order.Price);

                if (Math.Abs(newExposure) > ExposureLimit)
                {
                    //Rejeitar ordem
                    executionReport.ExecType = new ExecType(ExecType.REJECTED);
                    executionReport.OrdStatus = new OrdStatus(OrdStatus.REJECTED);
                    Console.WriteLine($"Ordem rejeitada: {order.ClOrdID.Value}, Limite de exposição excedido para o símbolo {symbol}. Exposição atual: {currentExposure}, Valor da ordem: {orderValue}, Nova exposição: {newExposure}");

                }
                else
                {
                    //Aceitar ordem
                    _exposureBySymbol[symbol] = newExposure;
                    executionReport.ExecType = new ExecType(ExecType.NEW);
                    executionReport.OrdStatus = new OrdStatus(OrdStatus.NEW);
                    Console.WriteLine($"Ordem aceita: {order.ClOrdID.Value}, Nova exposição para o símbolo {symbol}: {newExposure}");

                }

                Session.SendToTarget(executionReport, sessionID);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar ordem: {ex.Message}");
            }
        }

    }

    class Program
    {
        static void Main(string [] args)
        {
            try
            {
                Console.WriteLine("Iniciando Order Accumulator (Servidor FIX)...");
                var settings = new SessionSettings("orderaccumulator.cfg");
                var storefactory = new FileStoreFactory(settings);
                var logFactory = new FileLogFactory(settings);
                var application = new OrderAccumulatorApp();

                var acceptor = new ThreadedSocketAcceptor(application, storefactory, settings, logFactory);
                acceptor.Start();
                Console.WriteLine("Order Accumulator iniciado. Pressione qualquer tecla para sair.");
                Console.ReadKey();

                while (true)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na aplicação: {ex.Message}");
            }
        }

    }

}