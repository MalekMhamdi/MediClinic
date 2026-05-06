"""
token_cost_process.py — Suivi des tokens et coûts

POURQUOI CE FICHIER EXISTE :
  En production, vous devez savoir combien de tokens vous consommez.
  Même si Groq est gratuit maintenant, vous pouvez passer à OpenAI
  un jour. Ce module vous donne une visibilité sur la consommation.

UTILISATION :
  tracker = TokenCostProcess()
  tracker.add_usage(prompt_tokens=150, completion_tokens=80)
  print(tracker.get_summary())
"""

from dataclasses import dataclass, field
from datetime import datetime


# Coût en USD par 1000 tokens (mis à jour avril 2025)
MODEL_COSTS: dict[str, dict[str, float]] = {
    # Groq — GRATUIT pour les comptes dev
    "llama-3.3-70b-versatile": {"input": 0.0,      "output": 0.0},
    "llama-3.1-8b-instant":    {"input": 0.0,      "output": 0.0},
    "llama-4-scout":           {"input": 0.00011,  "output": 0.00034},
    "gemma2-9b-it":            {"input": 0.0,      "output": 0.0},
    # OpenAI (pour référence si vous changez un jour)
    "gpt-3.5-turbo":           {"input": 0.0005,   "output": 0.0015},
    "gpt-4o":                  {"input": 0.005,    "output": 0.015},
    "gpt-4o-mini":             {"input": 0.00015,  "output": 0.0006},
}


@dataclass
class TokenCostProcess:
    """
    Suit les tokens utilisés et calcule les coûts.
    Une instance par ClinicLLMLogic (durée de vie = durée du serveur).
    """
    model: str = "llama-3.3-70b-versatile"

    # Compteurs cumulatifs (toute la vie du serveur)
    total_prompt_tokens: int = 0
    total_completion_tokens: int = 0
    total_requests: int = 0
    errors_count: int = 0

    # Compteurs de la session courante (remis à zéro par reset_session())
    session_prompt_tokens: int = 0
    session_completion_tokens: int = 0

    started_at: datetime = field(default_factory=datetime.now)

    def add_usage(self, prompt_tokens: int, completion_tokens: int) -> None:
        """Enregistrer la consommation d'un appel LLM."""
        self.total_prompt_tokens     += prompt_tokens
        self.total_completion_tokens += completion_tokens
        self.session_prompt_tokens   += prompt_tokens
        self.session_completion_tokens += completion_tokens
        self.total_requests += 1

    def add_error(self) -> None:
        """Enregistrer une erreur LLM."""
        self.errors_count += 1

    @property
    def total_tokens(self) -> int:
        return self.total_prompt_tokens + self.total_completion_tokens

    @property
    def session_tokens(self) -> int:
        return self.session_prompt_tokens + self.session_completion_tokens

    def get_cost_usd(self) -> float:
        """Calcule le coût total en USD."""
        costs = MODEL_COSTS.get(self.model, {"input": 0.002, "output": 0.002})
        input_cost  = (self.total_prompt_tokens     / 1000) * costs["input"]
        output_cost = (self.total_completion_tokens / 1000) * costs["output"]
        return round(input_cost + output_cost, 6)

    def get_summary(self) -> dict:
        """Retourne un résumé complet pour les logs et le monitoring."""
        uptime = (datetime.now() - self.started_at).total_seconds()
        return {
            "model": self.model,
            "total_tokens": self.total_tokens,
            "prompt_tokens": self.total_prompt_tokens,
            "completion_tokens": self.total_completion_tokens,
            "total_requests": self.total_requests,
            "errors": self.errors_count,
            "cost_usd": self.get_cost_usd(),
            "uptime_seconds": round(uptime, 1),
            "avg_tokens_per_request": (
                round(self.total_tokens / self.total_requests)
                if self.total_requests > 0 else 0
            )
        }

    def reset_session(self) -> None:
        """Remet à zéro les compteurs de session (garde les totaux globaux)."""
        self.session_prompt_tokens   = 0
        self.session_completion_tokens = 0