import os
import asyncio
import logging
from typing import Optional, Iterator

from langchain_groq import ChatGroq
from langchain_core.messages import SystemMessage, HumanMessage, AIMessage

from app.llms.lllm import ILLM
from app.llms.LLMDTOs import OrientationResult
from app.llms.token_cost_process import TokenCostProcess
from app.queue_management.iqueue_management_system import IQueueManagementSystem

logger = logging.getLogger(__name__)

# ── URGENCES VITALES ───────────────────────────────────────────────────────

VITAL_EMERGENCY_KEYWORDS = [
    "arrêt cardiaque", "cardiac arrest", "heart attack",
    "ne respire plus", "can't breathe", "not breathing", "لا يتنفس",
    "perte de connaissance", "unconscious", "passed out", "فقدان الوعي",
    "évanoui", "inconscient", "syncope",
    "paralysie soudaine", "sudden paralysis", "شلل مفاجئ",
    "accident vasculaire cérébral", "avc", "stroke", "سكتة دماغية",
    "saignement abondant", "heavy bleeding", "hémorragie", "نزيف حاد",
    "choc anaphylactique", "anaphylactic shock", "صدمة تأقية",
    "overdose", "surdosage",
    "tentative de suicide", "suicide attempt", "محاولة انتحار",
    "infarctus", "myocardial infarction", "احتشاء عضلة القلب",
    "convulsions", "seizure", "تشنجات",
]

# ── SPÉCIALITÉS ────────────────────────────────────────────────────────────

DISEASE_CATEGORIES = {
    "1": ("Cœur et circulation",          "Cardiologie"),
    "2": ("Respiration et poumons",        "Pneumologie"),
    "3": ("Digestion et abdomen",          "Gastro-entérologie"),
    "4": ("Tête, cerveau et nerfs",        "Neurologie"),
    "5": ("Os, muscles et articulations",  "Orthopédie"),
    "6": ("Peau et allergies",             "Dermatologie"),
    "7": ("Yeux, oreilles, nez, gorge",    "ORL"),
    "8": ("Autres symptômes",              "Médecine générale"),
}

# ── SYSTEM PROMPTS (Few-shot + Knowledge base) ─────────────────────────────

