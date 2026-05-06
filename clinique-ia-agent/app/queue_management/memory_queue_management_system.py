import heapq
import asyncio
import logging
from datetime import datetime
from typing import Dict, List, Tuple, Optional

from app.queue_management.iqueue_management_system import IQueueManagementSystem

logger = logging.getLogger(__name__)


class MemoryQueueManagementSystem(IQueueManagementSystem):

    def __init__(self) -> None:
        self._heap: List[Tuple[int, int, dict]] = []
        self._arrival_counter: int = 0
        self._sessions: Dict[str, List[dict]] = {}
        self._session_locks: Dict[str, asyncio.Lock] = {}
        self._total_served: int = 0
        self._total_sessions_created: int = 0
        self._started_at: datetime = datetime.now()

    def enqueue(self, name: str, urgency: int, specialty: str = "", symptoms: str = "") -> str:
        if urgency not in (1, 2, 3):
            urgency = 3
        self._arrival_counter += 1
        urgency_labels = {1: "élevé", 2: "moyen", 3: "faible"}
        patient_data = {
            "name": name,
            "urgency": urgency,
            "urgency_label": urgency_labels.get(urgency, "faible"),
            "specialty": specialty,
            "symptoms": symptoms,
            "arrived_at": datetime.now().isoformat(),
            "arrival_order": self._arrival_counter,
            "status": "waiting",
        }
        heapq.heappush(self._heap, (urgency, self._arrival_counter, patient_data))
        msg = f"Patient {name} ajouté — urgence: {urgency_labels.get(urgency)} — position: {len(self._heap)}"
        logger.info(msg)
        return msg

    def dequeue(self) -> dict:
        if not self._heap:
            return {"error": "File vide"}
        _, _, patient_data = heapq.heappop(self._heap)
        patient_data["status"] = "called"
        patient_data["called_at"] = datetime.now().isoformat()
        self._total_served += 1
        logger.info(f"Patient appelé : {patient_data['name']} | {patient_data['specialty']}")
        return patient_data

    def get_queue_status(self) -> list:
        return [
            {**data, "queue_position": pos}
            for pos, (_, _, data) in enumerate(sorted(self._heap), start=1)
        ]

    def peek_next(self) -> Optional[dict]:
        if not self._heap:
            return None
        return self._heap[0][2]

    def get_stats(self) -> dict:
        queue = self.get_queue_status()
        uptime = (datetime.now() - self._started_at).total_seconds()
        counts = {"élevé": 0, "moyen": 0, "faible": 0}
        for p in queue:
            label = p.get("urgency_label", "faible")
            if label in counts:
                counts[label] += 1
        return {
            "total_waiting": len(queue),
            "urgent_count": counts["élevé"],
            "medium_count": counts["moyen"],
            "low_count": counts["faible"],
            "active_sessions": len(self._sessions),
            "total_served_today": self._total_served,
            "total_sessions_created": self._total_sessions_created,
            "uptime_seconds": round(uptime, 1),
            "urgency_breakdown": counts,
        }

    def get_history(self, session_id: str) -> List[dict]:
        return list(self._sessions.get(session_id, []))

    def add_message(self, session_id: str, role: str, content: str) -> None:
        if session_id not in self._sessions:
            self._sessions[session_id] = []
            self._session_locks[session_id] = asyncio.Lock()
            self._total_sessions_created += 1
        self._sessions[session_id].append({
            "role": role,
            "content": content,
            "timestamp": datetime.now().isoformat(),
        })

    def clear_session(self, session_id: str) -> None:
        self._sessions.pop(session_id, None)
        self._session_locks.pop(session_id, None)

    def get_session_step(self, session_id: str) -> int:
        return len([m for m in self.get_history(session_id) if m["role"] == "user"])

    def get_all_active_sessions(self) -> List[str]:
        return list(self._sessions.keys())

    def cleanup_old_sessions(self, max_steps: int = 20) -> int:
        to_delete = [sid for sid, msgs in self._sessions.items() if len(msgs) > max_steps * 2]
        for sid in to_delete:
            self._sessions.pop(sid, None)
            self._session_locks.pop(sid, None)
        if to_delete:
            logger.info(f"Nettoyage : {len(to_delete)} sessions abandonnées supprimées")
        return len(to_delete)