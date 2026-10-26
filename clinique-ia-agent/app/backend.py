import os
import uuid
import time
import asyncio
import logging
import io
import base64 as _b64
from contextlib import asynccontextmanager
from typing import Optional

import httpx as _httpx

from fastapi import FastAPI, HTTPException, Request, Depends, UploadFile, File, Query
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, StreamingResponse
from fastapi.openapi.utils import get_openapi
from fastapi.security import APIKeyHeader
from dotenv import load_dotenv

from app.llms.LLMDTOs import (
    OrientationRequest, OrientationResponse,
    SessionResponse, ResetResponse,
    NextPatientResponse, CleanupResponse,
    TestMessageRequest,
)
from app.llms.clinic_llm_logic import ClinicLLMLogic
from app.queue_management.memory_queue_management_system import MemoryQueueManagementSystem

load_dotenv(override=True)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s — %(message)s",
    datefmt="%H:%M:%S"
)
logger = logging.getLogger(__name__)

_start_time   = time.time()
ADMIN_KEY     = os.getenv("ADMIN_KEY", "clinic-admin-2025")
queue_system  = MemoryQueueManagementSystem()
llm_logic     = ClinicLLMLogic(queue_system)
api_key_scheme = APIKeyHeader(name="X-Admin-Key", auto_error=False)


async def _cleanup_task():
    while True:
        await asyncio.sleep(3600)
        try:
            deleted = queue_system.cleanup_old_sessions(max_steps=15)
            if deleted:
                logger.info(f"Nettoyage auto : {deleted} sessions supprimées")
        except Exception as e:
            logger.error(f"Erreur nettoyage : {e}")


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(f"Clinique IA démarrée — {os.getenv('GROQ_MODEL', 'llama-3.3-70b-versatile')}")
    task = asyncio.create_task(_cleanup_task())
    yield
    task.cancel()
    try:
        await task
    except asyncio.CancelledError:
        pass
    logger.info("Clinique IA arrêtée")


app = FastAPI(
    title="Clinique Intelligente — Module IA",
    description="""
## Agent d'orientation médicale intelligente

### Comment tester facilement (sans répéter les étapes manuellement) :

**Option 1 — Session de test persistante :**
1. `POST /api/test/session` → crée une session de test avec ID prévisible
2. `POST /api/test/chat` → envoie UN message avec ce session_id (répéter)
3. Chaque appel retourne la réponse + l'état de la conversation

**Option 2 — Conversation complète automatique :**
`POST /api/test/full-conversation` → remplit les 4 étapes automatiquement

### Flux production normal :
1. `GET /api/new-session` → obtenir un `session_id`
2. `POST /api/orientation` → envoyer les messages un par un

### Endpoints admin :
Cliquer sur **Authorize** 🔒 et entrer : `clinic-admin-2025`
    """,
    version="3.0.0",
    lifespan=lifespan,
)


def custom_openapi():
    if app.openapi_schema:
        return app.openapi_schema
    schema = get_openapi(
        title=app.title, version=app.version,
        description=app.description, routes=app.routes,
    )
    schema.setdefault("components", {})["securitySchemes"] = {
        "AdminKeyAuth": {
            "type": "apiKey", "in": "header", "name": "X-Admin-Key",
            "description": "Clé admin (défaut: clinic-admin-2025)",
        }
    }
    app.openapi_schema = schema
    return app.openapi_schema


app.openapi = custom_openapi

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:4200", "http://localhost:5000",
        "https://localhost:7000", "http://localhost:3000",
    ],
    allow_credentials=True,
    allow_methods=["GET", "POST", "DELETE", "OPTIONS"],
    allow_headers=["*"],
)


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    logger.error(f"Erreur sur {request.url.path}: {exc}", exc_info=True)
    return JSONResponse(status_code=500, content={"error": "Erreur interne", "detail": str(exc)})


