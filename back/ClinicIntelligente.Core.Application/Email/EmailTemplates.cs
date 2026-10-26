namespace ClinicIntelligente.Core.Application.Email;

/// <summary>HTML email templates. Uses plain .Replace() to avoid C# brace-escaping conflicts with CSS.</summary>
public static class EmailTemplates
{
    // ── Base layout (CSS uses raw {{ }} — no C# interpolation here) ────────
    private const string BaseLayout = @"
<!DOCTYPE html><html lang=""fr"">
<head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
<style>
  body{margin:0;padding:0;background:#f4f7fb;font-family:'Segoe UI',Arial,sans-serif;}
  .shell{max-width:600px;margin:32px auto;background:#fff;border-radius:14px;overflow:hidden;
         box-shadow:0 2px 16px rgba(0,0,0,.08);}
  .header{background:__COLOR__;padding:32px 36px 28px;}
  .header-logo{color:#fff;font-size:22px;font-weight:800;}
  .header-sub{color:rgba(255,255,255,.7);font-size:12px;margin-top:2px;}
  .email-icon{font-size:40px;text-align:center;margin:18px 0 10px;}
  .email-title{color:#fff;font-size:20px;font-weight:700;text-align:center;margin:0;}
  .body{padding:32px 36px;}
  .greeting{font-size:15px;color:#1a2332;font-weight:600;margin-bottom:12px;}
  .text{font-size:14px;color:#4a5568;line-height:1.65;margin:0 0 14px;}
  .card{background:#f4f7fb;border-radius:10px;padding:18px 22px;margin:20px 0;}
  .card-row{padding:7px 0;border-bottom:1px solid #e4eaf3;font-size:13px;
             display:flex;justify-content:space-between;}
  .card-row:last-child{border-bottom:none;}
  .card-label{color:#6b7a99;font-weight:500;}
  .card-value{color:#1a2332;font-weight:600;text-align:right;}
  .alert-box{background:#fff8e1;border-left:4px solid #f59e0b;border-radius:6px;
              padding:12px 16px;font-size:13px;color:#92400e;margin:14px 0;}
  .footer{text-align:center;padding:20px 36px 28px;font-size:11px;color:#aab4cc;}
</style>
</head>
<body>
<div class=""shell"">
  <div class=""header"">
    <div class=""header-logo"">MediClinic</div>
    <div class=""header-sub"">Centre Medical Accredite — Tunis, Tunisie</div>
    <div class=""email-icon"">__ICON__</div>
    <p class=""email-title"">__TITLE__</p>
  </div>
  <div class=""body"">__BODY__</div>
  <div class=""footer"">
    &copy; 2026 MediClinic &middot; Tunis, Tunisie &middot; +216 71 000 000<br/>
    Cet email a ete envoye automatiquement — merci de ne pas y repondre directement.
  </div>
</div>
</body></html>";

    private static string Wrap(string title, string icon, string color, string body)
        => BaseLayout
            .Replace("__COLOR__", color)
            .Replace("__ICON__",  icon)
            .Replace("__TITLE__", title)
            .Replace("__BODY__",  body);

    // ── J-1 REMINDER ───────────────────────────────────────────────────────
    public static string Reminder(
        string patientName, DateTime dateTime, string doctorName, string specialty)
    {
        var date = dateTime.ToString("dddd dd MMMM yyyy",
            System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
        var time = dateTime.ToString("HH:mm");

        var body = $@"
<p class=""greeting"">Bonjour {patientName},</p>
<p class=""text"">Ce message est un rappel : vous avez un rendez-vous <strong>demain</strong>.</p>
<div class=""card"">
  <div class=""card-row""><span class=""card-label"">Date</span>
    <span class=""card-value"">{date}</span></div>
  <div class=""card-row""><span class=""card-label"">Heure</span>
    <span class=""card-value"">{time}</span></div>
  <div class=""card-row""><span class=""card-label"">Medecin</span>
    <span class=""card-value"">Dr. {doctorName}</span></div>
  <div class=""card-row""><span class=""card-label"">Specialite</span>
    <span class=""card-value"">{specialty}</span></div>
  <div class=""card-row""><span class=""card-label"">Lieu</span>
    <span class=""card-value"">MediClinic — Batiment Principal</span></div>
</div>
<div class=""alert-box"">
  Veuillez vous presenter <strong>10 minutes avant</strong> l'heure de votre rendez-vous.
  N'oubliez pas votre carte d'identite.
</div>
<p class=""text"">Pour annuler ou reporter, connectez-vous a votre espace patient sur MediClinic.</p>";

        return Wrap("Rappel de Rendez-vous", "🔔", "#0B2D5C", body);
    }

    // ── APPOINTMENT CONFIRMED (patient) ────────────────────────────────────
    public static string AppointmentConfirmed(
        string patientName, DateTime dateTime, string doctorName, string specialty)
    {
        var date = dateTime.ToString("dddd dd MMMM yyyy",
            System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
        var time = dateTime.ToString("HH:mm");

        var body = $@"
<p class=""greeting"">Bonjour {patientName},</p>
<p class=""text"">Votre rendez-vous a ete <strong>confirme</strong> par le medecin.</p>
<div class=""card"">
  <div class=""card-row""><span class=""card-label"">Date</span>
    <span class=""card-value"">{date}</span></div>
  <div class=""card-row""><span class=""card-label"">Heure</span>
    <span class=""card-value"">{time}</span></div>
  <div class=""card-row""><span class=""card-label"">Medecin</span>
    <span class=""card-value"">Dr. {doctorName}</span></div>
  <div class=""card-row""><span class=""card-label"">Specialite</span>
    <span class=""card-value"">{specialty}</span></div>
</div>
<p class=""text"">Vous recevrez un rappel la veille de votre rendez-vous.</p>";

        return Wrap("Rendez-vous Confirme", "✅", "#27AE60", body);
    }

    // ── LAB RESULT AVAILABLE ───────────────────────────────────────────────
    public static string LabResultAvailable(string patientName, string analysisType)
    {
        var body = $@"
<p class=""greeting"">Bonjour {patientName},</p>
<p class=""text"">Votre resultat d'analyse pour <strong>{analysisType}</strong> est disponible.</p>
<p class=""text"">Connectez-vous a votre espace patient pour consulter le rapport dans votre dossier medical.</p>
<div class=""alert-box"">
  Les resultats d'analyse ne doivent pas etre interpretes sans l'avis de votre medecin.
</div>";

        return Wrap("Resultat d'Analyse Disponible", "🔬", "#8e44ad", body);
    }
}
