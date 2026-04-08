using Microsoft.AspNetCore.SignalR;

namespace WebUI.Hubs
{
    public class ImportProgressHub : Hub
    {
        public async Task SendProgress(int processed, int total, string message)
        {
            await Clients.All.SendAsync("ReceiveProgress", new
            {
                Processed = processed,
                Total = total,
                Percentage = (int)((processed * 100) / total),
                Message = message
            });
        }

        public async Task SendError(string errorMessage)
        {
            await Clients.All.SendAsync("ReceiveError", errorMessage);
        }

        public async Task SendComplete(int successCount, List<string> errors)
        {
            await Clients.All.SendAsync("ReceiveComplete", new
            {
                SuccessCount = successCount,
                Errors = errors,
                TotalErrors = errors.Count
            });
        }
    }
}
