import { Component, signal, computed, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AiService, AiChatResponse, AiOrientationResult, ChatMessage } from '../../shared/services/ai.service';

type AiState = 'idle' | 'loading-session' | 'chatting' | 'typing' | 'complete' | 'error';

@Component({
  selector: 'app-ai-agent',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-agent.html',
  styleUrl: './ai-agent.css'
})
export class AiAgentComponent implements AfterViewChecked {

  @ViewChild('messagesEnd') private messagesEnd!: ElementRef;

  // ── State ────────────────────────────────────────────────────────────
  state         = signal<AiState>('idle');
  sessionId     = signal<string | null>(null);
  messages      = signal<ChatMessage[]>([]);
  userInput     = signal('');
  errorMsg      = signal('');
  step          = signal(0);
  language      = signal<'fr' | 'ar' | 'en'>('fr');
  orientResult  = signal<AiOrientationResult | null>(null);
  suggestedDoc  = signal<{ id: number | null; name: string; specialty: string } | null>(null);
  typingDots    = signal('');

  private _typingInterval: any;
  private _shouldScroll = false;

  constructor(private ai: AiService, private router: Router) {}

  ngAfterViewChecked(): void {
    if (this._shouldScroll) {
      this.scrollToBottom();
      this._shouldScroll = false;
    }
  }

  // ── Computed ─────────────────────────────────────────────────────────
  isIdle     = computed(() => this.state() === 'idle');
  isLoading  = computed(() => this.state() === 'loading-session');
  isChatting = computed(() => this.state() === 'chatting' || this.state() === 'typing');
  isComplete = computed(() => this.state() === 'complete');
  isError    = computed(() => this.state() === 'error');
  isTyping   = computed(() => this.state() === 'typing');

  canSend = computed(() =>
    this.state() === 'chatting' && this.userInput().trim().length > 0
  );

  unreadCount = computed(() => this.messages().length);

  urgencyClass = computed(() => {
    const u = this.orientResult()?.urgency?.toLowerCase() ?? '';
    if (u.includes('élevé') || u.includes('urgent')) return 'urgency-high';
    if (u.includes('modér'))                           return 'urgency-medium';
    return 'urgency-low';
  });

  // ── Session ──────────────────────────────────────────────────────────
  async startSession(): Promise<void> {
    this.state.set('loading-session');
    this.messages.set([]);
    this.orientResult.set(null);
    this.suggestedDoc.set(null);
    this.errorMsg.set('');
    this.step.set(0);

    try {
      const session = await this.ai.createSession();
      this.sessionId.set(session.sessionId);

      // Auto-send empty first message to get the greeting
      this.state.set('chatting');
      await this._sendMessage('');
    } catch (e: any) {
      this.state.set('error');
      this.errorMsg.set('Unable to connect to the AI service. Make sure the backend and AI agent are running.');
    }
  }