def verify_admin(api_key: str = Depends(api_key_scheme)):
    if api_key != ADMIN_KEY:
        raise HTTPException(
            status_code=403,
            detail="Clé admin invalide. Cliquez sur Authorize 🔒 et entrez: clinic-admin-2025"
        )
    return True


# ── SYSTÈME ───────────────────────────────────────────────────────

@app.get("/", tags=["Système"], include_in_schema=False)
def root():
    return {"service": "Clinique Intelligente — Module IA", "status": "running", "docs": "/docs"}


@app.get("/health", tags=["Système"])
def health_check():
    return {
        "status": "ok",
        "model": os.getenv("GROQ_MODEL", "llama-3.3-70b-versatile"),
        "uptime_seconds": round(time.time() - _start_time, 1),
        "queue": queue_system.get_stats(),
    }


@app.get("/api/stats", tags=["Système"])
def get_stats():
    return {
        "queue": queue_system.get_stats(),
        "llm": llm_logic.get_stats(),
        "uptime_seconds": round(time.time() - _start_time, 1),
    }


# ── ORIENTATION IA — PRODUCTION ───────────────────────────────────

@app.get("/api/new-session", tags=["Orientation IA"], response_model=SessionResponse)
def create_session():
    session_id = f"session-{uuid.uuid4().hex[:16]}"
    logger.info(f"Nouvelle session : {session_id}")
    return SessionResponse(session_id=session_id)


@app.post("/api/orientation", tags=["Orientation IA"], response_model=OrientationResponse)
async def orientation(request: OrientationRequest):
    import asyncio
    session_id  = request.session_id
    history     = queue_system.get_history(session_id)
    step_before = queue_system.get_session_step(session_id)

    loop = asyncio.get_event_loop()
    output = await loop.run_in_executor(
        None,
        lambda: llm_logic.process_message(history, request.user_message, language=request.language, patient_name=request.patient_name)
    )

    if request.user_message:
        queue_system.add_message(session_id, "user", request.user_message)
    queue_system.add_message(session_id, "assistant", output["reply"])

    if output["is_complete"]:
        queue_system.clear_session(session_id)
        logger.info(f"Session {session_id} — orientation complète")

    return OrientationResponse(
        reply=output["reply"],
        is_complete=output["is_complete"],
        result=output.get("result"),
        session_step=step_before + 1,
        tokens_used=output.get("tokens_used", 0),
    )


@app.delete("/api/orientation/{session_id}", tags=["Orientation IA"], response_model=ResetResponse)
def reset_session(session_id: str):
    queue_system.clear_session(session_id)
    return ResetResponse(message="Session réinitialisée", session_id=session_id)


# ── STREAMING SSE ─────────────────────────────────────────────────

@app.post("/api/orientation/stream", tags=["Orientation IA"])
def orientation_stream(request: OrientationRequest):
    """
    Même que POST /api/orientation mais en streaming (Server-Sent Events).
    Le texte s'affiche mot par mot — effet "machine à écrire".

    Angular utilise EventSource ou fetch avec ReadableStream pour consommer ce flux.
    Format SSE : `data: <chunk>\\n\\n` puis `data: [DONE]\\n\\n`
    """
    session_id = request.session_id
    history    = queue_system.get_history(session_id)

    def generate():
        full_reply = ""
        for chunk in llm_logic.stream_message(history, request.user_message, language=request.language):
            full_reply += chunk
            yield f"data: {chunk}\n\n"

        if request.user_message:
            queue_system.add_message(session_id, "user", request.user_message)
        queue_system.add_message(session_id, "assistant", full_reply)

        is_complete = "COLLECTE_TERMINÉE" in full_reply or "COLLECTE_TERMINE" in full_reply
        if is_complete:
            queue_system.clear_session(session_id)

        yield f"data: [DONE]\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── VISION HELPER ────────────────────────────────────────────────

_VISION_MODEL = os.getenv("GROQ_VISION_MODEL", "meta-llama/llama-4-scout-17b-16e-instruct")

