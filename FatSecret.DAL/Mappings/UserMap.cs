using FatSecret.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FatSecret.DAL.Mappings;

public class UserMap : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.Login).IsRequired();
        builder.Property(x => x.FirstName).IsRequired();
        builder.Property(x => x.LastName).IsRequired();
        builder.Property(x => x.Password).IsRequired();
        
        // ????????? ?????????? ???????
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Login).IsUnique();
    }
}