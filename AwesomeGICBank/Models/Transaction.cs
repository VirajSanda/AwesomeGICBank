namespace AwesomeGICBank.Models
{
    public class Transaction
    {
        public DateTime Date { get; }
        public string AccountNumber { get; }
        public char Type { get; }
        public decimal Amount { get; }
        public decimal Balance { get; }
        public string TransactionId { get; }

        public Transaction(DateTime date, string accNumber, char type, decimal amount, decimal balance, string transactionId)
        {
            Date = date;
            AccountNumber = accNumber;
            Type = type;
            Amount = amount;
            Balance = balance;
            TransactionId = transactionId;
        }
    }
}