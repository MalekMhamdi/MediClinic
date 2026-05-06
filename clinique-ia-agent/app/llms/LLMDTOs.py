from pydantic import BaseModel, Field, field_validator
from typing import Optional, Literal
from datetime import datetime


class OrientationResult(BaseModel):
    patient_name: str = Field(description="Prénom du patient")
    symptoms_summary: str = Field(description="Résumé des symptômes en une phrase")
    symptoms_duration: str = Field(description="Durée des symptômes")
    disease_category: str = Field(default="Non précisée", description="Catégorie choisie par le patient")
    medical_specialty: Literal[
        "Cardiologie", "Neurologie", "Pneumologie",
        "Gastro-entérologie", "Dermatologie", "Orthopédie",
        "ORL", "Médecine générale", "Urgences",
    ] = Field(description="Spécialité médicale recommandée")
    urgency: Literal["faible", "moyen", "élevé"] = Field(description="Niveau d'urgence")
    urgency_reason: str = Field(description="Explication du niveau d'urgence")
    response_to_patient: str = Field(description="Message rassurant pour le patient. Max 3 phrases.")
    recommended_action: Literal[
        "salle_attente", "urgences_immediat", "consultation_programmee",
    ] = Field(description="Action recommandée")


class OrientationRequest(BaseModel):
    session_id: str = Field(description="Identifiant unique de session")
    user_message: str = Field(default="", description="Message du patient", max_length=2000)
    language: Optional[str] = Field(default="fr", description="Langue : fr / ar / en")

    @field_validator("user_message")
    @classmethod
    def strip_message(cls, v: str) -> str:
        return v.strip()

    @field_validator("session_id")
    @classmethod
    def validate_session_id(cls, v: str) -> str:
        v = v.strip()
        if not v:
            raise ValueError("session_id ne peut pas être vide")
        return v


class TestMessageRequest(BaseModel):
    """Utilisé par POST /api/test/chat — même structure qu'OrientationRequest."""
    session_id: str = Field(
        description="session_id obtenu via POST /api/test/session",
        examples=["test-patient-abc123"]
    )
    user_message: str = Field(
        default="",
        description="Message du patient. Commencez par '' (vide) pour la salutation.",
        examples=["", "Mohamed", "1", "j'ai des douleurs thoraciques", "depuis ce matin"],
        max_length=2000,
    )
    language: Optional[str] = Field(default="fr", description="fr / ar / en")

    @field_validator("user_message")
    @classmethod
    def strip_message(cls, v: str) -> str:
        return v.strip()


class OrientationResponse(BaseModel):
    reply: str = Field(description="Réponse du bot à afficher dans le chat")
    is_complete: bool = Field(default=False, description="True quand l'orientation est terminée")
    result: Optional[dict] = Field(default=None, description="Résultat complet si is_complete=True")
    session_step: int = Field(default=0, description="Numéro de l'échange (1, 2, 3...)")
    tokens_used: int = Field(default=0, description="Tokens consommés")
    timestamp: str = Field(default_factory=lambda: datetime.now().isoformat())


class PatientQueueItem(BaseModel):
    patient_name: str
    urgency: int
    urgency_label: str
    specialty: str
    symptoms_summary: str
    arrived_at: str
    queue_position: int
    recommended_action: Optional[str] = None


class QueueStatusResponse(BaseModel):
    queue: list
    stats: dict
    next_patient: Optional[dict] = None


class HealthResponse(BaseModel):
    status: str
    service: str = "Clinique Intelligente — Module IA"
    model: str
    environment: str
    uptime_seconds: float
    queue: dict


class SessionResponse(BaseModel):
    session_id: str
    message: str = "Session créée."


class ResetResponse(BaseModel):
    message: str
    session_id: str


class NextPatientResponse(BaseModel):
    message: str
    patient: Optional[dict] = None
    remaining: int = 0


class CleanupResponse(BaseModel):
    message: str
    sessions_deleted: int