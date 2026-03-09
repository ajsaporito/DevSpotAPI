using DevSpotAPI.Data;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Message;
using Microsoft.EntityFrameworkCore;

namespace DevSpotAPI.Services
{
    public class ChatService
    {
        private readonly Context _ctx;

        public ChatService(Context ctx)
        {
            _ctx = ctx;
        }

        public async Task<bool> IsChatMemberAsync(int chatId, int userId)
        {
            return await _ctx.Chats.AnyAsync(c =>
                c.ChatId == chatId && (c.UserAId == userId || c.UserBId == userId));
        }

        public async Task<Chat> GetOrCreateChatAsync(int currentUserId, int otherUserId)
        {
            int userAId = Math.Min(currentUserId, otherUserId);
            int userBId = Math.Max(currentUserId, otherUserId);

            var chat = await _ctx.Chats
                .FirstOrDefaultAsync(c => c.UserAId == userAId && c.UserBId == userBId);

            if (chat == null)
            {
                chat = new Chat
                {
                    UserAId = userAId,
                    UserBId = userBId,
                    CreatedAt = DateTime.UtcNow
                };
                _ctx.Chats.Add(chat);
                await _ctx.SaveChangesAsync();
            }

            return chat;
        }

        public async Task<List<ConversationSummaryDto>> GetConversationListAsync(int currentUserId)
        {
            var chats = await _ctx.Chats
                .Where(c => c.UserAId == currentUserId || c.UserBId == currentUserId)
                .Include(c => c.UserA)
                .Include(c => c.UserB)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var chatIds = chats.Select(c => c.ChatId).ToList();

            var unreadCounts = await _ctx.Notifications
                .Where(n => n.UserId == currentUserId
                         && n.ChatId.HasValue
                         && chatIds.Contains(n.ChatId.Value)
                         && n.Type == NotificationType.Message
                         && !n.IsRead)
                .GroupBy(n => n.ChatId!.Value)
                .Select(g => new { ChatId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ChatId, x => x.Count);

            return chats.Select(c =>
            {
                var other = c.UserAId == currentUserId ? c.UserB : c.UserA;
                var last = c.Messages.FirstOrDefault();
                return new ConversationSummaryDto
                {
                    ChatId = c.ChatId,
                    OtherUserId = other.UserId,
                    OtherUsername = other.Username,
                    OtherFirstName = other.FirstName,
                    OtherLastName = other.LastName,
                    OtherProfilePicUrl = other.ProfilePicUrl,
                    LastMessage = last?.Text,
                    LastMessageAt = last?.CreatedAt,
                    UnreadCount = unreadCounts.GetValueOrDefault(c.ChatId, 0)
                };
            }).ToList();
        }

        public async Task<List<MessageResponseDto>> GetMessagesPagedAsync(int chatId, int currentUserId, DateTime? before, int take = 20)
        {
            var query = _ctx.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .ToListAsync();

            return messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageResponseDto
                {
                    MessageId = m.MessageId,
                    ChatId = m.ChatId,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender.Username,
                    SenderProfilePicUrl = m.Sender.ProfilePicUrl,
                    Text = m.Text,
                    CreatedAt = m.CreatedAt,
                    IsOwn = m.SenderId == currentUserId
                })
                .ToList();
        }

        public async Task<MessageResponseDto> SendMessageAsync(int chatId, int senderId, string text)
        {
            var message = new Message
            {
                ChatId = chatId,
                SenderId = senderId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _ctx.Messages.Add(message);

            var chat = await _ctx.Chats.FindAsync(chatId);
            chat!.LastMessageAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            await _ctx.Entry(message).Reference(m => m.Sender).LoadAsync();

            var recipientId = chat.UserAId == senderId ? chat.UserBId : chat.UserAId;
            _ctx.Notifications.Add(new Notification
            {
                UserId = recipientId,
                ChatId = chatId,
                MessageId = message.MessageId,
                Type = NotificationType.Message,
                Title = $"New message from {message.Sender.Username}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _ctx.SaveChangesAsync();

            return new MessageResponseDto
            {
                MessageId = message.MessageId,
                ChatId = chatId,
                SenderId = senderId,
                SenderUsername = message.Sender.Username,
                SenderProfilePicUrl = message.Sender.ProfilePicUrl,
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                IsOwn = true
            };
        }
    }
}