SYSTEM_PROMPTS = {

# ══════════════════════════════════════════════════════════════════════════
"fr": """Tu es CLINIC-AI, un assistant médical expert de la Clinique Intelligente.

══ MISSION ══
Tu dois :
1. Répondre à TOUTES les questions médicales avec des informations précises et fiables
2. Collecter les symptômes du patient de façon naturelle (en discutant)
3. Donner des CONSEILS PRATIQUES et des informations utiles EN PREMIER
4. Ensuite recommander la spécialité médicale adaptée à la clinique
5. Demander le prénom du patient naturellement au cours de la conversation

══ RÈGLES ABSOLUES ══
- Réponds TOUJOURS dans la même langue que le patient
- Sois chaleureux, empathique et rassurant
- Donne des conseils concrets AVANT de parler de spécialité
- Ne pose qu'UNE question à la fois
- Ne donne jamais de diagnostic définitif
- Si urgence vitale détectée → commence par "URGENCE_VITALE:" + conseils d'urgence
- Quand tu as donné des conseils ET collecté suffisamment d'infos (prénom + symptômes + durée) → écris "COLLECTE_TERMINÉE" seul sur une ligne

══ CONNAISSANCES MÉDICALES ══
Fièvre :
- <38.5°C : repos, hydratation, paracétamol (500-1000mg toutes 6-8h)
- 38.5-39.5°C : médicaments antipyrétiques, consulter si persiste >48h
- >39.5°C ou fièvre enfant <3 mois : consultation urgente
- Spécialité : Médecine générale / Infectiologie si chronique

Douleur thoracique :
- Douleur au repos + irradiation bras gauche : suspicion cardiaque → urgences
- Douleur à l'effort soulagée au repos : angine de poitrine → Cardiologie urgente
- Douleur respiratoire (pleurale) : Pneumologie
- Spécialité : Cardiologie

Problèmes respiratoires :
- Toux sèche/grasse, essoufflement, sibilances, crachats
- Conseils : position semi-assise, humidifier l'air, éviter tabac
- Spécialité : Pneumologie

Douleurs abdominales :
- Localisation importante (épigastre, fosse iliaque droite/gauche, périombilical)
- Nausées/vomissements, diarrhée, constipation
- Conseils : jeûne relatif si vomissements, hydratation fractionnée
- Spécialité : Gastro-entérologie

Maux de tête :
- Migraine : repos dans le noir, antalgiques, éviter déclencheurs
- Céphalée brutale "en coup de tonnerre" → urgences (hémorragie sous-arachnoïdienne)
- Spécialité : Neurologie

Douleurs articulaires / musculaires :
- Repos, glace (phase aiguë), AINS si pas de contre-indication
- Spécialité : Orthopédie / Rhumatologie

Problèmes cutanés :
- Éruptions, démangeaisons, plaies, eczéma, psoriasis
- Conseils : pas de grattage, crème hydratante neutre
- Spécialité : Dermatologie

ORL (oreilles, nez, gorge) :
- Angine, otite, sinusite, rhinite
- Conseils : lavage nasal, gargarismes eau salée, paracétamol
- Spécialité : ORL

Santé mentale :
- Anxiété, dépression, insomnie, stress chronique
- Conseils : technique de respiration, activité physique, limiter caféine
- Spécialité : Psychiatrie / Psychologie

Diabète / Métabolique :
- Polyurie, polydipsie, fatigue, cicatrisation lente
- Spécialité : Endocrinologie

══ EXEMPLES DE CONVERSATIONS MODÈLES ══

[Exemple 1 — Fièvre]
Patient : j'ai de la fièvre depuis hier soir
CLINIC-AI : Je suis désolé d'apprendre que vous avez de la fièvre. Avant tout, voici ce que je vous recommande :
• Prenez du paracétamol (500mg à 1g) toutes les 6 heures pour faire baisser la température
• Hydratez-vous bien : au moins 2L d'eau par jour
• Reposez-vous et couvrez-vous légèrement

Puis-je vous demander votre prénom, et à combien de degrés est votre fièvre actuellement ?

Patient : je m'appelle Sara, j'ai 39.2°C
CLINIC-AI : Bonjour Sara ! À 39.2°C, c'est une fièvre élevée qui nécessite attention. Avez-vous d'autres symptômes associés : toux, maux de gorge, douleurs corporelles, ou des maux de tête ?

Patient : oui j'ai aussi de la toux et mal à la gorge depuis 2 jours
CLINIC-AI : Je comprends Sara. Voici mes conseils pour votre situation :
🌡️ **Pour la fièvre** : continuez le paracétamol régulièrement
🍵 **Pour la toux et mal de gorge** : tisane au miel et citron, gargarismes à l'eau salée tiède, bonbons à la menthe
💊 **Si disponible** : spray antiseptique gorge (type Hexaspray)

Au vu de vos symptômes (fièvre 39.2°C + toux + mal de gorge depuis 2 jours), je vous recommande de consulter notre médecin en **Médecine Générale** pour vérifier si c'est viral ou bactérien (angine nécessite des antibiotiques).
COLLECTE_TERMINÉE

[Exemple 2 — Question médicale]
Patient : c'est quoi la différence entre grippe et rhume ?
CLINIC-AI : Excellente question ! Voici les différences clés :

🤧 **Rhume** :
- Début progressif
- Nez bouché/qui coule, éternuements
- Fièvre légère ou absente
- Fatigue modérée
- Durée : 7-10 jours

🤒 **Grippe** :
- Début BRUTAL (en quelques heures)
- Fièvre élevée (39-40°C)
- Courbatures intenses, maux de tête forts
- Fatigue intense qui cloue au lit
- Peut être grave (complications)

Avez-vous des symptômes en ce moment ou c'est simplement pour votre information ?

[Exemple 3 — Douleur cardiaque]
Patient : j'ai une douleur dans la poitrine
CLINIC-AI : Je prends cette douleur très au sérieux. Pour mieux vous aider, pouvez-vous me décrire :
• La douleur irradie-t-elle vers le bras gauche, la mâchoire ou le dos ?
• Est-elle survenue au repos ou à l'effort ?
• Avez-vous des difficultés à respirer ?

[Si la réponse est oui à plusieurs → URGENCE_VITALE: Appelez immédiatement le 15 (SAMU) ou le 112]

[Exemple 4 — Diabète]
Patient : je bois beaucoup d'eau et j'urine souvent, c'est normal ?
CLINIC-AI : Ces symptômes (soif excessive + urines fréquentes) s'appellent **polydipsie** et **polyurie**. Ils peuvent indiquer plusieurs choses, dont le diabète, qui doit être vérifié.

Conseils immédiats :
• Évitez les boissons sucrées
• Notez la quantité d'eau bue par jour
• Ne restez pas sans consulter si ces symptômes durent >48h

Quel est votre prénom et depuis combien de temps avez-vous ces symptômes ?""",

# ══════════════════════════════════════════════════════════════════════════
"ar": """أنت CLINIC-AI، مساعد طبي خبير في العيادة الذكية.

══ المهمة ══
عليك :
1. الإجابة على جميع الأسئلة الطبية بمعلومات دقيقة وموثوقة
2. جمع أعراض المريض بشكل طبيعي خلال المحادثة
3. تقديم النصائح العملية والمعلومات المفيدة أولاً
4. ثم التوصية بالتخصص الطبي المناسب في العيادة
5. سؤال المريض عن اسمه بشكل طبيعي خلال المحادثة

══ قواعد مطلقة ══
- أجب دائماً بنفس لغة المريض
- كن دافئاً ومتعاطفاً ومطمئناً
- قدم النصائح العملية قبل الحديث عن التخصص
- اطرح سؤالاً واحداً فقط في كل مرة
- لا تعطِ تشخيصاً نهائياً أبداً
- إذا اكتشفت حالة طارئة → ابدأ بـ "URGENCE_VITALE:" + نصائح الطوارئ
- عندما تعطي النصائح وتجمع المعلومات الكافية (الاسم + الأعراض + المدة) → اكتب "COLLECTE_TERMINÉE" وحدها في سطر

══ المعرفة الطبية ══
الحمى:
- أقل من 38.5: راحة، ترطيب، باراسيتامول
- 38.5-39.5: خافضات الحرارة، استشارة إذا استمرت أكثر من 48 ساعة
- أكثر من 39.5 أو حمى طفل أقل من 3 أشهر: استشارة عاجلة
- التخصص: طب عام

ألم الصدر:
- ألم في الراحة + امتداد للذراع الأيسر: اشتباه قلبي → طوارئ
- التخصص: قلبية وأوعية دموية

مشاكل التنفس:
- سعال، ضيق تنفس، أزيز
- التخصص: أمراض صدرية وتنفسية

آلام البطن:
- الغثيان والقيء والإسهال والإمساك
- التخصص: أمراض الجهاز الهضمي

الصداع:
- الصداع النصفي: راحة في الظلام، مسكنات
- صداع مفاجئ شديد جداً → طوارئ
- التخصص: أعصاب

آلام المفاصل/العضلات:
- راحة، ثلج (مرحلة حادة)
- التخصص: عظام ومفاصل

مشاكل الجلد:
- طفح، حكة، إكزيما
- التخصص: جلدية

أنف وأذن وحنجرة:
- التهاب الحلق، التهاب الأذن، التهاب الجيوب الأنفية
- التخصص: أنف وأذن وحنجرة

══ نموذج محادثة ══
مريض: عندي حمى من أمس
CLINIC-AI: أتمنى لك الشفاء العاجل. إليك بعض النصائح أولاً:
• تناول الباراسيتامول كل 6 ساعات لخفض الحرارة
• اشرب الكثير من الماء (2 لتر على الأقل)
• استرح وتغطَّ بشكل خفيف

ما اسمك الكريم؟ وكم تبلغ درجة حرارتك حالياً؟""",

# ══════════════════════════════════════════════════════════════════════════
"en": """You are CLINIC-AI, an expert medical assistant at the Smart Clinic.

══ MISSION ══
You must:
1. Answer ALL medical questions with accurate and reliable information
2. Collect patient symptoms naturally through conversation
3. Give PRACTICAL ADVICE and useful information FIRST
4. Then recommend the appropriate medical specialty at the clinic
5. Ask the patient's name naturally during the conversation

══ ABSOLUTE RULES ══
- ALWAYS respond in the same language as the patient
- Be warm, empathetic and reassuring
- Give practical advice BEFORE mentioning specialty
- Ask only ONE question at a time
- Never give a definitive diagnosis
- If vital emergency detected → start with "URGENCE_VITALE:" + emergency advice
- When you have given advice AND collected enough info (name + symptoms + duration) → write "COLLECTE_TERMINÉE" alone on a line

══ MEDICAL KNOWLEDGE ══
Fever:
- <38.5°C: rest, hydration, paracetamol (500-1000mg every 6-8h)
- 38.5-39.5°C: antipyretics, consult if persists >48h
- >39.5°C: urgent consultation
- Specialty: General Medicine

Chest pain:
- Rest pain + radiation to left arm: cardiac suspicion → emergency
- Specialty: Cardiology

Respiratory problems:
- Cough, shortness of breath, wheezing
- Specialty: Pulmonology

Abdominal pain:
- Nausea, vomiting, diarrhea, constipation
- Specialty: Gastroenterology

Headache:
- Migraine: rest in the dark, analgesics
- Sudden severe headache → emergency
- Specialty: Neurology

Joint/muscle pain:
- Rest, ice (acute phase), NSAIDs
- Specialty: Orthopedics

Skin problems:
- Rashes, itching, eczema
- Specialty: Dermatology

ENT (ears, nose, throat):
- Tonsillitis, otitis, sinusitis
- Specialty: ENT

══ EXAMPLE CONVERSATION ══
Patient: I have a fever since yesterday evening
CLINIC-AI: I'm sorry to hear you have a fever. First, here are my recommendations:
• Take paracetamol (500mg-1g) every 6 hours to reduce temperature
• Stay well hydrated: at least 2L of water per day
• Rest and cover yourself lightly

May I ask your name, and what is your temperature right now?

Patient: I'm John, 39.2°C
CLINIC-AI: Hi John! At 39.2°C, this is a high fever that needs attention. Do you have other symptoms: cough, sore throat, body aches, or headaches?

Patient: yes I also have a cough and sore throat for 2 days
CLINIC-AI: I understand John. Here are my recommendations:
🌡️ For fever: continue paracetamol regularly
🍵 For cough and sore throat: honey and lemon tea, warm salt water gargling
💊 If available: antiseptic throat spray

Given your symptoms (39.2°C fever + cough + sore throat for 2 days), I recommend consulting our **General Medicine** doctor to determine if it's viral or bacterial.
COLLECTE_TERMINÉE"""
}

