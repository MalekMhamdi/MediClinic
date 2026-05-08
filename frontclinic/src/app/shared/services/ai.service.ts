import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const API = 'http://localhost:5217/api/v1/ai';

// ── Frontend interfaces (camelCase) ──────────────────────────────────────
export interface AiSessionResponse {
  sessionId: string;
  message:   string;
}

export interface AiOrientationResult {
  patientName:       string;
  symptomsSummary:   string;
  symptomsDuration:  string;
  medicalSpecialty:  string;
  urgency:           string;
  urgencyReason:     string;
  responseToPatient: string;
  recommendedAction: string;
}

export interface AiChatResponse {
  reply:               string;
  isComplete:          boolean;
  sessionStep:         number;
  result:              AiOrientationResult | null;
  suggestedDoctorId:   number | null;
  suggestedDoctorName: string;
  suggestedSpecialty:  string;
}

export interface ChatMessage {
  role:    'user' | 'assistant';
  content: string;
  step?:   number;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  constructor(private http: HttpClient) {}

  /** Step 1 — create a new AI session */
  async createSession(): Promise<AiSessionResponse> {
    // .NET sends { session_id, message } (snake_case from [JsonPropertyName])
    const raw = await firstValueFrom(this.http.get<any>(`${API}/session`));
    return {
      sessionId: raw.session_id ?? raw.sessionId ?? '',
      message:   raw.message ?? '',
    };
  }

  /** Step 2+ — send a message */
  async chat(sessionId: string, userMessage: string, language = 'fr', patientName?: string): Promise<AiChatResponse> {
    // .NET sends top-level camelCase but nested result fields are snake_case
    const raw = await firstValueFrom(
      this.http.post<any>(`${API}/chat`, { sessionId, userMessage, language, patientName })
    );
    return {
      reply:               raw.reply ?? '',
      isComplete:          raw.isComplete ?? false,
      sessionStep:         raw.sessionStep ?? 0,
      suggestedDoctorId:   raw.suggestedDoctorId ?? null,
      suggestedDoctorName: raw.suggestedDoctorName ?? '',
      suggestedSpecialty:  raw.suggestedSpecialty ?? '',
      result: raw.result ? {
        patientName:       raw.result.patient_name       ?? raw.result.patientName       ?? '',
        symptomsSummary:   raw.result.symptoms_summary   ?? raw.result.symptomsSummary   ?? '',
        symptomsDuration:  raw.result.symptoms_duration  ?? raw.result.symptomsDuration  ?? '',
        medicalSpecialty:  raw.result.medical_specialty  ?? raw.result.medicalSpecialty  ?? '',
        urgency:           raw.result.urgency            ?? '',
        urgencyReason:     raw.result.urgency_reason     ?? raw.result.urgencyReason     ?? '',
        responseToPatient: raw.result.response_to_patient ?? raw.result.responseToPatient ?? '',
        recommendedAction: raw.result.recommended_action  ?? raw.result.recommendedAction  ?? '',
      } : null,
    };
  }

  /** Reset / delete session */
  async resetSession(sessionId: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/session/${sessionId}`));
  }

  /** Upload a medical document (PDF/image) into the session context */
  async uploadContext(sessionId: string, file: File): Promise<{ extractedChars: number; preview: string; filename: string }> {
    const form = new FormData();
    form.append('file', file);
    const raw = await firstValueFrom(
      this.http.post<any>(`${API}/upload-context?sessionId=${encodeURIComponent(sessionId)}`, form)
    );
    return {
      extractedChars: raw.extractedChars ?? raw.extracted_chars ?? 0,
      preview:  raw.preview  ?? '',
      filename: raw.filename ?? file.name,
    };
  }
}
