using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LoanApplication> LoanApplications { get; set; }
    public DbSet<WhatsAppContact> WhatsAppContacts { get; set; }
    public DbSet<WhatsAppConversation> WhatsAppConversations { get; set; }
    public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure LoanApplication
        builder.Entity<LoanApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
                
            entity.Property(e => e.InterestRate)
                .HasColumnType("decimal(5,4)")
                .IsRequired();
                
            entity.Property(e => e.MonthlyPayment)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
                
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(e => e.User)
                .WithMany(u => u.LoanApplications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();
                
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();
                
            entity.Property(e => e.IdNumber)
                .HasMaxLength(13)
                .IsRequired();
                
            entity.Property(e => e.Address)
                .HasMaxLength(100);
                
            entity.Property(e => e.MonthlyIncome)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.IdNumber)
                .IsUnique();
                
            // Configure WhatsApp relationship
            entity.HasOne(e => e.WhatsAppContact)
                .WithOne(c => c.User)
                .HasForeignKey<WhatsAppContact>(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure WhatsAppContact
        builder.Entity<WhatsAppContact>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();
                
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100);
                
            entity.Property(e => e.FirstName)
                .HasMaxLength(100);
                
            entity.Property(e => e.LastName)
                .HasMaxLength(100);

            entity.HasIndex(e => e.PhoneNumber)
                .IsUnique();

            entity.HasMany(e => e.Conversations)
                .WithOne(c => c.Contact)
                .HasForeignKey(c => c.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Contact)
                .HasForeignKey(m => m.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WhatsAppConversation
        builder.Entity<WhatsAppConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Subject)
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Conversations)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LoanApplication)
                .WithMany()
                .HasForeignKey(e => e.LoanApplicationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WhatsAppMessage
        builder.Entity<WhatsAppMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.WhatsAppMessageId)
                .HasMaxLength(100);
                
            entity.Property(e => e.MessageText)
                .IsRequired();

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Direction)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.MediaUrl)
                .HasMaxLength(500);

            entity.Property(e => e.MediaType)
                .HasMaxLength(100);

            entity.Property(e => e.MediaCaption)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            entity.Property(e => e.TemplateName)
                .HasMaxLength(100);

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.HandledByUser)
                .WithMany(u => u.HandledMessages)
                .HasForeignKey(e => e.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.WhatsAppMessageId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}