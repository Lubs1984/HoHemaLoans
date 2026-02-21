import React, { useEffect, useRef, useState } from 'react';
import { apiService } from '../../services/api';
import { PaperAirplaneIcon, CheckIcon, ArrowPathIcon, PlusIcon, XMarkIcon, MagnifyingGlassIcon } from '@heroicons/react/24/solid';

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
  lastMessage?: { messageText: string; createdAt: string; direction: string };
  messages: WhatsAppMessage[];
}

// ─── New Message Modal ────────────────────────────────────────────────────────

interface NewMessageModalProps {
  onClose: () => void;
  onSent: (conversationId: number) => void;
}

const NewMessageModal: React.FC<NewMessageModalProps> = ({ onClose, onSent }) => {
  const [phoneNumber, setPhoneNumber] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [messageText, setMessageText] = useState('');
  const [contacts, setContacts] = useState<WhatsAppContact[]>([]);
  const [contactSearch, setContactSearch] = useState('');
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiService.request<WhatsAppContact[]>('/WhatsApp/contacts')
      .then(setContacts)
      .catch(() => {/* ignore */});
  }, []);

  const filteredContacts = contacts.filter(c =>
    contactSearch.length > 0 &&
    (c.displayName.toLowerCase().includes(contactSearch.toLowerCase()) ||
     c.phoneNumber.includes(contactSearch))
  );

  const selectContact = (c: WhatsAppContact) => {
    setPhoneNumber(c.phoneNumber);
    setDisplayName(c.displayName);
    setContactSearch('');
  };

  const handleSend = async () => {
    if (!phoneNumber.trim() || !messageText.trim()) {
      setError('Phone number and message are required');
      return;
    }
    try {
      setSending(true);
      setError(null);
      const result = await apiService.request<{ conversationId: number; sent: boolean }>(
        '/WhatsApp/send-direct',
        {
          method: 'POST',
          body: JSON.stringify({
            phoneNumber: phoneNumber.trim(),
            displayName: displayName.trim() || phoneNumber.trim(),
            messageText: messageText.trim(),
          }),
        }
      );
      onSent(result.conversationId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send');
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md mx-4">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-lg font-bold text-gray-900">New WhatsApp Message</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <XMarkIcon className="h-5 w-5" />
          </button>
        </div>

        <div className="px-6 py-4 space-y-4">
          {/* Contact search */}
          <div className="relative">
            <label className="block text-sm font-medium text-gray-700 mb-1">Search existing contact</label>
            <div className="relative">
              <MagnifyingGlassIcon className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
              <input
                type="text"
                placeholder="Search by name or number..."
                value={contactSearch}
                onChange={e => setContactSearch(e.target.value)}
                className="w-full pl-9 border border-gray-300 rounded-lg px-3 py-2 text-sm"
              />
            </div>
            {filteredContacts.length > 0 && (
              <div className="absolute z-10 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg max-h-40 overflow-y-auto">
                {filteredContacts.map(c => (
                  <button
                    key={c.id}
                    onClick={() => selectContact(c)}
                    className="w-full text-left px-4 py-2 hover:bg-green-50 text-sm"
                  >
                    <span className="font-medium">{c.displayName}</span>
                    <span className="text-gray-500 ml-2">{c.phoneNumber}</span>
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Phone number */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Phone Number <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              placeholder="e.g. 27821234567"
              value={phoneNumber}
              onChange={e => setPhoneNumber(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <p className="text-xs text-gray-400 mt-1">Include country code, no + or spaces (e.g. 27821234567)</p>
          </div>

          {/* Display name */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Display Name (optional)</label>
            <input
              type="text"
              placeholder="Customer name..."
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>

          {/* Message */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Message <span className="text-red-500">*</span>
            </label>
            <textarea
              placeholder="Type your message..."
              value={messageText}
              onChange={e => setMessageText(e.target.value)}
              rows={4}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm resize-none"
            />
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>

        {/* Footer */}
        <div className="flex justify-end space-x-3 px-6 py-4 border-t bg-gray-50 rounded-b-xl">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-100"
          >
            Cancel
          </button>
          <button
            onClick={handleSend}
            disabled={sending || !phoneNumber.trim() || !messageText.trim()}
            className="flex items-center space-x-2 px-5 py-2 bg-green-600 hover:bg-green-700 disabled:bg-gray-300 text-white text-sm font-medium rounded-lg transition"
          >
            {sending ? (
              <ArrowPathIcon className="h-4 w-4 animate-spin" />
            ) : (
              <PaperAirplaneIcon className="h-4 w-4" />
            )}
            <span>{sending ? 'Sending…' : 'Send'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};

// ─── Main Component ───────────────────────────────────────────────────────────

const AdminWhatsApp: React.FC = () => {
  const [conversations, setConversations] = useState<WhatsAppConversation[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<WhatsAppConversation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showNewMessage, setShowNewMessage] = useState(false);
  const [lastRefresh, setLastRefresh] = useState<Date>(new Date());
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);
  // Use a ref to track selected conversation ID — avoids stale closure in polling
  const selectedConvIdRef = useRef<number | null>(null);

  const loadConversationMessages = async (id: number) => {
    try {
      const detail = await apiService.request<WhatsAppConversation>(`/WhatsApp/conversations/${id}`);
      setSelectedConversation(detail);
    } catch {/* ignore */}
  };

  const fetchConversations = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      const params = new URLSearchParams({ page: '1', pageSize: '50' });
      const response = await apiService.request<WhatsAppConversation[]>(
        `/WhatsApp/conversations?${params}`
      );
      const list: WhatsAppConversation[] = Array.isArray(response) ? response : (response as any).data ?? [];
      setConversations(list);
      setLastRefresh(new Date());

      // Use the ref (not state) to get current selected ID — avoids stale closure
      const currentId = selectedConvIdRef.current;
      if (currentId !== null) {
        const found = list.find(c => c.id === currentId);
        if (found) loadConversationMessages(currentId);
      }
    } catch (err) {
      if (!silent) setError(err instanceof Error ? err.message : 'Failed to load conversations');
    } finally {
      if (!silent) setLoading(false);
    }
  };

  const selectConversation = async (conv: WhatsAppConversation) => {
    selectedConvIdRef.current = conv.id;
    setSelectedConversation(conv);
    await loadConversationMessages(conv.id);
  };

  // Keep ref in sync when selectedConversation changes (e.g. from New Message modal)
  useEffect(() => {
    selectedConvIdRef.current = selectedConversation?.id ?? null;
  }, [selectedConversation?.id]);

  // Start polling every 5 seconds — interval uses ref so it always has current ID
  useEffect(() => {
    fetchConversations();
    pollingRef.current = setInterval(() => fetchConversations(true), 5000);
    return () => { if (pollingRef.current) clearInterval(pollingRef.current); };
  }, []);

  // Re-fetch when search changes
  useEffect(() => {
    fetchConversations();
  }, [searchTerm]);

  const filteredConversations = searchTerm
    ? conversations.filter(c =>
        c.contact.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        c.contact.phoneNumber.includes(searchTerm)
      )
    : conversations;

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">{error}</p>
        <button onClick={() => fetchConversations()} className="mt-2 text-sm text-red-600 underline">Retry</button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">WhatsApp Communications</h1>
          <p className="text-gray-400 text-xs mt-1">
            Auto-refreshing · Last updated: {lastRefresh.toLocaleTimeString()}
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => fetchConversations()}
            className="flex items-center space-x-1 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 text-gray-700"
          >
            <ArrowPathIcon className="h-4 w-4" />
            <span>Refresh</span>
          </button>
          <button
            onClick={() => setShowNewMessage(true)}
            className="flex items-center space-x-2 px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg transition"
          >
            <PlusIcon className="h-4 w-4" />
            <span>New Message</span>
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6" style={{ height: 'calc(100vh - 220px)' }}>
        {/* Conversations List */}
        <div className="lg:col-span-1 bg-white rounded-lg shadow flex flex-col overflow-hidden">
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
          ) : filteredConversations.length === 0 ? (
            <div className="flex-1 flex flex-col items-center justify-center text-gray-500 space-y-3 p-4">
              <p>No conversations yet</p>
              <button
                onClick={() => setShowNewMessage(true)}
                className="flex items-center space-x-1 text-sm text-green-600 hover:underline"
              >
                <PlusIcon className="h-4 w-4" />
                <span>Start a new conversation</span>
              </button>
            </div>
          ) : (
            <div className="flex-1 overflow-y-auto">
              {filteredConversations.map((conv) => (
                <button
                  key={conv.id}
                  onClick={() => selectConversation(conv)}
                  className={`w-full text-left px-4 py-3 border-b transition ${
                    selectedConversation?.id === conv.id
                      ? 'bg-green-50 border-l-4 border-l-green-600'
                      : 'hover:bg-gray-50'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <p className="font-medium text-gray-900 text-sm">{conv.contact.displayName}</p>
                    {conv.lastMessage && (
                      <span className="text-xs text-gray-400 ml-1 shrink-0">
                        {new Date(conv.lastMessage.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-gray-500">{conv.contact.phoneNumber}</p>
                  <p className="text-xs text-gray-500 mt-1 truncate">
                    {conv.lastMessage
                      ? `${conv.lastMessage.direction === 'Outbound' ? 'You: ' : ''}${conv.lastMessage.messageText}`
                      : 'No messages yet'}
                  </p>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Chat View */}
        {selectedConversation ? (
          <div className="lg:col-span-2 bg-white rounded-lg shadow flex flex-col overflow-hidden">
            <ChatWindow
              conversation={selectedConversation}
              onRefresh={() => loadConversationMessages(selectedConversation.id)}
            />
          </div>
        ) : (
          <div className="lg:col-span-2 bg-white rounded-lg shadow flex flex-col items-center justify-center text-gray-500 space-y-3">
            <p>Select a conversation or start a new one</p>
            <button
              onClick={() => setShowNewMessage(true)}
              className="flex items-center space-x-2 px-4 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700"
            >
              <PlusIcon className="h-4 w-4" />
              <span>New Message</span>
            </button>
          </div>
        )}
      </div>

      {/* New Message Modal */}
      {showNewMessage && (
        <NewMessageModal
          onClose={() => setShowNewMessage(false)}
          onSent={async (conversationId) => {
            setShowNewMessage(false);
            await fetchConversations();
            const detail = await apiService.request<WhatsAppConversation>(`/WhatsApp/conversations/${conversationId}`);
            setSelectedConversation(detail);
          }}
        />
      )}
    </div>
  );
};

interface ChatWindowProps {
  conversation: WhatsAppConversation;
  onRefresh: () => void;
}

const ChatWindow: React.FC<ChatWindowProps> = ({ conversation, onRefresh }) => {
  const [messages, setMessages] = useState<WhatsAppMessage[]>(conversation.messages ?? []);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  // Sync messages when conversation prop updates (auto-refresh)
  useEffect(() => {
    setMessages(conversation.messages ?? []);
  }, [conversation.messages]);

  // Scroll to bottom on new messages
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSendMessage = async () => {
    if (!newMessage.trim()) return;
    const text = newMessage.trim();
    setNewMessage('');

    // Optimistic UI
    const optimistic: WhatsAppMessage = {
      id: Date.now(),
      messageText: text,
      direction: 'Outbound',
      createdAt: new Date().toISOString(),
      status: 'Sent',
    };
    setMessages(prev => [...prev, optimistic]);

    try {
      setLoading(true);
      await apiService.request(`/WhatsApp/messages`, {
        method: 'POST',
        body: JSON.stringify({
          conversationId: conversation.id,
          messageText: text,
        }),
      });
      // Refresh to get server state
      setTimeout(onRefresh, 800);
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to send message');
      // Remove optimistic message on failure
      setMessages(prev => prev.filter(m => m.id !== optimistic.id));
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
        <div className="flex items-center space-x-3">
          <button onClick={onRefresh} className="text-green-100 hover:text-white" title="Refresh messages">
            <ArrowPathIcon className="h-5 w-5" />
          </button>
          <div className="text-right">
            <p className="text-sm font-medium">{messages.length} messages</p>
            {conversation.status && (
              <p className="text-xs text-green-100">{conversation.status}</p>
            )}
          </div>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-3 bg-gray-50">
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
                className={`max-w-sm px-4 py-2 rounded-2xl shadow-sm ${
                  message.direction === 'Outbound'
                    ? 'bg-green-600 text-white rounded-br-none'
                    : 'bg-white text-gray-900 rounded-bl-none border border-gray-200'
                }`}
              >
                <p className="break-words text-sm">{message.messageText}</p>
                <div className="flex items-center justify-end mt-1 space-x-1">
                  <span className="text-xs opacity-60">
                    {new Date(message.createdAt).toLocaleTimeString([], {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </span>
                  {message.direction === 'Outbound' && (
                    <span className="text-xs opacity-80">
                      {message.status === 'Read' && <span title="Read">✓✓</span>}
                      {message.status === 'Delivered' && <span title="Delivered">✓</span>}
                      {(message.status === 'Sent' || message.status === 'sent') && (
                        <CheckIcon className="h-3 w-3 inline" title="Sent" />
                      )}
                      {message.status === 'Failed' && <span className="text-red-300" title="Failed">✗</span>}
                    </span>
                  )}
                </div>
              </div>
            </div>
          ))
        )}
        <div ref={bottomRef} />
      </div>

      {/* Message Input */}
      <div className="px-4 py-4 border-t bg-white rounded-b-lg">
        <div className="flex space-x-2">
          <input
            type="text"
            placeholder="Type a message..."
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            onKeyDown={(e) => {
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
