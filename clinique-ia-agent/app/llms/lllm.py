"""
lllm.py — Interface abstraite pour tout LLM

POURQUOI CE FICHIER EXISTE :
  Principe SOLID — Dependency Inversion.
  backend.py n'a pas besoin de savoir si vous utilisez Groq, OpenAI, Mistral...
  Il appelle juste process_message() sur n'importe quel objet ILLM.

  Demain si vous voulez changer de provider :
  1. Créez une nouvelle classe : class MistralLLMLogic(ILLM)
  2. Changez une seule ligne dans backend.py
  3. Tout le reste continue de fonctionner
"""

from abc import ABC, abstractmethod


class ILLM(ABC):
    """Interface abstraite pour n'importe quel LLM."""

    @abstractmethod
    def process_message(self, conversation_history: list, user_message: str) -> dict:
        """
        Traite un message patient dans le contexte de sa conversation.

        Args:
            conversation_history: liste de dicts [{"role": "user|assistant", "content": "..."}]
            user_message: le nouveau message du patient

        Returns:
            dict avec les clés :
              - reply (str)        : réponse à afficher au patient
              - is_complete (bool) : True si orientation terminée
              - result (dict|None) : JSON final si is_complete=True
              - tokens_used (int)  : nombre de tokens consommés
        """
        raise NotImplementedError

    @abstractmethod
    def get_stats(self) -> dict:
        """
        Retourne les statistiques de consommation (tokens, coûts, erreurs).

        Returns:
            dict avec les clés : total_tokens, total_requests, errors, cost_usd...
        """
        raise NotImplementedError