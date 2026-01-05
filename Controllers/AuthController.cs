using HotelBookingMVC.Models;
using HotelBookingMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        // Injection du DbContext (EF Core fait ça automatiquement)
        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // ========== INSCRIPTION ==========
        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            // Si déjà connecté, rediriger vers accueil
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken] // Sécurité contre les attaques CSRF
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Vérifier que le formulaire est valide
            if (!ModelState.IsValid)
                return View(model);

            // Vérifier si l'email existe déjà
            if (_context.Utilisateurs.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Cet email est déjà utilisé");
                return View(model);
            }

            // Créer le nouvel utilisateur
            var utilisateur = new Utilisateur
            {
                Nom = model.Nom,
                Prenom = model.Prenom,
                Email = model.Email,
                MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(model.MotDePasse), // Crypter le mot de passe
                Role = "Client", // Par défaut Client
                EstActif = true,
                DateCreation = DateTime.UtcNow
            };

            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Inscription réussie ! Vous pouvez maintenant vous connecter.";
            return RedirectToAction("Login");
        }

        // ========== CONNEXION ==========
        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Chercher l'utilisateur par email
            var utilisateur = _context.Utilisateurs
                .FirstOrDefault(u => u.Email == model.Email && u.EstActif);

            // Vérifier si utilisateur existe ET mot de passe correct
            if (utilisateur == null || !BCrypt.Net.BCrypt.Verify(model.MotDePasse, utilisateur.MotDePasseHash))
            {
                ModelState.AddModelError("", "Email ou mot de passe incorrect");
                return View(model);
            }

            // Créer les "Claims" (informations sur l'utilisateur stockées dans le cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{utilisateur.Prenom} {utilisateur.Nom}"),
                new Claim(ClaimTypes.Email, utilisateur.Email),
                new Claim(ClaimTypes.Role, utilisateur.Role) // Important pour [Authorize(Roles="Admin")]
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Configurer la durée du cookie
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.SeSouvenir, // Cookie persistant si "Se souvenir"
                ExpiresUtc = model.SeSouvenir
                    ? DateTimeOffset.UtcNow.AddDays(30) // 30 jours
                    : DateTimeOffset.UtcNow.AddHours(2) // 2 heures
            };

            // Créer le cookie de connexion
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties);

            TempData["Success"] = $"Bienvenue {utilisateur.Prenom} !";

            // Rediriger selon le rôle
            if (utilisateur.Role == "Admin")
                return RedirectToAction("Index", "Home"); // Plus tard : Dashboard Admin
            else
                return RedirectToAction("Index", "Home");
        }

        // ========== DÉCONNEXION ==========
        // GET: /Auth/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Vous êtes déconnecté";
            return RedirectToAction("Index", "Home");
        }

        // ========== ACCÈS REFUSÉ ==========
        // GET: /Auth/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
