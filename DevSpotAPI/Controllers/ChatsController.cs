using DevSpotAPI.Hubs;
using DevSpotAPI.Models.DTOs.Message;
using DevSpotAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DevSpotAPI.Controllers
{
    [ApiController]
    [Route("api/chats")]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatsController(ChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(value!);
        }

        // POST /api/chats/{otherUserId}  — get or create chat
        [HttpPost("{otherUserId:int}")]
        public async Task<ActionResult<ConversationSummaryDto>> GetOrCreateChat(int otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            var chat = await _chatService.GetOrCreateChatAsync(currentUserId, otherUserId);

            var conversations = await _chatService.GetConversationListAsync(currentUserId);
            var summary = conversations.FirstOrDefault(c => c.ChatId == chat.ChatId);

            return Ok(summary);
        }

        // GET /api/chats  — conversation list sorted by LastMessageAt DESC
        [HttpGet]
        public async Task<ActionResult<List<ConversationSummaryDto>>> GetConversations()
        {
            var currentUserId = GetCurrentUserId();
            var conversations = await _chatService.GetConversationListAsync(currentUserId);
            return Ok(conversations);
        }

        // GET /api/chats/{chatId}/messages?before=&take=20  — paginated messages
        [HttpGet("{chatId:int}/messages")]
        public async Task<ActionResult<List<MessageResponseDto>>> GetMessages(
            int chatId,
            [FromQuery] DateTime? before,
            [FromQuery] int take = 20)
        {
            var currentUserId = GetCurrentUserId();
            var messages = await _chatService.GetMessagesPagedAsync(chatId, currentUserId, before, take);
            return Ok(messages);
        }

        // POST /api/chats/{chatId}/messages  — send message
        [HttpPost("{chatId:int}/messages")]
        public async Task<ActionResult<MessageResponseDto>> SendMessage(int chatId, [FromBody] SendMessageDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var messageDto = await _chatService.SendMessageAsync(chatId, currentUserId, dto.Text);

            await _hubContext.Clients.Group($"chat:{chatId}")
                .SendAsync("ReceiveMessage", messageDto);

            return Ok(messageDto);
        }
    }
}