  // ── Send ─────────────────────────────────────────────────────────────
  async send(): Promise<void> {
    const msg = this.userInput().trim();
    if (!msg || this.state() !== 'chatting') return;
    this.userInput.set('');
    await this._sendMessage(msg);
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      this.send();
    }
  }

  // ── Quick replies ─────────────────────────────────────────────────────
  quickReply(text: string): void {
    if (this.state() !== 'chatting') return;
    this.userInput.set(text);
    this.send();
  }

  // ── Reset ─────────────────────────────────────────────────────────────
  async reset(): Promise<void> {
    const id = this.sessionId();
    if (id) {
      try { await this.ai.resetSession(id); } catch {}
    }
    this.sessionId.set(null);
    this.messages.set([]);
    this.orientResult.set(null);
    this.suggestedDoc.set(null);
    this.state.set('idle');
    this.step.set(0);
    this.userInput.set('');
    this._stopTyping();
  }

  // ── Navigate to book with suggested doctor ────────────────────────────
  bookSuggestedDoctor(): void {
    this.router.navigate(['/patient/book-appointment'], {
      queryParams: {
        doctorId:  this.suggestedDoc()?.id,
        specialty: this.suggestedDoc()?.specialty
      }
    });
  }

  // ── Navigate to book with specialty only (no specific doctor) ────────
  bookBySpecialty(): void {
    this.router.navigate(['/patient/book-appointment'], {
      queryParams: { specialty: this.orientResult()?.medicalSpecialty }
    });
  }

  // ── Language auto-detection ───────────────────────────────────────────
  private _detectLanguage(text: string): 'fr' | 'ar' | 'en' {
    if (!text.trim()) return this.language();

    // Arabic Unicode block
    if (/[؀-ۿݐ-ݿﭐ-﷿ﹰ-﻿]/.test(text))
      return 'ar';

    // French: accented chars or common French words
    if (/[àâäéèêëîïôùûüÿçæœ]/i.test(text) ||
        /\b(je|tu|il|elle|nous|vous|ils|elles|bonjour|salut|merci|oui|non|et|le|la|les|un|une|des|du|de|en|à|pour|avec|dans|sur|par|est|sont|avoir|être|faire|je suis|j'ai|j'ai|c'est|ça)\b/i.test(text))
      return 'fr';

    return 'en';
  }

  // ── Private helpers ───────────────────────────────────────────────────
  private async _sendMessage(text: string): Promise<void> {
    const sessionId = this.sessionId();
    if (!sessionId) return;

    // Detect language from user input and update the signal
    const lang = text ? this._detectLanguage(text) : this.language();
    if (text) this.language.set(lang);

    // Push user message (skip empty greeting trigger)
    if (text) {
      this.messages.update(msgs => [
        ...msgs,
        { role: 'user', content: text, step: this.step() }
      ]);
      this._shouldScroll = true;
    }

    this.state.set('typing');
    this._startTyping();

    try {
      const res: AiChatResponse = await this.ai.chat(sessionId, text, lang);
      this._stopTyping();
      this.step.set(res.sessionStep);

      // Push assistant reply
      this.messages.update(msgs => [
        ...msgs,
        { role: 'assistant', content: res.reply, step: res.sessionStep }
      ]);
      this._shouldScroll = true;

      if (res.isComplete && res.result) {
        this.orientResult.set(res.result);
        if (res.suggestedDoctorId) {
          this.suggestedDoc.set({
            id:        res.suggestedDoctorId,
            name:      res.suggestedDoctorName || 'Available Doctor',
            specialty: res.suggestedSpecialty  || res.result.medicalSpecialty
          });
        }
        this.state.set('complete');
      } else {
        this.state.set('chatting');
      }
    } catch (e: any) {
      this._stopTyping();
      this.errorMsg.set('Connection error. Please try again.');
      this.state.set('chatting');
      setTimeout(() => this.errorMsg.set(''), 4000);
    }
  }

  private _startTyping(): void {
    let count = 0;
    this._typingInterval = setInterval(() => {
      count = (count + 1) % 4;
      this.typingDots.set('.'.repeat(count));
    }, 400);
  }

  private _stopTyping(): void {
    clearInterval(this._typingInterval);
    this.typingDots.set('');
  }

  private scrollToBottom(): void {
    try {
      this.messagesEnd?.nativeElement?.scrollIntoView({ behavior: 'smooth' });
    } catch {}
  }

  // ── UI helpers ─────────────────────────────────────────────────────────
  setLanguage(lang: 'fr' | 'ar' | 'en'): void {
    this.language.set(lang);
  }

  formatTime(): string {
    return new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  }

  stepLabel(step: number): string {
    const labels: Record<number, string> = {
      0: 'Bonjour',
      1: 'Nom',
      2: 'Catégorie',
      3: 'Symptômes',
      4: 'Durée',
      5: 'Analyse'
    };
    return labels[step] ?? `Étape ${step}`;
  }

  // Quick numeric replies for category selection
  numericOptions = ['1','2','3','4','5','6','7','8'];

  isAtCategoryStep = computed(() => {
    const msgs = this.messages();
    const last = msgs.at(-1);
    return last?.role === 'assistant' && last.content.includes('1.') && this.step() === 2;
  });
}
