using AwesomeGICBank.Models;
using AwesomeGICBank.Services;
using System;
using System.Globalization;

namespace AwesomeGICBank
{
    class Program
    {
        private static readonly IBankService _bankService = new BankService();
        private readonly Dictionary<string, Account> _accounts = new Dictionary<string, Account>();
        private readonly List<InterestRule> _interestRules = new List<InterestRule>();
        private readonly Dictionary<DateTime, int> _dailyTransactionCount = new Dictionary<DateTime, int>();

        public void ProcessTransaction(DateTime date, string accNumber, char type, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            if (type != 'D' && type != 'W')
                throw new ArgumentException("Transaction type must be 'D' or 'W'.");

            if (!_accounts.TryGetValue(accNumber, out var account))
            {
                if (type == 'W')
                    throw new InvalidOperationException("First transaction for an account cannot be a withdrawal.");

                account = new Account(accNumber);
                _accounts[accNumber] = account;
            }

            if (type == 'W' && account.Balance < amount)
                throw new InvalidOperationException("Insufficient balance for withdrawal.");

            account.Balance = type == 'D' ? account.Balance + amount : account.Balance - amount;

            if (!_dailyTransactionCount.ContainsKey(date.Date))
                _dailyTransactionCount[date.Date] = 0;

            _dailyTransactionCount[date.Date]++;
            var transactionId = $"{date:yyyyMMdd}-{_dailyTransactionCount[date.Date]:00}";
            var transaction = new Transaction(date, accNumber, type, amount, account.Balance, transactionId);
            account.Transactions.Add(transaction);

            _bankService.PrintAccountStatement(accNumber);
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to AwesomeGIC Bank! What would you like to do?");

            while (true)
            {
                ShowMainMenu();
                var choice = Console.ReadLine()?.ToUpper();

                switch (choice)
                {
                    case "T":
                        ProcessTransactionMenu();
                        break;
                    case "I":
                        ProcessInterestRuleMenu();
                        break;
                    case "P":
                        ProcessPrintStatementMenu();
                        break;
                    case "Q":
                        Console.WriteLine("\nThank you for banking with AwesomeGIC Bank.");
                        Console.WriteLine("Have a nice day!");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        static void ShowMainMenu()
        {
            Console.WriteLine("\n[T] Input transactions");
            Console.WriteLine("[I] Define interest rules");
            Console.WriteLine("[P] Print statement");
            Console.WriteLine("[Q] Quit");
            Console.Write("> ");
        }
        static void ProcessTransactionMenu()
        {
            Console.WriteLine("\nPlease enter transaction details in <Date> <Account> <Type> <Amount> format");
            Console.WriteLine("Example: 20230626 AC001 W 100.00");
            Console.WriteLine("(or enter blank to go back to main menu):");

            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    return;

                try
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 4)
                        throw new FormatException("Invalid input format. Expected: <Date> <Account> <Type> <Amount>");

                    if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        throw new FormatException("Date must be in YYYYMMdd format.");

                    var account = parts[1];
                    var type = char.ToUpper(parts[2][0]);
                    var amount = decimal.Parse(parts[3]);

                    if (amount <= 0)
                        throw new ArgumentException("Amount must be greater than zero.");
                    if (Math.Round(amount, 2) != amount)
                        throw new ArgumentException("Amount can have maximum 2 decimal places.");

                    _bankService.ProcessTransaction(date, account, type, amount);

                    Console.WriteLine($"\nAccount: {account}");
                    Console.WriteLine("| Date     | Txn Id      | Type | Amount   | Balance    |");
                    Console.WriteLine("|----------|-------------|------|----------|------------|");

                    var transactions = _bankService.GetTransactionsForAccount(account);
                    foreach (var txn in transactions)
                    {
                        Console.WriteLine($"| {txn.Date:yyyyMMdd} | {txn.TransactionId} | {txn.Type}    | {txn.Amount,8:F2} | {txn.Balance,10:F2} |");
                    }

                    Console.WriteLine($"\nInterest earned: 0.00");
                    Console.WriteLine($"Final balance: {_bankService.GetAccountBalance(account):F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Please try again or press enter to go back.");
                }
            }
        }

        static void ProcessInterestRuleMenu()
        {
            Console.WriteLine("\nPlease enter interest rules details in <Date> <RuleId> <Rate in %> format");
            Console.WriteLine("Example: 20230615 RULE03 2.20");
            Console.WriteLine("(or enter blank to go back to main menu):");

            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    return;

                try
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 3)
                        throw new FormatException("Invalid input format. Expected: <Date> <RuleId> <Rate>");

                    if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        throw new FormatException("Date must be in YYYYMMdd format.");

                    var ruleId = parts[1];

                    if (!decimal.TryParse(parts[2], out var rate))
                        throw new FormatException("Rate must be a valid number.");

                    _bankService.AddInterestRule(date, ruleId, rate);
                    _bankService.PrintInterestRules();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Please try again or press enter to go back.");
                }
            }
        }

        static void ProcessPrintStatementMenu()
        {
            Console.WriteLine("\nPlease enter account and month to generate the statement <Account> <Year><Month>");
            Console.WriteLine("Example: AC001 202306");
            Console.WriteLine("(or enter blank to go back to main menu):");

            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    return;

                try
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        throw new FormatException("Invalid input format. Expected: <Account> <YearMonth>");

                    var accNumber = parts[0];
                    var yearMonth = parts[1];

                    if (yearMonth.Length != 6 || !int.TryParse(yearMonth.Substring(0, 4), out var year) ||
                        !int.TryParse(yearMonth.Substring(4, 2), out var month) || month < 1 || month > 12)
                        throw new FormatException("Month must be in YYYYMM format (e.g., 202306)");

                    _bankService.PrintMonthlyStatement(accNumber, year, month);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Please try again or press enter to go back.");
                }
            }
        }
    }
}