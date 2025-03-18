using System.Diagnostics.Contracts;

namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class BudgetsRequest
    {
        public int cagetory_id { get; set; }
        public int fiscal_year { get; set; }
        public double total_amount { get; set; }
        public double allocated_amount { get; set; }
        public double remaining_amount { get; set; }
        public DateTime created_at { get; set; }
    }
}
