namespace AwesomeGICBank.Models
{
    public class InterestRule
    {
        public DateTime Date { get; }
        public string RuleId { get; }
        public decimal Rate { get; }

        public InterestRule(DateTime date, string ruleId, decimal rate)
        {
            Date = date;
            RuleId = ruleId;
            Rate = rate;
        }

    }
}