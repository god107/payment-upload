using Microsoft.AspNetCore.SignalR;

namespace UploadPayments.Api.Hubs;

/// <summary>
/// SignalR hub for real-time payment upload progress notifications.
/// Clients connect to this hub and join groups based on upload tokens to receive targeted updates.
/// </summary>
public sealed class PaymentUploadHub : Hub<IPaymentUploadClient>
{
    /// <summary>
    /// Subscribe to updates for a specific upload using its token.
    /// Clients call this method to join a group and receive real-time notifications.
    /// </summary>
    public async Task SubscribeToUpload(string token)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"upload_{token}");
    }

    /// <summary>
    /// Unsubscribe from upload updates.
    /// </summary>
    public async Task UnsubscribeFromUpload(string token)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"upload_{token}");
    }
}