async def _extract_image_with_vision(content: bytes, ext: str) -> str:
    """Send image to Groq vision model and return the extracted medical text."""
    mime_map = {"png": "image/png", "webp": "image/webp", "jpg": "image/jpeg", "jpeg": "image/jpeg"}
    mime = mime_map.get(ext, "image/jpeg")
    b64 = _b64.b64encode(content).decode()
    api_key = os.getenv("GROQ_API_KEY", "").strip()

    payload = {
        "model": _VISION_MODEL,
        "temperature": 0.1,
        "max_tokens": 1024,
        "messages": [
            {
                "role": "user",
                "content": [
                    {
                        "type": "image_url",
                        "image_url": {"url": f"data:{mime};base64,{b64}"},
                    },
                    {
                        "type": "text",
                        "text": (
                            "This is a medical document (prescription or lab result). "
                            "Extract ALL visible medical information: patient name, date, "
                            "doctor name, test/exam names, measured values, normal reference ranges, "
                            "medications, dosages, diagnoses, and any clinical notes. "
                            "If a value is outside the reference range, mark it explicitly. "
                            "Format the output clearly and concisely."
                        ),
                    },
                ],
            }
        ],
    }

    async with _httpx.AsyncClient(verify=False, timeout=30) as client:
        resp = await client.post(
            "https://api.groq.com/openai/v1/chat/completions",
            headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
            json=payload,
        )
        resp.raise_for_status()
        return resp.json()["choices"][0]["message"]["content"].strip()


# ── UPLOAD PDF / IMAGE ────────────────────────────────────────────

@app.post("/api/orientation/upload-context", tags=["Orientation IA"])
async def upload_medical_context(
    session_id: str = Query(..., description="Session à enrichir avec le document"),
    file: UploadFile = File(..., description="PDF ou image médicale (ordonnance, analyse)"),
):
    """
    Le patient uploade un rapport médical (PDF ou image).
    Le texte est extrait et injecté dans le contexte de la session.

    Formats supportés : PDF, PNG, JPG, JPEG
    Taille max : 10 MB
    """
    MAX_SIZE = 10 * 1024 * 1024
    content = await file.read()

    if len(content) > MAX_SIZE:
        raise HTTPException(status_code=413, detail="Fichier trop volumineux (max 10 MB)")

    filename = file.filename or ""
    ext = filename.rsplit(".", 1)[-1].lower() if "." in filename else ""

    extracted_text = ""

    if ext == "pdf":
        try:
            from pypdf import PdfReader
            reader = PdfReader(io.BytesIO(content))
            raw_text = "\n".join(
                page.extract_text() or "" for page in reader.pages
            ).strip()
            if raw_text:
                extracted_text = raw_text[:3000]
            else:
                # Scanned PDF — try vision on first page if possible
                try:
                    extracted_text = await _extract_image_with_vision(content, "pdf")
                except Exception:
                    extracted_text = "[PDF scanné — texte non extractible automatiquement. Le patient a joint une ordonnance ou un résultat d'analyse en PDF.]"
        except Exception as e:
            raise HTTPException(status_code=422, detail=f"Impossible de lire le PDF : {e}")

    elif ext in ("png", "jpg", "jpeg", "webp"):
        try:
            extracted_text = await _extract_image_with_vision(content, ext)
        except Exception as e:
            logger.warning(f"Vision extraction failed for {filename}: {e}")
            # Fallback: acknowledge the upload without crashing
            extracted_text = f"[Image médicale jointe par le patient : {filename}. Le contenu exact n'a pas pu être extrait automatiquement. Demandez au patient de décrire les informations importantes de son document.]"

    else:
        raise HTTPException(status_code=415, detail=f"Format non supporté : .{ext}. Utilisez PDF, PNG, JPG ou WebP.")

    if not extracted_text:
        raise HTTPException(status_code=422, detail="Impossible d'extraire du texte de ce fichier.")

    context_message = f"[Document médical joint par le patient : {extracted_text}]"
    queue_system.add_message(session_id, "user", context_message)

    logger.info(f"Document uploadé pour session {session_id} — {len(extracted_text)} caractères extraits")

    return {
        "message": "Document intégré avec succès dans la conversation",
        "session_id": session_id,
        "extracted_chars": len(extracted_text),
        "filename": filename,
        "preview": extracted_text[:200] + "..." if len(extracted_text) > 200 else extracted_text,
    }


