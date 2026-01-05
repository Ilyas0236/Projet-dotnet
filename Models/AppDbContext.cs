using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Chambre> Chambres { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationLigne> ReservationLignes { get; set; }
        public DbSet<Paiement> Paiements { get; set; }
        public DbSet<Facture> Factures { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des décimaux pour les prix
            modelBuilder.Entity<Chambre>()
                .Property(c => c.PrixParNuit)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Reservation>()
                .Property(r => r.Total)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ReservationLigne>()
                .Property(rl => rl.PrixParNuit)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ReservationLigne>()
                .Property(rl => rl.SousTotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Paiement>()
                .Property(p => p.Montant)
                .HasColumnType("decimal(18,2)");

            // CORRIGER LES CASCADE DELETE pour éviter les cycles
            modelBuilder.Entity<ReservationLigne>()
                .HasOne(rl => rl.Chambre)
                .WithMany(c => c.ReservationLignes)
                .HasForeignKey(rl => rl.ChambreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationLigne>()
                .HasOne(rl => rl.Reservation)
                .WithMany(r => r.Lignes)
                .HasForeignKey(rl => rl.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Reservations)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Reservation)
                .WithMany()
                .HasForeignKey(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Facture>()
                .HasOne(f => f.Reservation)
                .WithMany()
                .HasForeignKey(f => f.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