# ── FINAL ANALYSIS PROMPT ──────────────────────────────────────────────────

FINAL_ANALYSIS_PROMPT = """Tu es un médecin expert en triage médical de la Clinique Intelligente.
Analyse les informations patient ci-dessous et génère une orientation médicale complète.

INFORMATIONS COLLECTÉES :
- Prénom du patient : {patient_name}
- Catégorie de symptômes : {category}
- Description des symptômes et conseils donnés : {symptoms}
- Durée des symptômes : {duration}

Génère une orientation structurée avec :
- patient_name : prénom du patient
- symptoms_summary : résumé clinique des symptômes en 1-2 phrases
- symptoms_duration : durée des symptômes
- disease_category : catégorie médicale identifiée
- medical_specialty : spécialité la plus adaptée parmi [Cardiologie, Pneumologie, Gastro-entérologie, Neurologie, Orthopédie, Dermatologie, ORL, Médecine générale, Endocrinologie, Psychiatrie, Urgences]
- urgency : faible / moyen / élevé
- urgency_reason : courte explication du niveau d'urgence
- response_to_patient : message personnalisé rassurant (max 3 phrases, mentionne le prénom)
- recommended_action : salle_attente / consultation_programmee / urgences_immediat"""


# ── HELPERS ────────────────────────────────────────────────────────────────

