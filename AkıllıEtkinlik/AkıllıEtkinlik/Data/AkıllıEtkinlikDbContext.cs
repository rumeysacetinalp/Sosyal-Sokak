using AkilliEtkinlik.Models;
using AkıllıEtkinlik.Models;
using Microsoft.EntityFrameworkCore;

public class AkıllıEtkinlikDbContext : DbContext
{
    public AkıllıEtkinlikDbContext(DbContextOptions<AkıllıEtkinlikDbContext> options) : base(options)
    {
    }

    public DbSet<Kullanıcı> Kullanıcılar { get; set; }
    public DbSet<Etkinlik> Etkinlikler { get; set; }
    public DbSet<Katılımcı> Katılımcılar { get; set; }
  
    public DbSet<Puan> Puanlar { get; set; }
    public DbSet<Admin> AdminPanel { get; set; }

    public DbSet<GeriBildirimler> GeriBildirim { get; set; }

    public DbSet<Mesaj> Mesajlar { get; set; }

    public DbSet<Bildirim> Bildirimler { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // İlişki ve yapılandırmaları burada tanımlayabilirsiniz.
    }


}
