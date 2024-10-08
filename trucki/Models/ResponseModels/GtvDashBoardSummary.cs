﻿namespace trucki.Models.ResponseModels
{
    public class GtvDashboardSummary
    {
        public float TotalGtv { get; set; }
        public float TotalPayout { get; set; }
        public float TotalRevenue { get; set; }
  
        public List<LineChartEntry> MonthlyData { get; set; }
    }
    
    public class LineChartEntry
    {
        public string Name { get; set; }
        public float Income { get; set; }  // Using GTV as "income"
        public float Revenue { get; set; }
        public float Payout { get; set; }
    }
    public class TruckDahsBoardData
    {
        public int CompletedOrders { get; set; }
        public int FlaggedOrders { get; set; }
        public decimal TotalOrderPrice { get; set; }
    }

    public class ManagerDashboardData
    {
        public int CompletedOrders { get; set; }
        public int FlaggedOrders { get; set; }
        public decimal TotalOrderPrice { get; set; }
    }
}
