import React, { useEffect, useState } from 'react';
import { apiService } from '../../services/api';
import { PaperAirplaneIcon, CheckIcon } from '@heroicons/react/24/solid';

interface WhatsAppContact {
  id: number;
  phoneNumber: string;
  displayName: string;
}

interface WhatsAppMessage {
  id: number;
  messageText: string;
  direction: string;
  createdAt: string;
  status: string;
  type?: string;
  mediaUrl?: string;
  mediaType?: string;
  deliveredAt?: string;
  readAt?: string;
}

interface WhatsAppConversation {
  id: number;
  contact: WhatsAppContact;
  subject?: string;
  status?: string;
  type?: string;
  createdAt: string;
  updatedAt: string;
  messageCount?: number;
  unreadCount?: number;
  lastMessage?: WhatsAppMessage;
  messages: WhatsAppMessage[];
}

interface ConversationResponse {
  totalCount: number;
  pageCount: number;
  currentPage: number;
  pageSize: number;
  data: WhatsAppConversation[];
}

const AdminWhatsApp: React.FC = () => {
  const [conversations, setConversations] = useState<WhatsAppConversation[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<WhatsAppConversation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  const fetchConversations = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: '1',
        pageSize: '50',
      });

      if (searchTerm) params.append('search', searchTerm);

      const response = await apiService.request<ConversationResponse>(
        `/admin/whatsapp/conversations?${params}`
      );
      setConversations(response.data);
      if (response.data.length > 0 && !selectedConversation) {
        setSelectedConversation(response.data[0]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load conversations');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchConversations();
  }, [searchTerm]);

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">{error}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">WhatsApp Communications</h1>
        <p className="text-gray-600 mt-2">Send and manage messages with customers</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 h-96">
        {/* Conversations List */}
        <div className="lg:col-span-1 bg-white rounded-lg shadow flex flex-col">
          <div className="p-4 border-b">
            <input
              type="text"
              placeholder="Search conversations..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>

          {loading ? (
            <div className="flex-1 flex items-center justify-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600"></div>
            </div>
          ) : conversations.length === 0 ? (
            <div className="flex-1 flex items-center justify-center text-gray-500">
              No conversations found
            </div>
          ) : (
            <div className="flex-1 overflow-y-auto">
              {conversations.map((conv) => (
                <button
                  key={conv.id}
                  onClick={() => setSelectedConversation(conv)}
                  className={`w-full text-left px-4 py-3 border-b transition ${
                    selectedConversation?.id === conv.id
                      ? 'bg-green-50 border-l-4 border-l-green-600'
                      : 'hover:bg-gray-50'
                  }`}
                >
                  <p className="font-medium text-gray-900">{conv.contact.displayName}</p>
                  <p className="text-sm text-gray-600 truncate">{conv.contact.phoneNumber}</p>
                  <p className="text-xs text-gray-500 mt-1 truncate">
                    {conv.lastMessage?.messageText || 'No messages yet'}
                  </p>
                  {conv.unreadCount && conv.unreadCount > 0 && (
                    <span className="inline-block bg-green-600 text-white text-xs px-2 py-0.5 rounded-full mt-1">
                      {conv.unreadCount}
                    </span>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Chat View */}
        {selectedConversation ? (
          <div className="lg:col-span-2 bg-white rounded-lg shadow flex flex-col">
            <ChatWindow conversation={selectedConversation} onRefresh={fetchConversations} />
          </div>
        ) : (
          <div className="lg:col-span-2 bg-white rounded-lg shadow flex items-center justify-center text-gray-500">
            Select a conversation to start messaging
          </div>
        )}
      </div>
    </div>
  );
};

interface ChatWindowProps {
  conversation: WhatsAppConversation;
  onRefresh: () => void;
}

const ChatWindow: React.FC<ChatWindowProps> = ({ conversation, onRefresh }) => {
  const [messages, setMessages] = useState<WhatsAppMessage[]>(conversation.messages);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSendMessage = async () => {
    if (!newMessage.trim()) return;

    try {
      setLoading(true);
      await apiService.request(`/admin/whatsapp/send-message`, {
        method: 'POST',
        body: JSON.stringify({
          conversationId: conversation.id,
          content: newMessage,
        }),
      });

      setNewMessage('');
      setMessages([
        ...messages,
        {
          id: Date.now(),
          messageText: newMessage,
          direction: 'Outbound',
          createdAt: new Date().toISOString(),
          status: 'Sent',
        },
      ]);
      
      // Refresh conversations to get latest state
      setTimeout(onRefresh, 500);
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to send message');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      {/* Header */}
      <div className="px-6 py-4 border-b bg-gradient-to-r from-green-500 to-green-600 text-white flex items-center justify-between">
        <div>
          <h2 className="text-lg font-bold">{conversation.contact.displayName}</h2>
          <p className="text-sm text-green-100">{conversation.contact.phoneNumber}</p>
        </div>
        <div className="text-right">
          <p className="text-sm font-medium">
            {conversation.messageCount || 0} messages
          </p>
          {conversation.status && (
            <p className="text-xs text-green-100">{conversation.status}</p>
          )}
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-gray-50">
        {messages.length === 0 ? (
          <div className="flex items-center justify-center h-full text-gray-500">
            No messages yet. Start the conversation!
          </div>
        ) : (
          messages.map((message) => (
            <div
              key={message.id}
              className={`flex ${message.direction === 'Outbound' ? 'justify-end' : 'justify-start'}`}
            >
              <div
                className={`max-w-xs px-4 py-2 rounded-lg ${
                  message.direction === 'Outbound'
                    ? 'bg-green-600 text-white rounded-br-none'
                    : 'bg-gray-300 text-gray-900 rounded-bl-none'
                }`}
              >
                <p className="break-words text-sm">{message.messageText}</p>
                <div className="flex items-center justify-between mt-1">
                  <span className="text-xs opacity-70">
                    {new Date(message.createdAt).toLocaleTimeString([], {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </span>
                  {message.direction === 'Outbound' && (
                    <span className="text-xs ml-2">
                      {message.status === 'Read' && '✓✓'}
                      {message.status === 'Delivered' && '✓'}
                      {message.status === 'Sent' && <CheckIcon className="h-3 w-3 inline" />}
                    </span>
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Message Input */}
      <div className="px-4 py-4 border-t bg-white rounded-b-lg">
        <div className="flex space-x-2">
          <input
            type="text"
            placeholder="Type a message..."
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                handleSendMessage();
              }
            }}
            disabled={loading}
            className="flex-1 border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
          />
          <button
            onClick={handleSendMessage}
            disabled={loading || !newMessage.trim()}
            className="bg-green-600 hover:bg-green-700 disabled:bg-gray-300 text-white px-6 py-2 rounded-lg font-medium flex items-center space-x-2 transition"
          >
            <PaperAirplaneIcon className="h-5 w-5" />
            <span className="hidden sm:inline">Send</span>
          </button>
        </div>
      </div>
    </>
  );
};

export default AdminWhatsApp;
