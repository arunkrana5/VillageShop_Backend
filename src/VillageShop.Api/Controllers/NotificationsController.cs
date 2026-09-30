using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public NotificationsController(IApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<long> ResolveTenantIdAsync()
    {
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tidStr) && long.TryParse(tidStr, out var tid) && tid > 0)
            return tid;

        if (Request.Query.TryGetValue("tenantId", out var qTidStr) && long.TryParse(qTidStr, out var qTid) && qTid > 0)
            return qTid;

        return 1;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] long? tenantId)
    {
        long targetTenantId = tenantId ?? await ResolveTenantIdAsync();
        var combinedList = new List<object>();

        try
        {
            var pushNotifications = await _context.PushNotifications
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(n => n.isdeleted == 0)
                .OrderByDescending(n => n.NotificationID)
                .Take(50)
                .ToListAsync();

            foreach (var p in pushNotifications)
            {
                if (p.TenantId.HasValue && p.TenantId > 0 && p.TenantId != targetTenantId)
                    continue;

                combinedList.Add(new
                {
                    id = p.NotificationID,
                    source = "PUSH",
                    title = p.Subject,
                    message = p.MessageContent,
                    category = string.IsNullOrWhiteSpace(p.Category) ? "GENERAL" : p.Category,
                    gotoUrl = p.GotoURL,
                    priority = p.Priority,
                    isRead = p.IsStatusRead,
                    createdDate = p.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    tableId = p.TableID,
                    tenantId = p.TenantId ?? targetTenantId
                });
            }
        }
        catch (Exception ex)
        {
            // Log or fallback if PushNotifications table query encounters schema mismatch
            Console.WriteLine($"[NotificationsController] PushNotifications fetch exception: {ex.Message}");
        }

        try
        {
            var userNotifications = await _context.UserNotifications
                .IgnoreQueryFilters()
                .AsNoTracking()
                .OrderByDescending(n => n.Id)
                .Take(50)
                .ToListAsync();

            foreach (var u in userNotifications)
            {
                if (u.TenantId.HasValue && u.TenantId > 0 && u.TenantId != targetTenantId)
                    continue;

                combinedList.Add(new
                {
                    id = u.Id,
                    source = "USER",
                    title = u.Title,
                    message = u.Message,
                    category = u.Type ?? "INFO",
                    gotoUrl = "",
                    priority = 1,
                    isRead = u.IsRead,
                    createdDate = u.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    tableId = u.ReferenceId ?? 0,
                    tenantId = u.TenantId ?? targetTenantId
                });
            }
        }
        catch (Exception ex)
        {
            // Log or fallback if UserNotifications table query encounters schema mismatch
            Console.WriteLine($"[NotificationsController] UserNotifications fetch exception: {ex.Message}");
        }

        return Ok(combinedList);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] long? tenantId)
    {
        long targetTenantId = tenantId ?? await ResolveTenantIdAsync();
        int pushUnread = 0;
        int userUnread = 0;

        try
        {
            pushUnread = await _context.PushNotifications
                .IgnoreQueryFilters()
                .Where(n => n.isdeleted == 0 && !n.IsStatusRead)
                .CountAsync();
        }
        catch (Exception) {}

        try
        {
            userUnread = await _context.UserNotifications
                .IgnoreQueryFilters()
                .Where(n => !n.IsRead)
                .CountAsync();
        }
        catch (Exception) {}

        return Ok(new { unreadCount = pushUnread + userUnread });
    }

    [HttpGet("fcm-config")]
    public async Task<IActionResult> GetFcmConfig([FromQuery] long? tenantId)
    {
        long targetTenantId = tenantId ?? await ResolveTenantIdAsync();

        var config = await _context.TenantConfigurations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == targetTenantId);

        string apiKey = "AIzaSyD4Ke1oq7cAUM47eaHOdCYSWB7WY8aqjGg";
        string authDomain = "uatweb-52210.firebaseapp.com";
        string projectId = "uatweb-52210";
        string storageBucket = "uatweb-52210.firebasestorage.app";
        string senderId = "292445139375";
        string appId = "1:292445139375:web:13ef000481be4897d53f5";
        string measurementId = "G-N5JDJ61BW3";
        string vapidKey = "BJqD_-Lk8oW0DIrftlgsId4JsWJks288xq-tV72KnwXTJ3rWMy6Zs6hjIU8fcjxgVSfLT4laSibnpPsJzSM4gZw";

        if (config != null && !string.IsNullOrWhiteSpace(config.FeatureJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(config.FeatureJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("fcmApiKey", out var p1) && !string.IsNullOrWhiteSpace(p1.GetString())) apiKey = p1.GetString()!;
                if (root.TryGetProperty("fcmAuthDomain", out var p2) && !string.IsNullOrWhiteSpace(p2.GetString())) authDomain = p2.GetString()!;
                if (root.TryGetProperty("fcmProjectId", out var p3) && !string.IsNullOrWhiteSpace(p3.GetString())) projectId = p3.GetString()!;
                if (root.TryGetProperty("fcmStorageBucket", out var p4) && !string.IsNullOrWhiteSpace(p4.GetString())) storageBucket = p4.GetString()!;
                if (root.TryGetProperty("fcmMessagingSenderId", out var p5) && !string.IsNullOrWhiteSpace(p5.GetString())) senderId = p5.GetString()!;
                if (root.TryGetProperty("fcmAppId", out var p6) && !string.IsNullOrWhiteSpace(p6.GetString())) appId = p6.GetString()!;
                if (root.TryGetProperty("measurementId", out var p7) && !string.IsNullOrWhiteSpace(p7.GetString())) measurementId = p7.GetString()!;
                if (root.TryGetProperty("fcmVapidKey", out var p8) && !string.IsNullOrWhiteSpace(p8.GetString())) vapidKey = p8.GetString()!;
            }
            catch (Exception) {}
        }

        return Ok(new
        {
            fcmApiKey = apiKey,
            fcmAuthDomain = authDomain,
            fcmProjectId = projectId,
            fcmStorageBucket = storageBucket,
            fcmMessagingSenderId = senderId,
            fcmAppId = appId,
            measurementId = measurementId,
            fcmVapidKey = vapidKey
        });
    }

    [HttpPost("fcm-config")]
    public async Task<IActionResult> SaveFcmConfig([FromBody] JsonElement payload, [FromQuery] long? tenantId)
    {
        long targetTenantId = tenantId ?? await ResolveTenantIdAsync();

        var config = await _context.TenantConfigurations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == targetTenantId);

        if (config == null)
        {
            config = new TenantConfiguration { TenantId = targetTenantId };
            _context.TenantConfigurations.Add(config);
        }

        config.FeatureJson = payload.GetRawText();
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("FCM configuration saved successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest req, [FromQuery] long? tenantId)
    {
        long targetTenantId = tenantId ?? await ResolveTenantIdAsync();

        if (req == null) req = new CreateNotificationRequest();

        var pushNotif = new PushNotification
        {
            Subject = string.IsNullOrWhiteSpace(req.Subject) ? (req.Title ?? "Alert") : req.Subject,
            MessageContent = string.IsNullOrWhiteSpace(req.MessageContent) ? (req.Message ?? "New Notification") : req.MessageContent,
            Category = string.IsNullOrWhiteSpace(req.Category) ? "SYSTEM" : req.Category,
            GotoURL = req.GotoURL ?? "",
            Priority = req.Priority > 0 ? req.Priority : 1,
            IsStatusRead = false,
            CreatedDate = DateTime.UtcNow,
            TenantId = targetTenantId,
            IsActive = true,
            IsRecent = true,
            isdeleted = 0
        };

        _context.PushNotifications.Add(pushNotif);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("Notification created & sent successfully.", pushNotif.NotificationID));
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var push = await _context.PushNotifications.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.NotificationID == id);
        if (push != null)
        {
            push.IsStatusRead = true;
        }

        var userNotif = await _context.UserNotifications.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (userNotif != null)
        {
            userNotif.IsRead = true;
            userNotif.ReadDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Notification marked as read."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var push = await _context.PushNotifications.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.NotificationID == id);
        if (push != null)
        {
            push.isdeleted = 1;
        }

        var userNotif = await _context.UserNotifications.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (userNotif != null)
        {
            _context.UserNotifications.Remove(userNotif);
        }

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Notification deleted successfully."));
    }
}

public class CreateNotificationRequest
{
    public string? Subject { get; set; }
    public string? Title { get; set; }
    public string? MessageContent { get; set; }
    public string? Message { get; set; }
    public string? Category { get; set; }
    public string? GotoURL { get; set; }
    public int Priority { get; set; } = 1;
}
