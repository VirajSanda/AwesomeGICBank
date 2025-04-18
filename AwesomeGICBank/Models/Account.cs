
namespace AwesomeGICBank.Models
{
    public class Account
    {
        public string AccountNumber { get; }
        public decimal Balance { get; set; }
        public List<Transaction> Transactions { get; } = new List<Transaction>();

        public Account(string accNumber)
        {
            AccountNumber = accNumber;
            Balance = 0;
        }
    }
}