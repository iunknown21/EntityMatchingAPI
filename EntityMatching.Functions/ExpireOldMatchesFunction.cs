using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Timer-triggered function to expire old match requests
    /// Runs every hour to check for expired match requests
    /// </summary>
    public class ExpireOldMatchesFunction
    {
        private readonly IMatchService _matchService;
        private readonly ILogger<ExpireOldMatchesFunction> _logger;

        public ExpireOldMatchesFunction(
            IMatchService matchService,
            ILogger<ExpireOldMatchesFunction> logger)
        {
            _matchService = matchService;
            _logger = logger;
        }

        /// <summary>
        /// Timer trigger that runs every hour (0 */1 * * * = top of every hour)
        /// Schedule format: {second} {minute} {hour} {day} {month} {day-of-week}
        /// Example schedules:
        /// - "0 */1 * * * *" = Every hour
        /// - "0 0 * * * *" = Every hour at minute 0
        /// - "0 0 */4 * * *" = Every 4 hours
        /// - "0 0 2 * * *" = Daily at 2:00 AM
        /// </summary>
        [Function("ExpireOldMatches")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo)
        {
            try
            {
                _logger.LogInformation("ExpireOldMatches function started at {Time}", DateTime.UtcNow);

                var expiredCount = await _matchService.ExpireOldMatchRequestsAsync();

                _logger.LogInformation(
                    "ExpireOldMatches function completed at {Time}. Expired {Count} match requests. Next run at {NextRun}",
                    DateTime.UtcNow, expiredCount, timerInfo.ScheduleStatus?.Next);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExpireOldMatches function");
                throw; // Re-throw to let Azure Functions runtime handle it
            }
        }
    }

    /// <summary>
    /// Timer info for scheduled functions
    /// </summary>
    public class TimerInfo
    {
        public ScheduleStatus? ScheduleStatus { get; set; }
        public bool IsPastDue { get; set; }
    }

    public class ScheduleStatus
    {
        public DateTime Last { get; set; }
        public DateTime Next { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