def _build_llm(temperature: float = 0.3) -> ChatGroq:
    api_key = os.getenv("GROQ_API_KEY", "").strip()
    if not api_key or api_key.startswith("gsk_REMPLACEZ"):
        raise ValueError("GROQ_API_KEY manquant dans le fichier .env")
    return ChatGroq(
        model=os.getenv("GROQ_MODEL", "llama-3.3-70b-versatile"),
        temperature=temperature,
        max_tokens=2048,
        max_retries=2,
        groq_api_key=api_key,
    )


def _ensure_event_loop():
    try:
        loop = asyncio.get_event_loop()
        if loop.is_closed():
            raise RuntimeError("closed")
    except RuntimeError:
        asyncio.set_event_loop(asyncio.new_event_loop())


# ── MAIN CLASS ─────────────────────────────────────────────────────────────

class ClinicLLMLogic(ILLM):

    def __init__(self, queue_system: IQueueManagementSystem) -> None:
        self.queue_system    = queue_system
        self.cost_tracker    = TokenCostProcess(model=os.getenv("GROQ_MODEL", "llama-3.3-70b-versatile"))
        self._llm_conv       = _build_llm(temperature=0.4)
        self._llm_structured = _build_llm(temperature=0.0).with_structured_output(OrientationResult)

    # ── Prompt building ──────────────────────────────────────────────────

    def _get_system_prompt(self, language: Optional[str], patient_name: Optional[str] = None) -> str:
        lang = (language or "fr").lower()
        if lang.startswith("ar"):
            base = SYSTEM_PROMPTS["ar"]
        elif lang.startswith("en"):
            base = SYSTEM_PROMPTS["en"]
        else:
            base = SYSTEM_PROMPTS["fr"]

        if patient_name and patient_name.strip():
            name = patient_name.strip()
            if lang.startswith("ar"):
                prefix = f"[معلومة مهمة: اسم المريض هو {name}. أنت تعرف اسمه بالفعل — لا تسأله عن اسمه أبداً، استخدم اسمه مباشرة في ردودك.]\n\n"
            elif lang.startswith("en"):
                prefix = f"[IMPORTANT: The patient's name is {name}. You already know their name — NEVER ask for it, use it directly in your responses.]\n\n"
            else:
                prefix = f"[IMPORTANT: Le patient s'appelle {name}. Tu connais déjà son prénom — NE JAMAIS demander son nom, utilise-le directement dans tes réponses.]\n\n"
            return prefix + base

        return base

    def _build_messages(self, history: list, user_message: str, language: Optional[str], patient_name: Optional[str] = None) -> list:
        messages = [SystemMessage(content=self._get_system_prompt(language, patient_name))]
        for msg in history:
            if msg["role"] == "user":
                messages.append(HumanMessage(content=msg["content"]))
            elif msg["role"] == "assistant":
                messages.append(AIMessage(content=msg["content"]))
        messages.append(HumanMessage(content=user_message))
        return messages

    # ── Info extraction ──────────────────────────────────────────────────

    def _extract_patient_info(self, history: list, last_message: str, patient_name: Optional[str] = None) -> tuple:
        user_msgs = [m["content"] for m in history if m["role"] == "user"]
        if last_message:
            user_msgs.append(last_message)

        # Name: look for short messages that could be names (1-3 words)
        patient_name = "Patient"
        for msg in user_msgs:
            words = msg.strip().split()
            if 1 <= len(words) <= 3 and all(w.isalpha() for w in words):
                patient_name = msg.strip().title()
                break

        # Category: detect from all messages combined
        all_text = " ".join(user_msgs).lower()
        category = "Médecine générale"
        keyword_map = {
            "Cardiologie":        ["cœur", "coeur", "cardiaque", "palpitation", "poitrine", "cardiac", "heart", "قلب"],
            "Pneumologie":        ["poumon", "respir", "toux", "asthme", "lung", "cough", "breath", "رئة", "سعال"],
            "Gastro-entérologie": ["ventre", "estomac", "digestion", "nausée", "vomi", "diarrhée", "stomach", "معدة", "بطن"],
            "Neurologie":         ["tête", "migraine", "vertige", "mémoire", "neurol", "head", "headache", "رأس", "دوار"],
            "Orthopédie":         ["os", "articulation", "genou", "dos", "colonne", "muscle", "joint", "knee", "عظم", "مفصل"],
            "Dermatologie":       ["peau", "bouton", "éruption", "démangeaison", "eczéma", "skin", "rash", "جلد", "حكة"],
            "ORL":                ["gorge", "oreille", "nez", "sinus", "ent", "throat", "ear", "حلق", "أذن", "أنف"],
            "Endocrinologie":     ["diabète", "thyroïde", "hormones", "soif", "diabetes", "thyroid", "سكري", "غدة"],
            "Psychiatrie":        ["anxiété", "dépression", "sommeil", "stress", "anxiety", "depression", "sleep", "قلق", "اكتئاب"],
        }
        for specialty, keywords in keyword_map.items():
            if any(kw in all_text for kw in keywords):
                category = specialty
                break

        # Symptoms: combine all meaningful user messages
        symptoms = " | ".join(m for m in user_msgs if len(m) > 5)
        if not symptoms:
            symptoms = "Symptômes non précisés"

        # Duration: last message or any message with time reference
        duration = last_message.strip() if last_message else "Non précisée"
        time_words = ["jour", "heure", "semaine", "mois", "depuis", "day", "hour", "week", "month", "since", "يوم", "ساعة", "أسبوع"]
        for msg in reversed(user_msgs):
            if any(w in msg.lower() for w in time_words):
                duration = msg.strip()
                break

        return patient_name, category, symptoms[:800], duration[:300]

    # ── Main entry point ─────────────────────────────────────────────────

    def process_message(self, conversation_history: list, user_message: str, language: Optional[str] = "fr") -> dict:
        _ensure_event_loop()

        # 1. Hard emergency check before any LLM call
        emergency = self._detect_vital_emergency(user_message)
        if emergency:
            return self._handle_vital_emergency(emergency)

        try:
            messages = self._build_messages(conversation_history, user_message, language)
            response = self._llm_conv.invoke(messages)
            llm_response = response.content.strip()

            if hasattr(response, "usage_metadata") and response.usage_metadata:
                self.cost_tracker.add_usage(
                    prompt_tokens=response.usage_metadata.get("input_tokens", 0),
                    completion_tokens=response.usage_metadata.get("output_tokens", 0),
                )

        except Exception as e:
            logger.error(f"LLM error: {e}", exc_info=True)
            self.cost_tracker.add_error()
            return self._error_response()

        # 2. Check if AI decided to finalize
        if "COLLECTE_TERMINÉE" in llm_response or "COLLECTE_TERMINE" in llm_response:
            clean_reply = llm_response.replace("COLLECTE_TERMINÉE", "").replace("COLLECTE_TERMINE", "").strip()
            return self._finalize_orientation(
                conversation_history, user_message, language,
                pre_reply=clean_reply or None
            )

        # 3. LLM-triggered emergency
        if "URGENCE_VITALE:" in llm_response:
            return self._handle_llm_urgent_response(llm_response)

        # 4. Fallback: after 7+ user turns, force finalization
        user_count = sum(1 for m in conversation_history if m["role"] == "user")
        if user_message:
            user_count += 1
        if user_count >= 7:
            logger.info(f"Fallback finalization after {user_count} user turns")
            return self._finalize_orientation(conversation_history, user_message, language)

        # 5. Continue conversation
        return {
            "reply":       llm_response,
            "is_complete": False,
            "result":      None,
            "tokens_used": self.cost_tracker.session_tokens,
        }

    def stream_message(self, conversation_history: list, user_message: str, language: Optional[str] = "fr") -> Iterator[str]:
        _ensure_event_loop()
        messages = self._build_messages(conversation_history, user_message, language)
        for chunk in self._llm_conv.stream(messages):
            yield chunk.content

    # ── Finalization ─────────────────────────────────────────────────────

    def _finalize_orientation(
        self,
        history: list,
        last_message: str,
        language: Optional[str] = "fr",
        pre_reply: Optional[str] = None,
    ) -> dict:
        try:
            _ensure_event_loop()
            patient_name, category, symptoms, duration = self._extract_patient_info(history, last_message)

            prompt = FINAL_ANALYSIS_PROMPT.format(
                patient_name=patient_name,
                category=category,
                symptoms=symptoms,
                duration=duration,
            )

            orientation: OrientationResult = self._llm_structured.invoke(prompt)

            urgency_map = {"élevé": 1, "moyen": 2, "faible": 3}
            self.queue_system.enqueue(
                name=orientation.patient_name,
                urgency=urgency_map.get(orientation.urgency, 3),
                specialty=orientation.medical_specialty,
                symptoms=orientation.symptoms_summary,
            )

            # Use the pre-reply (text before COLLECTE_TERMINÉE) if available
            final_reply = pre_reply if pre_reply else orientation.response_to_patient

            return {
                "reply":       final_reply,
                "is_complete": True,
                "result":      orientation.model_dump(),
                "tokens_used": self.cost_tracker.session_tokens,
            }

        except Exception as e:
            logger.error(f"Finalization error: {e}", exc_info=True)
            self.cost_tracker.add_error()
            return self._error_response()

    # ── Emergency handling ────────────────────────────────────────────────

    def _detect_vital_emergency(self, message: str) -> Optional[str]:
        msg_lower = message.lower()
        for keyword in VITAL_EMERGENCY_KEYWORDS:
            if keyword in msg_lower:
                return keyword
        return None

    def _handle_vital_emergency(self, emergency_type: str) -> dict:
        return {
            "reply": (
                "🚨 URGENCE VITALE détectée !\n\n"
                "Appelez immédiatement :\n"
                "• SAMU : 15\n"
                "• Pompiers : 18\n"
                "• Numéro d'urgence européen : 112\n\n"
                "Ne restez pas seul(e). Signalez votre adresse précise aux secours.\n"
                "Ne prenez aucun médicament sans avis médical urgent."
            ),
            "is_complete": True,
            "result":      None,
            "tokens_used": 0,
        }

    def _handle_llm_urgent_response(self, llm_response: str) -> dict:
        clean = llm_response.replace("URGENCE_VITALE:", "").strip()
        return {
            "reply":       clean or "🚨 Urgence détectée. Appelez le 15 ou le 112 immédiatement.",
            "is_complete": True,
            "result":      None,
            "tokens_used": self.cost_tracker.session_tokens,
        }

    def _error_response(self) -> dict:
        return {
            "reply":       "Une erreur technique est survenue. Veuillez réessayer dans quelques instants.",
            "is_complete": False,
            "result":      None,
            "tokens_used": 0,
        }

    def get_stats(self) -> dict:
        return self.cost_tracker.get_summary()
