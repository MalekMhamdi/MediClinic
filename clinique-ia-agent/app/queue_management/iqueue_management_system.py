from abc import ABC, abstractmethod
from typing import List, Optional


class IQueueManagementSystem(ABC):

    @abstractmethod
    def enqueue(self, name: str, urgency: int, specialty: str = "", symptoms: str = "") -> str:
        raise NotImplementedError

    @abstractmethod
    def dequeue(self) -> dict:
        raise NotImplementedError

    @abstractmethod
    def get_queue_status(self) -> list:
        raise NotImplementedError

    @abstractmethod
    def peek_next(self) -> Optional[dict]:
        raise NotImplementedError

    @abstractmethod
    def get_stats(self) -> dict:
        raise NotImplementedError

    @abstractmethod
    def get_history(self, session_id: str) -> List[dict]:
        raise NotImplementedError

    @abstractmethod
    def add_message(self, session_id: str, role: str, content: str) -> None:
        raise NotImplementedError

    @abstractmethod
    def clear_session(self, session_id: str) -> None:
        raise NotImplementedError

    @abstractmethod
    def get_session_step(self, session_id: str) -> int:
        raise NotImplementedError

    @abstractmethod
    def get_all_active_sessions(self) -> List[str]:
        raise NotImplementedError

    @abstractmethod
    def cleanup_old_sessions(self, max_steps: int = 20) -> int:
        raise NotImplementedError