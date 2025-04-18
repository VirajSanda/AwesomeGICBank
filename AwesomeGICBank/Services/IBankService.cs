using AwesomeGICBank.Models;
using System.Security.Principal;

namespace AwesomeGICBank.Services
{
    public interface IBankService
    {
        void ProcessTransaction(DateTime date, string accNumber, char type, decimal amount);
        void AddInterestRule(DateTime date, string ruleId, decimal rate);
        void PrintAccountStatement(string accNumber);
        void PrintInterestRules();
        void PrintMonthlyStatement(string accNumber, int year, int month);
        List<Transaction> GetTransactionsForAccount(string accNumber);
        decimal GetAccountBalance(string accNumber);
    }
}