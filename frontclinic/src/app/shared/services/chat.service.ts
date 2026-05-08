import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

const API = 'http://localhost:5217/api/v1';
const HUB = 'http://localhost:5217/hubs/chat';

export interface ChatMessage {
  id:           number;
  senderId:     number;
  senderName:   string;
  receiverId:   number;
  receiverName: string;
  content:      string;
  sentAt:       Date;
  isRead:       boolean;
}

export interface ChatContact {
  userId:        number;
  fullName:      string;
  role:          string;
  unreadCount:   number;
  lastMessage:   string | null;
  lastMessageAt: Date | null;
}

@Injectable({ providedIn: 'root' })
export class ChatService {

  private hub!: signalR.HubConnection;

  contacts        = signal<ChatContact[]>([]);
  activeMessages  = signal<ChatMessage[]>([]);
  activeContactId = signal<number | null>(null);
  unreadTotal     = signal(0);
  newMsgAlert     = signal<ChatMessage | null>(null);
  connected       = signal(false);

  constructor(private auth: AuthService, private http: HttpClient) {}

  // ── Connexion SignalR ─────────────────────────────────────────
  async connect(): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(HUB, { accessTokenFactory: () => this.auth.token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('ReceiveMessage', (msg: any) => {
      const m = this._map(msg);
      if (this.activeContactId() === m.senderId || this.activeContactId() === m.receiverId) {
        this.activeMessages.update(list => [...list, m]);
      }
      this._updateContactOnReceive(m);
      if (m.senderId !== this.auth.userId) {
        this.unreadTotal.update(n => n + 1);
        this.newMsgAlert.set(m);
        setTimeout(() => this.newMsgAlert.set(null), 5000);
      }
    });

    this.hub.on('MessageDeleted', (id: number) => {
      this.activeMessages.update(list => list.filter(m => m.id !== id));
    });

    this.hub.on('ConversationRead', (fromUserId: number) => {
      this.contacts.update(list =>
        list.map(c => c.userId === fromUserId ? { ...c, unreadCount: 0 } : c)
      );
    });

    await this.hub.start();
    this.connected.set(true);
    await this._loadUnreadCount();
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.connected.set(false);
  }

  // ── Charger contacts depuis appointments selon le rôle ────────
  async loadContacts(): Promise<void> {
    const user = this.auth.currentUser();
    if (!user) return;

    // 1. Contacts déjà en chat (avec compteur non-lus + dernier message)
    let chatContacts: ChatContact[] = [];
    try {
      const res = await firstValueFrom(this.http.get<any[]>(`${API}/Chat/contacts`));
      chatContacts = res.map(c => ({
        ...c,
        lastMessageAt: c.lastMessageAt ? new Date(c.lastMessageAt) : null,
      }));
    } catch { }

    // 2. Contacts depuis les appointments (selon le rôle)
    const apptContacts = await this._loadAppointmentContacts(user.backRole, user.id);

    // 3. Fusionner : priorité aux contacts chat (ont unreadCount + lastMessage)
    const merged = new Map<number, ChatContact>();
    for (const c of apptContacts) merged.set(c.userId, c);
    for (const c of chatContacts) merged.set(c.userId, c); // écrase avec données fraîches

    this.contacts.set(
      [...merged.values()].sort((a, b) =>
        (b.lastMessageAt?.getTime() ?? 0) - (a.lastMessageAt?.getTime() ?? 0)
      )
    );
  }

  private async _loadAppointmentContacts(role: string, userId: number): Promise<ChatContact[]> {
    try {
      if (role === 'Doctor') {
        // Médecin → tous ses patients uniques depuis ses rendez-vous
        const appts = await firstValueFrom(
          this.http.get<any[]>(`${API}/Appointment/by-doctor/${userId}`)
        );
        const seen = new Map<number, ChatContact>();
        for (const a of appts) {
          if (!seen.has(a.patientId)) {
            seen.set(a.patientId, {
              userId:        a.patientId,
              fullName:      a.patientFullName,
              role:          'Patient',
              unreadCount:   0,
              lastMessage:   null,
              lastMessageAt: null,
            });
          }
        }
        return [...seen.values()];

      } else if (role === 'Patient') {
        // Patient → tous ses médecins uniques depuis ses rendez-vous
        const appts = await firstValueFrom(
          this.http.get<any[]>(`${API}/Appointment/by-patient/${userId}`)
        );
        const seen = new Map<number, ChatContact>();
        for (const a of appts) {
          if (!seen.has(a.doctorId)) {
            seen.set(a.doctorId, {
              userId:        a.doctorId,
              fullName:      a.doctorFullName,
              role:          'Doctor',
              unreadCount:   0,
              lastMessage:   null,
              lastMessageAt: null,
            });
          }
        }
        return [...seen.values()];
      }
    } catch { }
    return [];
  }

  // ── Ouvrir une conversation ───────────────────────────────────
  async openConversation(contactId: number): Promise<void> {
    this.activeContactId.set(contactId);
    const res = await firstValueFrom(
      this.http.get<any[]>(`${API}/Chat/history/${contactId}`)
    );
    this.activeMessages.set(res.map(m => this._map(m)));

    await firstValueFrom(this.http.patch(`${API}/Chat/read/${contactId}`, {}));
    if (this.hub?.state === signalR.HubConnectionState.Connected) {
      await this.hub.invoke('MarkRead', contactId);
    }
    this.contacts.update(list =>
      list.map(c => c.userId === contactId ? { ...c, unreadCount: 0 } : c)
    );
    this.unreadTotal.set(
      Math.max(0, this.contacts().reduce((s, c) => s + c.unreadCount, 0))
    );
  }

  closeConversation(): void {
    this.activeContactId.set(null);
    this.activeMessages.set([]);
  }

  // ── Envoyer un message ────────────────────────────────────────
  async send(receiverId: number, content: string): Promise<void> {
    if (!content.trim()) return;
    await this.hub.invoke('SendMessage', receiverId, content);
    this.contacts.update(list =>
      list.map(c => c.userId === receiverId
        ? { ...c, lastMessage: content, lastMessageAt: new Date() }
        : c)
    );
  }

  // ── Supprimer un message ──────────────────────────────────────
  async deleteMessage(id: number): Promise<void> {
    await this.hub.invoke('DeleteMessage', id);
  }

  // ── Effacer la conversation localement (optimiste, sans API) ─
  clearConversationLocally(otherUserId: number): void {
    this.contacts.update(list =>
      list.map(c => c.userId === otherUserId
        ? { ...c, lastMessage: null, lastMessageAt: null, unreadCount: 0 }
        : c)
    );
    if (this.activeContactId() === otherUserId) {
      this.activeMessages.set([]);
    }
  }

  // ── Supprimer toute la conversation (appel backend uniquement) ─
  async deleteConversation(otherUserId: number): Promise<void> {
    await firstValueFrom(
      this.http.delete(`${API}/Chat/conversation/${otherUserId}`)
    );
  }

  // ── Privé ─────────────────────────────────────────────────────
  private async _loadUnreadCount(): Promise<void> {
    try {
      const res = await firstValueFrom(
        this.http.get<{ unreadCount: number }>(`${API}/Chat/unread-count`)
      );
      this.unreadTotal.set(res.unreadCount);
    } catch { }
  }

  private _map(m: any): ChatMessage {
    return { ...m, sentAt: new Date(m.sentAt) };
  }

  private _updateContactOnReceive(m: ChatMessage): void {
    const otherId   = m.senderId === this.auth.userId ? m.receiverId : m.senderId;
    const otherName = m.senderId === this.auth.userId ? m.receiverName : m.senderName;

    this.contacts.update(list => {
      const exists = list.find(c => c.userId === otherId);
      if (exists) {
        return list.map(c => c.userId === otherId
          ? { ...c,
              lastMessage:   m.content,
              lastMessageAt: m.sentAt,
              unreadCount:   m.senderId !== this.auth.userId
                               ? c.unreadCount + 1
                               : c.unreadCount,
            }
          : c
        );
      }
      return [{
        userId:        otherId,
        fullName:      otherName,
        role:          '',
        unreadCount:   m.senderId !== this.auth.userId ? 1 : 0,
        lastMessage:   m.content,
        lastMessageAt: m.sentAt,
      }, ...list];
    });
  }
}
