using AwesomeGICBank.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace AwesomeGICBank.Services
{
    public class BankService : IBankService
    {
        private readonly Dictionary<string, Account> _accounts = new Dictionary<string, Account>();
        private readonly List<InterestRule> _interestRules = new List<InterestRule>();
        private int _transactionCounter = 1;

        public void ProcessTransaction(DateTime date, string accNumber, char type, decimal amount)
        {
            if (!_accounts.TryGetValue(accNumber, out var account))
            {
                account = new Account(accNumber);
                _accounts[accNumber] = account;
            }

            if (type == 'W' && account.Balance < amount)
                throw new InvalidOperationException("Insufficient balance");

            account.Balance = type == 'D' ? account.Balance + amount : account.Balance - amount;

            var transactionId = $"T{_transactionCounter++:000}";
            var transaction = new Transaction(date, accNumber, type, amount, account.Balance, transactionId);
            account.Transactions.Add(transaction);
        }
        public void AddInterestRule(DateTime date, string ruleId, decimal rate)
        {
            if (rate <= 0 || rate >= 100)
                throw new ArgumentException("Interest rate must be greater than 0 and less than 100.");

            var existingRule = _interestRules.FirstOrDefault(r => r.Date == date);
            if (existingRule != null)
                _interestRules.Remove(existingRule);

            _interestRules.Add(new InterestRule(date, ruleId, rate));
            _interestRules.Sort();
        }

        public void PrintInterestRules()
        {
            Console.WriteLine("\nInterest rules:");
            Console.WriteLine("| Date     | RuleId | Rate (%) |");
            Console.WriteLine("|----------|--------|----------|");

            foreach (var rule in _interestRules.OrderBy(r => r.Date))
            {
                Console.WriteLine($"| {rule.Date:yyyyMMdd} | {rule.RuleId,-6} | {rule.Rate,8:F2} |");
            }
        }

        public void PrintAccountStatement(string accNumber)
        {
            if (!_accounts.TryGetValue(accNumber, out var account))
                throw new KeyNotFoundException("Account not found.");

            Console.WriteLine($"Account: {accNumber}");
            Console.WriteLine("Date     | Txn Id      | Type | Amount   | Balance");
            Console.WriteLine("---------|-------------|------|----------|---------");

            foreach (var txn in account.Transactions.OrderBy(t => t.Date))
            {
                Console.WriteLine($"{txn.Date:yyyyMMdd} | {txn.TransactionId} | {(txn.Type == 'D' ? "Deposit" : "Withdraw")}  | {txn.Amount,8:F2} | {txn.Balance,8:F2}");
            }

            var interest = CalculateInterest(account);
            Console.WriteLine($"\nInterest earned: {interest:F2}");
            Console.WriteLine($"Final balance: {(account.Balance + interest):F2}");
        }

        private decimal CalculateInterest(Account account)
        {
            if (account.Transactions.Count == 0 || _interestRules.Count == 0)
                return 0;

            var sortedTxns = account.Transactions.OrderBy(t => t.Date).ToList();
            var sortedRules = _interestRules.OrderBy(r => r.Date).ToList();

            decimal totalInterest = 0;
            decimal currentRate = 0;
            DateTime periodStart = sortedTxns[0].Date;
            decimal periodBalance = 0;

            foreach (var txn in sortedTxns)
            {
                var ruleToApply = sortedRules.LastOrDefault(r => r.Date <= txn.Date);
                if (ruleToApply != null && ruleToApply.Rate != currentRate)
                {
                    if (currentRate != 0) 
                    {
                        var days = (txn.Date - periodStart).Days;
                        totalInterest += periodBalance * (currentRate / 100) * days / 365;
                    }
                    currentRate = ruleToApply.Rate;
                    periodStart = txn.Date;
                }
                periodBalance = txn.Balance;
            }

            if (currentRate != 0)
            {
                var endDate = DateTime.Now.Date;
                var days = (endDate - periodStart).Days;
                if (days > 0)
                    totalInterest += periodBalance * (currentRate / 100) * days / 365;
            }

            return totalInterest;
        }

        public void PrintMonthlyStatement(string accNumber, int year, int month)
        {
            if (!_accounts.TryGetValue(accNumber, out var account))
                throw new KeyNotFoundException("Account not found.");

            var monthlyTransactions = account.Transactions
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .OrderBy(t => t.Date)
                .ToList();

            var interest = CalculateMonthlyInterest(accNumber, year, month);
            var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            Console.WriteLine($"Account: {accNumber}");
            Console.WriteLine("| Date     | Txn Id      | Type | Amount   | Balance   |");
            Console.WriteLine("|----------|-------------|------|----------|-----------|");

            foreach (var txn in monthlyTransactions)
            {
                Console.WriteLine($"| {txn.Date:yyyyMMdd} | {txn.TransactionId} | {txn.Type}    | {txn.Amount,8:F2} | {txn.Balance,9:F2} |");
            }

            if (interest > 0)
            {
                Console.WriteLine($"| {lastDayOfMonth:yyyyMMdd} |             | I    | {interest,8:F2} | {(monthlyTransactions.LastOrDefault()?.Balance ?? 0) + interest,9:F2} |");
            }
            else if (monthlyTransactions.Any())
            {
                Console.WriteLine($"| {lastDayOfMonth:yyyyMMdd} |             | I    |    0.00 | {monthlyTransactions.Last().Balance,9:F2} |");
            }
        }

        private decimal CalculateMonthlyInterest(string accNumber, int year, int month)
        {
            if (!_accounts.TryGetValue(accNumber, out var account) || !account.Transactions.Any())
                return 0;

            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var dailyBalances = GetDailyBalances(account, startDate, endDate);

            decimal totalInterest = 0;
            DateTime currentPeriodStart = startDate;
            decimal currentRate = 0;
            decimal currentBalance = 0;

            var applicableRules = _interestRules
                .Where(r => r.Date <= endDate)
                .OrderBy(r => r.Date)
                .ToList();

            if (!applicableRules.Any())
                return 0;

            applicableRules.Add(new InterestRule(endDate.AddDays(1), "DUMMY", 0));

            foreach (var rule in applicableRules)
            {
                var ruleDate = rule.Date > endDate ? endDate : rule.Date;

                if (ruleDate > currentPeriodStart)
                {
                    var daysInPeriod = (ruleDate - currentPeriodStart).Days;
                    if (daysInPeriod > 0 && currentRate > 0)
                    {
                        var periodBalance = dailyBalances.ContainsKey(currentPeriodStart)
                            ? dailyBalances[currentPeriodStart]
                            : currentBalance;

                        var periodInterest = periodBalance * currentRate / 100 * daysInPeriod / 365;
                        totalInterest += periodInterest;
                    }
                }

                currentPeriodStart = ruleDate;
                currentRate = rule.Rate;
            }

            return Math.Round(totalInterest, 2);
        }

        private Dictionary<DateTime, decimal> GetDailyBalances(Account account, DateTime startDate, DateTime endDate)
        {
            var dailyBalances = new Dictionary<DateTime, decimal>();
            var transactions = account.Transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderBy(t => t.Date)
                .ToList();

            decimal currentBalance = 0;
            DateTime currentDate = startDate;

            var previousTransactions = account.Transactions
                .Where(t => t.Date < startDate)
                .OrderBy(t => t.Date)
                .ToList();

            if (previousTransactions.Any())
                currentBalance = previousTransactions.Last().Balance;

            while (currentDate <= endDate)
            {
                var dailyTransactions = transactions.Where(t => t.Date.Date == currentDate.Date);
                foreach (var txn in dailyTransactions)
                {
                    currentBalance = txn.Balance;
                }

                dailyBalances[currentDate.Date] = currentBalance;
                currentDate = currentDate.AddDays(1);
            }

            return dailyBalances;
        }
        public List<Transaction> GetTransactionsForAccount(string accNumber)
        {
            if (!_accounts.TryGetValue(accNumber, out var account))
                throw new KeyNotFoundException("Account not found.");

            return account.Transactions.OrderBy(t => t.Date).ToList();
        }

        public decimal GetAccountBalance(string accNumber)
        {
            if (!_accounts.TryGetValue(accNumber, out var account))
                throw new KeyNotFoundException("Account not found.");
            return account.Balance;
        }
    }
}