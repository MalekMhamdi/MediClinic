import { Component, signal, OnInit, OnDestroy, computed, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, ChatContact } from '../services/chat.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-chat-popup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-popup.html',
  styleUrl: './chat-popup.css',
})
export class ChatPopupComponent implements OnInit, OnDestroy {

  @Input() autoLoadContacts = true;

  isOpen       = signal(false);
  newMsg       = signal('');
  sending      = signal(false);
  hoveredMsg   = signal<number | null>(null);
  confirmDel   = signal<number | null>(null); // contactId en cours de confirmation de suppression

  contacts     = computed(() => this.chat.contacts());
  messages     = computed(() => this.chat.activeMessages());
  activeId     = computed(() => this.chat.activeContactId());
  unreadTotal  = computed(() => this.chat.unreadTotal());
  newMsgAlert  = computed(() => this.chat.newMsgAlert());
  myId         = computed(() => this.auth.userId);

  constructor(public chat: ChatService, public auth: AuthService) {}

  async ngOnInit(): Promise<void> {
    await this.chat.connect();
    if (this.autoLoadContacts) await this.chat.loadContacts();
  }

  ngOnDestroy(): void { this.chat.disconnect(); }

  toggleOpen(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen() && this.contacts().length === 0) this.chat.loadContacts();
  }

  async openChat(contact: ChatContact): Promise<void> {
    this.confirmDel.set(null);
    await this.chat.openConversation(contact.userId);
  }

  backToContacts(): void {
    this.chat.closeConversation();
    this.confirmDel.set(null);
  }

  // ── Envoyer message ───────────────────────────────────────────
  async sendMessage(): Promise<void> {
    const content = this.newMsg().trim();
    const rid     = this.activeId();
    if (!content || !rid || this.sending()) return;
    this.sending.set(true);
    try {
      await this.chat.send(rid, content);
      this.newMsg.set('');
    } finally {
      this.sending.set(false);
    }
  }

  handleKey(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.sendMessage(); }
  }

  // ── Supprimer un message ──────────────────────────────────────
  async deleteMsg(id: number): Promise<void> {
    await this.chat.deleteMessage(id);
  }

  // ── Supprimer toute la conversation ──────────────────────────
  askDeleteConversation(contactId: number, e: Event): void {
    e.stopPropagation();
    this.confirmDel.set(this.confirmDel() === contactId ? null : contactId);
  }

  async confirmDeleteConversation(contactId: number): Promise<void> {
    // 1. Fermer la confirmation immédiatement (pas d'attente)
    this.confirmDel.set(null);

    // 2. Mise à jour optimiste de l'UI AVANT l'appel HTTP
    this.chat.clearConversationLocally(contactId);

    // 3. Appel backend en arrière-plan
    try {
      await this.chat.deleteConversation(contactId);
    } catch (e) {
      console.warn('Suppression conversation backend échouée:', e);
      // L'UI est déjà nettoyée — comportement acceptable
    }
  }

  cancelDelete(): void { this.confirmDel.set(null); }

  // ── Alert click ───────────────────────────────────────────────
  openFromAlert(msg: { senderId: number; senderName: string }): void {
    this.isOpen.set(true);
    const contact = this.contacts().find(c => c.userId === msg.senderId);
    if (contact) this.openChat(contact);
    else {
      this.chat.openConversation(msg.senderId);
    }
    this.chat.newMsgAlert.set(null);
  }

  // ── Helpers ───────────────────────────────────────────────────
  isMine(senderId: number): boolean { return senderId === this.auth.userId; }

  formatTime(d: Date | string): string {
    return new Date(d).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  }

  formatAgo(d: Date | string | null): string {
    if (!d) return '';
    const diff = Date.now() - new Date(d).getTime();
    if (diff < 60_000)    return 'just now';
    if (diff < 3600_000)  return `${Math.floor(diff / 60_000)} min`;
    if (diff < 86400_000) return this.formatTime(d);
    return new Date(d).toLocaleDateString('en-US', { day: '2-digit', month: 'short' });
  }

  roleIcon(role: string): string {
    const r = role?.toLowerCase();
    if (r.includes('doctor')) return '👨‍⚕️';
    if (r.includes('patient')) return '🧑';
    return '👤';
  }

  get activeContact(): ChatContact | undefined {
    return this.contacts().find(c => c.userId === this.activeId());
  }
}
