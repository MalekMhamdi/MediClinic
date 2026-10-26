import { Component, signal, computed, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AiService, AiChatResponse, AiOrientationResult, ChatMessage } from '../../shared/services/ai.service';
import { AuthService } from '../../shared/services/auth.service';

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
  @ViewChild('fileInput') private fileInputRef!: ElementRef<HTMLInputElement>;

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

  // ── Upload ───────────────────────────────────────────────────────────
  uploading     = signal(false);
  uploadedFiles = signal<{ name: string; chars: number }[]>([]);

  constructor(private ai: AiService, private router: Router, private auth: AuthService) {}

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
    if (u === 'high'   || u.includes('élevé') || u.includes('urgent')) return 'urgency-high';
    if (u === 'medium' || u.includes('moyen') || u.includes('modér'))  return 'urgency-medium';
    return 'urgency-low';
  });

  urgencyLabel = computed(() => {
    const u = this.orientResult()?.urgency?.toLowerCase() ?? '';
    if (u === 'high'   || u.includes('élevé')) return 'High';
    if (u === 'medium' || u.includes('moyen')) return 'Medium';
    return 'Low';
  });

  actionLabel = computed(() => {
    const a = this.orientResult()?.recommendedAction ?? '';
    const map: Record<string, string> = {
      emergency:                '🚨 Go to Emergency',
      scheduled_consultation:   '📅 Schedule a Consultation',
      waiting_room:             '🏥 Proceed to Waiting Room',
      urgences_immediat:        '🚨 Go to Emergency',
      consultation_programmee:  '📅 Schedule a Consultation',
      salle_attente:            '🏥 Proceed to Waiting Room',
    };
    return map[a] ?? a;
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

  // ── Prescription mode — start session then open file picker ─────────
  async startPrescriptionMode(): Promise<void> {
    await this.startSession();
    // Wait until chatting state is reached before triggering the file picker
    const waitForChat = () => new Promise<void>(resolve => {
      const check = setInterval(() => {
        if (this.state() === 'chatting') { clearInterval(check); resolve(); }
      }, 100);
      setTimeout(() => { clearInterval(check); resolve(); }, 5000);
    });
    await waitForChat();
    this.fileInputRef?.nativeElement?.click();
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
      const res: AiChatResponse = await this.ai.chat(sessionId, text, lang, this.auth.userName || undefined);
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
      0: 'Welcome',
      1: 'Name',
      2: 'Category',
      3: 'Symptoms',
      4: 'Duration',
      5: 'Analysis'
    };
    return labels[step] ?? `Step ${step}`;
  }

  // Quick numeric replies for category selection
  numericOptions = ['1','2','3','4','5','6','7','8'];

  // Common symptom chips shown on the idle screen (click = start session)
  quickChips = [
    { icon: '🤕', label: 'Headache' },
    { icon: '🫀', label: 'Chest pain' },
    { icon: '🤒', label: 'Fever' },
    { icon: '🔙', label: 'Back pain' },
    { icon: '🤢', label: 'Stomach pain' },
    { icon: '😮‍💨', label: 'Shortness of breath' },
    { icon: '😴', label: 'Fatigue' },
    { icon: '💫', label: 'Dizziness' },
    { icon: '🦴', label: 'Joint pain' },
    { icon: '👁️', label: 'Eye pain' },
    { icon: '🤧', label: 'Cough' },
    { icon: '🩹', label: 'Skin rash' },
  ];

  isAtCategoryStep = computed(() => {
    const msgs = this.messages();
    const last = msgs.at(-1);
    return last?.role === 'assistant' && last.content.includes('1.') && this.step() === 2;
  });

  // ── File upload ───────────────────────────────────────────────────────
  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || !this.sessionId() || this.state() !== 'chatting') return;

    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!['pdf', 'png', 'jpg', 'jpeg', 'webp'].includes(ext)) {
      this.errorMsg.set('Unsupported format. Use PDF, PNG, JPG or WebP.');
      setTimeout(() => this.errorMsg.set(''), 4000);
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.errorMsg.set('File too large (max 10 MB).');
      setTimeout(() => this.errorMsg.set(''), 4000);
      return;
    }

    this.uploading.set(true);

    // Show a system message while uploading
    this.messages.update(msgs => [
      ...msgs,
      { role: 'user', content: `📎 Uploading document: ${file.name}…`, step: this.step() }
    ]);
    this._shouldScroll = true;

    try {
      const res = await this.ai.uploadContext(this.sessionId()!, file);
      this.uploadedFiles.update(list => [...list, { name: res.filename, chars: res.extractedChars }]);

      // Replace uploading placeholder with confirmed upload
      this.messages.update(msgs => [
        ...msgs.slice(0, -1),
        { role: 'user', content: `📎 ${res.filename}`, step: this.step() }
      ]);
      this._shouldScroll = true;

      // Automatically ask AI to analyze and explain the document
      const analyzePrompt = this.language() === 'ar'
        ? 'حلل هذا المستند الطبي واشرح لي محتواه بالتفصيل حتى أتمكن من تطبيقه.'
        : this.language() === 'en'
          ? 'Please analyze this medical document and explain it to me in detail so I can follow it correctly.'
          : 'Analyse ce document médical et explique-moi son contenu en détail pour que je puisse l\'appliquer correctement.';

      await this._sendMessage(analyzePrompt);

    } catch {
      this.messages.update(msgs => msgs.slice(0, -1));
      this.errorMsg.set('Failed to upload document. Please try again.');
      setTimeout(() => this.errorMsg.set(''), 4000);
    } finally {
      this.uploading.set(false);
    }
  }
}
