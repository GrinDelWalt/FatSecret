using FatSecret.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FatSecret.DAL.Mappings;

// public class UserTokenMap : IEntityTypeConfiguration<UserToken>
// {
//     // public void Configure(EntityTypeBuilder<UserToken> builder)
//     // {
//     //     builder.ToTable("UserTokens");
//     //
//     //     builder.HasKey(x => x.Id);
//     //
//     //     builder.Property(x => x.Id).ValueGeneratedOnAdd();
//     //
//     //     builder.HasOne(x => x.User).WithMany(x => x.UserTokens).HasForeignKey(x => x.UserId);
//     // }
// }