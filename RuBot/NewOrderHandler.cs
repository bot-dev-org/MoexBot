using RuBot.Models.Terminal;
using RuBot.Utils;
using RuBot.ViewModels.Strategies;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuBot
{
    public class NewOrderHandler
    {
        private SecurityInfo Security;
        public QuikTerminalModel TerminalModel;
        private readonly List<Transaction> Transactions = new List<Transaction>();
        private readonly string _ACCID;
        private DateTime nextQuoteTime = DateTime.Now + TimeSpan.FromDays(1);
        private object _lock = new object();
        private BaseStrategy RaisedStrategy;
        private decimal deltaPrice;
        private int quotePeriod = 60;
        private readonly List<BaseStrategy> Strategies = new List<BaseStrategy>();
        public int IsAlive = 1;
        public NewOrderHandler(SecurityInfo security, QuikTerminalModel terminalModel, string accid)
        {
            Security = security;
            TerminalModel = terminalModel;
            TerminalModel._quik.Events.OnOrder += OrderChanged;
            _ACCID = accid;
        }
        public void RegisterStrategy(BaseStrategy strategy)
        {
            Strategies.Add(strategy);
            strategy.OrderHandler = this;
        }
        public long GetCurrentPosition(SecurityInfo Security)
        {
            return TerminalModel.GetPositions(Security);
        }
        public int UpdatePositions(decimal price, decimal _deltaPrice, int _quotePeriod, BaseStrategy raisedStrategy)
        {
            if (Strategies.Sum(strat => strat.MakeDeals ? 1 : 0) == 0)
            {
                nextQuoteTime = DateTime.Now + TimeSpan.FromDays(1);
                return 0; 
            }
            lock (_lock)
            {
                RaisedStrategy = raisedStrategy;
                deltaPrice = _deltaPrice;
                quotePeriod = _quotePeriod;
                var orders = TerminalModel._quik.Orders.GetOrders(Security.ClassCode, Security.SecCode).Result;
                foreach (var order in orders)
                {
                    Logger.LogDebug($"Check order to kill: {order.SecCode} {order.Operation} {order.Balance} {order.OrderNum} {order.Price} {order.State}");
                    if (order.State == State.Active)
                        KillOrder(order);
                }
                Transactions.Clear();
                var currentPosition = GetCurrentPosition(Security);
                var targetPosition = Strategies.Sum(strat => strat.MakeDeals ? strat.LastValue * strat.InitialVolume : 0) * IsAlive;
                var toDeal = targetPosition - currentPosition;
                if (toDeal == 0)
                {
                    nextQuoteTime = DateTime.Now + TimeSpan.FromDays(1);
                    return (int)toDeal;
                }
                var target_price = price + Convert.ToDecimal(deltaPrice * (toDeal > 0 ? -1 : 1));
                var transaction = new Transaction
                {
                    ACTION = TransactionAction.NEW_ORDER,
                    ACCOUNT = _ACCID,
                    CLASSCODE = Security.ClassCode,
                    SECCODE = Security.SecCode,
                    QUANTITY = (int)Math.Abs(toDeal),
                    OPERATION = toDeal > 0 ? TransactionOperation.B : TransactionOperation.S,
                    PRICE = target_price
                };
                try
                {
                    nextQuoteTime = DateTime.Now + TimeSpan.FromSeconds(quotePeriod);
                    Logger.Log($"{Security.SecCode} positions: current = {currentPosition} target = {targetPosition}. Place order {(toDeal>0?"Buy":"Sell")} at {target_price}. Current price {price}");
                    if (RegisterTransaction(transaction) < 0)
                    {
                        nextQuoteTime = DateTime.Now;
                    }
                }
                catch (Exception exp)
                {
                    Logger.LogException(exp);
                    return -1;
                }
                return (int)toDeal;
            }
        }
        public void OrderChanged(Order order)
        {
            if (order.SecCode != Security.SecCode || RaisedStrategy == null)
                return;
            lock (Transactions)
            {
                var trans = Transactions.FirstOrDefault(t => t.TRANS_ID == order.TransID);
                if (trans != null)
                {
                    Transactions.Remove(trans);
                    RaisedStrategy.OrderHandler_OnNewOrder(order);
                }
            }
        }
        public void CheckOrders(AllTrade trade)
        {
            lock (_lock)
            {
                if (DateTime.Now > nextQuoteTime)
                {
                    UpdatePositions((decimal)trade.Price, deltaPrice, quotePeriod, RaisedStrategy);
                }
            }
        }
        private int RegisterTransaction(Transaction transaction)
        {
            try
            {
                transaction.TRANS_ID = null;
                transaction.CLIENT_CODE = null;
                if (transaction.PRICE > Security.MaxPrice && Security.MaxPrice > 0)
                {
                    Logger.Log($"Changing price to max {Security.MaxPrice}");
                    transaction.PRICE = Security.MaxPrice;
                }
                if (transaction.PRICE < Security.MinPrice && Security.MinPrice > 0)
                {
                    Logger.Log($"Changing price to min {Security.MinPrice}");
                    transaction.PRICE = Security.MinPrice;
                }
                if (Security.MinPrice <= 0 || Security.MaxPrice <= 0)
                {
                    Logger.Log("MinPrice or MaxPrice equals to zero");
                }
                if (transaction.QUANTITY <= 0)
                {
                    Logger.Log("newOrder.QUANTITY <= 0");
                }
                var result = TerminalModel._quik.Trading.SendTransaction(transaction).Result;
                if (result < 0)
                {
                    Logger.Log($"Failed raising order: Operation={transaction.OPERATION}, Price={transaction.PRICE}, Quantity={transaction.QUANTITY} - {transaction.ErrorMessage}");
                    return -1;
                }
                Transactions.Add(transaction);
            }
            catch (Exception exp)
            {
                Logger.Log($"Cannot execute order: {transaction.OPERATION} {transaction.PRICE} Volume: {transaction.QUANTITY}");
                Logger.LogException(exp);
                return -1;
            }
            return 1;
        }
        private long KillOrder(Order toKill)
        {
            Logger.LogDebug($"Killing order: Operation={toKill.Operation}, Price={toKill.Price}, Quantity={toKill.Quantity}");
            var killOrderTransaction = new Transaction
            {
                ACTION = TransactionAction.KILL_ORDER,
                CLASSCODE = toKill.ClassCode,
                SECCODE = toKill.SecCode,
                ORDER_KEY = toKill.OrderNum.ToString()
            };
            var result = TerminalModel._quik.Trading.SendTransaction(killOrderTransaction).Result;
            if (result < 0)
            {
                Logger.Log($"Failed killing order: Operation={toKill.Operation}, Price={toKill.Price}, Quantity={toKill.Quantity} - {killOrderTransaction.ErrorMessage}");
            }
            return result;
        }
    }
}
