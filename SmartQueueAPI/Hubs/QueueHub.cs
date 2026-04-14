using Microsoft.AspNetCore.SignalR;
using SmartQueue.Core.Interfaces;

namespace SmartQueueAPI.Hubs
{
    public class QueueHub : Hub
    {
        private readonly IEstimationService _estimationService;

        public QueueHub(IEstimationService estimationService)
        {
            _estimationService = estimationService;
        }

        // ── Client joins a queue group ────────────────────────────────────────
        // Called when customer opens the queue screen on mobile/kiosk
        public async Task JoinQueueGroup(int queueId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueId}");

            // Send current queue status immediately on join
            var status = await _estimationService.GetQueueStatusAsync(queueId);
            await Clients.Caller.SendAsync("QueueStatusUpdated", status);
        }

        // ── Client leaves a queue group ───────────────────────────────────────
        // Called when customer closes the screen or is served
        public async Task LeaveQueueGroup(int queueId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"queue-{queueId}");
        }

        // ── Notify all clients in a queue group ───────────────────────────────
        // Called internally from TicketController when ticket status changes
        public static async Task NotifyQueueUpdated(
            IHubContext<QueueHub> hubContext,
            IEstimationService estimationService,
            int queueId)
        {
            var status = await estimationService.GetQueueStatusAsync(queueId);
            await hubContext.Clients
                .Group($"queue-{queueId}")
                .SendAsync("QueueStatusUpdated", status);
        }
    }
}


/*Why Groups: Each queue has its own SignalR group (queue-1, queue-2 etc.). 
 * When a ticket is called in Queue 1, 
 * ONLY customers waiting in Queue 1 get the update — not everyone connected to the system.
 * This is efficient and scalable.
Why send status immediately on JoinQueueGroup:
 * When a customer opens the app, they instantly see the 
 * current queue state without waiting for the next update. No extra HTTP call needed.
Why static NotifyQueueUpdated: Controllers can't directly call Hub methods
 * — they use IHubContext<QueueHub> instead. 
 * This static helper wraps that pattern cleanly so TicketController just calls one method.*/