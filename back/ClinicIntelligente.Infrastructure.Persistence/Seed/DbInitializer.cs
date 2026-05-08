using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ClinicIntelligente.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // ── 1. Specialties ───────────────────────────────────────────────
        var allSpecialties = new[]
        {
            ("Cardiologie",        "Maladies du cœur et des vaisseaux"),
            ("Pneumologie",        "Maladies respiratoires et des poumons"),
            ("Gastro-entérologie", "Maladies digestives et abdominales"),
            ("Neurologie",         "Maladies du cerveau et du système nerveux"),
            ("Orthopédie",         "Maladies des os, muscles et articulations"),
            ("Dermatologie",       "Maladies de la peau et des muqueuses"),
            ("ORL",                "Oreilles, nez, gorge et voies aériennes"),
            ("Médecine générale",  "Médecine de premier recours et suivi"),
            ("Endocrinologie",     "Diabète, thyroïde et troubles hormonaux"),
            ("Psychiatrie",        "Santé mentale, anxiété et dépression"),
            ("Radiologie",         "Imagerie médicale et diagnostique"),
            ("Ophtalmologie",      "Maladies des yeux et de la vision"),
            ("Gynécologie",        "Santé féminine et obstétrique"),
            ("Pédiatrie",          "Médecine de l'enfant et de l'adolescent"),
            ("Urologie",           "Voies urinaires et système urogénital"),
            ("Rhumatologie",       "Maladies inflammatoires et auto-immunes"),
            ("Infectiologie",      "Maladies infectieuses et parasitaires"),
            ("Chirurgie générale", "Interventions chirurgicales courantes"),
            ("Urgences",           "Prise en charge des urgences médicales"),
        };

        foreach (var (name, desc) in allSpecialties)
        {
            if (!await context.Specialties.AnyAsync(s => s.Name == name))
                context.Specialties.Add(new Specialty { Name = name, Description = desc });
        }
        await context.SaveChangesAsync();

        // Load all specialties into a lookup dict
        var specs = await context.Specialties.ToDictionaryAsync(s => s.Name, s => s.Id);

        // ── 2. Admin + base users ────────────────────────────────────────
        if (!await context.Users.AnyAsync(u => u.Email == "admin@clinic.com"))
        {
            await context.Users.AddRangeAsync(
                // ── Admin ──────────────────────────────────────────────────
                new User
                {
                    FirstName = "Admin", LastName = "System",
                    Email = "admin@clinic.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = Role.Admin, IsActive = true, CreatedAt = DateTime.UtcNow
                },
                // ── Patients ───────────────────────────────────────────────
                new Patient
                {
                    FirstName = "Mariem", LastName = "Saadaoui",
                    Email = "mariem@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient123!"),
                    Role = Role.Patient, IsActive = true, CreatedAt = DateTime.UtcNow,
                    Cin = "AA111111", DateOfBirth = new DateTime(2000, 1, 1),
                    Phone = "55112233", BloodGroup = "O+",
                    Address = "12 Rue de la République, Tunis",
                    EmergencyContactName = "Sami Saadaoui",
                    EmergencyContactPhone = "55000001"
                },
                new Patient
                {
                    FirstName = "Ahmed", LastName = "Ben Salah",
                    Email = "ahmed@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient123!"),
                    Role = Role.Patient, IsActive = true, CreatedAt = DateTime.UtcNow,
                    Cin = "BB222222", DateOfBirth = new DateTime(1985, 3, 15),
                    Phone = "55221144", BloodGroup = "A+",
                    Address = "45 Avenue Habib Bourguiba, Sfax",
                    EmergencyContactName = "Lena Ben Salah",
                    EmergencyContactPhone = "55000002"
                },
                new Patient
                {
                    FirstName = "Fatma", LastName = "Zouari",
                    Email = "fatma@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient123!"),
                    Role = Role.Patient, IsActive = true, CreatedAt = DateTime.UtcNow,
                    Cin = "CC333333", DateOfBirth = new DateTime(1992, 7, 20),
                    Phone = "55331155", BloodGroup = "B+",
                    Address = "8 Rue Ibn Khaldoun, Sousse",
                    EmergencyContactName = "Tarek Zouari",
                    EmergencyContactPhone = "55000003"
                },
                new Patient
                {
                    FirstName = "Karim", LastName = "Belhadj",
                    Email = "karim@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient123!"),
                    Role = Role.Patient, IsActive = true, CreatedAt = DateTime.UtcNow,
                    Cin = "DD444444", DateOfBirth = new DateTime(1978, 11, 5),
                    Phone = "55441166", BloodGroup = "AB-",
                    Address = "22 Rue du Lac, Monastir",
                    EmergencyContactName = "Rania Belhadj",
                    EmergencyContactPhone = "55000004"
                },
                // ── Lab Technicians ────────────────────────────────────────
                new LabTechnicien
                {
                    FirstName = "Ali", LastName = "Lab",
                    Email = "lab@clinic.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lab123!"),
                    Role = Role.LabTechnicien, IsActive = true, CreatedAt = DateTime.UtcNow,
                    SpecialtyId = specs["Radiologie"], Phone = "55998877"
                },
                new LabTechnicien
                {
                    FirstName = "Sarra", LastName = "Ben Amor",
                    Email = "sarra.lab@clinic.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lab123!"),
                    Role = Role.LabTechnicien, IsActive = true, CreatedAt = DateTime.UtcNow,
                    SpecialtyId = specs["Radiologie"], Phone = "55887766"
                }
            );
            await context.SaveChangesAsync();
        }

        // ── 3. Doctors (check by email, add only missing) ────────────────
        var doctorsToSeed = new[]
        {
            // Cardiologie
            ("Gregory",  "House",      "doctor@clinic.com",          "55001122", "DOC001", "Cardiologie"),
            ("Sana",     "Bouali",     "sana.bouali@clinic.com",     "55001133", "DOC002", "Cardiologie"),
            ("Tarek",    "Maaroufi",   "tarek.maaroufi@clinic.com",  "55001144", "DOC003", "Cardiologie"),

            // Pneumologie
            ("Karim",    "Mansouri",   "karim.mansouri@clinic.com",  "55002211", "DOC004", "Pneumologie"),
            ("Leila",    "Trabelsi",   "leila.trabelsi@clinic.com",  "55002222", "DOC005", "Pneumologie"),

            // Gastro-entérologie
            ("Omar",     "Khelifi",    "omar.khelifi@clinic.com",    "55003311", "DOC006", "Gastro-entérologie"),
            ("Nadia",    "Hammami",    "nadia.hammami@clinic.com",   "55003322", "DOC007", "Gastro-entérologie"),

            // Neurologie
            ("Amine",    "Slama",      "amine.slama@clinic.com",     "55004411", "DOC008", "Neurologie"),
            ("Fatma",    "Gharbi",     "fatma.gharbi@clinic.com",    "55004422", "DOC009", "Neurologie"),

            // Orthopédie
            ("Youssef",  "Ben Ali",    "youssef.benali@clinic.com",  "55005511", "DOC010", "Orthopédie"),
            ("Maroua",   "Ferchichi",  "maroua.ferchichi@clinic.com","55005522", "DOC011", "Orthopédie"),

            // Dermatologie
            ("Rim",      "Chaabane",   "rim.chaabane@clinic.com",    "55006611", "DOC012", "Dermatologie"),
            ("Mehdi",    "Dridi",      "mehdi.dridi@clinic.com",     "55006622", "DOC013", "Dermatologie"),

            // ORL
            ("Hanen",    "Ounis",      "hanen.ounis@clinic.com",     "55007711", "DOC014", "ORL"),
            ("Bilel",    "Mbarki",     "bilel.mbarki@clinic.com",    "55007722", "DOC015", "ORL"),

            // Médecine générale
            ("Sihem",    "Jebali",     "sihem.jebali@clinic.com",    "55008811", "DOC016", "Médecine générale"),
            ("Rami",     "Saidi",      "rami.saidi@clinic.com",      "55008822", "DOC017", "Médecine générale"),
            ("Wafa",     "Belhaj",     "wafa.belhaj@clinic.com",     "55008833", "DOC018", "Médecine générale"),

            // Endocrinologie
            ("Olfa",     "Ferjani",    "olfa.ferjani@clinic.com",    "55009911", "DOC019", "Endocrinologie"),
            ("Hedi",     "Zouari",     "hedi.zouari@clinic.com",     "55009922", "DOC020", "Endocrinologie"),

            // Psychiatrie
            ("Amira",    "Karray",     "amira.karray@clinic.com",    "55010011", "DOC021", "Psychiatrie"),
            ("Slim",     "Cherif",     "slim.cherif@clinic.com",     "55010022", "DOC022", "Psychiatrie"),

            // Ophtalmologie
            ("Sonia",    "Mejri",      "sonia.mejri@clinic.com",     "55011111", "DOC023", "Ophtalmologie"),
            ("Zied",     "Khemiri",    "zied.khemiri@clinic.com",    "55011122", "DOC024", "Ophtalmologie"),

            // Gynécologie
            ("Asma",     "Brik",       "asma.brik@clinic.com",       "55012211", "DOC025", "Gynécologie"),
            ("Ichraf",   "Haddad",     "ichraf.haddad@clinic.com",   "55012222", "DOC026", "Gynécologie"),

            // Pédiatrie
            ("Mouna",    "Chebbi",     "mouna.chebbi@clinic.com",    "55013311", "DOC027", "Pédiatrie"),
            ("Fares",    "Ayari",      "fares.ayari@clinic.com",     "55013322", "DOC028", "Pédiatrie"),

            // Urologie
            ("Adel",     "Hajji",      "adel.hajji@clinic.com",      "55014411", "DOC029", "Urologie"),
            ("Nizar",    "Baccouche",  "nizar.baccouche@clinic.com", "55014422", "DOC030", "Urologie"),

            // Rhumatologie
            ("Salma",    "Grira",      "salma.grira@clinic.com",     "55015511", "DOC031", "Rhumatologie"),
            ("Lotfi",    "Moussa",     "lotfi.moussa@clinic.com",    "55015522", "DOC032", "Rhumatologie"),

            // Infectiologie
            ("Hatem",    "Bannour",    "hatem.bannour@clinic.com",   "55016611", "DOC033", "Infectiologie"),
            ("Khaled",   "Jerbi",      "khaled.jerbi@clinic.com",    "55016622", "DOC034", "Infectiologie"),

            // Chirurgie générale
            ("Nour",     "Sfar",       "nour.sfar@clinic.com",       "55017711", "DOC035", "Chirurgie générale"),
            ("Imed",     "Jelassi",    "imed.jelassi@clinic.com",    "55017722", "DOC036", "Chirurgie générale"),

            // Urgences
            ("Anis",     "Hamdi",      "anis.hamdi@clinic.com",      "55018811", "DOC037", "Urgences"),
            ("Dorra",    "Elloumi",    "dorra.elloumi@clinic.com",   "55018822", "DOC038", "Urgences"),

            // Radiologie
            ("Yassine",  "Charfi",     "yassine.charfi@clinic.com",  "55019911", "DOC039", "Radiologie"),
        };

        foreach (var (first, last, email, phone, license, specialty) in doctorsToSeed)
        {
            if (!await context.Users.AnyAsync(u => u.Email == email))
            {
                context.Users.Add(new Doctor
                {
                    FirstName     = first,
                    LastName      = last,
                    Email         = email,
                    PasswordHash  = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                    Role          = Role.Doctor,
                    IsActive      = true,
                    CreatedAt     = DateTime.UtcNow,
                    SpecialtyId   = specs[specialty],
                    Phone         = phone,
                    LicenseNumber = license,
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