# ── TEST — MEILLEURE PRATIQUE SWAGGER ─────────────────────────────

@app.post("/api/test/session", tags=["Test"])
def create_test_session(name: str = Query(default="test-patient", description="Nom identifiable pour la session")):
    """
    Crée une session de test avec un ID lisible.

    Utilisez cet ID dans `POST /api/test/chat` pour tester
    chaque message séparément dans Swagger.

    Workflow recommandé dans Swagger :
    1. Appeler cet endpoint → noter le `session_id`
    2. Coller ce `session_id` dans `POST /api/test/chat`
    3. Envoyer les messages un par un et observer les réponses
    """
    session_id = f"test-{name}-{uuid.uuid4().hex[:6]}"
    return {
        "session_id": session_id,
        "tip": "Copiez ce session_id et utilisez-le dans POST /api/test/chat",
        "steps": [
            "Étape 1 : message vide '' → salutation du bot",
            "Étape 2 : votre prénom",
            "Étape 3 : numéro de catégorie (1-8)",
            "Étape 4 : description des symptômes",
            "Étape 5 : durée → is_complete: true",
        ]
    }


@app.post("/api/test/chat", tags=["Test"], response_model=OrientationResponse)
async def test_chat(request: TestMessageRequest):
    """
    Envoie UN message dans une session existante et reçoit la réponse du bot.

    **C'est la meilleure façon de tester dans Swagger** :
    - Gardez le même `session_id` entre les appels
    - Changez uniquement `user_message` à chaque étape
    - Observez `session_step` pour savoir où vous en êtes
    - Quand `is_complete: true` → regardez `result` pour le diagnostic

    Commencez avec `user_message: ""` (vide) pour la salutation.
    """
    import asyncio
    session_id  = request.session_id
    history     = queue_system.get_history(session_id)
    step_before = queue_system.get_session_step(session_id)

    loop = asyncio.get_event_loop()
    output = await loop.run_in_executor(
        None,
        lambda: llm_logic.process_message(history, request.user_message, language=request.language)
    )

    if request.user_message:
        queue_system.add_message(session_id, "user", request.user_message)
    queue_system.add_message(session_id, "assistant", output["reply"])

    if output["is_complete"]:
        queue_system.clear_session(session_id)

    return OrientationResponse(
        reply=output["reply"],
        is_complete=output["is_complete"],
        result=output.get("result"),
        session_step=step_before + 1,
        tokens_used=output.get("tokens_used", 0),
    )


@app.get("/api/test/history/{session_id}", tags=["Test"])
def get_test_history(session_id: str):
    """
    Affiche l'historique complet d'une session de test.
    Utile pour déboguer ce que le LLM a reçu.
    """
    history = queue_system.get_history(session_id)
    return {
        "session_id": session_id,
        "total_messages": len(history),
        "current_step": queue_system.get_session_step(session_id),
        "history": history,
    }


@app.delete("/api/test/session/{session_id}", tags=["Test"])
def reset_test_session(session_id: str):
    """Réinitialise une session de test pour recommencer depuis le début."""
    queue_system.clear_session(session_id)
    return {"message": f"Session {session_id} réinitialisée — recommencez avec un message vide."}


