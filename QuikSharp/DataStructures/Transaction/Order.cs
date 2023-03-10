// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QuikSharp.DataStructures.Transaction
{
    /// <summary>
    /// Описание параметров Таблицы заявок
    /// </summary>
    public class Order : IWithLuaTimeStamp
    {
        [JsonProperty("lua_timestamp")]
        public long LuaTimeStamp { get; internal set; }

        /// <summary>
        /// Номер заявки в торговой системе
        /// </summary>
        [JsonProperty("order_num")]
        public long OrderNum { get; set; }

        /// <summary>
        /// Поручение/комментарий, обычно: код клиента/номер поручения
        /// </summary>
        [JsonProperty("brokerref")]
        public string Comment { get; set; }

        /// <summary>
        /// Идентификатор трейдера
        /// </summary>
        [JsonProperty("userid")]
        public string UserId { get; set; }

        /// <summary>
        /// Идентификатор фирмы
        /// </summary>
        [JsonProperty("firmid")]
        public string FirmId { get; set; }

        /// <summary>
        /// Торговый счет
        /// </summary>
        [JsonProperty("account")]
        public string Account { get; set; }

        /// <summary>
        /// Цена
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Количество в лотах
        /// </summary>
        [JsonProperty("qty")]
        public int Quantity { get; set; }

        /// <summary>
        /// Остаток
        /// </summary>
        [JsonProperty("balance")]
        public int Balance { get; set; }

        /// <summary>
        /// Объем в денежных средствах
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// Накопленный купонный доход
        /// </summary>
        [JsonProperty("accruedint")]
        public decimal AccruedInterest { get; set; }

        /// <summary>
        /// Доходность
        /// </summary>
        [JsonProperty("yield")]
        public decimal Yield { get; set; }

        /// <summary>
        /// Идентификатор транзакции
        /// </summary>
        [JsonProperty("trans_id")]
        public long TransID { get; set; }

        /// <summary>
        /// Код клиента
        /// </summary>
        [JsonProperty("client_code")]
        public string ClientCode { get; set; }

        /// <summary>
        /// Цена выкупа
        /// </summary>
        [JsonProperty("price2")]
        public decimal Price2 { get; set; }

        /// <summary>
        /// Код расчетов
        /// </summary>
        [JsonProperty("settlecode")]
        public string Settlecode { get; set; }

        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        [JsonProperty("uid")]
        public long Uid { get; set; }

        /// <summary>
        /// Код биржи в торговой системе
        /// </summary>
        [JsonProperty("exchange_code")]
        public string ExchangeCode { get; set; }

        /// <summary>
        /// Время активации
        /// </summary>
        [JsonProperty("activation_time")]
        public decimal ActivationTime { get; set; }

        /// <summary>
        /// Номер заявки в торговой системе
        /// </summary>
        [JsonProperty("linkedorder")]
        public long Linkedorder { get; set; }

        /// <summary>
        /// Дата окончания срока действия заявки
        /// </summary>
        [JsonProperty("expiry")]
        public decimal Expiry { get; set; }

        /// <summary>
        /// Код бумаги заявки
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// Код класса заявки
        /// </summary>
        [JsonProperty("class_code")]
        public string ClassCode { get; set; }

        /// <summary>
        /// Дата и время
        /// </summary>
        [JsonProperty("datetime")]
        public QuikDateTime Datetime { get; set; }

        /// <summary>
        /// Дата и время снятия заявки
        /// </summary>
        [JsonProperty("withdraw_datetime")]
        public QuikDateTime WithdrawDatetime { get; set; }

        /// <summary>
        /// Идентификатор расчетного счета/кода в клиринговой организации
        /// </summary>
        [JsonProperty("bank_acc_id")]
        public string BankAccID { get; set; }

        /// <summary>
        /// Способ указания объема заявки. Возможные значения: «0» – по количеству, «1» – по объему
        /// </summary>
        [JsonProperty("value_entry_type")]
        public int ValueEntryType { get; set; }

        /// <summary>
        /// Срок РЕПО, в календарных днях
        /// </summary>
        [JsonProperty("repoterm")]
        public decimal Repoterm { get; set; }

        /// <summary>
        /// Сумма РЕПО на текущую дату. Отображается с точностью 2 знака
        /// </summary>
        [JsonProperty("repovalue")]
        public decimal Repovalue { get; set; }

        /// <summary>
        /// Объём сделки выкупа РЕПО. Отображается с точностью 2 знака
        /// </summary>
        [JsonProperty("repo2value")]
        public decimal Repo2Value { get; set; }

        /// <summary>
        /// Остаток суммы РЕПО за вычетом суммы привлеченных или предоставленных по сделке РЕПО денежных средств в неисполненной части заявки, по состоянию на текущую дату. Отображается с точностью 2 знака
        /// </summary>
        [JsonProperty("repo_value_balance")]
        public decimal RepoValueBalance { get; set; }

        /// <summary>
        /// Начальный дисконт, в %
        /// </summary>
        [JsonProperty("start_discount")]
        public decimal StartDiscount { get; set; }

        /// <summary>
        /// Причина отклонения заявки брокером
        /// </summary>
        [JsonProperty("reject_reason")]
        public string RejectReason { get; set; }

        /// <summary>
        /// Битовое поле для получения специфических параметров с западных площадок
        /// </summary>
        [JsonProperty("ext_order_flags")]
        public int ExtOrderFlags { get; set; }

        /// <summary>
        /// Минимально допустимое количество, которое можно указать в заявке по данному инструменту. Если имеет значение «0», значит ограничение по количеству не задано
        /// </summary>
        [JsonProperty("min_qty")]
        public int MinQty { get; set; }

        /// <summary>
        /// Тип исполнения заявки. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("exec_type")]
        public int ExecType { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("side_qualifier")]
        public int SideQualifier { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("acnt_type")]
        public int AcntType { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("capacity")]
        public int Capacity { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("passive_only_order")]
        public int PassiveOnlyOrder { get; set; }

        /// <summary>
        /// Видимое количество. Параметр айсберг-заявок, для обычных заявок выводится значение: «0»
        /// </summary>
        [JsonProperty("visible")]
        public int Visible { get; set; }

        /// <summary>
        /// Средняя цена приобретения. Актуально, когда заявка выполнилась частями
        /// </summary>
        [JsonProperty("awg_price")]
        public decimal AwgPrice { get; set; }

        /// <summary>
        /// Время окончания срока действия заявки в формате "ЧЧММСС DESIGNTIMESP=19552". Для GTT-заявок, используется вместе со сроком истечения заявки (Expiry)
        /// </summary>
        [JsonProperty("expiry_time")]
        public int ExpiryTime { get; set; }

        /// <summary>
        /// Номер ревизии заявки. Используется, если заявка была заменена с сохранением номера
        /// </summary>
        [JsonProperty("revision_number")]
        public int RevisionNumber { get; set; }

        /// <summary>
        /// Валюта цены заявки
        /// </summary>
        [JsonProperty("price_currency")]
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Расширенный статус заявки. Возможные значения: 
        /// «0» (по умолчанию) – не определено; 
        /// «1» – заявка активна; 
        /// «2» – заявка частично исполнена; 
        /// «3» – заявка исполнена; 
        /// «4» – заявка отменена; 
        /// «5» – заявка заменена; 
        /// «6» – заявка в состоянии отмены; 
        /// «7» – заявка отвергнута; 
        /// «8» – приостановлено исполнение заявки; 
        /// «9» – заявка в состоянии регистрации; 
        /// «10» – заявка снята по времени действия; 
        /// «11» – заявка в состоянии замены
        /// </summary>
        [JsonProperty("ext_order_status")]
        public int ExtOrderStatus { get; set; }

        /// <summary>
        /// UID пользователя-менеджера, подтвердившего заявку при работе в режиме с подтверждениями
        /// </summary>
        [JsonProperty("accepted_uid")]
        public long AcceptedUID { get; set; }

        /// <summary>
        /// Исполненный объем заявки в валюте цены для частично или полностью исполненных заявок
        /// </summary>
        [JsonProperty("filled_value")]
        public int FilledValue { get; set; }

        /// <summary>
        /// Внешняя ссылка, используется для обратной связи с внешними системами
        /// </summary>
        [JsonProperty("extref")]
        public string ExtRef { get; set; }

        /// <summary>
        /// Валюта расчетов по заявке
        /// </summary>
        [JsonProperty("settle_currency")]
        public string SettleCurrency { get; set; }

        /// <summary>
        /// UID пользователя, от имени которого выставлена заявка
        /// </summary>
        [JsonProperty("on_behalf_of_uid")]
        public long OnBehalfOfUID { get; set; }

        /// <summary>
        /// Квалификатор клиента, от имени которого выставлена заявка. Возможные значения: 
        /// «0» – не определено; 
        /// «1» – Natural Person; 
        /// «3» – Legal Entity
        /// </summary>
        [JsonProperty("client_qualifier")]
        public int ClientQualifier { get; set; }

        /// <summary>
        /// Краткий идентификатор клиента, от имени которого выставлена заявка
        /// </summary>
        [JsonProperty("client_short_code")]
        public long ClientShortCode { get; set; }

        /// <summary>
        /// Квалификатор принявшего решение о выставлении заявки. Возможные значения: 
        /// «0» – не определено; 
        /// «1» – Natural Person; 
        /// «2» – Algorithm
        /// </summary>
        [JsonProperty("investment_decision_maker_qualifier")]
        public long InvestmentDecisionMakerQualifier { get; set; }

        /// <summary>
        /// Краткий идентификатор принявшего решение о выставлении заявки
        /// </summary>
        [JsonProperty("investment_decision_maker_short_code")]
        public long InvestmentDecisionMakerShortCode { get; set; }

        /// <summary>
        /// Квалификатор трейдера, исполнившего заявку. Возможные значения: 
        /// «0» – не определено; 
        /// «1» – Natural Person; 
        /// «2» – Algorithm
        /// </summary>
        [JsonProperty("executing_trader_qualifier")]
        public long ExecutingTraderQualifier { get; set; }

        /// <summary>
        /// Краткий идентификатор трейдера, исполнившего заявку
        /// </summary>
        [JsonProperty("executing_trader_short_code")]
        public long ExecutingTraderShortCode { get; set; }

        /// <summary>
        /// Тип операции - Buy или Sell
        /// </summary>
        [JsonIgnore]
        public Operation Operation { get; set; }

        /// <summary>
        /// Состояние заявки.
        /// </summary>
        [JsonIgnore]
        public State State { get; set; }

        private OrderTradeFlags _flags;

        /// <summary>
        /// Набор битовых флагов
        /// http://help.qlua.org/ch9_1.htm
        /// </summary>
        [JsonProperty("flags")]
        public OrderTradeFlags Flags
        {
            get => _flags;
            set
            {
                _flags = value;
                ParseFlags();
            }
        }

        private void ParseFlags()
        {
            Operation = Flags.HasFlag(OrderTradeFlags.IsSell) ? Operation.Sell : Operation.Buy;

            State = Flags.HasFlag(OrderTradeFlags.Active)
                ? State.Active
                : (Flags.HasFlag(OrderTradeFlags.Canceled)
                    ? State.Canceled
                    : State.Completed);
        }
    }
}