@app.post("/api/test/full-conversation", tags=["Test"])
def test_full_conversation(
    prenom: str = Query(default="Mohamed"),
    categorie: str = Query(default="1", description="1=Cœur · 2=Poumons · 3=Digestion · 4=Neurologie · 5=Orthopédie · 6=Peau · 7=ORL · 8=Autres"),
    symptomes: str = Query(default="douleur thoracique modérée"),
    duree: str = Query(default="depuis ce matin"),
    langue: str = Query(default="fr", description="fr / ar / en"),
):
    """
    Simule une conversation complète automatiquement.
    Retourne le transcript + le résultat final.
    """
    session_id = f"auto-{uuid.uuid4().hex[:8]}"
    exchanges  = []
    history    = []

    for msg in ["", prenom, categorie, symptomes, duree]:
        output = llm_logic.process_message(history, msg, language=langue)
        if msg:
            history.append({"role": "user", "content": msg})
        history.append({"role": "assistant", "content": output["reply"]})
        exchanges.append({
            "step": len(exchanges) + 1,
            "user": msg or "(salutation)",
            "bot": output["reply"],
            "is_complete": output["is_complete"],
        })
        if output["is_complete"]:
            return {
                "session_id": session_id, "status": "completed",
                "total_exchanges": len(exchanges),
                "final_result": output.get("result"),
                "conversation": exchanges,
                "tokens_used": output.get("tokens_used", 0),
            }

    return {"session_id": session_id, "status": "incomplete", "exchanges": exchanges}


# ── ADMINISTRATION ────────────────────────────────────────────────

@app.get(
    "/api/queue/status", tags=["Administration"],
    summary="File d'attente complète (🔒 admin)",
    openapi_extra={"security": [{"AdminKeyAuth": []}]},
)
def get_queue(admin=Depends(verify_admin)):
    queue = queue_system.get_queue_status()
    return {"queue": queue, "stats": queue_system.get_stats(), "next_patient": queue[0] if queue else None}


@app.get(
    "/api/queue/sessions", tags=["Administration"],
    summary="Sessions actives (🔒 admin)",
    openapi_extra={"security": [{"AdminKeyAuth": []}]},
)
def get_active_sessions(admin=Depends(verify_admin)):
    """
    Liste toutes les sessions de conversation en cours.
    Utile pour monitorer combien de patients sont en train de remplir le formulaire.
    """
    sessions = queue_system.get_all_active_sessions()
    details = []
    for sid in sessions:
        details.append({
            "session_id": sid,
            "step": queue_system.get_session_step(sid),
            "messages_count": len(queue_system.get_history(sid)),
        })
    return {
        "total_active": len(sessions),
        "sessions": details,
    }


@app.post(
    "/api/queue/next", tags=["Administration"],
    response_model=NextPatientResponse,
    summary="Appeler le prochain patient ( admin)",
    openapi_extra={"security": [{"AdminKeyAuth": []}]},
)
def call_next_patient(admin=Depends(verify_admin)):
    patient = queue_system.dequeue()
    if "error" in patient:
        return NextPatientResponse(message="Aucun patient en attente", patient=None, remaining=0)
    remaining = len(queue_system.get_queue_status())
    logger.info(f"Patient appelé : {patient.get('name')} — restants: {remaining}")
    return NextPatientResponse(
        message=f"Patient appelé : {patient['name']} → {patient.get('specialty', '')}",
        patient=patient, remaining=remaining,
    )


@app.post(
    "/api/queue/cleanup", tags=["Administration"],
    response_model=CleanupResponse,
    summary="Nettoyer sessions abandonnées ( admin)",
    openapi_extra={"security": [{"AdminKeyAuth": []}]},
)
def cleanup_sessions(admin=Depends(verify_admin)):
    deleted = queue_system.cleanup_old_sessions(max_steps=15)
    return CleanupResponse(message="Nettoyage effectué", sessions_deleted=deleted)


if __name__ == "__main__":
    import uvicorn
    port = int(os.getenv("APP_PORT", 8000))
    uvicorn.run("app.backend:app", host="0.0.0.0", port=port, reload=True)